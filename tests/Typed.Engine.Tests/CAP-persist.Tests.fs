namespace Typed.Engine.Tests

open System
open System.IO
open Microsoft.Data.Sqlite
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Persist
open Typed.Engine.Tests.ModelsFixtures
open Typed.Engine.Tests.AccessFixtures

/// Models, overrides, and store helpers of the persistence checks. The checks use only these.
module PersistFixtures =

    /// An owned model with a title field, no listSort, and no listView.
    type Note = { Heading: Title; Body: Notes }

    /// Shapes of Todo for the migration checks. The model name is "Todo" in each module.
    module DueRenamed =
        type Todo =
            { List: Ref<TodoList>
              Title: Title
              Notes: Notes option
              Priority: Priority
              DueDate: DateOnly option
              Done: bool
              CompletedAt: DateTimeOffset option }

    module WithTags =
        type Todo =
            { List: Ref<TodoList>
              Title: Title
              Notes: Notes option
              Priority: Priority
              Due: DateOnly option
              Done: bool
              CompletedAt: DateTimeOffset option
              Tags: Title option }

    module WithCode =
        type Todo =
            { List: Ref<TodoList>
              Title: Title
              Notes: Notes option
              Priority: Priority
              Due: DateOnly option
              Done: bool
              CompletedAt: DateTimeOffset option
              Code: Title }

    let accessOf (modelName: string) : Access =
        (rules |> List.find (fun r -> r.ModelName = modelName)).Access

    let todoAccess = accessOf "Todo"
    let listAccess = accessOf "TodoList"
    let userAccess = accessOf "User"
    let noteAccess = (rule<Note> owned).Access

    let todoModel = describeOk typeof<Todo>
    let listModel = describeOk typeof<TodoList>
    let userModel = describeOk typeof<User>
    let noteModel = describeOk typeof<Note>

    let todoItems =
        (overrides<Todo>
            [ listColumns
                  [ col <@ fun (t: Todo) -> t.Title @>
                    col <@ fun (t: Todo) -> t.Due @>
                    col <@ fun (t: Todo) -> t.Priority @>
                    col <@ fun (t: Todo) -> t.Done @> ]
              listSort <@ fun (t: Todo) -> t.Due @> Ascending ])
            .Items

    let listItems =
        (overrides<TodoList>
            [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
              listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ])
            .Items

    let userColumnItems =
        (overrides<User> [ column <@ fun (u: User) -> u.Subject @> "ExternalSubject" ]).Items

    let userSensitiveItems =
        (overrides<User> [ sensitive <@ fun (u: User) -> u.Subject @> ]).Items

    /// The three models of the todo app with their overrides and access.
    let appModels =
        [ (listModel, listItems, listAccess)
          (todoModel, todoItems, todoAccess)
          (userModel, [], userAccess) ]

    let sqlServerSchema = schema SqlServer appModels

    // ---- store helpers ----

    let fixedNow () = DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)

    let okOf (r: Result<'a, Finding>) : 'a =
        match r with
        | Ok v -> v
        | Result.Error f -> failwithf "Expected Ok, got: %A" f

    let errOf (r: Result<'a, Finding>) : Finding =
        match r with
        | Ok v -> failwithf "Expected an error, got Ok: %A" v
        | Result.Error f -> f

    let scopeOf (user: Guid) (modelName: string) : Scope =
        match scopeFor rules user modelName with
        | Ok s -> s
        | Result.Error f -> failwithf "Expected a scope, got: %A" f

    /// An open in-memory SQLite connection with the schema of the todo app.
    let openDb () : SqliteConnection =
        let conn = new SqliteConnection("Data Source=:memory:")
        conn.Open()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- schema Sqlite appModels
        cmd.ExecuteNonQuery() |> ignore
        conn

    let newStore (conn: SqliteConnection) (hook: (int -> unit) option) : Store =
        Store.create conn Sqlite fixedNow hook

    let listRec (name: string) : Record =
        { Model = "TodoList"
          Id = Guid.Empty
          Owner = None
          Version = 0L
          Values = Map [ "Name", box name; "Archived", box false ] }

    let todoRec (listId: Guid) (title: string) (due: DateOnly option) : Record =
        let baseValues =
            Map [ "List", box listId
                  "Title", box title
                  "Priority", box "Normal"
                  "Done", box false ]
        let values =
            match due with
            | Some d -> baseValues.Add("Due", box d)
            | None -> baseValues
        { Model = "Todo"
          Id = Guid.Empty
          Owner = None
          Version = 0L
          Values = values }

    let addList store (user: Guid) (name: string) : Record =
        okOf (add store (scopeOf user "TodoList") listModel (listRec name))

    let addTodo store (user: Guid) (listId: Guid) (title: string) (due: DateOnly option) : Record =
        okOf (add store (scopeOf user "Todo") todoModel (todoRec listId title due))

    let findTodo store (user: Guid) (id: Guid) =
        find store (scopeOf user "Todo") todoModel id

    let findList store (user: Guid) (id: Guid) =
        find store (scopeOf user "TodoList") listModel id

    let isGone (r: Result<Record, Finding>) : bool =
        match r with
        | Result.Error f -> f.Code = ErrorCode.NOT_FOUND
        | Ok _ -> false

    let pageTodos store (user: Guid) (listId: Guid) (cursor: string option) (size: int option) =
        page store (scopeOf user "Todo") todoModel todoItems todoAccess None (Some listId) cursor size

    // ---- shape helpers ----

    let shapeFor (t: Type) : Shape =
        shapeOf [ (describeOk t, [], todoAccess) ]

    let tempDir () : string =
        let d = Path.Combine(Path.GetTempPath(), "persist-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory d |> ignore
        d

    let hasIndex (cols: string list) (ixs: IndexDef list) : bool =
        ixs |> List.exists (fun i -> i.Columns = cols)

open PersistFixtures

module ``CAP-persist-R1`` =

    [<Fact>]
    let ``CAP-persist.R1 Todo gets a table with the five system columns`` () =
        Assert.Contains("CREATE TABLE [Todo]", sqlServerSchema)
        Assert.Contains("[Id] uniqueidentifier NOT NULL PRIMARY KEY", sqlServerSchema)
        Assert.Contains("[Owner] uniqueidentifier NOT NULL", sqlServerSchema)
        Assert.Contains("[Version] rowversion NOT NULL", sqlServerSchema)
        Assert.Contains("[CreatedAt] datetimeoffset NOT NULL", sqlServerSchema)
        Assert.Contains("[UpdatedAt] datetimeoffset NOT NULL", sqlServerSchema)

    [<Fact>]
    let ``CAP-persist.R1 User with the rule self has no Owner column`` () =
        let sql = createTable SqlServer userModel [] userAccess
        Assert.Contains("CREATE TABLE [User]", sql)
        Assert.DoesNotContain("[Owner]", sql)

    [<Fact>]
    let ``CAP-persist.R1 Todo with an owned rule keeps the Owner column`` () =
        let sql = createTable SqlServer todoModel todoItems todoAccess
        Assert.Contains("[Owner] uniqueidentifier NOT NULL", sql)

    [<Fact>]
    let ``CAP-persist.R1 Todo fields get their SQL types`` () =
        let sql = createTable SqlServer todoModel todoItems todoAccess
        Assert.Contains("[Title] nvarchar(200) NOT NULL", sql)
        Assert.Contains("[Notes] nvarchar(max) NULL", sql)
        Assert.Contains("[Priority] varchar(16) NOT NULL", sql)
        Assert.Contains("[Due] date NULL", sql)
        Assert.Contains("[CompletedAt] datetimeoffset NULL", sql)

    [<Fact>]
    let ``CAP-persist.R1 listSort on Due adds the persisted HasDue column`` () =
        let sql = createTable SqlServer todoModel todoItems todoAccess
        Assert.Contains("[HasDue] AS (CASE WHEN [Due] IS NULL THEN 0 ELSE 1 END) PERSISTED", sql)

    [<Fact>]
    let ``CAP-persist.R1 no listSort means no HasDue column`` () =
        let sql = createTable SqlServer todoModel [] todoAccess
        Assert.DoesNotContain("HasDue", sql)

    [<Fact>]
    let ``CAP-persist.R1 the column override renames Subject to ExternalSubject`` () =
        let sql = createTable SqlServer userModel userColumnItems userAccess
        Assert.Contains("[ExternalSubject] nvarchar(255) NOT NULL", sql)
        Assert.DoesNotContain("[Subject]", sql)

    [<Fact>]
    let ``CAP-persist.R1 the sensitive override leaves the schema as it was`` () =
        let plain = schema SqlServer [ (userModel, [], userAccess) ]
        let withSensitive = schema SqlServer [ (userModel, userSensitiveItems, userAccess) ]
        Assert.Equal(plain, withSensitive)

module ``CAP-persist-R2`` =

    [<Fact>]
    let ``CAP-persist.R2 Todo gets the foreign key FK_Todo_List`` () =
        Assert.Contains(
            "ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES [TodoList] ([Id]);",
            sqlServerSchema
        )

    [<Fact>]
    let ``CAP-persist.R2 foreignKeys lists the key of Todo once`` () =
        let fks = foreignKeys todoModel todoItems todoAccess
        Assert.Equal(1, fks.Length)
        Assert.Contains("[FK_Todo_List]", fks.Head)

    [<Fact>]
    let ``CAP-persist.R2 the schema of the three models has no CASCADE`` () =
        Assert.DoesNotContain("CASCADE", sqlServerSchema, StringComparison.OrdinalIgnoreCase)

    [<Fact>]
    let ``CAP-persist.R2 exactly one Todo index starts with the column List`` () =
        let starts = indexes todoModel todoItems todoAccess |> List.filter (fun i -> i.Columns.Head = "List")
        Assert.Equal(1, starts.Length)

module ``CAP-persist-R3`` =

    [<Fact>]
    let ``CAP-persist.R3 TodoList with two views gets one index on Owner Archived Name Id`` () =
        let ixs = indexes listModel listItems listAccess
        let matching = ixs |> List.filter (fun i -> i.Columns = [ "Owner"; "Archived"; "Name"; "Id" ])
        Assert.Equal(1, matching.Length)

    [<Fact>]
    let ``CAP-persist.R3 Todo with listSort on Due gets one index on Owner List HasDue Due Id`` () =
        let ixs = indexes todoModel todoItems todoAccess
        let matching = ixs |> List.filter (fun i -> i.Columns = [ "Owner"; "List"; "HasDue"; "Due"; "Id" ])
        Assert.Equal(1, matching.Length)

    [<Fact>]
    let ``CAP-persist.R3 User gets a unique index on Subject and no list index`` () =
        let ixs = indexes userModel [] userAccess
        Assert.Equal(1, ixs.Length)
        Assert.Equal<string list>([ "Subject" ], ixs.Head.Columns)
        Assert.True ixs.Head.Unique

    [<Fact>]
    let ``CAP-persist.R3 an owned model with a title and no sort gets an index on Owner Heading Id`` () =
        let ixs = indexes noteModel [] noteAccess
        Assert.True(hasIndex [ "Owner"; "Heading"; "Id" ] ixs)

module ``CAP-persist-R4`` =

    let private todoQuery () =
        (listQuery SqlServer todoModel todoItems todoAccess None false).Sql

    [<Fact>]
    let ``CAP-persist.R4 the Todo list query names its columns`` () =
        Assert.StartsWith("SELECT TOP (@limit) [Id], [Title], [Due], [Priority], [Done] FROM [Todo]", todoQuery ())

    [<Fact>]
    let ``CAP-persist.R4 the Todo list query filters by owner and parent`` () =
        let sql = todoQuery ()
        Assert.Contains("WHERE", sql)
        Assert.Contains("[Owner] = @owner", sql)
        Assert.Contains("[List] = @parent", sql)

    [<Fact>]
    let ``CAP-persist.R4 the Todo list query orders by HasDue descending then Due then Id`` () =
        Assert.EndsWith("ORDER BY [HasDue] DESC, [Due] ASC, [Id] ASC", todoQuery ())

    [<Fact>]
    let ``CAP-persist.R4 the Archived view adds Archived = 1`` () =
        let sql = (listQuery SqlServer listModel listItems listAccess (Some "Archived") false).Sql
        Assert.Contains("[Archived] = 1", sql)
        Assert.Contains("[Owner] = @owner", sql)

    [<Fact>]
    let ``CAP-persist.R4 the Active view does not hold Archived = 1`` () =
        let sql = (listQuery SqlServer listModel listItems listAccess (Some "Active") false).Sql
        Assert.DoesNotContain("[Archived] = 1", sql)

    [<Fact>]
    let ``CAP-persist.R4 TodoList with no listColumns selects Id Name Archived`` () =
        let sql = (listQuery SqlServer listModel listItems listAccess None false).Sql
        Assert.StartsWith("SELECT TOP (@limit) [Id], [Name], [Archived] FROM [TodoList]", sql)

    [<Fact>]
    let ``CAP-persist.R4 Todo with no listColumns does not select the wide Notes column`` () =
        let sql = (listQuery SqlServer todoModel [] todoAccess None false).Sql
        Assert.DoesNotContain("[Notes]", sql)

module ``CAP-persist-R5`` =

    let private d1 = DateOnly(2026, 11, 3)
    let private d2 = DateOnly(2026, 11, 1)
    let private d3 = DateOnly(2026, 11, 2)

    /// 25 todos in one list of u1: 6 with no due date, 19 spread over 3 dates, not in order.
    /// Decoys: another list of u1 and a list of u2.
    let private seed (store: Store) =
        let list = addList store u1 "Groceries"
        let other = addList store u1 "Other"
        let foreign = addList store u2 "Foreign"
        let dates = [| d1; d2; d3 |]
        let dueOf i = if i % 4 = 0 then None else Some dates.[i % 3]
        let todos = [ for i in 1..25 -> addTodo store u1 list.Id (sprintf "Todo %d" i) (dueOf i) ]
        addTodo store u1 other.Id "Decoy one" (Some d1) |> ignore
        addTodo store u2 foreign.Id "Decoy two" None |> ignore
        list, todos

    let private collectAll store (listId: Guid) (size: int) : Guid list =
        let rec loop cursor acc n =
            if n > 30 then failwith "Paging did not end after 30 pages"
            let p = okOf (pageTodos store u1 listId cursor (Some size))
            let acc = acc @ (p.Rows |> List.map (fun r -> r.Id))
            match p.Next with
            | Some c when not p.Rows.IsEmpty -> loop (Some c) acc (n + 1)
            | _ -> acc
        loop None [] 0

    [<Fact>]
    let ``CAP-persist.R5 paging 4 at a time returns each of the 25 todos one time`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seed store
        let got = collectAll store list.Id 4
        Assert.Equal(25, got.Length)
        Assert.Equal(25, (got |> List.distinct).Length)
        Assert.Equal<Guid list>((todos |> List.map (fun t -> t.Id) |> List.sort), List.sort got)

    [<Fact>]
    let ``CAP-persist.R5 paging follows the sort: dated todos by date, then the empty ones`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seed store
        let dueById =
            todos
            |> List.map (fun t ->
                t.Id,
                (match t.Values.TryFind "Due" with
                 | Some v when not (isNull v) -> (0, v :?> DateOnly)
                 | _ -> (1, DateOnly.MinValue)))
            |> Map.ofList
        let keys = collectAll store list.Id 4 |> List.map (fun id -> dueById.[id])
        Assert.Equal<(int * DateOnly) list>(List.sort keys, keys)
        Assert.Equal(6, keys |> List.filter (fun (g, _) -> g = 1) |> List.length)
        Assert.Equal(3, keys |> List.map snd |> List.filter (fun d -> d <> DateOnly.MinValue) |> List.distinct |> List.length)

    [<Fact>]
    let ``CAP-persist.R5 a page of size 1 runs one query`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, _ = seed store
        let before = queryCount store
        let p = okOf (pageTodos store u1 list.Id None (Some 1))
        Assert.Equal(1, p.Rows.Length)
        Assert.Equal(1, queryCount store - before)

    [<Fact>]
    let ``CAP-persist.R5 a page of size 200 runs one query`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, _ = seed store
        let before = queryCount store
        let p = okOf (pageTodos store u1 list.Id None (Some 200))
        Assert.Equal(25, p.Rows.Length)
        Assert.Equal(1, queryCount store - before)

    [<Fact>]
    let ``CAP-persist.R5 a page of 200 holds 200 of 210 todos`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        for i in 1..210 do
            addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
        let p = okOf (pageTodos store u1 list.Id None (Some 200))
        Assert.Equal(200, p.Rows.Length)
        Assert.True p.Next.IsSome

    let private limitError (size: int) =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        errOf (pageTodos store u1 list.Id None (Some size))

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(201)>]
    [<InlineData(300)>]
    [<InlineData(-1)>]
    let ``CAP-persist.R5 a page size outside 1 to 200 gives VALIDATION_FAILED on limit`` (size: int) =
        let f = limitError size
        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
        Assert.Equal(Some "limit", f.Field)
        Assert.Equal("must be between 1 and 200", f.Message)

    [<Fact>]
    let ``CAP-persist.R5 a page size of 200 and a page size of 1 are accepted`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        addTodo store u1 list.Id "One" None |> ignore
        Assert.Equal(1, (okOf (pageTodos store u1 list.Id None (Some 200))).Rows.Length)
        Assert.Equal(1, (okOf (pageTodos store u1 list.Id None (Some 1))).Rows.Length)

    [<Fact>]
    let ``CAP-persist.R5 no page size gives a page of 50`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        for i in 1..55 do
            addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
        let p = okOf (pageTodos store u1 list.Id None None)
        Assert.Equal(50, p.Rows.Length)
        Assert.True p.Next.IsSome

    [<Fact>]
    let ``CAP-persist.R5 a bad cursor gives VALIDATION_FAILED on cursor`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        let f = errOf (pageTodos store u1 list.Id (Some "not-a-cursor") (Some 4))
        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
        Assert.Equal(Some "cursor", f.Field)
        Assert.Equal("is not valid", f.Message)

module ``CAP-persist-R6`` =

    let private children = [ (todoModel, "List") ]

    let private seedList (store: Store) (user: Guid) =
        let list = addList store user "Old"
        let todos = [ for i in 1..3 -> addTodo store user list.Id (sprintf "Todo %d" i) None ]
        list, todos

    [<Fact>]
    let ``CAP-persist.R6 removing a list removes the list and its 3 todos`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seedList store u1
        okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
        Assert.True(isGone (findList store u1 list.Id))
        for t in todos do
            Assert.True(isGone (findTodo store u1 t.Id))

    [<Fact>]
    let ``CAP-persist.R6 removing a list keeps the todos of another list`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, _ = seedList store u1
        let other = addList store u1 "Other"
        let keep = addTodo store u1 other.Id "Keep" None
        okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
        Assert.False(isGone (findTodo store u1 keep.Id))

    [<Fact>]
    let ``CAP-persist.R6 a failure after the second todo leaves the list and all 3 todos`` () =
        use conn = openDb ()
        let hook n = if n = 2 then failwith "disk failure"
        let store = newStore conn (Some hook)
        let list, todos = seedList store u1
        let outcome =
            try
                Some(remove store (scopeOf u1 "TodoList") listModel children list.Id)
            with _ ->
                None
        match outcome with
        | Some(Ok()) -> failwith "The remove passed although the hook failed"
        | _ -> ()
        Assert.False(isGone (findList store u1 list.Id))
        for t in todos do
            Assert.False(isGone (findTodo store u1 t.Id))

    [<Fact>]
    let ``CAP-persist.R6 u1 removing a list of u2 gives NOT_FOUND and the list stays`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seedList store u2
        let f = errOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
        Assert.False(isGone (findList store u2 list.Id))
        for t in todos do
            Assert.False(isGone (findTodo store u2 t.Id))

module ``CAP-persist-R7`` =

    let private withTitle (title: string) (r: Record) : Record =
        { r with Values = r.Values.Add("Title", box title) }

    /// A todo of u1 at the given version, made by saves from version 1.
    let private todoAt (store: Store) (target: int64) : Record =
        let list = addList store u1 "Groceries"
        let mutable t = addTodo store u1 list.Id "Original" None
        for _ in 1L .. target - 1L do
            let next = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some t.Version) t)
            Assert.Equal(t.Version + 1L, next.Version)
            t <- next
        t

    [<Fact>]
    let ``CAP-persist.R7 a save with version 3 on a todo at version 3 passes and gives version 4`` () =
        use conn = openDb ()
        let store = newStore conn None
        let t = todoAt store 3L
        Assert.Equal(3L, t.Version)
        let saved = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
        Assert.Equal(4L, saved.Version)
        Assert.Equal(4L, (okOf (findTodo store u1 t.Id)).Version)

    [<Fact>]
    let ``CAP-persist.R7 a save with version 3 on a todo at version 4 gives STALE_VERSION`` () =
        use conn = openDb ()
        let store = newStore conn None
        let t = todoAt store 4L
        let before = okOf (findTodo store u1 t.Id)
        let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
        Assert.Equal(ErrorCode.STALE_VERSION, f.Code)
        let after = okOf (findTodo store u1 t.Id)
        Assert.Equal(4L, after.Version)
        Assert.Equal<Map<string, obj>>(before.Values, after.Values)

    [<Fact>]
    let ``CAP-persist.R7 a new todo is stored at version 1`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        let t = addTodo store u1 list.Id "New" None
        Assert.Equal(1L, t.Version)
        Assert.Equal(1L, (okOf (findTodo store u1 t.Id)).Version)

    [<Fact>]
    let ``CAP-persist.R7 a save with no version gives VALIDATION_FAILED on Version`` () =
        use conn = openDb ()
        let store = newStore conn None
        let t = todoAt store 3L
        let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id None (withTitle "Changed" t))
        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
        Assert.Equal(Some "Version", f.Field)
        Assert.Equal("is required", f.Message)
        Assert.Equal(3L, (okOf (findTodo store u1 t.Id)).Version)

module ``CAP-persist-R8`` =

    let private v1 () = shapeFor typeof<Todo>

    let private migrationSql (previous: Shape option) (next: Shape) (renames: Rename list) : string =
        okOf (migration previous next renames)

    [<Fact>]
    let ``CAP-persist.R8 a renamed column with no renamedFrom gives UNPLANNED_DROP`` () =
        let f = errOf (migration (Some(v1 ())) (shapeFor typeof<DueRenamed.Todo>) [])
        Assert.Equal(ErrorCode.UNPLANNED_DROP, f.Code)
        Assert.Equal("Todo.Due would be dropped. Add renamedFrom or an explicit drop.", f.Message)

    [<Fact>]
    let ``CAP-persist.R8 renamedFrom Due on DueDate gives sp_rename`` () =
        let rename = { Model = "Todo"; From = "Due"; To = "DueDate" }
        let sql = migrationSql (Some(v1 ())) (shapeFor typeof<DueRenamed.Todo>) [ rename ]
        Assert.Contains("EXEC sp_rename 'Todo.Due', 'DueDate', 'COLUMN';", sql)
        Assert.DoesNotContain("DROP COLUMN", sql)

    [<Fact>]
    let ``CAP-persist.R8 one more optional column gives ALTER TABLE ADD`` () =
        let sql = migrationSql (Some(v1 ())) (shapeFor typeof<WithTags.Todo>) []
        Assert.Contains("ALTER TABLE [Todo] ADD [Tags] nvarchar(200) NULL", sql)

    [<Fact>]
    let ``CAP-persist.R8 one more required text column gives UNSAFE_MIGRATION`` () =
        let f = errOf (migration (Some(v1 ())) (shapeFor typeof<WithCode.Todo>) [])
        Assert.Equal(ErrorCode.UNSAFE_MIGRATION, f.Code)
        Assert.Equal("Todo.Code is required and has no default, so existing rows would fail.", f.Message)

    [<Fact>]
    let ``CAP-persist.R8 no stored shape gives the full schema`` () =
        let models = [ (todoModel, [], todoAccess) ]
        let sql = migrationSql None (shapeOf models) []
        Assert.Equal(schema SqlServer models, sql)

    [<Fact>]
    let ``CAP-persist.R8 a written shape reads back the same`` () =
        let dir = tempDir ()
        try
            let path = Path.Combine(dir, "shape.json")
            let shape = v1 ()
            writeShape path shape
            Assert.Equal(Some shape, okOf (readShape path))
        finally
            Directory.Delete(dir, true)

    [<Fact>]
    let ``CAP-persist.R8 no shape file reads as None`` () =
        let dir = tempDir ()
        try
            Assert.Equal(None, okOf (readShape (Path.Combine(dir, "shape.json"))))
        finally
            Directory.Delete(dir, true)

    [<Fact>]
    let ``CAP-persist.R8 a shape file that is not JSON gives SHAPE_UNREADABLE`` () =
        let dir = tempDir ()
        try
            let path = Path.Combine(dir, "shape.json")
            File.WriteAllText(path, "{ this is not json")
            let f = errOf (readShape path)
            Assert.Equal(ErrorCode.SHAPE_UNREADABLE, f.Code)
            Assert.Equal("build/shape.json cannot be read. Delete it to start from an empty shape.", f.Message)
        finally
            Directory.Delete(dir, true)
