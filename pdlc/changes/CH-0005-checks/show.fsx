// Shows one acceptance line of CAP-checks in the terminal. Run from the project root:
//   dotnet fsi --quiet pdlc/changes/CH-0005-checks/show.fsx R1.2
#r "../../../src/Typed.Engine/bin/Debug/net8.0/Typed.Engine.dll"

open System
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Checks

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
type Child = { Parent: Ref<NoTitle> }

let def (clr: Type) kind : TypeDefault = { Clr = clr; Kind = kind; Check = (fun _ -> []); Hidden = false; Unique = false }
let defaults : TypeDefault list =
    [ def typeof<Title> (Text 200)
      def typeof<Notes> (Text 5000)
      def typeof<TimeZoneId> (Text 64)
      { def typeof<ExternalId> (Text 255) with Hidden = true; Unique = true } ]

let readOnlyTodoOwnedVia =
    ownedVia <@ fun (t: Todo) -> t.List @> |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>
let appTypes : Type list = [ typeof<User>; typeof<TodoList>; typeof<Todo> ]
let appRules : AccessRule list = [ rule<User> self; rule<TodoList> owned; rule<Todo> readOnlyTodoOwnedVia ]
let appSets : ModelOverrides list =
    [ overrides<Todo>
        [ readOnly <@ fun (t: Todo) -> t.CompletedAt @>
          listEdit <@ fun (t: Todo) -> t.Done @>
          listSort <@ fun (t: Todo) -> t.Due @> Ascending
          listColumns [ col <@ fun (t: Todo) -> t.Title @>; col <@ fun (t: Todo) -> t.Due @>
                        col <@ fun (t: Todo) -> t.Priority @>; col <@ fun (t: Todo) -> t.Done @> ] ]
      overrides<TodoList>
        [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
          listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ] ]
let todoSet (items: OverrideItem list) : ModelOverrides = { ModelName = "Todo"; Items = items }

// ---- print helpers ----
let cut (n: int) (s: string) = if s.Length > n then s.Substring(0, n - 3) + "..." else s
let say (s: string) = printfn "%s" (cut 100 s)
/// Splits the text in lines of at most n characters, at spaces.
let wrap (n: int) (text: string) : string list =
    let words = text.Split([| ' '; '\n' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList
    let rec go (cur: string) ws acc =
        match ws with
        | [] -> List.rev (if cur = "" then acc else cur :: acc)
        | w :: rest when cur = "" -> go w rest acc
        | w :: rest when cur.Length + 1 + w.Length <= n -> go (cur + " " + w) rest acc
        | w :: rest -> go w rest (cur :: acc)
    go "" words []
let sev (f: Finding) = match f.Severity with Severity.Error -> "error" | _ -> "warning"
/// Prints the call, then each finding as severity, code, message.
let show (call: string) (fs: Finding list) =
    say ("> " + call)
    if fs.IsEmpty then say "findings: none"
    else
        say (sprintf "findings: %d" fs.Length)
        for f in fs do
            say (sprintf "  %s %A" (sev f) f.Code)
            for l in wrap 88 f.Message do say ("    " + l)
/// Prints how many findings hold the code.
let count (code: ErrorCode) (fs: Finding list) =
    say (sprintf "findings with %A: %d" code (fs |> List.filter (fun f -> f.Code = code) |> List.length))

let appCall = "Checks.run defaults [User; TodoList; Todo] appRules"

let key = fsi.CommandLineArgs |> Array.last
printfn "$ show %s" key
match key with
| "R1.1" ->
    let fs = run defaults [ typeof<TodoList> ] [] []
    show "Checks.run defaults [TodoList] [] []" fs
| "R1.2" ->
    let fs = run defaults [ typeof<TodoList> ] [ rule<TodoList> owned; rule<TodoList> owned ] []
    show "Checks.run defaults [TodoList] [owned; owned] []" fs
| "R1.3" ->
    let fs = run defaults appTypes appRules []
    show (appCall + " []") fs
    count ErrorCode.MISSING_ACCESS_RULE fs
| "R2.1" ->
    let ghost = { ModelName = "Ghost"; Items = [ Label("Name", "Called") ] }
    show (appCall + " [Ghost: Label(\"Name\")]") (run defaults appTypes appRules [ ghost ])
| "R2.2" ->
    show (appCall + " [Todo: Label(\"Dew\")]") (run defaults appTypes appRules [ todoSet [ Label("Dew", "Dewy") ] ])
| "R3.1" ->
    let item = Rule("Due", typeof<string>, (fun _ -> true), "Not valid")
    show (appCall + " [Todo: Rule(Due, string)]") (run defaults appTypes appRules [ todoSet [ item ] ])
| "R3.2" ->
    let item = Rule("Title", typeof<Title>, (fun _ -> true), "Not valid")
    let fs = run defaults appTypes appRules [ todoSet [ item ] ]
    show (appCall + " [Todo: Rule(Title, Title)]") fs
    count ErrorCode.WRONG_CONFIG_TYPE fs
| "R4.1" ->
    show (appCall + " [Todo: Column(\"Version\")]") (run defaults appTypes appRules [ todoSet [ Column("Version", "Rev") ] ])
| "R5.1" ->
    show "Checks.run defaults [Todo] [Todo rule] []" (run defaults [ typeof<Todo> ] [ rule<Todo> readOnlyTodoOwnedVia ] [])
| "R6.1" ->
    let rules = [ rule<NoTitle> owned; rule<Child> (ownedVia <@ fun (c: Child) -> c.Parent @>) ]
    show "Checks.run defaults [NoTitle; Child] rules []" (run defaults [ typeof<NoTitle>; typeof<Child> ] rules [])
| "R6.2" ->
    let rules = [ rule<NoTitle> owned; rule<Child> (ownedVia <@ fun (c: Child) -> c.Parent @>) ]
    let sets = [ overrides<NoTitle> [ titleField <@ fun (n: NoTitle) -> n.Count @> ] ]
    let fs = run defaults [ typeof<NoTitle>; typeof<Child> ] rules sets
    show "Checks.run defaults [NoTitle; Child] rules [NoTitle: titleField Count]" fs
    count ErrorCode.NO_TITLE_FIELD fs
| "R7.1" ->
    let bad = [ rule<User> self; rule<TodoList> owned; { ModelName = "Todo"; Access = OwnedVia("Title", "TodoList", None) } ]
    show "Checks.run defaults appTypes [Todo: ownedVia Title] []" (run defaults appTypes bad [])
| "R7.2" ->
    let bad = [ rule<User> self; rule<TodoList> owned; { ModelName = "Todo"; Access = OwnedVia("List", "TodoList", Some("TodoList", "Name")) } ]
    show "Checks.run defaults appTypes [Todo: readOnlyWhenParent Name] []" (run defaults appTypes bad [])
| "R8.1" ->
    let fs = run defaults appTypes appRules [ overrides<TodoList> [ unsafe "rawName" "uses raw SQL" ] ]
    show (appCall + " [TodoList: unsafe \"rawName\" \"uses raw SQL\"]") fs
    say (sprintf "errors: %d, warnings: %d" (errors fs).Length (warnings fs).Length)
| "R9.1" ->
    let fs = run defaults appTypes appRules [ overrides<Todo> [ label <@ fun (t: Todo) -> t.Due @> "Due" ] ]
    show (appCall + " [Todo: label Due \"Due\"]") fs
| "R9.2" ->
    let fs = run defaults appTypes appRules [ overrides<Todo> [ label <@ fun (t: Todo) -> t.Due @> "Due date" ] ]
    show (appCall + " [Todo: label Due \"Due date\"]") fs
    count ErrorCode.REDUNDANT_OVERRIDE fs
| "R10.1" ->
    let fs = run defaults appTypes appRules appSets
    show "Checks.run defaults appTypes appRules appSets" fs
    say (sprintf "errors: %d" (errors fs).Length)
| "R11.1" ->
    let sets = [ todoSet [ Label("Due", "Due date"); Label("Due", "Deadline") ] ]
    show (appCall + " [Todo: Label Due; Label Due]") (run defaults appTypes appRules sets)
| "R11.2" ->
    let sets = [ todoSet [ Label("Due", "Due date"); ReadOnly "Due" ] ]
    let fs = run defaults appTypes appRules sets
    show (appCall + " [Todo: Label Due; ReadOnly Due]") fs
    count ErrorCode.WRONG_CONFIG_TYPE fs
| "R12.1" ->
    show "Checks.run defaults [TodoList; Todo] [] []" (run defaults [ typeof<TodoList>; typeof<Todo> ] [] [] |> errors)
| "R12.2" ->
    show "Checks.run defaults [] [] []" (run defaults [] [] [])
| "R13.1" ->
    let sets = [ overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Notes @> Ascending ] ]
    show (appCall + " [Todo: listSort Notes]") (run defaults appTypes appRules sets)
| "R13.2" ->
    let sets = [ overrides<Todo> [ listView "Long" <@ fun (t: Todo) -> (match t.Title with Title s -> s.Length) > 3 @> ] ]
    show (appCall + " [Todo: listView \"Long\" Title.Length > 3]") (run defaults appTypes appRules sets)
| "R13.3" ->
    let sets = [ overrides<Todo> [ listView "High" <@ fun (t: Todo) -> t.Priority = High @> ] ]
    let fs = run defaults appTypes appRules sets
    show (appCall + " [Todo: listView \"High\" Priority = High]") fs
    count ErrorCode.WRONG_CONFIG_TYPE fs
| other -> printfn "unknown key %s" other
