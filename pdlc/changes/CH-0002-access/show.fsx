// Shows one acceptance line of CAP-access in the terminal. Run from the project root:
//   dotnet fsi --quiet pdlc/changes/CH-0002-access/show.fsx R1.1
#r "../../../src/Typed.Engine/bin/Debug/net8.0/Typed.Engine.dll"

open System
open Typed.Engine.Model
open Typed.Engine.Access

[<TitleType>]
type Title = Title of string
type ExternalId = ExternalId of string
type TimeZoneId = TimeZoneId of string
type User = { Subject: ExternalId; DisplayName: Title; TimeZone: TimeZoneId }
type TodoList = { Name: Title; Archived: bool }
type Todo = { List: Ref<TodoList>; Title: Title; Done: bool }
type Audit = { Note: string }

let rules =
    [ rule<User> self
      rule<TodoList> owned
      rule<Todo> (ownedVia <@ fun (t: Todo) -> t.List @> |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>)
      rule<Audit> system ]

let u1 = Guid.Parse "11111111-1111-1111-1111-111111111111"
let u2 = Guid.Parse "22222222-2222-2222-2222-222222222222"
let list (owner: Guid) (archived: bool) : Record =
    { Model = "TodoList"; Id = Guid.NewGuid(); Owner = Some owner; Version = 1L
      Values = Map [ "Name", box (if archived then "Old" else "Groceries"); "Archived", box archived ] }

let scope r = match r with Ok (s: Scope) -> sprintf "Ok %A" s.Kind | Result.Error (f: Finding) -> sprintf "Error %A: %s" f.Code f.Message
let show r = match r with Ok v -> sprintf "Ok %A" v | Result.Error (f: Finding) -> sprintf "Error %A: %s" f.Code f.Message

let key = fsi.CommandLineArgs |> Array.last
printfn "$ show %s" key
match key with
| "R1.1" -> let r = rule<TodoList> owned in printfn "rule<TodoList> owned -> %s, %A" r.ModelName r.Access
| "R1.2" -> printfn "rule<Todo> (ownedVia List) -> %A" (rule<Todo> (ownedVia <@ fun (t: Todo) -> t.List @>)).Access
| "R1.3" -> printfn "rule<User> self -> %A" (rule<User> self).Access
| "R1.4" -> printfn "rule<Audit> system -> %A" (rule<Audit> system).Access
| "R2.1" -> printfn "ownedVia List |> readOnlyWhenParent Archived -> %A" (rules.[2].Access)
| "R2.2" -> (try owned |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore; printfn "no error" with e -> printfn "owned |> readOnlyWhenParent -> fails: %s" e.Message)
| "R3.1" -> printfn "scopeFor u1 TodoList -> %s" (scope (scopeFor rules u1 "TodoList"))
| "R3.2" -> printfn "scopeFor u1 User (self) -> %s" (scope (scopeFor rules u1 "User"))
| "R3.3" -> printfn "scopeFor u1 Todo (ownedVia) -> %s" (scope (scopeFor rules u1 "Todo"))
| "R3.4" -> printfn "scopeFor u1 Audit (system) -> %s" (scope (scopeFor rules u1 "Audit"))
| "R3.5" -> printfn "engineScope.Kind -> %A" engineScope.Kind
| "R4.1" -> printfn "checkChange Change Todo (list Old, Archived=true) -> %s" (show (checkChange rules Change "Todo" (Some (list u1 true))))
| "R4.2" -> printfn "checkChange Change Todo (list Groceries, Archived=false) -> %s" (show (checkChange rules Change "Todo" (Some (list u1 false))))
| "R4.3" -> printfn "checkChange Delete TodoList (the list itself) -> %s" (show (checkChange rules Delete "TodoList" None))
| "R4.4" -> printfn "checkChange Change Todo (no list) -> %s" (show (checkChange rules Change "Todo" None))
| "R4.5" -> printfn "checkChange Create Todo (list Old, Archived=true) -> %s" (show (checkChange rules Create "Todo" (Some (list u1 true))))
| "R5.1" -> printfn "ownerFor u1 Todo (list owned by u1) -> %s" (show (ownerFor rules u1 "Todo" (Some (list u1 false))))
| "R5.2" -> printfn "ownerFor u1 Todo (list owned by u2) -> %s" (show (ownerFor rules u1 "Todo" (Some (list u2 false))))
| other -> printfn "unknown key %s" other
