module Todo.App.Defaults

open System
open Typed.Engine.Model
open Todo.App.Model

let private oneLine (s: string) : string list =
    if s.IndexOfAny [| '\r'; '\n' |] >= 0 then
        [ "must be on one line" ]
    else
        []

let private knownZone (s: string) : string list =
    if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then
        []
    else
        [ "must be a known time zone, such as Pacific/Auckland" ]

let typeDefaults: TypeDefault list =
    [ { Clr = typeof<Title>
        Kind = Text 200
        Check = oneLine
        Hidden = false
        Unique = false }
      { Clr = typeof<Notes>
        Kind = Text 5000
        Check = (fun _ -> [])
        Hidden = false
        Unique = false }
      { Clr = typeof<TimeZoneId>
        Kind = Text 64
        Check = knownZone
        Hidden = false
        Unique = false }
      { Clr = typeof<ExternalId>
        Kind = Text 255
        Check = (fun _ -> [])
        Hidden = true
        Unique = true } ]

let models: Type list = [ typeof<User>; typeof<TodoList>; typeof<Todo> ]
