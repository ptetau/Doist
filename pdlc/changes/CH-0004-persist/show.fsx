// Shows one acceptance line of CAP-persist in the terminal. Run from the project root:
//   dotnet fsi --quiet pdlc/changes/CH-0004-persist/show.fsx R5.2
#r "../../../src/Typed.Engine/bin/Debug/net8.0/Typed.Engine.dll"
#r "nuget: Microsoft.Data.Sqlite, 8.0.11"

open System
open System.IO
open Microsoft.Data.Sqlite
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Persist

[<TitleType>]
type Title = Title of string
type Notes = Notes of string
type Priority = Low | Normal | High
type TimeZoneId = TimeZoneId of string
type ExternalId = ExternalId of string
type User = { Subject: ExternalId; DisplayName: Title; TimeZone: TimeZoneId }
type TodoList = { Name: Title; Archived: bool }
type Todo =
    { List: Ref<TodoList>; Title: Title; Notes: Notes option; Priority: Priority
      Due: DateOnly option; Done: bool; CompletedAt: DateTimeOffset option }
type Note = { Heading: Title; Body: Notes }

module DueRenamed =
    type Todo =
        { List: Ref<TodoList>; Title: Title; Notes: Notes option; Priority: Priority
          DueDate: DateOnly option; Done: bool; CompletedAt: DateTimeOffset option }
module WithTags =
    type Todo =
        { List: Ref<TodoList>; Title: Title; Notes: Notes option; Priority: Priority
          Due: DateOnly option; Done: bool; CompletedAt: DateTimeOffset option; Tags: Title option }
module WithCode =
    type Todo =
        { List: Ref<TodoList>; Title: Title; Notes: Notes option; Priority: Priority
          Due: DateOnly option; Done: bool; CompletedAt: DateTimeOffset option; Code: Title }

let def (clr: Type) kind : TypeDefault = { Clr = clr; Kind = kind; Check = (fun _ -> []); Hidden = false; Unique = false }
let defaults : TypeDefault list =
    [ def typeof<Title> (Text 200)
      def typeof<Notes> (Text 5000)
      def typeof<TimeZoneId> (Text 64)
      { def typeof<ExternalId> (Text 255) with Hidden = true; Unique = true } ]
let describeOk (t: Type) : Model =
    match describe defaults t with Ok m -> m | Result.Error fs -> failwithf "%A" fs

let u1 = Guid.Parse "11111111-1111-1111-1111-111111111111"
let u2 = Guid.Parse "22222222-2222-2222-2222-222222222222"
let rules : AccessRule list =
    [ rule<TodoList> owned
      rule<Todo> (ownedVia <@ fun (t: Todo) -> t.List @> |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>)
      rule<User> self ]
let accessOf (name: string) : Access = (rules |> List.find (fun r -> r.ModelName = name)).Access
let todoAccess, listAccess, userAccess = accessOf "Todo", accessOf "TodoList", accessOf "User"
let noteAccess = (rule<Note> owned).Access
let todoModel, listModel, userModel, noteModel =
    describeOk typeof<Todo>, describeOk typeof<TodoList>, describeOk typeof<User>, describeOk typeof<Note>

let todoItems =
    (overrides<Todo>
        [ listColumns [ col <@ fun (t: Todo) -> t.Title @>; col <@ fun (t: Todo) -> t.Due @>
                        col <@ fun (t: Todo) -> t.Priority @>; col <@ fun (t: Todo) -> t.Done @> ]
          listSort <@ fun (t: Todo) -> t.Due @> Ascending ]).Items
let listItems =
    (overrides<TodoList>
        [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
          listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ]).Items
let userColumnItems = (overrides<User> [ column <@ fun (u: User) -> u.Subject @> "ExternalSubject" ]).Items
let userSensitiveItems = (overrides<User> [ sensitive <@ fun (u: User) -> u.Subject @> ]).Items
let appModels = [ (listModel, listItems, listAccess); (todoModel, todoItems, todoAccess); (userModel, [], userAccess) ]
let sqlServerSchema = schema SqlServer appModels

// ---- store helpers ----
let fixedNow () = DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)
let okOf (r: Result<'a, Finding>) : 'a = match r with Ok v -> v | Result.Error f -> failwithf "Expected Ok, got: %A" f
let errOf (r: Result<'a, Finding>) : Finding = match r with Ok v -> failwithf "Expected an error, got Ok: %A" v | Result.Error f -> f
let scopeOf (user: Guid) (modelName: string) : Scope =
    match scopeFor rules user modelName with Ok s -> s | Result.Error f -> failwithf "%A" f
let openDb () : SqliteConnection =
    let conn = new SqliteConnection("Data Source=:memory:")
    conn.Open()
    use cmd = conn.CreateCommand()
    cmd.CommandText <- schema Sqlite appModels
    cmd.ExecuteNonQuery() |> ignore
    conn
let newStore (conn: SqliteConnection) hook : Store = Store.create conn Sqlite fixedNow hook
let listRec (name: string) : Record =
    { Model = "TodoList"; Id = Guid.Empty; Owner = None; Version = 0L
      Values = Map [ "Name", box name; "Archived", box false ] }
let todoRec (listId: Guid) (title: string) (due: DateOnly option) : Record =
    let b = Map [ "List", box listId; "Title", box title; "Priority", box "Normal"; "Done", box false ]
    { Model = "Todo"; Id = Guid.Empty; Owner = None; Version = 0L
      Values = (match due with Some d -> b.Add("Due", box d) | None -> b) }
let addList store (user: Guid) name : Record = okOf (add store (scopeOf user "TodoList") listModel (listRec name))
let addTodo store (user: Guid) (listId: Guid) title due : Record = okOf (add store (scopeOf user "Todo") todoModel (todoRec listId title due))
let findTodo store (user: Guid) (id: Guid) = find store (scopeOf user "Todo") todoModel id
let findList store (user: Guid) (id: Guid) = find store (scopeOf user "TodoList") listModel id
let pageTodos store (user: Guid) (listId: Guid) cursor size =
    page store (scopeOf user "Todo") todoModel todoItems todoAccess None (Some listId) cursor size
let gone (r: Result<Record, Finding>) = match r with Result.Error f -> f.Code = ErrorCode.NOT_FOUND | Ok _ -> false
let shapeFor (t: Type) : Shape = shapeOf [ (describeOk t, [], todoAccess) ]
let tempDir () =
    let d = Path.Combine("/tmp", "persist-show-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory d |> ignore
    d

// ---- print helpers ----
let cut (n: int) (s: string) = if s.Length > n then s.Substring(0, n - 3) + "..." else s
let say (s: string) = printfn "%s" (cut 100 s)
/// Prints each line of the text that holds one of the fragments.
let lines (subs: string list) (text: string) =
    for l in text.Split('\n') do
        if subs |> List.exists (fun s -> l.Contains(s: string)) then say (l.Trim().TrimEnd(','))
let cols (ixs: IndexDef list) = ixs |> List.map (fun i -> sprintf "%s%s(%s)" (if i.Unique then "unique " else "") i.Name (String.concat ", " i.Columns))
let finding (f: Finding) = sprintf "%A on %A: \"%s\"" f.Code f.Field f.Message

/// 25 todos in one list of u1: 6 with no due date, 19 over 3 dates. Decoys: another list of u1, and a list of u2.
let seed (store: Store) =
    let list = addList store u1 "Groceries"
    let other = addList store u1 "Other"
    let foreign = addList store u2 "Foreign"
    let dates = [| DateOnly(2026, 11, 3); DateOnly(2026, 11, 1); DateOnly(2026, 11, 2) |]
    let todos = [ for i in 1..25 -> addTodo store u1 list.Id (sprintf "Todo %d" i) (if i % 4 = 0 then None else Some dates.[i % 3]) ]
    addTodo store u1 other.Id "Decoy one" (Some dates.[0]) |> ignore
    addTodo store u2 foreign.Id "Decoy two" None |> ignore
    list, todos
let collectAll store (listId: Guid) (size: int) =
    let rec loop cursor acc n =
        if n > 30 then failwith "Paging did not end"
        let p = okOf (pageTodos store u1 listId cursor (Some size))
        let acc = acc @ [ p.Rows |> List.map (fun r -> r.Id) ]
        match p.Next with
        | Some c when not p.Rows.IsEmpty -> loop (Some c) acc (n + 1)
        | _ -> acc
    loop None [] 0
let limitError (size: int) =
    use conn = openDb ()
    let store = newStore conn None
    let list = addList store u1 "Groceries"
    errOf (pageTodos store u1 list.Id None (Some size))
let todoAt (store: Store) (target: int64) : Record =
    let list = addList store u1 "Groceries"
    let mutable t = addTodo store u1 list.Id "Original" None
    for _ in 1L .. target - 1L do
        t <- okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some t.Version) t)
    t
let withTitle (title: string) (r: Record) : Record = { r with Values = r.Values.Add("Title", box title) }
let seedList (store: Store) (user: Guid) =
    let list = addList store user "Old"
    list, [ for i in 1..3 -> addTodo store user list.Id (sprintf "Todo %d" i) None ]
let children = [ (todoModel, "List") ]
let countTodos (store: Store) (user: Guid) (todos: Record list) =
    todos |> List.filter (fun t -> not (gone (findTodo store user t.Id))) |> List.length

let key = fsi.CommandLineArgs |> Array.last
printfn "$ show %s" key
match key with
| "R1.1" ->
    let sql = createTable SqlServer todoModel todoItems todoAccess
    lines [ "CREATE TABLE"; "[Id]"; "[Owner]"; "[Version]"; "[CreatedAt]"; "[UpdatedAt]" ] sql
| "R1.2" ->
    let sql = createTable SqlServer userModel [] userAccess
    lines [ "CREATE TABLE"; "[Id]" ] sql
    say (sprintf "User table holds [Owner]: %b" (sql.Contains "[Owner]"))
| "R1.3" ->
    lines [ "[Title]"; "[Notes]"; "[Priority]"; "[Due] date"; "[CompletedAt]" ] (createTable SqlServer todoModel todoItems todoAccess)
| "R1.4" ->
    lines [ "[HasDue]" ] (createTable SqlServer todoModel todoItems todoAccess)
| "R1.5" ->
    let sql = createTable SqlServer userModel userColumnItems userAccess
    lines [ "ExternalSubject" ] sql
    say (sprintf "User table holds [Subject]: %b" (sql.Contains "[Subject]"))
| "R1.6" ->
    let plain = schema SqlServer [ (userModel, [], userAccess) ]
    let withSensitive = schema SqlServer [ (userModel, userSensitiveItems, userAccess) ]
    say (sprintf "schema without sensitive: %d chars" plain.Length)
    say (sprintf "schema with sensitive:    %d chars, equal: %b" withSensitive.Length (plain = withSensitive))
| "R2.1" ->
    lines [ "FK_Todo_List" ] sqlServerSchema
| "R2.2" ->
    say (sprintf "schema of the 3 models: %d lines, %d chars" (sqlServerSchema.Split('\n').Length) sqlServerSchema.Length)
    say (sprintf "contains \"CASCADE\": %b" (sqlServerSchema.Contains("CASCADE", StringComparison.OrdinalIgnoreCase)))
| "R2.3" ->
    let ixs = indexes todoModel todoItems todoAccess
    for c in cols ixs do say c
    say (sprintf "indexes that start with List: %d" (ixs |> List.filter (fun i -> i.Columns.Head = "List") |> List.length))
| "R3.1" ->
    for c in cols (indexes listModel listItems listAccess) do say c
| "R3.2" ->
    for c in cols (indexes todoModel todoItems todoAccess) do say c
| "R3.3" ->
    let ixs = indexes userModel [] userAccess
    for c in cols ixs do say c
    say (sprintf "indexes: %d, unique: %b" ixs.Length ixs.Head.Unique)
| "R3.4" ->
    for c in cols (indexes noteModel [] noteAccess) do say c
| "R4.1" ->
    let sql = (listQuery SqlServer todoModel todoItems todoAccess None false).Sql
    say (sql.Substring(0, sql.IndexOf " WHERE"))
| "R4.2" ->
    let sql = (listQuery SqlServer todoModel todoItems todoAccess None false).Sql
    say (sql.Substring(sql.IndexOf "WHERE"))
| "R4.3" ->
    let sql = (listQuery SqlServer todoModel todoItems todoAccess None false).Sql
    say (sql.Substring(sql.IndexOf "ORDER BY"))
| "R4.4" ->
    let sql = (listQuery SqlServer listModel listItems listAccess (Some "Archived") false).Sql
    say sql.[sql.IndexOf "WHERE" ..]
| "R4.5" ->
    let sql = (listQuery SqlServer listModel listItems listAccess None false).Sql
    say (sql.Substring(0, sql.IndexOf " WHERE"))
| "R5.1" ->
    use conn = openDb ()
    let store = newStore conn None
    let list, todos = seed store
    let pages = collectAll store list.Id 4
    let got = List.concat pages
    let mine = todos |> List.map (fun t -> t.Id) |> Set.ofList
    say (sprintf "pages of 4: %d, sizes: %s" pages.Length (pages |> List.map (fun p -> string p.Length) |> String.concat " "))
    say (sprintf "todos returned: %d, distinct: %d" got.Length (List.distinct got).Length)
    say (sprintf "returned ids that are not among the 25: %d" (got |> List.filter (fun id -> not (mine.Contains id)) |> List.length))
| "R5.2" ->
    use conn = openDb ()
    let store = newStore conn None
    let list, _ = seed store
    for size in [ 1; 200 ] do
        let before = queryCount store
        let p = okOf (pageTodos store u1 list.Id None (Some size))
        say (sprintf "page size %d: queryCount before %d, after %d, rows %d" size before (queryCount store) p.Rows.Length)
| "R5.3" ->
    say (finding (limitError 300))
| "R5.4" ->
    say (finding (limitError 0))
| "R5.5" ->
    use conn = openDb ()
    let store = newStore conn None
    let list = addList store u1 "Groceries"
    for i in 1..210 do addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
    let p = okOf (pageTodos store u1 list.Id None (Some 200))
    say (sprintf "210 todos, page size 200: rows %d, next cursor: %b" p.Rows.Length p.Next.IsSome)
| "R5.6" ->
    use conn = openDb ()
    let store = newStore conn None
    let list = addList store u1 "Groceries"
    for i in 1..55 do addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
    let p = okOf (pageTodos store u1 list.Id None None)
    say (sprintf "55 todos, no page size: rows %d, next cursor: %b" p.Rows.Length p.Next.IsSome)
| "R5.7" ->
    use conn = openDb ()
    let store = newStore conn None
    let list = addList store u1 "Groceries"
    say (finding (errOf (pageTodos store u1 list.Id (Some "not-a-cursor") (Some 4))))
| "R6.1" ->
    use conn = openDb ()
    let store = newStore conn None
    let list, todos = seedList store u1
    say (sprintf "before: list exists %b, todos %d" (not (gone (findList store u1 list.Id))) (countTodos store u1 todos))
    okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
    say (sprintf "after remove: list exists %b, todos %d" (not (gone (findList store u1 list.Id))) (countTodos store u1 todos))
| "R6.2" ->
    use conn = openDb ()
    let hook n = if n = 2 then failwith "disk failure"
    let store = newStore conn (Some hook)
    let list, todos = seedList store u1
    let outcome = try Ok(remove store (scopeOf u1 "TodoList") listModel children list.Id) with e -> Result.Error e.Message
    say (sprintf "remove with a failure after the 2nd todo: %s" (match outcome with Result.Error m -> "raised \"" + m + "\"" | Ok _ -> "no failure"))
    say (sprintf "after: list exists %b, todos %d" (not (gone (findList store u1 list.Id))) (countTodos store u1 todos))
| "R6.3" ->
    use conn = openDb ()
    let store = newStore conn None
    let list, todos = seedList store u2
    let f = errOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
    say (sprintf "u1 removes the list of u2: %A" f.Code)
    say (sprintf "after: list exists %b, todos %d" (not (gone (findList store u2 list.Id))) (countTodos store u2 todos))
| "R7.1" ->
    use conn = openDb ()
    let store = newStore conn None
    let t = todoAt store 3L
    let saved = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
    say (sprintf "todo at version %d, save with version 3: Ok, now version %d" t.Version saved.Version)
| "R7.2" ->
    use conn = openDb ()
    let store = newStore conn None
    let t = todoAt store 4L
    let before = okOf (findTodo store u1 t.Id)
    let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
    let after = okOf (findTodo store u1 t.Id)
    say (sprintf "todo at version %d, save with version 3: %A" t.Version f.Code)
    say (sprintf "stored todo: version %d, Title %A, same values: %b" after.Version after.Values.["Title"] (before.Values = after.Values))
| "R7.3" ->
    use conn = openDb ()
    let store = newStore conn None
    let list = addList store u1 "Groceries"
    let t = addTodo store u1 list.Id "New" None
    say (sprintf "add: version %d, stored: version %d" t.Version (okOf (findTodo store u1 t.Id)).Version)
| "R7.4" ->
    use conn = openDb ()
    let store = newStore conn None
    let t = todoAt store 3L
    let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id None (withTitle "Changed" t))
    say (finding f)
    say (sprintf "stored version: %d" (okOf (findTodo store u1 t.Id)).Version)
| "R8.1" ->
    let f = errOf (migration (Some(shapeFor typeof<Todo>)) (shapeFor typeof<DueRenamed.Todo>) [])
    say (sprintf "%A: %s" f.Code f.Message)
| "R8.2" ->
    let rename = { Model = "Todo"; From = "Due"; To = "DueDate" }
    let sql = okOf (migration (Some(shapeFor typeof<Todo>)) (shapeFor typeof<DueRenamed.Todo>) [ rename ])
    lines [ "sp_rename" ] sql
    say (sprintf "contains DROP COLUMN: %b" (sql.Contains "DROP COLUMN"))
| "R8.3" ->
    lines [ "ALTER TABLE" ] (okOf (migration (Some(shapeFor typeof<Todo>)) (shapeFor typeof<WithTags.Todo>) []))
| "R8.4" ->
    let f = errOf (migration (Some(shapeFor typeof<Todo>)) (shapeFor typeof<WithCode.Todo>) [])
    say (sprintf "%A: %s" f.Code f.Message)
| "R8.5" ->
    let models = [ (todoModel, [], todoAccess) ]
    let next = shapeOf models
    let sql = okOf (migration None next [])
    say (sprintf "migration with no stored shape = schema SqlServer: %b (%d chars)" (sql = schema SqlServer models) sql.Length)
    let dir = tempDir ()
    try
        let path = Path.Combine(dir, "shape.json")
        writeShape path next
        say (sprintf "writeShape then readShape: file %d bytes, same shape: %b" (FileInfo(path).Length) (okOf (readShape path) = Some next))
    finally
        Directory.Delete(dir, true)
| "R8.6" ->
    let dir = tempDir ()
    try
        let path = Path.Combine(dir, "shape.json")
        File.WriteAllText(path, "{ this is not json")
        let f = errOf (readShape path)
        say (sprintf "%A: %s" f.Code f.Message)
    finally
        Directory.Delete(dir, true)
| other -> printfn "unknown key %s" other
