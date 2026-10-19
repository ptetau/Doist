# CH-0006 · Todo app

Intent: IN-0001 · Scope: CAP-todo-app · Status: building · Ticket: none · Branch: pdlc/CH-0006-todo-app · Base: pdlc/CH-0005-checks
Tests-Locked: 9c3951347ac1bdee08445c497dbd1013a9aed668

## Delivers

The todo app exists as models, type defaults, access rules, overrides, and two hooks, and nothing else. It passes every build check.

## Requirements

### CAP-todo-app.R1 · The three models and five semantic types of the spec

- Given the todo app, when the engine describes its models, then the models are `User`,
  `TodoList`, and `Todo`, with the fields of the spec.
- Given the todo app. When the engine reads its type defaults, then `Title` is text 200,
  `Notes` is text 5000, `TimeZoneId` is text 64. Also `ExternalId` is text 255, unique,
  and hidden.

### CAP-todo-app.R2 · The three access rules of the spec

- Given the todo app. When a caller reads its access rules, then `User` is `self`,
  `TodoList` is `owned`, and `Todo` is `ownedVia List`, read-only when the list is
  `Archived`.

### CAP-todo-app.R3 · The overrides of the spec

- Given the todo app, when a caller reads the overrides of `Todo`, then they are
  `readOnly CompletedAt`, `listEdit Done`, `listSort Due Ascending`, and `listColumns
  [Title; Due; Priority; Done]`.
- Given the todo app. When a caller reads the overrides of `TodoList`, then they are the
  views "Active" (not `Archived`) and "Archived" (`Archived`), in that order.

### CAP-todo-app.R4 · Ticking a todo sets its completion time

- Given the time 2026-10-18T09:00:00Z and a todo with `Done = false`, when the hook runs
  on a save with `Done = true`, then `CompletedAt` is 2026-10-18T09:00:00Z.
- Given a todo with `Done = true` and `CompletedAt` = 2026-10-18T09:00:00Z, when the
  hook runs on a save with `Done = false`, then `CompletedAt` is empty.
- Given a todo with `Done = true` and `CompletedAt` = 2026-10-18T09:00:00Z, when the
  hook runs on a save with `Done = true` at 2026-10-18T12:00:00Z, then `CompletedAt` is
  still 2026-10-18T09:00:00Z.

### CAP-todo-app.R5 · A todo is overdue by the user's calendar

- Given the user's time zone "Pacific/Auckland" and the time 2026-10-18T14:00:00Z, when
  the hook reads a todo due 2026-10-18 with `Done = false`, then the todo is overdue.
- Given the same time zone and time, when the hook reads a todo due 2026-10-18 with
  `Done = true`, then the todo is not overdue.
- Given the same time zone and time, when the hook reads a todo due 2026-10-19 with
  `Done = false`, then the todo is not overdue.
- Given the same time zone and time, when the hook reads a todo with no due date, then
  the todo is not overdue.

### CAP-todo-app.R6 · The app has no other code

- Given the app project, when a caller lists its source files, then the files are
  `Model.fs`, `Defaults.fs`, `Schema.fs`, and `Hooks.fs`, and no other `.fs` file.
  (assumed)
- Given the app project, when the checks of the engine run over its models, rules, and
  overrides, then there are no error findings.

## Relies on

- CAP-models (used, not changed)
- CAP-access (used, not changed)
- CAP-checks (used, not changed)
- DES-names
- DES-errors
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

Project `src/Todo.App/Todo.App.fsproj` (references `Typed.Engine`):

- `src/Todo.App/Model.fs`, module `Todo.App.Model`: the types `Title`, `Notes`, `Priority`, `TimeZoneId`, `ExternalId`, and the records `User`, `TodoList`, `Todo`, exactly as the spec writes them.
- `src/Todo.App/Defaults.fs`, module `Todo.App.Defaults`: `typeDefaults: TypeDefault list` and `models: Type list` (`User`, `TodoList`, `Todo`, in that order).
- `src/Todo.App/Schema.fs`, module `Todo.App.Schema`: `access: AccessRule list`, `todoDisplay: ModelOverrides`, `todoListDisplay: ModelOverrides`, `overrideSets: ModelOverrides list`.
- `src/Todo.App/Hooks.fs`, module `Todo.App.Hooks`:
  - `completionTime: HookContext -> previous: Todo option -> Todo -> Todo` (before save)
  - `isOverdue: HookContext -> Todo -> bool` (computed value)
- The check for CAP-todo-app.R6 (no other code) lists the `.fs` files in `src/Todo.App/` on disk, and runs `Checks.run` over `Defaults.typeDefaults`, `Defaults.models`, `Schema.access`, and `Schema.overrideSets`.

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

- clock and time zone: `HookContext.Now` and `HookContext.TimeZone`. The checks build the context by hand.
- `HookContext` is in `Typed.Engine.Model`: `{ User: Guid; TimeZone: TimeZoneInfo; Now: DateTimeOffset }`. Build it with a record expression.
- `Ref<'T>` is the case `Ref of Guid`, in `Typed.Engine.Model`. Write `Ref (Guid.NewGuid())`.
- `Typed.Engine.Model.localToday ctx` gives the date of `ctx.Now` in `ctx.TimeZone`.

## Files

- src/Todo.App/Model.fs
- src/Todo.App/Defaults.fs
- src/Todo.App/Schema.fs
- src/Todo.App/Hooks.fs
- src/Todo.App/Todo.App.fsproj (assumed)

## Check files

- tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

## Steps

### Step 1 · The three models and five semantic types of the spec · [ ]

- Requirement: CAP-todo-app.R1
- Check: the checks named `CAP-todo-app.R1 ...` in `tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs`
- Done when: every check of CAP-todo-app.R1 (the three models and five semantic types of the spec) passes

### Step 2 · The three access rules of the spec · [ ]

- Requirement: CAP-todo-app.R2
- Check: the checks named `CAP-todo-app.R2 ...` in `tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs`
- Done when: every check of CAP-todo-app.R2 (the three access rules of the spec) passes

### Step 3 · The overrides of the spec · [ ]

- Requirement: CAP-todo-app.R3
- Check: the checks named `CAP-todo-app.R3 ...` in `tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs`
- Done when: every check of CAP-todo-app.R3 (the overrides of the spec) passes

### Step 4 · Ticking a todo sets its completion time · [ ]

- Requirement: CAP-todo-app.R4
- Check: the checks named `CAP-todo-app.R4 ...` in `tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs`
- Done when: every check of CAP-todo-app.R4 (ticking a todo sets its completion time) passes

### Step 5 · A todo is overdue by the user's calendar · [ ]

- Requirement: CAP-todo-app.R5
- Check: the checks named `CAP-todo-app.R5 ...` in `tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs`
- Done when: every check of CAP-todo-app.R5 (a todo is overdue by the user's calendar) passes

### Step 6 · The app has no other code · [ ]

- Requirement: CAP-todo-app.R6
- Check: the checks named `CAP-todo-app.R6 ...` in `tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs`
- Done when: every check of CAP-todo-app.R6 (the app has no other code) passes

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `CAP-todo-app` as the ready stage left them.
- Sent back to tests: two R3 checks compare OverrideItem values that hold a quotation or function, so equality is by reference.
