// Shows one acceptance line of CAP-todo-app in the terminal. Run from the project root:
//   dotnet fsi --quiet pdlc/changes/CH-0006-todo-app/show.fsx R4.1
#r "../../../src/Typed.Engine/bin/Debug/net8.0/Typed.Engine.dll"
#r "../../../src/Todo.App/bin/Debug/net8.0/Todo.App.dll"

open System
open System.IO
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Checks
open Todo.App.Model

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
/// Prints the value on one line, or on wrapped lines.
let flat (x: obj) = (sprintf "%A" x).Replace("\r", " ").Replace("\n", " ")
let sayWrapped (prefix: string) (text: string) =
    for l in wrap 92 text do say (prefix + l)
let sev (f: Finding) = match f.Severity with Severity.Error -> "error" | _ -> "warning"

// ---- fixtures ----
let ctxAt (zone: string) (now: string) : HookContext =
    { User = Guid.Empty
      TimeZone = TimeZoneInfo.FindSystemTimeZoneById zone
      Now = DateTimeOffset.Parse(now) }
let todo (done': bool) (completed: string option) (due: string option) : Todo =
    { List = Ref Guid.Empty
      Title = Title "Buy milk"
      Notes = None
      Priority = Normal
      Due = due |> Option.map DateOnly.Parse
      Done = done'
      CompletedAt = completed |> Option.map DateTimeOffset.Parse }
let at9 = "2026-10-18T09:00:00Z"
let showDone (t: Todo) =
    let c = match t.CompletedAt with Some c -> c.ToString("yyyy-MM-ddTHH:mm:ssZ") | None -> "empty"
    say (sprintf "  Done = %b, CompletedAt = %s" t.Done c)
/// Runs the completion hook and prints the todo before and after.
let hook (label: string) (now: string) (previous: Todo) (saved: Todo) =
    let ctx = ctxAt "UTC" now
    say (sprintf "> Hooks.completionTime ctx(%s) previous saved" now)
    say "previous:"
    showDone previous
    say (sprintf "saved with Done = %b:" saved.Done)
    let after = Todo.App.Hooks.completionTime ctx (Some previous) saved
    say "result:"
    showDone after
    say label
let nzCtx = ctxAt "Pacific/Auckland" "2026-10-18T14:00:00Z"
/// Runs the overdue hook and prints the call and the result.
let overdue (due: string option) (done': bool) =
    let t = todo done' None due
    let dueText = match due with Some d -> d | None -> "none"
    say "zone Pacific/Auckland, now 2026-10-18T14:00:00Z"
    say (sprintf "local date: %s" ((localToday nzCtx).ToString("yyyy-MM-dd")))
    say (sprintf "> Hooks.isOverdue ctx todo(Due = %s, Done = %b)" dueText done')
    say (sprintf "overdue: %b" (Todo.App.Hooks.isOverdue nzCtx t))

let key = fsi.CommandLineArgs |> Array.last
printfn "$ show %s" key
match key with
| "R1.1" ->
    say "> Defaults.models |> List.map (describe Defaults.typeDefaults)"
    for t in Todo.App.Defaults.models do
        match describe Todo.App.Defaults.typeDefaults t with
        | Ok m ->
            say (sprintf "%s (%d fields)" m.Name m.Fields.Length)
            for f in m.Fields do say (sprintf "  %s: %s" f.Name (flat f.Kind))
        | Result.Error fs -> say (sprintf "%s: %d findings" t.Name fs.Length)
| "R1.2" ->
    say "> Defaults.typeDefaults"
    for d in Todo.App.Defaults.typeDefaults do
        say (sprintf "%-10s %s, hidden = %b, unique = %b" d.Clr.Name (flat d.Kind) d.Hidden d.Unique)
| "R2.1" ->
    say "> Schema.access"
    for r in Todo.App.Schema.access do
        sayWrapped "" (sprintf "%s: %s" r.ModelName (flat r.Access))
| "R3.1" ->
    let m = Todo.App.Schema.todoDisplay
    say "> Schema.todoDisplay"
    say m.ModelName
    for i in m.Items do sayWrapped "  " (flat i)
| "R3.2" ->
    let m = Todo.App.Schema.todoListDisplay
    say "> Schema.todoListDisplay"
    say m.ModelName
    for i in m.Items do
        match i with
        | ListView(name, filter) ->
            say (sprintf "  view \"%s\"" name)
            sayWrapped "    filter " (flat filter)
        | other -> sayWrapped "  " (flat other)
| "R4.1" ->
    hook "CompletedAt is the time of the save" at9 (todo false None None) (todo true None None)
| "R4.2" ->
    hook "CompletedAt is empty" "2026-10-18T12:00:00Z" (todo true (Some at9) None) (todo false (Some at9) None)
| "R4.3" ->
    hook "CompletedAt is still 09:00" "2026-10-18T12:00:00Z" (todo true (Some at9) None) (todo true (Some at9) None)
| "R5.1" -> overdue (Some "2026-10-18") false
| "R5.2" -> overdue (Some "2026-10-18") true
| "R5.3" -> overdue (Some "2026-10-19") false
| "R5.4" -> overdue None false
| "R6.1" ->
    say "> ls src/Todo.App/*.fs"
    for f in Directory.GetFiles("src/Todo.App", "*.fs", SearchOption.TopDirectoryOnly) |> Array.map Path.GetFileName |> Array.sort do
        say f
| "R6.2" ->
    let fs = run Todo.App.Defaults.typeDefaults Todo.App.Defaults.models Todo.App.Schema.access Todo.App.Schema.overrideSets
    say "> Checks.run Defaults.typeDefaults Defaults.models Schema.access Schema.overrideSets"
    if fs.IsEmpty then say "findings: none"
    else
        say (sprintf "findings: %d" fs.Length)
        for f in fs do say (sprintf "  %s %A: %s" (sev f) f.Code f.Message)
    say (sprintf "errors: %d, warnings: %d" (errors fs).Length (warnings fs).Length)
| other -> printfn "unknown key %s" other
