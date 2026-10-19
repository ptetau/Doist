module Typed.Engine.Generator

open System
open System.IO
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access

type Config =
    { Defaults: TypeDefault list
      Models: Type list
      Rules: AccessRule list
      Overrides: ModelOverrides list
      OutDir: string }

let private usage = "usage: generate [--out <folder>]"

let private line (f: Finding) : string =
    let severity =
        match f.Severity with
        | Severity.Error -> "error"
        | Severity.Warning -> "warning"
    sprintf "%s %s %s" severity (string f.Code) f.Message

/// The models with their override items and access, for the schema and the shape.
let private parts (config: Config) : (Model * OverrideItem list * Access) list =
    config.Models
    |> List.choose (fun t ->
        match describe config.Defaults t with
        | Ok model -> Some model
        | Result.Error _ -> None)
    |> List.map (fun model ->
        let items =
            config.Overrides
            |> List.filter (fun s -> s.ModelName = model.Name)
            |> List.collect (fun s -> s.Items)
        let rule = config.Rules |> List.find (fun r -> r.ModelName = model.Name)
        model, items, rule.Access)

/// Runs the checks, writes one line for each finding, and writes the files when no finding is an error.
/// Gives the exit code.
let run (config: Config) (stdout: TextWriter) : int =
    let findings = Checks.run config.Defaults config.Models config.Rules config.Overrides
    for f in findings do
        stdout.WriteLine(line f)
    if Checks.errors findings <> [] then
        1
    else
        let models = parts config
        Directory.CreateDirectory config.OutDir |> ignore
        File.WriteAllText(Path.Combine(config.OutDir, "schema.sql"), Persist.schema Persist.SqlServer models)
        Persist.writeShape (Path.Combine(config.OutDir, "shape.json")) (Persist.shapeOf models)
        0

let todoConfig (outDir: string) : Config =
    { Defaults = Todo.App.Defaults.typeDefaults
      Models = [ typeof<Todo.App.Model.User>; typeof<Todo.App.Model.TodoList>; typeof<Todo.App.Model.Todo> ]
      Rules = Todo.App.Schema.access
      Overrides = Todo.App.Schema.overrideSets
      OutDir = outDir }

/// The folder after the --out flag, if the arguments hold it.
let private outFolder (args: string[]) : string option =
    match Array.tryFindIndex ((=) "--out") args with
    | Some i when i + 1 < args.Length -> Some args.[i + 1]
    | _ -> None

/// Parses the arguments and runs the command. Gives the exit code.
let execute (args: string[]) (config: Config) (stdout: TextWriter) (stderr: TextWriter) : int =
    match Array.toList args with
    | "generate" :: rest when rest.IsEmpty || (outFolder args).IsSome ->
        let folder = defaultArg (outFolder args) config.OutDir
        run { config with OutDir = folder } stdout
    | _ ->
        stdout.WriteLine usage
        2
