namespace Typed.Engine.Tests

open System
open Microsoft.FSharp.Quotations
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides

/// Models, semantic types, and type defaults of the todo app. The checks use only these.
module ModelsFixtures =

    [<TitleType>]
    type Title = Title of string

    type Notes = Notes of string

    type Priority =
        | Low
        | Normal
        | High

    type TimeZoneId = TimeZoneId of string

    type ExternalId = ExternalId of string

    type User =
        { Subject: ExternalId
          DisplayName: Title
          TimeZone: TimeZoneId }

    type TodoList = { Name: Title; Archived: bool }

    type Todo =
        { List: Ref<TodoList>
          Title: Title
          Notes: Notes option
          Priority: Priority
          Due: DateOnly option
          Done: bool
          CompletedAt: DateTimeOffset option }

    type NoTitle = { Count: int }

    type Tagged = { Tags: Title list }

    type TwoMissing = { Tags: Title list; Aliases: Notes list }

    let private def (clr: Type) kind : TypeDefault =
        { Clr = clr
          Kind = kind
          Check = (fun _ -> [])
          Hidden = false
          Unique = false }

    /// Exactly the four entries of the spec. The engine gives Priority and Ref kinds itself.
    let defaults: TypeDefault list =
        [ def typeof<Title> (Text 200)
          def typeof<Notes> (Text 5000)
          def typeof<TimeZoneId> (Text 64)
          { def typeof<ExternalId> (Text 255) with
              Hidden = true
              Unique = true } ]

    let describeOk (t: Type) : Model =
        match describe defaults t with
        | Ok m -> m
        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs

    let describeErrors (t: Type) : Finding list =
        match describe defaults t with
        | Ok m -> failwithf "Expected errors, got a plan for %s" m.Name
        | Result.Error fs -> fs

    let field (m: Model) (name: string) : Field =
        m.Fields |> List.find (fun f -> f.Name = name)

open ModelsFixtures

module ``CAP-models.R1`` =

    [<Fact>]
    let ``CAP-models.R1 Title has kind text 200`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Text 200, (field m "Title").Kind)

    [<Fact>]
    let ``CAP-models.R1 Notes has kind optional of text 5000`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Optional(Text 5000), (field m "Notes").Kind)

    [<Fact>]
    let ``CAP-models.R1 Priority has kind enum with Low Normal High`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Enum [ "Low"; "Normal"; "High" ], (field m "Priority").Kind)

    [<Fact>]
    let ``CAP-models.R1 Due has kind optional of date`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Optional Date, (field m "Due").Kind)

    [<Fact>]
    let ``CAP-models.R1 List has kind reference to TodoList`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Reference "TodoList", (field m "List").Kind)

    [<Fact>]
    let ``CAP-models.R1 Done is boolean and CompletedAt is optional of timestamp`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Boolean, (field m "Done").Kind)
        Assert.Equal(Optional Timestamp, (field m "CompletedAt").Kind)

    [<Fact>]
    let ``CAP-models.R1 a union type as a model is WRONG_CONFIG_TYPE`` () =
        let fs = describeErrors typeof<Priority>
        Assert.Equal(1, fs.Length)
        Assert.Equal(ErrorCode.WRONG_CONFIG_TYPE, fs.Head.Code)
        Assert.Equal("Priority is not a record type.", fs.Head.Message)

module ``CAP-models.R2`` =

    [<Fact>]
    let ``CAP-models.R2 a list of Title has no default and names each engine`` () =
        let fs = describeErrors typeof<Tagged>
        Assert.Equal(1, fs.Length)
        let f = fs.Head
        Assert.Equal(ErrorCode.MISSING_DEFAULT, f.Code)
        Assert.Equal("Tagged", f.Model)
        Assert.Equal(Some "Tags", f.Field)
        Assert.Equal(
            "Tagged.Tags has type 'Title list', which has no default in Persist, Validate, Display, API.",
            f.Message
        )

    [<Fact>]
    let ``CAP-models.R2 two fields with no default give two MISSING_DEFAULT errors`` () =
        let fs = describeErrors typeof<TwoMissing>
        Assert.Equal(2, fs.Length)
        Assert.All(fs, fun f -> Assert.Equal(ErrorCode.MISSING_DEFAULT, f.Code))
        let names = fs |> List.map (fun f -> f.Field) |> List.sort
        Assert.Equal<string option list>([ Some "Aliases"; Some "Tags" ], names)

module ``CAP-models.R3`` =

    [<Fact>]
    let ``CAP-models.R3 the system field names are known`` () =
        Assert.Equal<string list>([ "Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt" ], systemFields)

    [<Fact>]
    let ``CAP-models.R3 the plan of Todo holds its seven fields and no system field`` () =
        let m = describeOk typeof<Todo>
        let names = m.Fields |> List.map (fun f -> f.Name)
        Assert.Equal<string list>(
            [ "List"; "Title"; "Notes"; "Priority"; "Due"; "Done"; "CompletedAt" ],
            names
        )
        for s in systemFields do
            Assert.DoesNotContain(s, names)

module ``CAP-models.R4`` =

    [<Fact>]
    let ``CAP-models.R4 CompletedAt gives Completed at`` () =
        Assert.Equal("Completed at", labelOf "CompletedAt")

    [<Fact>]
    let ``CAP-models.R4 Due gives Due`` () = Assert.Equal("Due", labelOf "Due")

    [<Fact>]
    let ``CAP-models.R4 TimeZone gives Time zone`` () =
        Assert.Equal("Time zone", labelOf "TimeZone")

    [<Fact>]
    let ``CAP-models.R4 ExternalId gives External id`` () =
        Assert.Equal("External id", labelOf "ExternalId")

    [<Fact>]
    let ``CAP-models.R4 HasDue gives Has due`` () =
        Assert.Equal("Has due", labelOf "HasDue")

    [<Fact>]
    let ``CAP-models.R4 the label of a described field comes from its name`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal("Completed at", (field m "CompletedAt").Label)

module ``CAP-models.R5`` =

    [<Fact>]
    let ``CAP-models.R5 Notes is wide`` () =
        let m = describeOk typeof<Todo>
        Assert.True(isWide (field m "Notes").Kind)

    [<Fact>]
    let ``CAP-models.R5 Title is not wide`` () =
        let m = describeOk typeof<Todo>
        Assert.False(isWide (field m "Title").Kind)

    [<Fact>]
    let ``CAP-models.R5 text of length 400 is not wide`` () =
        Assert.False(isWide (Text 400))

    [<Fact>]
    let ``CAP-models.R5 text of length 401 is wide`` () =
        Assert.True(isWide (Text 401))

    [<Fact>]
    let ``CAP-models.R5 a whole number is not wide`` () =
        Assert.False(isWide WholeNumber)

module ``CAP-models.R6`` =

    let private titleOf (t: Type) (items: OverrideItem list) =
        let m = describeOk t
        titleFieldName m.Fields items

    [<Fact>]
    let ``CAP-models.R6 TodoList has the title field Name`` () =
        Assert.Equal(Some "Name", titleOf typeof<TodoList> [])

    [<Fact>]
    let ``CAP-models.R6 User has the title field DisplayName`` () =
        Assert.Equal(Some "DisplayName", titleOf typeof<User> [])

    [<Fact>]
    let ``CAP-models.R6 NoTitle has no title field`` () =
        Assert.Equal(None, titleOf typeof<NoTitle> [])

    [<Fact>]
    let ``CAP-models.R6 the titleField override names Count`` () =
        let mo = overrides<NoTitle> [ titleField <@ fun (n: NoTitle) -> n.Count @> ]
        Assert.Equal(Some "Count", titleOf typeof<NoTitle> mo.Items)

module ``CAP-models.R7`` =

    [<Fact>]
    let ``CAP-models.R7 the selector for Due reads the name Due`` () =
        let q = <@ fun (t: Todo) -> t.Due @> :> Expr
        Assert.Equal("Due", fieldName q)

    [<Fact>]
    let ``CAP-models.R7 listSort gives one ListSort item for Due`` () =
        let mo = overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Due @> Ascending ]
        Assert.Equal("Todo", mo.ModelName)
        match mo.Items with
        | [ ListSort(f, d) ] ->
            Assert.Equal("Due", f)
            Assert.Equal(Ascending, d)
        | other -> failwithf "Expected one ListSort item, got %d items" other.Length

    [<Fact>]
    let ``CAP-models.R7 listColumns holds Title then Due in order`` () =
        let mo =
            overrides<Todo>
                [ listColumns
                      [ col <@ fun (t: Todo) -> t.Title @>
                        col <@ fun (t: Todo) -> t.Due @> ] ]
        match mo.Items with
        | [ ListColumns fs as item ] ->
            Assert.Equal<string list>([ "Title"; "Due" ], fs)
            Assert.Equal<string list>([ "Title"; "Due" ], fieldsOf item)
        | other -> failwithf "Expected one ListColumns item, got %d items" other.Length

    [<Fact>]
    let ``CAP-models.R7 listView uses the field Archived only`` () =
        let mo =
            overrides<TodoList> [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @> ]
        match mo.Items with
        | [ ListView(name, filter) as item ] ->
            Assert.Equal("Active", name)
            Assert.Equal<string list>([ "Archived" ], referencedFields filter)
            Assert.Equal<string list>([ "Archived" ], fieldsOf item)
        | other -> failwithf "Expected one ListView item, got %d items" other.Length
