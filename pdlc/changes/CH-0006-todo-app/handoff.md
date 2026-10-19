# Handoff · CH-0006 (todo app)

## Checks

`tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs` holds the checks for CAP-todo-app.R1 to R6. The lock is in `change.md`.

## What to build

- The project `src/Todo.App/Todo.App.fsproj`, which references `Typed.Engine`. The test project already references it.
- Four files in this order: `Model.fs`, `Defaults.fs`, `Schema.fs`, `Hooks.fs`. No other `.fs` file.
- The record shapes are in `pdlc/changes/CH-0003-validate/show.fsx` and in the spec.

## Notes

- `completionTime` has no check for `previous = None`. The spec is silent.
- The R6 check lists the `.fs` files on disk under `src/Todo.App`.
