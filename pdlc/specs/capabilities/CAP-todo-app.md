# CAP-todo-app · The todo list app as models, rules, overrides, and hooks

Status: active

## What it is for

This capability holds the first app built on the engine. It gives the later page and API
jobs their models, access rules, overrides, and the two hooks. It holds no other code.
The acceptance test of the spec is that no hand-written code lives outside these four
parts.

## Code map

- src/Todo.App/Model.fs
- src/Todo.App/Defaults.fs
- src/Todo.App/Schema.fs
- src/Todo.App/Hooks.fs

## Requirements

### CAP-todo-app.R1 · The three models and five semantic types of the spec

Status: verified · Intents: IN-0001

- Given the todo app, when the engine describes its models, then the models are `User`,
  `TodoList`, and `Todo`, with the fields of the spec.
- Given the todo app. When the engine reads its type defaults, then `Title` is text 200,
  `Notes` is text 5000, `TimeZoneId` is text 64. Also `ExternalId` is text 255, unique,
  and hidden.

### CAP-todo-app.R2 · The three access rules of the spec

Status: verified · Intents: IN-0001

- Given the todo app. When a caller reads its access rules, then `User` is `self`,
  `TodoList` is `owned`, and `Todo` is `ownedVia List`, read-only when the list is
  `Archived`.

### CAP-todo-app.R3 · The overrides of the spec

Status: verified · Intents: IN-0001

- Given the todo app, when a caller reads the overrides of `Todo`, then they are
  `readOnly CompletedAt`, `listEdit Done`, `listSort Due Ascending`, and `listColumns
  [Title; Due; Priority; Done]`.
- Given the todo app. When a caller reads the overrides of `TodoList`, then they are the
  views "Active" (not `Archived`) and "Archived" (`Archived`), in that order.

### CAP-todo-app.R4 · Ticking a todo sets its completion time

Status: verified · Intents: IN-0001

The hook runs before the save. If `Done` changed to true, `CompletedAt` is now. If `Done`
changed to false, `CompletedAt` is empty. If `Done` did not change, `CompletedAt` keeps
its value.

- Given the time 2026-10-18T09:00:00Z and a todo with `Done = false`, when the hook runs
  on a save with `Done = true`, then `CompletedAt` is 2026-10-18T09:00:00Z.
- Given a todo with `Done = true` and `CompletedAt` = 2026-10-18T09:00:00Z, when the
  hook runs on a save with `Done = false`, then `CompletedAt` is empty.
- Given a todo with `Done = true` and `CompletedAt` = 2026-10-18T09:00:00Z, when the
  hook runs on a save with `Done = true` at 2026-10-18T12:00:00Z, then `CompletedAt` is
  still 2026-10-18T09:00:00Z.

### CAP-todo-app.R5 · A todo is overdue by the user's calendar

Status: verified · Intents: IN-0001

A todo is overdue when `Due` is before today in the user's time zone, and `Done` is
false.

- Given the user's time zone "Pacific/Auckland" and the time 2026-10-18T14:00:00Z, when
  the hook reads a todo due 2026-10-18 with `Done = false`, then the todo is overdue.
- Given the same time zone and time, when the hook reads a todo due 2026-10-18 with
  `Done = true`, then the todo is not overdue.
- Given the same time zone and time, when the hook reads a todo due 2026-10-19 with
  `Done = false`, then the todo is not overdue.
- Given the same time zone and time, when the hook reads a todo with no due date, then
  the todo is not overdue.

### CAP-todo-app.R6 · The app has no other code

Status: verified · Intents: IN-0001

- Given the app project, when a caller lists its source files, then the files are
  `Model.fs`, `Defaults.fs`, `Schema.fs`, and `Hooks.fs`, and no other `.fs` file.
  (assumed)
- Given the app project, when the checks of the engine run over its models, rules, and
  overrides, then there are no error findings.
