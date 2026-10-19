module Program

open System
open Typed.Engine.Generator

[<EntryPoint>]
let main argv =
    let out =
        match Array.tryFindIndex ((=) "--out") argv with
        | Some i when i + 1 < argv.Length -> argv.[i + 1]
        | _ -> "build"
    execute argv (todoConfig out) Console.Out Console.Error
