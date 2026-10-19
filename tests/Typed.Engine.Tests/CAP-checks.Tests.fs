namespace Typed.Engine.Tests

open System
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Checks
open Typed.Engine.Tests.ModelsFixtures
open Typed.Engine.Tests.AccessFixtures

/// Models, rules, override sets, and helpers of the build checks. The checks use only these.
module ChecksFixtures =

    /// A model that refers to NoTitle (CAP-checks.R6).
    type Child = { Parent: Ref<NoTitle> }

    /// Text types at the edge of wide text (CAP-checks.R13).
    type Big400 = Big400 of string

    type Big401 = Big401 of string

    type Edge400 = { Body: Big400 }

    type Edge401 = { Body: Big401 }

    let private textDefault (clr: Type) (len: int) : TypeDefault =
        { Clr = clr
          Kind = Text len
          Check = (fun _ -> [])
          Hidden = false
          Unique = false }

    let edgeDefaults: TypeDefault list =
        defaults
        @ [ textDefault typeof<Big400> 400
            textDefault typeof<Big401> 401 ]

    let appTypes: Type list = [ typeof<User>; typeof<TodoList>; typeof<Todo> ]

    /// The three access rules of the todo app.
    let appRules: AccessRule list =
        [ rule<User> self
          rule<TodoList> owned
          rule<Todo> readOnlyTodoOwnedVia ]

    /// The two override sets of the todo app.
    let appSets: ModelOverrides list =
        [ overrides<Todo>
              [ readOnly <@ fun (t: Todo) -> t.CompletedAt @>
                listEdit <@ fun (t: Todo) -> t.Done @>
                listSort <@ fun (t: Todo) -> t.Due @> Ascending
                listColumns
                    [ col <@ fun (t: Todo) -> t.Title @>
                      col <@ fun (t: Todo) -> t.Due @>
                      col <@ fun (t: Todo) -> t.Priority @>
                      col <@ fun (t: Todo) -> t.Done @> ] ]
          overrides<TodoList>
              [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
                listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ] ]

    let runApp (rules: AccessRule list) (sets: ModelOverrides list) : Finding list =
        run defaults appTypes rules sets

    /// The app with its own rules, and extra override sets.
    let runAppWith (sets: ModelOverrides list) : Finding list = runApp appRules sets

    let todoSet (items: OverrideItem list) : ModelOverrides = { ModelName = "Todo"; Items = items }

    let withCode (code: ErrorCode) (fs: Finding list) : Finding list =
        fs |> List.filter (fun (f: Finding) -> f.Code = code)

    let messagesOf (fs: Finding list) : string list =
        fs |> List.map (fun (f: Finding) -> f.Message)

    /// Asserts exactly one finding with the code, and returns its message.
    let onlyMessage (code: ErrorCode) (fs: Finding list) : string =
        match withCode code fs with
        | [ f ] -> f.Message
        | other -> failwithf "Expected one %A finding, got %d: %A" code other.Length (messagesOf fs)

open ChecksFixtures

module ``CAP-checks-R1`` =

    [<Fact>]
    let ``CAP-checks.R1 a model with no access rule is an error`` () =
        let fs = run defaults [ typeof<TodoList> ] [] []
        Assert.Equal("Model TodoList has no access rule.", onlyMessage ErrorCode.MISSING_ACCESS_RULE fs)
        Assert.Equal(Severity.Error, (withCode ErrorCode.MISSING_ACCESS_RULE fs).Head.Severity)

    [<Fact>]
    let ``CAP-checks.R1 a model with two access rules is an error`` () =
        let fs = run defaults [ typeof<TodoList> ] [ rule<TodoList> owned; rule<TodoList> owned ] []
        Assert.Equal(
            "Model TodoList has more than one access rule.",
            onlyMessage ErrorCode.MISSING_ACCESS_RULE fs
        )

    [<Fact>]
    let ``CAP-checks.R1 a model with exactly one access rule has no such finding`` () =
        let fs = run defaults [ typeof<TodoList> ] [ rule<TodoList> owned ] []
        Assert.Empty(withCode ErrorCode.MISSING_ACCESS_RULE fs)

    [<Fact>]
    let ``CAP-checks.R1 the three models with the three rules have no such finding`` () =
        Assert.Empty(withCode ErrorCode.MISSING_ACCESS_RULE (runApp appRules []))

module ``CAP-checks-R2`` =

    [<Fact>]
    let ``CAP-checks.R2 an override set for a model that is not in the list is an error`` () =
        let ghost =
            { ModelName = "Ghost"
              Items = [ Label("Name", "Called") ] }
        let fs = runAppWith [ ghost ]
        Assert.Equal("Overrides name Ghost, which is not a model.", onlyMessage ErrorCode.MISSING_FIELD fs)

    [<Fact>]
    let ``CAP-checks.R2 a Label item for a field that does not exist is an error`` () =
        let fs = runAppWith [ todoSet [ Label("Dew", "Dewy") ] ]
        Assert.Equal("Override names missing field Todo.Dew.", onlyMessage ErrorCode.MISSING_FIELD fs)

    [<Fact>]
    let ``CAP-checks.R2 a Label item for a field that exists has no such finding`` () =
        let fs = runAppWith [ todoSet [ Label("Due", "Due date") ] ]
        Assert.Empty(withCode ErrorCode.MISSING_FIELD fs)

module ``CAP-checks-R3`` =

    [<Fact>]
    let ``CAP-checks.R3 a rule predicate of type string on Due is an error`` () =
        let item = Rule("Due", typeof<string>, (fun _ -> true), "Not valid")
        let fs = runAppWith [ todoSet [ item ] ]
        let msg = onlyMessage ErrorCode.WRONG_CONFIG_TYPE fs
        Assert.Contains("Todo.Due", msg)
        // The predicate type.
        Assert.Contains("string", msg, StringComparison.OrdinalIgnoreCase)
        // The type of the field.
        Assert.Contains("DateOnly", msg, StringComparison.OrdinalIgnoreCase)

    [<Fact>]
    let ``CAP-checks.R3 a rule predicate of type Title on Title has no such finding`` () =
        let item = Rule("Title", typeof<Title>, (fun _ -> true), "Not valid")
        let fs = runAppWith [ todoSet [ item ] ]
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE fs)

module ``CAP-checks-R4`` =

    [<Fact>]
    let ``CAP-checks.R4 a Column item for Version is an error`` () =
        let fs = runAppWith [ todoSet [ Column("Version", "Rev") ] ]
        Assert.Equal("Override targets system field Todo.Version.", onlyMessage ErrorCode.SYSTEM_FIELD_TARGET fs)

    [<Fact>]
    let ``CAP-checks.R4 a Column item for an ordinary field has no such finding`` () =
        let fs = runAppWith [ todoSet [ Column("Title", "Heading") ] ]
        Assert.Empty(withCode ErrorCode.SYSTEM_FIELD_TARGET fs)

module ``CAP-checks-R5`` =

    [<Fact>]
    let ``CAP-checks.R5 a reference to a model that is not in the list is an error`` () =
        let fs = run defaults [ typeof<Todo> ] [ rule<Todo> readOnlyTodoOwnedVia ] []
        Assert.Equal(
            "Field Todo.List refers to TodoList, which is not a model.",
            onlyMessage ErrorCode.DANGLING_REFERENCE fs
        )

    [<Fact>]
    let ``CAP-checks.R5 a reference to a model in the list has no such finding`` () =
        Assert.Empty(withCode ErrorCode.DANGLING_REFERENCE (runApp appRules []))

module ``CAP-checks-R6`` =

    let private types = [ typeof<NoTitle>; typeof<Child> ]

    let private rules =
        [ rule<NoTitle> owned
          rule<Child> (ownedVia <@ fun (c: Child) -> c.Parent @>) ]

    [<Fact>]
    let ``CAP-checks.R6 a referenced model with no title field is an error`` () =
        let fs = run defaults types rules []
        Assert.Equal(
            "NoTitle has no title field, but Child.Parent refers to it.",
            onlyMessage ErrorCode.NO_TITLE_FIELD fs
        )

    [<Fact>]
    let ``CAP-checks.R6 a titleField override removes the finding`` () =
        let sets = [ overrides<NoTitle> [ titleField <@ fun (n: NoTitle) -> n.Count @> ] ]
        let fs = run defaults types rules sets
        Assert.Empty(withCode ErrorCode.NO_TITLE_FIELD fs)

module ``CAP-checks-R7`` =

    [<Fact>]
    let ``CAP-checks.R7 an ownedVia rule on a field that is not a reference is an error`` () =
        let bad =
            [ rule<User> self
              rule<TodoList> owned
              { ModelName = "Todo"
                Access = OwnedVia("Title", "TodoList", None) } ]
        let fs = runApp bad []
        Assert.Equal(
            "ownedVia Todo.Title must be a reference to TodoList.",
            onlyMessage ErrorCode.WRONG_CONFIG_TYPE fs
        )

    [<Fact>]
    let ``CAP-checks.R7 a read-only condition on a field that is not bool is an error`` () =
        let bad =
            [ rule<User> self
              rule<TodoList> owned
              { ModelName = "Todo"
                Access = OwnedVia("List", "TodoList", Some("TodoList", "Name")) } ]
        let fs = runApp bad []
        Assert.Equal(
            "readOnlyWhenParent names TodoList.Name, which is not a bool field.",
            onlyMessage ErrorCode.WRONG_CONFIG_TYPE fs
        )

    [<Fact>]
    let ``CAP-checks.R7 a read-only condition on a bool field has no such finding`` () =
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE (runApp appRules []))

module ``CAP-checks-R8`` =

    [<Fact>]
    let ``CAP-checks.R8 an unsafe override is one warning with its reason and no error`` () =
        let sets = [ overrides<TodoList> [ unsafe "rawName" "uses raw SQL" ] ]
        let fs = runAppWith sets
        Assert.Empty(errors fs)
        Assert.Equal(
            "Unsafe escape hatch TodoList.rawName: uses raw SQL",
            onlyMessage ErrorCode.UNSAFE_ESCAPE_HATCH (warnings fs)
        )
        Assert.Equal(Severity.Warning, (withCode ErrorCode.UNSAFE_ESCAPE_HATCH fs).Head.Severity)

    [<Fact>]
    let ``CAP-checks.R8 the reason of each unsafe override is its own`` () =
        let sets =
            [ overrides<TodoList>
                  [ unsafe "rawName" "uses raw SQL"
                    unsafe "otherName" "reads a view" ] ]
        let ws = withCode ErrorCode.UNSAFE_ESCAPE_HATCH (runAppWith sets) |> messagesOf
        Assert.Equal<string list>(
            [ "Unsafe escape hatch TodoList.rawName: uses raw SQL"
              "Unsafe escape hatch TodoList.otherName: reads a view" ],
            ws
        )

module ``CAP-checks-R9`` =

    [<Fact>]
    let ``CAP-checks.R9 a label equal to the default is a warning`` () =
        let sets = [ overrides<Todo> [ label <@ fun (t: Todo) -> t.Due @> "Due" ] ]
        let fs = runAppWith sets
        Assert.Equal(
            "Override Todo.Due label matches the default.",
            onlyMessage ErrorCode.REDUNDANT_OVERRIDE (warnings fs)
        )
        Assert.Empty(errors fs)

    [<Fact>]
    let ``CAP-checks.R9 a label equal to the default made from a camel-case name is a warning`` () =
        let sets = [ overrides<Todo> [ label <@ fun (t: Todo) -> t.CompletedAt @> "Completed at" ] ]
        Assert.Equal(
            "Override Todo.CompletedAt label matches the default.",
            onlyMessage ErrorCode.REDUNDANT_OVERRIDE (runAppWith sets)
        )

    [<Fact>]
    let ``CAP-checks.R9 a label that differs from the default has no warning`` () =
        let sets = [ overrides<Todo> [ label <@ fun (t: Todo) -> t.Due @> "Due date" ] ]
        Assert.Empty(withCode ErrorCode.REDUNDANT_OVERRIDE (runAppWith sets))

module ``CAP-checks-R10`` =

    [<Fact>]
    let ``CAP-checks.R10 the todo app has no error findings`` () =
        let fs = runApp appRules appSets
        Assert.Empty(errors fs)

module ``CAP-checks-R11`` =

    [<Fact>]
    let ``CAP-checks.R11 two label overrides on one field are an error`` () =
        let fs = runAppWith [ todoSet [ Label("Due", "Due date"); Label("Due", "Deadline") ] ]
        Assert.Equal("Todo.Due has two label overrides.", onlyMessage ErrorCode.WRONG_CONFIG_TYPE fs)

    [<Fact>]
    let ``CAP-checks.R11 one label and one readOnly on a field have no such finding`` () =
        let fs = runAppWith [ todoSet [ Label("Due", "Due date"); ReadOnly "Due" ] ]
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE fs)

    [<Fact>]
    let ``CAP-checks.R11 one label on each of two fields have no such finding`` () =
        let fs = runAppWith [ todoSet [ Label("Due", "Due date"); Label("Title", "Heading") ] ]
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE fs)

module ``CAP-checks-R12`` =

    let private noRule = [ "Model TodoList has no access rule."; "Model Todo has no access rule." ]

    [<Fact>]
    let ``CAP-checks.R12 findings follow the order of the models TodoList then Todo`` () =
        let fs = run defaults [ typeof<TodoList>; typeof<Todo> ] [] [] |> errors
        Assert.Equal<string list>(noRule, withCode ErrorCode.MISSING_ACCESS_RULE fs |> messagesOf)

    [<Fact>]
    let ``CAP-checks.R12 findings follow the order of the models Todo then TodoList`` () =
        let fs = run defaults [ typeof<Todo>; typeof<TodoList> ] [] [] |> errors
        Assert.Equal<string list>(List.rev noRule, withCode ErrorCode.MISSING_ACCESS_RULE fs |> messagesOf)

    [<Fact>]
    let ``CAP-checks.R12 findings of one model follow the order of its fields`` () =
        // The Done item comes first in the set, but Title comes first in the record.
        let items =
            [ Rule("Done", typeof<string>, (fun _ -> true), "x")
              Rule("Title", typeof<int>, (fun _ -> true), "x") ]
        let fs = runAppWith [ todoSet items ] |> errors |> withCode ErrorCode.WRONG_CONFIG_TYPE
        match fs with
        | [ first; second ] ->
            Assert.Contains("Todo.Title", first.Message)
            Assert.Contains("Todo.Done", second.Message)
        | other -> failwithf "Expected two findings, got %d" other.Length

    [<Fact>]
    let ``CAP-checks.R12 no models and no rules give no findings`` () =
        Assert.Empty(run defaults [] [] [])

module ``CAP-checks-R13`` =

    let private edgeRules = [ rule<Edge400> owned; rule<Edge401> owned ]

    let private runEdge (sets: ModelOverrides list) : Finding list =
        run edgeDefaults [ typeof<Edge400>; typeof<Edge401> ] edgeRules sets

    [<Fact>]
    let ``CAP-checks.R13 a sort by wide text is an error`` () =
        let sets = [ overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Notes @> Ascending ] ]
        Assert.Equal(
            "Cannot sort Todo by Notes: wide text is never indexed.",
            onlyMessage ErrorCode.WRONG_CONFIG_TYPE (runAppWith sets)
        )

    [<Fact>]
    let ``CAP-checks.R13 a sort by text of 401 characters is an error`` () =
        let sets = [ overrides<Edge401> [ listSort <@ fun (e: Edge401) -> e.Body @> Ascending ] ]
        Assert.Equal(
            "Cannot sort Edge401 by Body: wide text is never indexed.",
            onlyMessage ErrorCode.WRONG_CONFIG_TYPE (runEdge sets)
        )

    [<Fact>]
    let ``CAP-checks.R13 a sort by text of 400 characters has no such finding`` () =
        let sets = [ overrides<Edge400> [ listSort <@ fun (e: Edge400) -> e.Body @> Ascending ] ]
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE (runEdge sets))

    [<Fact>]
    let ``CAP-checks.R13 a sort by Title has no such finding`` () =
        let sets = [ overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Title @> Ascending ] ]
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE (runAppWith sets))

    [<Fact>]
    let ``CAP-checks.R13 a view on a computed length is an error`` () =
        // The record Title has no Length member, so the view reads the length of the text inside.
        let sets =
            [ overrides<Todo>
                  [ listView "Long" <@ fun (t: Todo) -> (match t.Title with Title s -> s.Length) > 3 @> ] ]
        let msg = onlyMessage ErrorCode.WRONG_CONFIG_TYPE (runAppWith sets)
        Assert.StartsWith("Unsupported list view for Todo:", msg)

    [<Fact>]
    let ``CAP-checks.R13 a view on an equality has no such finding`` () =
        let sets = [ overrides<Todo> [ listView "High" <@ fun (t: Todo) -> t.Priority = High @> ] ]
        Assert.Empty(withCode ErrorCode.WRONG_CONFIG_TYPE (runAppWith sets))
