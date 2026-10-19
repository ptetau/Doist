# Handoff · CH-0006 (todo app)

## Built

The project `src/Todo.App/Todo.App.fsproj` holds `Model.fs`, `Defaults.fs`, `Schema.fs`, and `Hooks.fs`. The hooks are `completionTime` and `isOverdue`.

## How to run

`dotnet test tests/Typed.Engine.Tests --filter "DisplayName~CAP-todo-app"` runs the app checks. The whole run has 206 checks.

## Notes

- `completionTime` treats a missing `previous` as not done. The spec is silent on this case.
