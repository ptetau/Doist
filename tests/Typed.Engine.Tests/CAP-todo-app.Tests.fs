namespace Typed.Engine.Tests

open System
open System.IO
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Checks

// Short names for the app modules. The app type names (Todo, User) must not hide the namespace.
module AppDefaults = Todo.App.Defaults
module AppSchema = Todo.App.Schema

module TodoAppFixtures =

    open Todo.App.Model

    let describeApp (t: Type) : Model =
        match describe AppDefaults.typeDefaults t with
        | Ok m -> m
        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs

    let fieldOf (m: Model) (name: string) : Field =
        m.Fields |> List.find (fun f -> f.Name = name)

    let defaultFor (t: Type) : TypeDefault =
        AppDefaults.typeDefaults |> List.find (fun d -> d.Clr = t)

    let typeDefaultsFor () =
        defaultFor typeof<Title>, defaultFor typeof<Notes>, defaultFor typeof<TimeZoneId>, defaultFor typeof<ExternalId>

    let ruleFor (name: string) : AccessRule =
        AppSchema.access |> List.find (fun r -> r.ModelName = name)

    let setFor (name: string) : ModelOverrides =
        AppSchema.overrideSets |> List.find (fun s -> s.ModelName = name)

    /// Builders for the expected items. They use the same builders as the spec.
    let expectedTodoItems: OverrideItem list =
        (overrides<Todo>
            [ readOnly <@ fun (t: Todo) -> t.CompletedAt @>
              listEdit <@ fun (t: Todo) -> t.Done @>
              listSort <@ fun (t: Todo) -> t.Due @> Ascending
              listColumns
                  [ col <@ fun (t: Todo) -> t.Title @>
                    col <@ fun (t: Todo) -> t.Due @>
                    col <@ fun (t: Todo) -> t.Priority @>
                    col <@ fun (t: Todo) -> t.Done @> ] ])
            .Items

    let expectedListItems: OverrideItem list =
        (overrides<TodoList>
            [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
              listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ])
            .Items

open TodoAppFixtures

module ``CAP-todo-app-R1`` =

    open Todo.App.Model

    [<Fact>]
    let ``CAP-todo-app.R1 the models are User then TodoList then Todo`` () =
        Assert.Equal<string list>([ "User"; "TodoList"; "Todo" ], AppDefaults.models |> List.map (fun t -> t.Name))

    [<Fact>]
    let ``CAP-todo-app.R1 User has the fields Subject, DisplayName, and TimeZone`` () =
        let m = describeApp typeof<User>
        Assert.Equal<string list>([ "Subject"; "DisplayName"; "TimeZone" ], m.Fields |> List.map (fun f -> f.Name))
        Assert.Equal(Text 255, (fieldOf m "Subject").Kind)
        Assert.Equal(Text 200, (fieldOf m "DisplayName").Kind)
        Assert.Equal(Text 64, (fieldOf m "TimeZone").Kind)

    [<Fact>]
    let ``CAP-todo-app.R1 TodoList has the fields Name and Archived`` () =
        let m = describeApp typeof<TodoList>
        Assert.Equal<string list>([ "Name"; "Archived" ], m.Fields |> List.map (fun f -> f.Name))
        Assert.Equal(Text 200, (fieldOf m "Name").Kind)
        Assert.Equal(Boolean, (fieldOf m "Archived").Kind)

    [<Fact>]
    let ``CAP-todo-app.R1 Todo has its seven fields with their kinds`` () =
        let m = describeApp typeof<Todo>
        Assert.Equal<string list>(
            [ "List"; "Title"; "Notes"; "Priority"; "Due"; "Done"; "CompletedAt" ],
            m.Fields |> List.map (fun f -> f.Name)
        )
        Assert.Equal(Reference "TodoList", (fieldOf m "List").Kind)
        Assert.Equal(Text 200, (fieldOf m "Title").Kind)
        Assert.Equal(Optional(Text 5000), (fieldOf m "Notes").Kind)
        Assert.Equal(Enum [ "Low"; "Normal"; "High" ], (fieldOf m "Priority").Kind)
        Assert.Equal(Optional Date, (fieldOf m "Due").Kind)
        Assert.Equal(Boolean, (fieldOf m "Done").Kind)
        Assert.Equal(Optional Timestamp, (fieldOf m "CompletedAt").Kind)

    [<Fact>]
    let ``CAP-todo-app.R1 Title is text 200, not hidden, not unique`` () =
        let d, _, _, _ = typeDefaultsFor ()
        Assert.Equal(Text 200, d.Kind)
        Assert.False(d.Hidden)
        Assert.False(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 Notes is text 5000, not hidden, not unique`` () =
        let _, d, _, _ = typeDefaultsFor ()
        Assert.Equal(Text 5000, d.Kind)
        Assert.False(d.Hidden)
        Assert.False(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 TimeZoneId is text 64, not hidden, not unique`` () =
        let _, _, d, _ = typeDefaultsFor ()
        Assert.Equal(Text 64, d.Kind)
        Assert.False(d.Hidden)
        Assert.False(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 ExternalId is text 255, unique, and hidden`` () =
        let _, _, _, d = typeDefaultsFor ()
        Assert.Equal(Text 255, d.Kind)
        Assert.True(d.Hidden)
        Assert.True(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 the type defaults hold exactly the four types with a default`` () =
        let clrs = AppDefaults.typeDefaults |> List.map (fun d -> d.Clr.Name) |> List.sort
        Assert.Equal<string list>([ "ExternalId"; "Notes"; "TimeZoneId"; "Title" ], clrs)

module ``CAP-todo-app-R2`` =

    open Todo.App.Model

    [<Fact>]
    let ``CAP-todo-app.R2 there are exactly three access rules`` () =
        Assert.Equal<string list>(
            [ "Todo"; "TodoList"; "User" ],
            AppSchema.access |> List.map (fun r -> r.ModelName) |> List.sort
        )

    [<Fact>]
    let ``CAP-todo-app.R2 User is self`` () =
        Assert.Equal(Self, (ruleFor "User").Access)

    [<Fact>]
    let ``CAP-todo-app.R2 TodoList is owned`` () =
        Assert.Equal(Owned, (ruleFor "TodoList").Access)

    [<Fact>]
    let ``CAP-todo-app.R2 Todo is ownedVia List and read-only when the list is Archived`` () =
        Assert.Equal(OwnedVia("List", "TodoList", Some("TodoList", "Archived")), (ruleFor "Todo").Access)

module ``CAP-todo-app-R3`` =

    [<Fact>]
    let ``CAP-todo-app.R3 the overrides of Todo are the four of the spec`` () =
        let items = (setFor "Todo").Items
        Assert.Equal(4, items.Length)
        for expected in expectedTodoItems do
            Assert.Contains(expected, items)

    [<Fact>]
    let ``CAP-todo-app.R3 listSort of Todo is Due ascending`` () =
        let sorts =
            (setFor "Todo").Items
            |> List.filter (fun i ->
                match i with
                | ListSort _ -> true
                | _ -> false)
        match sorts with
        | [ ListSort(f, d) ] ->
            Assert.Equal("Due", f)
            Assert.Equal(Ascending, d)
        | other -> failwithf "Expected one ListSort item, got %d" other.Length

    [<Fact>]
    let ``CAP-todo-app.R3 listColumns of Todo are Title, Due, Priority, Done in that order`` () =
        let cols =
            (setFor "Todo").Items
            |> List.choose (fun i ->
                match i with
                | ListColumns fs -> Some fs
                | _ -> None)
        Assert.Equal<string list list>([ [ "Title"; "Due"; "Priority"; "Done" ] ], cols)

    [<Fact>]
    let ``CAP-todo-app.R3 the overrides of TodoList are the views Active then Archived`` () =
        let items = (setFor "TodoList").Items
        let names =
            items
            |> List.map (fun i ->
                match i with
                | ListView(n, _) -> n
                | other -> failwithf "Expected a view, got %A" other)
        Assert.Equal<string list>([ "Active"; "Archived" ], names)
        for i in items do
            Assert.Equal<string list>([ "Archived" ], fieldsOf i)

    [<Fact>]
    let ``CAP-todo-app.R3 the Active view is not Archived and the Archived view is Archived`` () =
        let items = (setFor "TodoList").Items
        Assert.Equal<OverrideItem list>(expectedListItems, items)

module HookFixtures =

    open Todo.App.Model

    let at (h: int) : DateTimeOffset = DateTimeOffset(2026, 10, 18, h, 0, 0, TimeSpan.Zero)

    let contextAt (zone: string) (now: DateTimeOffset) : HookContext =
        { User = Guid.NewGuid()
          TimeZone = TimeZoneInfo.FindSystemTimeZoneById zone
          Now = now }

    let todo (done': bool) (completedAt: DateTimeOffset option) (due: DateOnly option) : Todo =
        { List = Ref(Guid.NewGuid())
          Title = Title "Buy milk"
          Notes = None
          Priority = Normal
          Due = due
          Done = done'
          CompletedAt = completedAt }

open HookFixtures

module ``CAP-todo-app-R4`` =

    open Todo.App.Model

    [<Fact>]
    let ``CAP-todo-app.R4 ticking a todo sets CompletedAt to now`` () =
        let ctx = contextAt "UTC" (at 9)
        let previous = todo false None None
        let saved = todo true None None
        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
        Assert.Equal(Some(at 9), result.CompletedAt)
        Assert.True(result.Done)

    [<Fact>]
    let ``CAP-todo-app.R4 unticking a todo empties CompletedAt`` () =
        let ctx = contextAt "UTC" (at 12)
        let previous = todo true (Some(at 9)) None
        // The saved todo still carries the old time, so the hook must clear it.
        let saved = todo false (Some(at 9)) None
        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
        Assert.Equal(None, result.CompletedAt)
        Assert.False(result.Done)

    [<Fact>]
    let ``CAP-todo-app.R4 saving a todo that stays done keeps the first CompletedAt`` () =
        let ctx = contextAt "UTC" (at 12)
        let previous = todo true (Some(at 9)) None
        let saved = todo true (Some(at 9)) None
        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
        Assert.Equal(Some(at 9), result.CompletedAt)

module ``CAP-todo-app-R5`` =

    open Todo.App.Model

    // 2026-10-18T14:00:00Z is 2026-10-19 03:00 in Auckland (UTC+13). It is still 2026-10-18 in UTC.
    let private ctx () = contextAt "Pacific/Auckland" (at 14)

    [<Fact>]
    let ``CAP-todo-app.R5 an open todo due on the Auckland yesterday is overdue`` () =
        let t = todo false None (Some(DateOnly(2026, 10, 18)))
        Assert.True(Todo.App.Hooks.isOverdue (ctx ()) t)

    [<Fact>]
    let ``CAP-todo-app.R5 a done todo due on the Auckland yesterday is not overdue`` () =
        let t = todo true (Some(at 9)) (Some(DateOnly(2026, 10, 18)))
        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)

    [<Fact>]
    let ``CAP-todo-app.R5 an open todo due on the Auckland today is not overdue`` () =
        let t = todo false None (Some(DateOnly(2026, 10, 19)))
        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)

    [<Fact>]
    let ``CAP-todo-app.R5 an open todo with no due date is not overdue`` () =
        let t = todo false None None
        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)

module ``CAP-todo-app-R6`` =

    let private appFolder () : string =
        let rec up (dir: DirectoryInfo) =
            if isNull dir then
                failwith "Folder src/Todo.App not found above the test run folder."
            else
                let candidate = Path.Combine(dir.FullName, "src", "Todo.App")
                if Directory.Exists candidate then candidate else up dir.Parent
        up (DirectoryInfo AppContext.BaseDirectory)

    [<Fact>]
    let ``CAP-todo-app.R6 the source files are Model, Defaults, Schema, and Hooks`` () =
        let root = appFolder ()
        let files =
            Directory.GetFiles(root, "*.fs", SearchOption.AllDirectories)
            |> Array.map (fun p -> Path.GetRelativePath(root, p))
            // Build output is not source.
            |> Array.filter (fun p -> not (p.StartsWith "obj" || p.StartsWith "bin"))
            |> Array.sort
            |> List.ofArray
        Assert.Equal<string list>([ "Defaults.fs"; "Hooks.fs"; "Model.fs"; "Schema.fs" ], files)

    [<Fact>]
    let ``CAP-todo-app.R6 the engine checks find no error in the models, rules, and overrides`` () =
        let fs = run AppDefaults.typeDefaults AppDefaults.models AppSchema.access AppSchema.overrideSets
        Assert.Empty(errors fs)
