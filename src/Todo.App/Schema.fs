module Todo.App.Schema

open Typed.Engine.Overrides
open Typed.Engine.Access
open Todo.App.Model

let access: AccessRule list =
    [ rule<User> self
      rule<TodoList> owned
      rule<Todo> (ownedVia <@ fun (t: Todo) -> t.List @> |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>) ]

let todoDisplay: ModelOverrides =
    overrides<Todo>
        [ readOnly <@ fun (t: Todo) -> t.CompletedAt @>
          listEdit <@ fun (t: Todo) -> t.Done @>
          listSort <@ fun (t: Todo) -> t.Due @> Ascending
          listColumns
              [ col <@ fun (t: Todo) -> t.Title @>
                col <@ fun (t: Todo) -> t.Due @>
                col <@ fun (t: Todo) -> t.Priority @>
                col <@ fun (t: Todo) -> t.Done @> ] ]

let todoListDisplay: ModelOverrides =
    overrides<TodoList>
        [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
          listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ]

let overrideSets: ModelOverrides list = [ todoDisplay; todoListDisplay ]
