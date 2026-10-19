// Shows one acceptance line of CAP-models in the terminal. Run from the project root:
//   dotnet fsi --quiet pdlc/changes/CH-0001-models/show.fsx R1.1
#r "../../../src/Typed.Engine/bin/Debug/net8.0/Typed.Engine.dll"

open System
open Microsoft.FSharp.Quotations
open Typed.Engine.Model
open Typed.Engine.Overrides

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
type Tagged = { Tags: Title list }
type TwoMissing = { Tags: Title list; Aliases: Notes list }

let def (clr: Type) kind : TypeDefault = { Clr = clr; Kind = kind; Check = (fun _ -> []); Hidden = false; Unique = false }
let defaults =
    [ def typeof<Title> (Text 200); def typeof<Notes> (Text 5000); def typeof<TimeZoneId> (Text 64)
      { def typeof<ExternalId> (Text 255) with Hidden = true; Unique = true } ]

let plan (t: Type) = match describe defaults t with Ok m -> m | Result.Error fs -> failwithf "%A" fs
let errors (t: Type) = match describe defaults t with Ok _ -> [] | Result.Error fs -> fs
let kindOf (t: Type) name = ((plan t).Fields |> List.find (fun f -> f.Name = name)).Kind
let title (t: Type) items = titleFieldName (plan t).Fields items

let key = fsi.CommandLineArgs |> Array.last
printfn "$ show %s" key
match key with
| "R1.1" -> printfn "describe Todo -> Title.Kind = %A" (kindOf typeof<Todo> "Title")
| "R1.2" -> printfn "describe Todo -> Notes.Kind = %A" (kindOf typeof<Todo> "Notes")
| "R1.3" -> printfn "describe Todo -> Priority.Kind = %A" (kindOf typeof<Todo> "Priority")
| "R1.4" -> printfn "describe Todo -> Due.Kind = %A" (kindOf typeof<Todo> "Due")
| "R1.5" -> printfn "describe Todo -> List.Kind = %A" (kindOf typeof<Todo> "List")
| "R1.6" -> printfn "describe Todo -> Done.Kind = %A, CompletedAt.Kind = %A" (kindOf typeof<Todo> "Done") (kindOf typeof<Todo> "CompletedAt")
| "R1.7" -> for f in errors typeof<Priority> do printfn "describe Priority -> %A: %s" f.Code f.Message
| "R2.1" -> for f in errors typeof<Tagged> do printfn "describe Tagged -> %A: %s" f.Code f.Message
| "R2.2" -> let fs = errors typeof<TwoMissing> in printfn "describe TwoMissing -> %d errors, fields %A" fs.Length (fs |> List.map (fun f -> f.Field))
| "R3.1" -> printfn "systemFields = %A" systemFields
| "R3.2" -> printfn "describe Todo -> fields = %A" ((plan typeof<Todo>).Fields |> List.map (fun f -> f.Name))
| "R4.1" -> printfn "labelOf \"CompletedAt\" = %A" (labelOf "CompletedAt")
| "R4.2" -> printfn "labelOf \"Due\" = %A" (labelOf "Due")
| "R4.3" -> printfn "labelOf \"TimeZone\" = %A" (labelOf "TimeZone")
| "R4.4" -> printfn "labelOf \"ExternalId\" = %A" (labelOf "ExternalId")
| "R4.5" -> printfn "labelOf \"HasDue\" = %A" (labelOf "HasDue")
| "R5.1" -> printfn "isWide Notes = %b" (isWide (kindOf typeof<Todo> "Notes"))
| "R5.2" -> printfn "isWide Title = %b" (isWide (kindOf typeof<Todo> "Title"))
| "R5.3" -> printfn "isWide (Text 400) = %b" (isWide (Text 400))
| "R5.4" -> printfn "isWide (Text 401) = %b" (isWide (Text 401))
| "R6.1" -> printfn "titleFieldName TodoList [] = %A" (title typeof<TodoList> [])
| "R6.2" -> printfn "titleFieldName User [] = %A" (title typeof<User> [])
| "R6.3" -> printfn "titleFieldName NoTitle [] = %A" (title typeof<NoTitle> [])
| "R6.4" -> printfn "titleFieldName NoTitle [titleField Count] = %A" (title typeof<NoTitle> (overrides<NoTitle> [ titleField <@ fun (n: NoTitle) -> n.Count @> ]).Items)
| "R7.1" -> printfn "fieldName <@ fun t -> t.Due @> = %A" (fieldName (<@ fun (t: Todo) -> t.Due @> :> Expr))
| "R7.2" -> let o = overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Due @> Ascending ] in printfn "overrides<Todo> [listSort Due Ascending] -> %s %A" o.ModelName o.Items
| "R7.3" -> let o = overrides<Todo> [ listColumns [ col <@ fun (t: Todo) -> t.Title @>; col <@ fun (t: Todo) -> t.Due @> ] ] in printfn "listColumns [Title; Due] -> %A" o.Items
| "R7.4" -> let o = overrides<TodoList> [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @> ] in printfn "listView \"Active\" (not Archived) -> fieldsOf = %A" (fieldsOf o.Items.Head)
| other -> printfn "unknown key %s" other
