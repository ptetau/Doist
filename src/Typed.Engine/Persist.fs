module Typed.Engine.Persist

open System
open System.Data
open System.Data.Common
open System.Globalization
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Reflection
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access

type Dialect =
    | SqlServer
    | Sqlite

type IndexDef =
    { Name: string
      Table: string
      Columns: string list
      Unique: bool }

// ---- Shape: the stored form of every table ----

type ColumnShape =
    { ColName: string
      SqlType: string
      Nullable: bool
      /// The expression of a computed column, or "" for a plain column.
      Computed: string }

type ForeignKeyShape =
    { FkName: string
      FkColumn: string
      FkTarget: string }

type TableShape =
    { TableName: string
      Cols: ColumnShape list
      ForeignKeys: ForeignKeyShape list
      Indexes: IndexDef list }

type Shape = { Tables: TableShape list }

/// The `renamedFrom` note: the column To of the model replaces the column From.
type Rename = { Model: string; From: string; To: string }

// ---- Names and helpers ----

let private q (name: string) : string = "[" + name + "]"

let private finding (code: ErrorCode) (model: string) (field: string option) (message: string) : Finding =
    { Severity = Severity.Error
      Code = code
      Model = model
      Field = field
      Message = message }

let private notFound (model: string) : Finding =
    finding NOT_FOUND model None (sprintf "%s was not found." model)

let private colName (items: OverrideItem list) (field: string) : string =
    items
    |> List.tryPick (function
        | Column(f, n) when f = field -> Some n
        | _ -> None)
    |> Option.defaultValue field

let private hasOwner (access: Access) : bool =
    match access with
    | Owned
    | OwnedVia _ -> true
    | Self
    | SystemOnly -> false

let private sqlType (kind: Kind) : string =
    match innerKind kind with
    | Text n when n > 4000 -> "nvarchar(max)"
    | Text n -> sprintf "nvarchar(%d)" n
    | WholeNumber -> "bigint"
    | Decimal -> "decimal(18,4)"
    | Boolean -> "bit"
    | Enum _ -> "varchar(16)"
    | Date -> "date"
    | Timestamp -> "datetimeoffset"
    | Id
    | Reference _ -> "uniqueidentifier"
    | Optional _ -> "nvarchar(max)"

let private sqliteType (t: string) : string =
    if t.StartsWith "nvarchar" || t.StartsWith "varchar" || t = "date" || t = "datetimeoffset" || t = "uniqueidentifier" then
        "text"
    elif t = "bit" || t = "bigint" || t = "rowversion" || t = "int" then
        "integer"
    elif t.StartsWith "decimal" then
        "numeric"
    else
        t

// ---- Sort keys ----

type private KeyInfo =
    { KeyName: string
      KeyCol: string
      KeyDir: Direction
      KeyKind: Kind
      KeyNullable: bool
      /// The field that supplies the value of the key.
      KeyField: string
      /// True for a `HasX` key, which is 1 when the field X has a value.
      KeyIsFlag: bool }

let private sortTarget (model: Model) (items: OverrideItem list) : (Field * Direction) option =
    let chosen =
        match items |> List.tryPick (function ListSort(f, d) -> Some(f, d) | _ -> None) with
        | Some chosen -> Some chosen
        | None -> titleFieldName model.Fields items |> Option.map (fun f -> f, Ascending)
    chosen
    |> Option.bind (fun (name, d) ->
        model.Fields |> List.tryFind (fun f -> f.Name = name) |> Option.map (fun f -> f, d))

let private keyInfos (model: Model) (items: OverrideItem list) : KeyInfo list =
    let idKey =
        { KeyName = "Id"
          KeyCol = "Id"
          KeyDir = Ascending
          KeyKind = Id
          KeyNullable = false
          KeyField = "Id"
          KeyIsFlag = false }
    match sortTarget model items with
    | None -> [ idKey ]
    | Some(f, d) ->
        let flag =
            if isOptional f.Kind then
                [ { KeyName = "Has" + f.Name
                    KeyCol = "Has" + f.Name
                    KeyDir = Descending
                    KeyKind = WholeNumber
                    KeyNullable = false
                    KeyField = f.Name
                    KeyIsFlag = true } ]
            else
                []
        let key =
            { KeyName = f.Name
              KeyCol = colName items f.Name
              KeyDir = d
              KeyKind = innerKind f.Kind
              KeyNullable = isOptional f.Kind
              KeyField = f.Name
              KeyIsFlag = false }
        flag @ [ key; idKey ]

let sortKeys (model: Model) (items: OverrideItem list) : (string * Direction) list =
    keyInfos model items |> List.map (fun k -> k.KeyName, k.KeyDir)

// ---- Indexes and foreign keys ----

let private parentColumn (items: OverrideItem list) (access: Access) : string list =
    match access with
    | OwnedVia(reference, _, _) -> [ colName items reference ]
    | _ -> []

let indexes (model: Model) (items: OverrideItem list) (access: Access) : IndexDef list =
    let indexName prefix (cols: string list) =
        sprintf "%s_%s_%s" prefix model.Name (String.concat "_" cols)
    let listIndex =
        if hasOwner access then
            let viewCols =
                items
                |> List.collect (fun i ->
                    match i with
                    | ListView(_, filter) -> referencedFields filter |> List.map (colName items)
                    | _ -> [])
            let cols =
                ("Owner" :: parentColumn items access)
                @ viewCols
                @ (keyInfos model items |> List.map (fun k -> k.KeyCol))
                |> List.distinct
            [ { Name = indexName "IX" cols; Table = model.Name; Columns = cols; Unique = false } ]
        else
            []
    let uniqueIndexes =
        model.Fields
        |> List.filter (fun f -> f.Unique)
        |> List.map (fun f ->
            let cols = [ colName items f.Name ]
            { Name = indexName "UX" cols; Table = model.Name; Columns = cols; Unique = true })
    let startsWith (col: string) =
        (listIndex @ uniqueIndexes) |> List.exists (fun i -> i.Columns.Head = col)
    let foreignKeyIndexes =
        model.Fields
        |> List.filter (fun f -> (refTarget f.Kind).IsSome)
        |> List.map (fun f -> colName items f.Name)
        |> List.filter (fun col -> not (startsWith col))
        |> List.map (fun col ->
            { Name = indexName "IX" [ col ]; Table = model.Name; Columns = [ col ]; Unique = false })
    listIndex @ foreignKeyIndexes @ uniqueIndexes

// ---- Schema ----

let private tableShape (model: Model, items: OverrideItem list, access: Access) : TableShape =
    let column name sqlTypeName nullable : ColumnShape =
        { ColName = name; SqlType = sqlTypeName; Nullable = nullable; Computed = "" }
    let system =
        [ column "Id" "uniqueidentifier" false ]
        @ (if hasOwner access then [ column "Owner" "uniqueidentifier" false ] else [])
        @ [ column "Version" "rowversion" false
            column "CreatedAt" "datetimeoffset" false
            column "UpdatedAt" "datetimeoffset" false ]
    let fields =
        model.Fields
        |> List.map (fun f -> column (colName items f.Name) (sqlType f.Kind) (isOptional f.Kind))
    let computed =
        keyInfos model items
        |> List.filter (fun k -> k.KeyIsFlag)
        |> List.map (fun k ->
            { ColName = k.KeyCol
              SqlType = "int"
              Nullable = false
              Computed = sprintf "CASE WHEN %s IS NULL THEN 0 ELSE 1 END" (q (colName items k.KeyField)) })
    let foreignKeys =
        model.Fields
        |> List.choose (fun f ->
            refTarget f.Kind
            |> Option.map (fun target ->
                { FkName = sprintf "FK_%s_%s" model.Name f.Name
                  FkColumn = colName items f.Name
                  FkTarget = target }))
    { TableName = model.Name
      Cols = system @ fields @ computed
      ForeignKeys = foreignKeys
      Indexes = indexes model items access }

let shapeOf (models: (Model * OverrideItem list * Access) list) : Shape =
    { Tables = models |> List.map tableShape }

let private renderColumn (dialect: Dialect) (c: ColumnShape) : string =
    let typ =
        match dialect with
        | SqlServer -> c.SqlType
        | Sqlite -> sqliteType c.SqlType
    if c.Computed <> "" then
        match dialect with
        | SqlServer -> sprintf "%s AS (%s) PERSISTED" (q c.ColName) c.Computed
        | Sqlite -> sprintf "%s %s AS (%s) STORED" (q c.ColName) typ c.Computed
    else
        sprintf
            "%s %s %s%s"
            (q c.ColName)
            typ
            (if c.Nullable then "NULL" else "NOT NULL")
            (if c.ColName = "Id" then " PRIMARY KEY" else "")

let private renderTable (dialect: Dialect) (t: TableShape) : string =
    let columns = t.Cols |> List.map (fun c -> "    " + renderColumn dialect c) |> String.concat ",\n"
    sprintf "CREATE TABLE %s (\n%s\n);" (q t.TableName) columns

let private renderForeignKey (table: string) (fk: ForeignKeyShape) : string =
    sprintf
        "ALTER TABLE %s ADD CONSTRAINT %s FOREIGN KEY (%s) REFERENCES %s (%s);"
        (q table)
        (q fk.FkName)
        (q fk.FkColumn)
        (q fk.FkTarget)
        (q "Id")

let private renderIndex (i: IndexDef) : string =
    sprintf
        "CREATE %sINDEX %s ON %s (%s);"
        (if i.Unique then "UNIQUE " else "")
        (q i.Name)
        (q i.Table)
        (i.Columns |> List.map q |> String.concat ", ")

/// SQLite has no ALTER TABLE ADD CONSTRAINT, so its schema holds no foreign keys.
let private renderForeignKeys (dialect: Dialect) (t: TableShape) : string list =
    match dialect with
    | SqlServer -> t.ForeignKeys |> List.map (renderForeignKey t.TableName)
    | Sqlite -> []

let private renderShape (dialect: Dialect) (shape: Shape) : string =
    (shape.Tables |> List.map (renderTable dialect))
    @ (shape.Tables |> List.collect (renderForeignKeys dialect))
    @ (shape.Tables |> List.collect (fun t -> t.Indexes |> List.map renderIndex))
    |> String.concat "\n"

let createTable (dialect: Dialect) (model: Model) (items: OverrideItem list) (access: Access) : string =
    renderTable dialect (tableShape (model, items, access))

let foreignKeys (model: Model) (items: OverrideItem list) (access: Access) : string list =
    renderForeignKeys SqlServer (tableShape (model, items, access))

let schema (dialect: Dialect) (models: (Model * OverrideItem list * Access) list) : string =
    renderShape dialect (shapeOf models)

// ---- List views and list queries ----

/// A list view as a WHERE clause with its literals, or the reason that the view is not supported.
let viewToSql (items: OverrideItem list) (expr: Expr) : Result<string * (string * obj) list, string> =
    let literals = ResizeArray<string * obj>()
    let unsupported (e: Expr) : Result<string, string> =
        Result.Error(sprintf "The view %A is not supported." e)
    let isBoolProperty (p: Reflection.PropertyInfo) = p.PropertyType = typeof<bool>
    let column (p: Reflection.PropertyInfo) = q (colName items p.Name)
    let literal (v: obj) : string =
        match v with
        | :? bool as b -> if b then "1" else "0"
        | _ ->
            let name = sprintf "v%d" literals.Count
            literals.Add((name, v))
            "@" + name
    let operand (e: Expr) : Result<string, string> =
        match e with
        | PropertyGet(Some(Var _), p, []) -> Ok(column p)
        | Value(v, _) when not (isNull v) -> Ok(literal v)
        | NewUnionCase(case, []) -> Ok(literal (box case.Name))
        | _ -> unsupported e
    let comparison (op: string) (a: Expr) (b: Expr) : Result<string, string> =
        match operand a, operand b with
        | Ok l, Ok r -> Ok(sprintf "%s %s %s" l op r)
        | Result.Error m, _
        | _, Result.Error m -> Result.Error m
    let rec cond (e: Expr) : Result<string, string> =
        match e with
        | Lambda(_, body) -> cond body
        | PropertyGet(Some(Var _), p, []) when isBoolProperty p -> Ok(sprintf "%s = 1" (column p))
        | Call(None, mi, [ PropertyGet(Some(Var _), p, []) ]) when mi.Name = "Not" && isBoolProperty p ->
            Ok(sprintf "%s = 0" (column p))
        | Call(None, mi, [ a ]) when mi.Name = "Not" -> cond a |> Result.map (sprintf "NOT (%s)")
        | IfThenElse(a, b, Value((:? bool as f), _)) when not f -> combine "AND" a b
        | IfThenElse(a, Value((:? bool as t), _), b) when t -> combine "OR" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_Equality" -> comparison "=" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_Inequality" -> comparison "<>" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_LessThan" -> comparison "<" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_GreaterThan" -> comparison ">" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_LessThanOrEqual" -> comparison "<=" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_GreaterThanOrEqual" -> comparison ">=" a b
        | _ -> unsupported e
    and combine (word: string) (a: Expr) (b: Expr) : Result<string, string> =
        match cond a, cond b with
        | Ok l, Ok r -> Ok(sprintf "(%s) %s (%s)" l word r)
        | Result.Error m, _
        | _, Result.Error m -> Result.Error m
    cond expr |> Result.map (fun sql -> sql, List.ofSeq literals)

type ListQuery =
    { Sql: string
      Keys: (string * Direction) list
      Literals: (string * obj) list }

/// The fields that the list shows: the `listColumns` override, else every field that is not wide.
let private listedFields (model: Model) (items: OverrideItem list) : string list =
    match items |> List.tryPick (function ListColumns cs -> Some cs | _ -> None) with
    | Some cs -> cs
    | None -> model.Fields |> List.filter (fun f -> not (isWide f.Kind)) |> List.map (fun f -> f.Name)

/// The fields that a list query reads: the listed ones, then the key fields that the list does not show.
let private selectedFields (model: Model) (items: OverrideItem list) : string list =
    let listed = listedFields model items
    let extra =
        keyInfos model items
        |> List.map (fun k -> k.KeyField)
        |> List.filter (fun f -> f <> "Id" && not (List.contains f listed))
        |> List.distinct
    listed @ extra

let private keysetCondition (keys: KeyInfo list) : string =
    let equal (i: int) (k: KeyInfo) =
        if k.KeyNullable then
            sprintf "(%s = @c%d OR (%s IS NULL AND @c%d IS NULL))" (q k.KeyCol) i (q k.KeyCol) i
        else
            sprintf "%s = @c%d" (q k.KeyCol) i
    let after (i: int) (k: KeyInfo) =
        let op = if k.KeyDir = Ascending then ">" else "<"
        sprintf "%s %s @c%d" (q k.KeyCol) op i
    keys
    |> List.mapi (fun i k ->
        let before = keys |> List.take i |> List.mapi equal
        "(" + String.concat " AND " (before @ [ after i k ]) + ")")
    |> String.concat " OR "
    |> sprintf "(%s)"

let listQuery
    (dialect: Dialect)
    (model: Model)
    (items: OverrideItem list)
    (access: Access)
    (view: string option)
    (afterCursor: bool)
    : ListQuery =
    let keys = keyInfos model items
    let columns =
        "Id" :: (selectedFields model items |> List.map (colName items)) |> List.map q |> String.concat ", "
    let owner =
        match access with
        | Owned -> [ "[Owner] = @owner" ]
        | OwnedVia(reference, _, _) -> [ "[Owner] = @owner"; sprintf "%s = @parent" (q (colName items reference)) ]
        | Self -> [ "[Id] = @owner" ]
        | SystemOnly -> []
    let viewSql, literals =
        match view with
        | None -> [], []
        | Some name ->
            match items |> List.tryPick (function ListView(n, f) when n = name -> Some f | _ -> None) with
            | None -> failwithf "%s has no list view '%s'." model.Name name
            | Some filter ->
                match viewToSql items filter with
                | Ok(sql, lits) -> [ "(" + sql + ")" ], lits
                | Result.Error reason -> failwith reason
    let cursor = if afterCursor then [ keysetCondition keys ] else []
    let filters = owner @ viewSql @ cursor
    let where = if filters.IsEmpty then "" else " WHERE " + String.concat " AND " filters
    let order =
        keys
        |> List.map (fun k -> sprintf "%s %s" (q k.KeyCol) (if k.KeyDir = Ascending then "ASC" else "DESC"))
        |> String.concat ", "
    let head, tail =
        match dialect with
        | SqlServer -> "SELECT TOP (@limit) ", ""
        | Sqlite -> "SELECT ", " LIMIT @limit"
    { Sql = sprintf "%s%s FROM %s%s ORDER BY %s%s" head columns (q model.Name) where order tail
      Keys = keys |> List.map (fun k -> k.KeyName, k.KeyDir)
      Literals = literals }

// ---- Values in the database ----

let private invariant = CultureInfo.InvariantCulture

let private toDb (dialect: Dialect) (v: obj) : obj =
    match v with
    | null -> box DBNull.Value
    | :? Guid as g when dialect = Sqlite -> box (g.ToString())
    | :? DateOnly as d when dialect = Sqlite -> box (d.ToString("yyyy-MM-dd", invariant))
    | :? DateTimeOffset as t when dialect = Sqlite -> box (t.ToString("o", invariant))
    | _ -> v

let rec private fromDb (kind: Kind) (v: obj) : obj =
    match kind with
    | Text _
    | Enum _ -> box (string v)
    | Boolean ->
        match v with
        | :? bool -> v
        | _ -> box (Convert.ToInt64(v, invariant) <> 0L)
    | WholeNumber ->
        match v with
        | :? (byte[]) as bytes -> box (BitConverter.ToInt64(Array.rev bytes, 0))
        | _ -> box (Convert.ToInt64(v, invariant))
    | Decimal -> box (Convert.ToDecimal(v, invariant))
    | Date ->
        match v with
        | :? DateOnly -> v
        | :? DateTime as d -> box (DateOnly.FromDateTime d)
        | _ -> box (DateOnly.Parse(string v, invariant))
    | Timestamp ->
        match v with
        | :? DateTimeOffset -> v
        | _ -> box (DateTimeOffset.Parse(string v, invariant))
    | Id
    | Reference _ ->
        match v with
        | :? Guid -> v
        | _ -> box (Guid.Parse(string v))
    | Optional k -> fromDb k v

/// A value as text, for the cursor.
let private render (v: obj) : string =
    match v with
    | :? Guid as g -> g.ToString("D")
    | :? DateOnly as d -> d.ToString("yyyy-MM-dd", invariant)
    | :? DateTimeOffset as t -> t.ToString("o", invariant)
    | :? bool as b -> if b then "1" else "0"
    | :? decimal as d -> d.ToString(invariant)
    | _ -> Convert.ToString(v, invariant)

let private parse (kind: Kind) (text: string) : obj =
    match innerKind kind with
    | Boolean -> box (text = "1")
    | WholeNumber -> box (Int64.Parse(text, invariant))
    | Decimal -> box (System.Decimal.Parse(text, invariant))
    | Date -> box (DateOnly.ParseExact(text, "yyyy-MM-dd", invariant))
    | Timestamp -> box (DateTimeOffset.Parse(text, invariant))
    | Id
    | Reference _ -> box (Guid.Parse text)
    | _ -> box text

// ---- Cursor ----

let private encodeCursor (values: string option list) : string =
    let array = JsonArray()
    for v in values do
        match v with
        | Some s -> array.Add(JsonValue.Create(s))
        | None -> array.Add(null)
    Convert.ToBase64String(Encoding.UTF8.GetBytes(array.ToJsonString()))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_')

/// The key values of a cursor, or None when the cursor is not valid.
let private decodeCursor (keys: KeyInfo list) (cursor: string) : obj option list option =
    try
        let base64 = cursor.Replace('-', '+').Replace('_', '/')
        let padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')
        let text = Encoding.UTF8.GetString(Convert.FromBase64String padded)
        let items = JsonNode.Parse(text).AsArray() |> Seq.toList
        if items.Length <> keys.Length then
            None
        else
            List.map2
                (fun (k: KeyInfo) (n: JsonNode) ->
                    if isNull n then None else Some(parse k.KeyKind (n.GetValue<string>())))
                keys
                items
            |> Some
    with _ ->
        None

// ---- Store ----

type Store internal (connection: DbConnection, dialect: Dialect, now: unit -> DateTimeOffset, hook: (int -> unit) option) =
    let mutable commands = 0
    member _.Connection = connection
    member _.Dialect = dialect
    member _.Now() = now ()
    member _.Hook = hook
    member _.Commands = commands
    member _.Counted() = commands <- commands + 1

module Store =
    let create
        (conn: DbConnection)
        (dialect: Dialect)
        (now: unit -> DateTimeOffset)
        (onBeforeChildDelete: (int -> unit) option)
        : Store =
        Store(conn, dialect, now, onBeforeChildDelete)

let queryCount (store: Store) : int = store.Commands

type Page = { Rows: Record list; Next: string option }

let private bind (store: Store) (tx: DbTransaction option) (sql: string) (args: (string * obj) list) : DbCommand =
    let cmd = store.Connection.CreateCommand()
    cmd.CommandText <- sql
    tx |> Option.iter (fun t -> cmd.Transaction <- t)
    for (name, value) in args do
        let p = cmd.CreateParameter()
        p.ParameterName <- "@" + name
        p.Value <- toDb store.Dialect value
        cmd.Parameters.Add p |> ignore
    store.Counted()
    cmd

let private run (store: Store) (tx: DbTransaction option) (sql: string) (args: (string * obj) list) : int =
    use cmd = bind store tx sql args
    cmd.ExecuteNonQuery()

/// The rows of a query. A column with no value has no key.
let private fetch
    (store: Store)
    (tx: DbTransaction option)
    (sql: string)
    (args: (string * obj) list)
    (cols: (string * Kind) list)
    : Map<string, obj> list =
    use cmd = bind store tx sql args
    use reader = cmd.ExecuteReader()
    [ while reader.Read() do
          yield
              cols
              |> List.mapi (fun i (name, kind) ->
                  if reader.IsDBNull i then None else Some(name, fromDb kind (reader.GetValue i)))
              |> List.choose id
              |> Map.ofList ]

let private fieldCols (model: Model) : (string * Kind) list =
    model.Fields |> List.map (fun f -> f.Name, innerKind f.Kind)

let private scopeFilter (scope: Scope) : string * (string * obj) list =
    match scope.Kind with
    | OwnerFilter u -> " AND [Owner] = @owner", [ "owner", box u ]
    | SelfFilter u -> " AND [Id] = @owner", [ "owner", box u ]
    | Engine -> "", []

let private toRecord (model: Model) (row: Map<string, obj>) : Record =
    let names = model.Fields |> List.map (fun f -> f.Name)
    ({ Model = model.Name
       Id = unbox<Guid> row["Id"]
       Owner = row.TryFind "Owner" |> Option.map unbox<Guid>
       Version = row.TryFind "Version" |> Option.map unbox<int64> |> Option.defaultValue 0L
       Values = row |> Map.filter (fun k _ -> List.contains k names) }
    : Record)

let find (store: Store) (scope: Scope) (model: Model) (id: Guid) : Result<Record, Finding> =
    let owned =
        match scope.Kind with
        | OwnerFilter _ -> true
        | _ -> false
    let cols =
        [ "Id", Id ]
        @ (if owned then [ "Owner", Id ] else [])
        @ [ "Version", WholeNumber ]
        @ fieldCols model
    let filter, args = scopeFilter scope
    let sql =
        sprintf
            "SELECT %s FROM %s WHERE [Id] = @id%s"
            (cols |> List.map (fst >> q) |> String.concat ", ")
            (q model.Name)
            filter
    match fetch store None sql (("id", box id) :: args) cols with
    | [] -> Result.Error(notFound model.Name)
    | row :: _ -> Ok(toRecord model row)

let private validation (model: Model) (field: string) (message: string) : Finding =
    finding VALIDATION_FAILED model.Name (Some field) message

let page
    (store: Store)
    (scope: Scope)
    (model: Model)
    (items: OverrideItem list)
    (access: Access)
    (view: string option)
    (parent: Guid option)
    (cursor: string option)
    (size: int option)
    : Result<Page, Finding> =
    let limit = defaultArg size 50
    let keys = keyInfos model items
    let decoded =
        match cursor with
        | None -> Ok None
        | Some c ->
            match decodeCursor keys c with
            | Some values -> Ok(Some values)
            | None -> Result.Error(validation model "cursor" "is not valid")
    let owner =
        match scope.Kind with
        | OwnerFilter u
        | SelfFilter u -> Some u
        | Engine -> None
    if limit < 1 || limit > 200 then
        Result.Error(validation model "limit" "must be between 1 and 200")
    else
        match decoded, owner with
        | Result.Error f, _ -> Result.Error f
        | Ok _, None -> Result.Error(notFound model.Name)
        | Ok values, Some ownerId ->
            let query = listQuery store.Dialect model items access view values.IsSome
            let parentArgs =
                match access with
                | OwnedVia _ -> [ "parent", (match parent with Some g -> box g | None -> null) ]
                | _ -> []
            let cursorArgs =
                match values with
                | Some vs -> vs |> List.mapi (fun i v -> sprintf "c%d" i, Option.toObj v)
                | None -> []
            let args = [ "limit", box (limit + 1); "owner", box ownerId ] @ parentArgs @ query.Literals @ cursorArgs
            let fieldKinds = fieldCols model |> Map.ofList
            let cols =
                ("Id", Id)
                :: (selectedFields model items |> List.map (fun f -> f, fieldKinds[f]))
            let rows = fetch store None query.Sql args cols
            let shown = rows |> List.truncate limit
            let listed = listedFields model items
            let records =
                shown
                |> List.map (fun row ->
                    ({ Model = model.Name
                       Id = unbox<Guid> row["Id"]
                       Owner = None
                       Version = 0L
                       Values = row |> Map.filter (fun k _ -> List.contains k listed) }
                    : Record))
            let next =
                if rows.Length > limit then
                    let last = List.last shown
                    let keyValue (k: KeyInfo) : string option =
                        if k.KeyIsFlag then
                            Some(if last.ContainsKey k.KeyField then "1" else "0")
                        else
                            last.TryFind k.KeyField |> Option.map render
                    Some(encodeCursor (keys |> List.map keyValue))
                else
                    None
            Ok { Rows = records; Next = next }

/// The version as the database compares it.
let private versionArg (store: Store) (version: int64) : obj =
    match store.Dialect with
    | Sqlite -> box version
    | SqlServer -> box (Array.rev (BitConverter.GetBytes version))

let private presentValues (model: Model) (record: Record) : (string * obj) list =
    model.Fields
    |> List.choose (fun f ->
        match record.Values.TryFind f.Name with
        | Some v when not (isNull v) -> Some(f.Name, v)
        | _ -> None)

let add (store: Store) (scope: Scope) (model: Model) (record: Record) : Result<Record, Finding> =
    let now = store.Now()
    let id =
        match scope.Kind with
        | SelfFilter u -> u
        | Engine when record.Id <> Guid.Empty -> record.Id
        | _ -> Guid.NewGuid()
    let owner =
        match scope.Kind with
        | OwnerFilter u -> Some u
        | SelfFilter _ -> None
        | Engine -> record.Owner
    let system =
        [ "Id", box id ]
        @ (match owner with Some o -> [ "Owner", box o ] | None -> [])
        @ (if store.Dialect = Sqlite then [ "Version", box 1L ] else [])
        @ [ "CreatedAt", box now; "UpdatedAt", box now ]
    let columns = system @ presentValues model record
    let sql =
        sprintf
            "INSERT INTO %s (%s) VALUES (%s)"
            (q model.Name)
            (columns |> List.map (fst >> q) |> String.concat ", ")
            (columns |> List.map (fun (n, _) -> "@p_" + n) |> String.concat ", ")
    run store None sql (columns |> List.map (fun (n, v) -> "p_" + n, v)) |> ignore
    find store scope model id

let revise
    (store: Store)
    (scope: Scope)
    (model: Model)
    (id: Guid)
    (version: int64 option)
    (record: Record)
    : Result<Record, Finding> =
    match version with
    | None -> Result.Error(validation model "Version" "is required")
    | Some v ->
        match find store scope model id with
        | Result.Error f -> Result.Error f
        | Ok _ ->
            let present = presentValues model record
            let sets =
                (model.Fields
                 |> List.map (fun f ->
                     if present |> List.exists (fun (n, _) -> n = f.Name) then
                         sprintf "%s = @p_%s" (q f.Name) f.Name
                     else
                         sprintf "%s = NULL" (q f.Name)))
                @ [ "[UpdatedAt] = @now" ]
                @ (if store.Dialect = Sqlite then [ "[Version] = [Version] + 1" ] else [])
            let filter, scopeArgs = scopeFilter scope
            let sql =
                sprintf
                    "UPDATE %s SET %s WHERE [Id] = @id AND [Version] = @version%s"
                    (q model.Name)
                    (String.concat ", " sets)
                    filter
            let args =
                (present |> List.map (fun (n, value) -> "p_" + n, value))
                @ [ "now", box (store.Now()); "id", box id; "version", versionArg store v ]
                @ scopeArgs
            if run store None sql args = 0 then
                Result.Error(finding STALE_VERSION model.Name None (sprintf "%s was changed by someone else." model.Name))
            else
                find store scope model id

let remove
    (store: Store)
    (scope: Scope)
    (model: Model)
    (children: (Model * string) list)
    (id: Guid)
    : Result<unit, Finding> =
    match find store scope model id with
    | Result.Error f -> Result.Error f
    | Ok _ ->
        use tx = store.Connection.BeginTransaction()
        let inTx = Some tx
        let mutable deleted = 0
        for (child, reference) in children do
            let ids =
                fetch
                    store
                    inTx
                    (sprintf "SELECT [Id] FROM %s WHERE %s = @id" (q child.Name) (q reference))
                    [ "id", box id ]
                    [ "Id", Id ]
            for row in ids do
                match store.Hook with
                | Some hook -> hook deleted
                | None -> ()
                run store inTx (sprintf "DELETE FROM %s WHERE [Id] = @id" (q child.Name)) [ "id", row["Id"] ]
                |> ignore
                deleted <- deleted + 1
        let filter, scopeArgs = scopeFilter scope
        run store inTx (sprintf "DELETE FROM %s WHERE [Id] = @id%s" (q model.Name) filter) (("id", box id) :: scopeArgs)
        |> ignore
        tx.Commit()
        Ok()

// ---- Shape file ----

let private jsonString (s: string) : JsonNode = JsonValue.Create(s) :> JsonNode
let private jsonBool (b: bool) : JsonNode = JsonValue.Create(b) :> JsonNode

let private jsonStrings (xs: string list) : JsonNode =
    JsonArray(xs |> List.map jsonString |> Array.ofList) :> JsonNode

let private jsonObject (pairs: (string * JsonNode) list) : JsonNode =
    let o = JsonObject()
    for (k, v) in pairs do
        o[k] <- v
    o :> JsonNode

let private jsonArray (xs: JsonNode list) : JsonNode = JsonArray(Array.ofList xs) :> JsonNode

let private shapeToJson (shape: Shape) : string =
    let column (c: ColumnShape) =
        jsonObject
            [ "name", jsonString c.ColName
              "type", jsonString c.SqlType
              "nullable", jsonBool c.Nullable
              "computed", jsonString c.Computed ]
    let foreignKey (fk: ForeignKeyShape) =
        jsonObject [ "name", jsonString fk.FkName; "column", jsonString fk.FkColumn; "target", jsonString fk.FkTarget ]
    let index (i: IndexDef) =
        jsonObject
            [ "name", jsonString i.Name
              "table", jsonString i.Table
              "columns", jsonStrings i.Columns
              "unique", jsonBool i.Unique ]
    let table (t: TableShape) =
        jsonObject
            [ "name", jsonString t.TableName
              "columns", jsonArray (t.Cols |> List.map column)
              "foreignKeys", jsonArray (t.ForeignKeys |> List.map foreignKey)
              "indexes", jsonArray (t.Indexes |> List.map index) ]
    (jsonObject [ "tables", jsonArray (shape.Tables |> List.map table) ])
        .ToJsonString(JsonSerializerOptions(WriteIndented = true))

let private shapeOfJson (text: string) : Shape =
    let root = JsonNode.Parse text
    let str (n: JsonNode) (key: string) = n[key].GetValue<string>()
    let flag (n: JsonNode) (key: string) = n[key].GetValue<bool>()
    let list (n: JsonNode) (key: string) = n[key].AsArray() |> Seq.toList
    let column (n: JsonNode) : ColumnShape =
        { ColName = str n "name"
          SqlType = str n "type"
          Nullable = flag n "nullable"
          Computed = str n "computed" }
    let foreignKey (n: JsonNode) : ForeignKeyShape =
        { FkName = str n "name"
          FkColumn = str n "column"
          FkTarget = str n "target" }
    let index (n: JsonNode) : IndexDef =
        { Name = str n "name"
          Table = str n "table"
          Columns = list n "columns" |> List.map (fun c -> c.GetValue<string>())
          Unique = flag n "unique" }
    let table (n: JsonNode) : TableShape =
        { TableName = str n "name"
          Cols = list n "columns" |> List.map column
          ForeignKeys = list n "foreignKeys" |> List.map foreignKey
          Indexes = list n "indexes" |> List.map index }
    { Tables = list root "tables" |> List.map table }

let readShape (path: string) : Result<Shape option, Finding> =
    if not (File.Exists path) then
        Ok None
    else
        try
            Ok(Some(shapeOfJson (File.ReadAllText path)))
        with _ ->
            Result.Error(
                finding
                    SHAPE_UNREADABLE
                    ""
                    None
                    "build/shape.json cannot be read. Delete it to start from an empty shape."
            )

let writeShape (path: string) (shape: Shape) : unit =
    let dir = Path.GetDirectoryName path
    if not (String.IsNullOrEmpty dir) then
        Directory.CreateDirectory dir |> ignore
    File.WriteAllText(path, shapeToJson shape)

// ---- Migration ----

let private tableStatements (t: TableShape) : string list =
    [ renderTable SqlServer t ]
    @ (t.ForeignKeys |> List.map (renderForeignKey t.TableName))
    @ (t.Indexes |> List.map renderIndex)

let private tableChanges (renames: Rename list) (previous: TableShape) (next: TableShape) : Result<string list, Finding> =
    let table = next.TableName
    let here = renames |> List.filter (fun r -> r.Model = table)
    let nextNames = next.Cols |> List.map (fun c -> c.ColName)
    let previousNames = previous.Cols |> List.map (fun c -> c.ColName)
    let dropped = previous.Cols |> List.filter (fun c -> not (List.contains c.ColName nextNames))
    let renameOf (c: ColumnShape) =
        here |> List.tryFind (fun r -> r.From = c.ColName && List.contains r.To nextNames)
    match dropped |> List.tryFind (fun c -> (renameOf c).IsNone) with
    | Some c ->
        Result.Error(
            finding
                UNPLANNED_DROP
                table
                (Some c.ColName)
                (sprintf "%s.%s would be dropped. Add renamedFrom or an explicit drop." table c.ColName)
        )
    | None ->
        let renamed = dropped |> List.choose renameOf
        let renameSql =
            renamed |> List.map (fun r -> sprintf "EXEC sp_rename '%s.%s', '%s', 'COLUMN';" table r.From r.To)
        let added =
            next.Cols
            |> List.filter (fun c ->
                not (List.contains c.ColName previousNames)
                && not (renamed |> List.exists (fun r -> r.To = c.ColName)))
        match added |> List.tryFind (fun c -> not c.Nullable && c.Computed = "") with
        | Some c ->
            Result.Error(
                finding
                    UNSAFE_MIGRATION
                    table
                    (Some c.ColName)
                    (sprintf "%s.%s is required and has no default, so existing rows would fail." table c.ColName)
            )
        | None ->
            let addSql =
                added |> List.map (fun c -> sprintf "ALTER TABLE %s ADD %s;" (q table) (renderColumn SqlServer c))
            let newForeignKeys =
                next.ForeignKeys
                |> List.filter (fun fk -> not (previous.ForeignKeys |> List.exists (fun p -> p.FkName = fk.FkName)))
                |> List.map (renderForeignKey table)
            let newIndexes =
                next.Indexes
                |> List.filter (fun i -> not (previous.Indexes |> List.exists (fun p -> p.Name = i.Name)))
                |> List.map renderIndex
            Ok(renameSql @ addSql @ newForeignKeys @ newIndexes)

/// The SQL Server migration from the previous shape to the next one. With no previous shape it is the full schema.
let migration (previous: Shape option) (next: Shape) (renames: Rename list) : Result<string, Finding> =
    match previous with
    | None -> Ok(renderShape SqlServer next)
    | Some prev ->
        let changes =
            next.Tables
            |> List.map (fun t ->
                match prev.Tables |> List.tryFind (fun p -> p.TableName = t.TableName) with
                | None -> Ok(tableStatements t)
                | Some p -> tableChanges renames p t)
        match changes |> List.tryPick (function Result.Error f -> Some f | Ok _ -> None) with
        | Some f -> Result.Error f
        | None -> Ok(changes |> List.collect (function Ok s -> s | Result.Error _ -> []) |> String.concat "\n")
