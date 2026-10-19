module Todo.App.Hooks

open Typed.Engine.Model
open Todo.App.Model

/// Before save: set CompletedAt when Done turns true, clear it when Done turns false.
let completionTime (ctx: HookContext) (previous: Todo option) (saved: Todo) : Todo =
    let wasDone =
        match previous with
        | Some p -> p.Done
        | None -> false

    match wasDone, saved.Done with
    | false, true -> { saved with CompletedAt = Some ctx.Now }
    | true, false -> { saved with CompletedAt = None }
    | _ -> saved

/// Computed value: the todo is open and due before today in the user's time zone.
let isOverdue (ctx: HookContext) (todo: Todo) : bool =
    match todo.Due with
    | Some due -> not todo.Done && due < localToday ctx
    | None -> false
