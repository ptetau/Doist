// Shows one acceptance line of CAP-validate in the terminal. Run from the project root:
//   dotnet fsi --quiet pdlc/changes/CH-0003-validate/show.fsx R2.5
#r "../../../src/Typed.Engine/bin/Debug/net8.0/Typed.Engine.dll"

open System
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Validate

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
type NoTitle = { Count: int }

let oneLine (s: string) = if s.IndexOfAny([| '\r'; '\n' |]) >= 0 then [ "must be on one line" ] else []
let knownZone (s: string) =
    if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then []
    else [ "must be a known time zone, such as Pacific/Auckland" ]
let defaults : TypeDefault list =
    [ { Clr = typeof<Title>; Kind = Text 200; Check = oneLine; Hidden = false; Unique = false }
      { Clr = typeof<Notes>; Kind = Text 5000; Check = (fun _ -> []); Hidden = false; Unique = false }
      { Clr = typeof<TimeZoneId>; Kind = Text 64; Check = knownZone; Hidden = false; Unique = false }
      { Clr = typeof<ExternalId>; Kind = Text 255; Check = (fun _ -> []); Hidden = true; Unique = true } ]

let plan (t: Type) = match describe defaults t with Ok m -> m | Result.Error fs -> failwithf "%A" fs
let field (t: Type) name = (plan t).Fields |> List.find (fun f -> f.Name = name)
let list name : TodoList = { Name = Title name; Archived = false }
let todo : Todo =
    { List = Ref(Guid.NewGuid()); Title = Title "Pay rent"; Notes = None; Priority = Normal
      Due = None; Done = false; CompletedAt = None }
let user zone : User = { Subject = ExternalId "s1"; DisplayName = Title "Pat"; TimeZone = TimeZoneId zone }
let v (t: Type) items (r: obj) = sprintf "%A" (validate (plan t) items r)
let p (f: Field) (raw: string option) =
    match parseField f raw with
    | Ok o -> sprintf "Ok %A" o
    | Result.Error e -> sprintf "Error on %s: %s" e.Field e.Message
let chars n = String('a', n)

let key = fsi.CommandLineArgs |> Array.last
printfn "$ show %s" key
match key with
| "R1.1" -> printfn "validate TodoList {Name=\"Groceries\"} -> %s" (v typeof<TodoList> [] (list "Groceries"))
| "R1.2" -> printfn "validate Todo {Title=\"   \"; Notes=5001 chars} -> %s" (v typeof<Todo> [] { todo with Title = Title "   "; Notes = Some(Notes(String('n', 5001))) })
| "R2.1" -> printfn "Name \"  Groceries  \" -> %s" (v typeof<TodoList> [] (list "  Groceries  "))
| "R2.2" -> printfn "Name \"   \" -> %s" (v typeof<TodoList> [] (list "   "))
| "R2.3" -> printfn "Name of 200 chars -> %s" (v typeof<TodoList> [] (list (chars 200)))
| "R2.4" -> printfn "Name of 201 chars -> %s" (v typeof<TodoList> [] (list (chars 201)))
| "R2.5" -> printfn "Name \"Shop\\nlater\" -> %s" (v typeof<TodoList> [] (list "Shop\nlater"))
| "R2.6" -> printfn "Name \"Shop\\tlater\" -> %s" (v typeof<TodoList> [] (list "Shop\tlater"))
| "R2.7" -> printfn "Name of 200 chars with 3 spaces at each end -> %s" (v typeof<TodoList> [] (list ("   " + chars 200 + "   ")))
| "R2.8" -> printfn "Name \"<script>alert(1)</script>\" -> %s" (v typeof<TodoList> [] (list "<script>alert(1)</script>"))
| "R3.1" -> printfn "Notes = None -> %s" (v typeof<Todo> [] todo)
| "R3.2" -> printfn "Notes of 5000 chars -> %s" (v typeof<Todo> [] { todo with Notes = Some(Notes(String('n', 5000))) })
| "R3.3" -> printfn "Notes of 5001 chars -> %s" (v typeof<Todo> [] { todo with Notes = Some(Notes(String('n', 5001))) })
| "R3.4" -> printfn "Notes of 5000 chars plus one space -> %s" (v typeof<Todo> [] { todo with Notes = Some(Notes(String('n', 5000) + " ")) })
| "R4.1" -> printfn "TimeZone \"Pacific/Auckland\" -> %s" (v typeof<User> [] (user "Pacific/Auckland"))
| "R4.2" -> printfn "TimeZone \"Mars/Base\" -> %s" (v typeof<User> [] (user "Mars/Base"))
| "R4.3" -> printfn "TimeZone \"pacific/auckland\" -> %s" (v typeof<User> [] (user "pacific/auckland"))
| "R5.1" -> printfn "List = Ref Guid.Empty -> %s" (v typeof<Todo> [] { todo with List = Ref Guid.Empty })
| "R5.2" -> printfn "List = Ref <id> -> %s" (v typeof<Todo> [] todo)
| "R6.1" -> let items = (overrides<Todo> [ rule <@ fun (t: Todo) -> t.Title @> (fun (Title s) -> s <> "forbidden") "Title is reserved" ]).Items in printfn "rule on Title, title \"forbidden\" -> %s" (v typeof<Todo> items { todo with Title = Title "forbidden" })
| "R6.2" -> let items = (overrides<Todo> [ rule <@ fun (t: Todo) -> t.Title @> (fun (Title s) -> s <> "forbidden") "Title is reserved" ]).Items in printfn "rule on Title, title \"Pay rent\" -> %s" (v typeof<Todo> items todo)
| "R7.1" -> printfn "parseField Priority \"Urgent\" -> %s" (p (field typeof<Todo> "Priority") (Some "Urgent"))
| "R7.2" -> printfn "parseField Priority \"High\" -> %s" (p (field typeof<Todo> "Priority") (Some "High"))
| "R8.1" -> printfn "parseField Count \"abc\" -> %s" (p (field typeof<NoTitle> "Count") (Some "abc"))
| "R8.2" -> printfn "parseField Due \"2026-02-30\" -> %s" (p (field typeof<Todo> "Due") (Some "2026-02-30"))
| "R8.3" -> printfn "parseField Due \"2026-10-18\" -> %s" (p (field typeof<Todo> "Due") (Some "2026-10-18"))
| "R8.4" -> printfn "parseField Due \"\" (optional) -> %s" (p (field typeof<Todo> "Due") (Some ""))
| "R8.5" -> printfn "parseField Title \"\" (required) -> %s" (p (field typeof<Todo> "Title") (Some ""))
| "R8.6" -> let d = field typeof<Todo> "Done" in printfn "parseField Done \"on\" -> %s, \"true\" -> %s, \"false\" -> %s, missing -> %s" (p d (Some "on")) (p d (Some "true")) (p d (Some "false")) (p d None)
| "R8.7" -> printfn "parseField Done \"yes\" -> %s" (p (field typeof<Todo> "Done") (Some "yes"))
| other -> printfn "unknown key %s" other
