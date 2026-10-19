namespace Typed.Engine.Tests

open System
open Xunit
open Typed.Engine.Model
open Typed.Engine.Access
open Typed.Engine.Tests.ModelsFixtures

/// Models and rules of the todo app. The checks use only these.
module AccessFixtures =

    type Audit = { Action: string }

    /// The model has no rule in any list below.
    type Unruled = { Name: string }

    /// A second parent model, to pin the reason rule.
    type Project = { Code: Title; Closed: bool }

    type Task = { Project: Ref<Project>; Title: Title }

    let u1 = Guid.Parse "11111111-1111-1111-1111-111111111111"
    let u2 = Guid.Parse "22222222-2222-2222-2222-222222222222"

    let todoOwnedVia = ownedVia <@ fun (t: Todo) -> t.List @>

    let readOnlyTodoOwnedVia =
        todoOwnedVia |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>

    /// The rules of the todo app, with the read-only condition on Todo.
    let rules: AccessRule list =
        [ rule<TodoList> owned
          rule<Todo> readOnlyTodoOwnedVia
          rule<User> self
          rule<Audit> system ]

    let scopeKind (r: Result<Scope, Finding>) : ScopeKind =
        match r with
        | Ok s -> s.Kind
        | Result.Error f -> failwithf "Expected a scope, got: %A" f

    let scopeError (r: Result<Scope, Finding>) : Finding =
        match r with
        | Ok s -> failwithf "Expected an error, got a scope: %A" s.Kind
        | Result.Error f -> f

open AccessFixtures

module ``CAP-access-R1`` =

    [<Fact>]
    let ``CAP-access.R1 owned gives model TodoList and access Owned`` () =
        let r = rule<TodoList> owned
        Assert.Equal("TodoList", r.ModelName)
        Assert.Equal(Owned, r.Access)

    [<Fact>]
    let ``CAP-access.R1 ownedVia gives reference List and parent TodoList`` () =
        let r = rule<Todo> todoOwnedVia
        Assert.Equal("Todo", r.ModelName)
        Assert.Equal(OwnedVia("List", "TodoList", None), r.Access)

    [<Fact>]
    let ``CAP-access.R1 self gives access Self`` () =
        let r = rule<User> self
        Assert.Equal("User", r.ModelName)
        Assert.Equal(Self, r.Access)

    [<Fact>]
    let ``CAP-access.R1 system gives access SystemOnly`` () =
        let r = rule<Audit> system
        Assert.Equal("Audit", r.ModelName)
        Assert.Equal(SystemOnly, r.Access)

module ``CAP-access-R2`` =

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent names TodoList and Archived`` () =
        let r = rule<Todo> readOnlyTodoOwnedVia
        Assert.Equal(OwnedVia("List", "TodoList", Some("TodoList", "Archived")), r.Access)

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent on owned fails with its message`` () =
        let ex =
            Assert.Throws<ArgumentException>(fun () ->
                owned |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent on self fails with its message`` () =
        let ex =
            Assert.Throws<ArgumentException>(fun () ->
                self |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent on system fails with its message`` () =
        let ex =
            Assert.Throws<ArgumentException>(fun () ->
                system |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)

module ``CAP-access-R3`` =

    [<Fact>]
    let ``CAP-access.R3 TodoList scope is the owner filter for u1`` () =
        Assert.Equal(OwnerFilter u1, scopeKind (scopeFor rules u1 "TodoList"))

    [<Fact>]
    let ``CAP-access.R3 the owner filter follows the user`` () =
        Assert.Equal(OwnerFilter u2, scopeKind (scopeFor rules u2 "TodoList"))

    [<Fact>]
    let ``CAP-access.R3 User scope is the self scope for u1`` () =
        Assert.Equal(SelfFilter u1, scopeKind (scopeFor rules u1 "User"))

    [<Fact>]
    let ``CAP-access.R3 Todo with ownedVia scope is the owner filter for u1`` () =
        Assert.Equal(OwnerFilter u1, scopeKind (scopeFor rules u1 "Todo"))

    [<Fact>]
    let ``CAP-access.R3 Audit with system gives NOT_FOUND`` () =
        let f = scopeError (scopeFor rules u1 "Audit")
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)

    [<Fact>]
    let ``CAP-access.R3 a model with no rule gives NOT_FOUND`` () =
        let f = scopeError (scopeFor rules u1 "Unruled")
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)

    [<Fact>]
    let ``CAP-access.R3 the engine scope is the engine-only scope`` () =
        Assert.Equal(Engine, engineScope.Kind)

module RecordFixtures =

    /// A TodoList record with the given owner and Archived value.
    let listRecord (owner: Guid) (name: string) (archived: bool) : Record =
        { Model = "TodoList"
          Id = Guid.NewGuid()
          Owner = Some owner
          Version = 1L
          Values = Map [ "Name", box name; "Archived", box archived ] }

    let okUnit (r: Result<unit, Finding>) : unit =
        match r with
        | Ok () -> ()
        | Result.Error f -> failwithf "Expected Ok, got: %A" f

    let errorOf (r: Result<'a, Finding>) : Finding =
        match r with
        | Ok v -> failwithf "Expected an error, got Ok: %A" v
        | Result.Error f -> f

open RecordFixtures

module ``CAP-access-R4`` =

    [<Fact>]
    let ``CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD`` () =
        let parent = listRecord u1 "Old" true
        let f = errorOf (checkChange rules Change "Todo" (Some parent))
        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
        Assert.Equal("This list is archived", f.Message)

    [<Fact>]
    let ``CAP-access.R4 change to a todo of a live list passes`` () =
        let parent = listRecord u1 "Groceries" false
        okUnit (checkChange rules Change "Todo" (Some parent))

    [<Fact>]
    let ``CAP-access.R4 delete of an archived list itself passes`` () =
        okUnit (checkChange rules Delete "TodoList" None)

    [<Fact>]
    let ``CAP-access.R4 change to a todo whose list does not exist gives NOT_FOUND`` () =
        let f = errorOf (checkChange rules Change "Todo" None)
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)

    [<Fact>]
    let ``CAP-access.R4 creation of a todo in an archived list gives READ_ONLY_RECORD`` () =
        let parent = listRecord u1 "Old" true
        let f = errorOf (checkChange rules Create "Todo" (Some parent))
        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
        Assert.Equal("This list is archived", f.Message)

    [<Fact>]
    let ``CAP-access.R4 creation of a todo in a live list passes`` () =
        let parent = listRecord u1 "Groceries" false
        okUnit (checkChange rules Create "Todo" (Some parent))

    [<Fact>]
    let ``CAP-access.R4 the reason comes from the parent model and field`` () =
        let projectRules =
            [ rule<Task> (
                  ownedVia <@ fun (t: Task) -> t.Project @>
                  |> readOnlyWhenParent <@ fun (p: Project) -> p.Closed @>
              ) ]

        let parent: Record =
            { Model = "Project"
              Id = Guid.NewGuid()
              Owner = Some u1
              Version = 1L
              Values = Map [ "Closed", box true ] }

        let f = errorOf (checkChange projectRules Change "Task" (Some parent))
        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
        Assert.Equal("This project is closed", f.Message)

module ``CAP-access-R5`` =

    [<Fact>]
    let ``CAP-access.R5 todo in a list owned by u1 gets owner u1`` () =
        let parent = listRecord u1 "Groceries" false
        match ownerFor rules u1 "Todo" (Some parent) with
        | Ok owner -> Assert.Equal(u1, owner)
        | Result.Error f -> failwithf "Expected an owner, got: %A" f

    [<Fact>]
    let ``CAP-access.R5 the owner follows the parent and user`` () =
        let parent = listRecord u2 "Groceries" false
        match ownerFor rules u2 "Todo" (Some parent) with
        | Ok owner -> Assert.Equal(u2, owner)
        | Result.Error f -> failwithf "Expected an owner, got: %A" f

    [<Fact>]
    let ``CAP-access.R5 todo in a list owned by u2 for user u1 gives NOT_FOUND`` () =
        let parent = listRecord u2 "Groceries" false
        let f = errorOf (ownerFor rules u1 "Todo" (Some parent))
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
