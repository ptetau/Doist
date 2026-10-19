# Review packet · CH-0006-todo-app · specification

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: specification

Default: yes

You judge if the code does what the spec says. You do not see the checks.

- The code meets the acceptance lines of each requirement. For a requirement marked
  `(retire)`, the change removes the code that served the requirement instead.
- The code does nothing that the spec did not ask for.
- Only the files in "Files" changed. In a file that "Files" lists with parts, only those
  parts changed. The change stays in its one scope. A job change must not edit the code of
  a capability.
- No old wording survives. The title and the summary of the spec, the cross-references in
  its requirements, and a README in "Files" show the behaviour as it is now. They include
  what the change adds. Nothing that the change removes or changes still shows the old
  way. The README says how to run the checks, and names any seam that a user can set.
- The spec text, the README text, and the code comments in the change follow the writing
  rules (`DES-writing-style`, in the packet). Each break of a rule is a problem. Give the
  line and the rule.
- Each finding under "Writing check" in the packet is a problem, unless you judge the
  finding to be a false alarm. For each false alarm, say why under "Notes".
- If the packet lists frames, read each frame. Each frame must show what its caption
  claims, after the step, not the screen before the step. A blank frame, or a frame that
  shows something else, is a problem.

## The change spec

````
# CH-0006 · Todo app

Intent: IN-0001 · Scope: CAP-todo-app · Status: built · Ticket: none · Branch: pdlc/CH-0006-todo-app · Base: pdlc/CH-0005-checks
Tests-Locked: 3e52d322a80fce1c8d63131adbf5daa3d79f81c2

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
- Sent back to tests: two R3 (overrides) checks compare OverrideItem values that hold a quotation or function, so equality is by reference.
- Built: 206 checks pass (22 new). Tests were re-locked three times for weak checks.
````

## The spec it covers: title and summary

````
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
````

## The requirements, in full

### CAP-todo-app.R1 · The three models and five semantic types of the spec

Status: built · Intents: IN-0001

- Given the todo app, when the engine describes its models, then the models are `User`,
  `TodoList`, and `Todo`, with the fields of the spec.
- Given the todo app. When the engine reads its type defaults, then `Title` is text 200,
  `Notes` is text 5000, `TimeZoneId` is text 64. Also `ExternalId` is text 255, unique,
  and hidden.

### CAP-todo-app.R2 · The three access rules of the spec

Status: built · Intents: IN-0001

- Given the todo app. When a caller reads its access rules, then `User` is `self`,
  `TodoList` is `owned`, and `Todo` is `ownedVia List`, read-only when the list is
  `Archived`.

### CAP-todo-app.R3 · The overrides of the spec

Status: built · Intents: IN-0001

- Given the todo app, when a caller reads the overrides of `Todo`, then they are
  `readOnly CompletedAt`, `listEdit Done`, `listSort Due Ascending`, and `listColumns
  [Title; Due; Priority; Done]`.
- Given the todo app. When a caller reads the overrides of `TodoList`, then they are the
  views "Active" (not `Archived`) and "Archived" (`Archived`), in that order.

### CAP-todo-app.R4 · Ticking a todo sets its completion time

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

- Given the app project, when a caller lists its source files, then the files are
  `Model.fs`, `Defaults.fs`, `Schema.fs`, and `Hooks.fs`, and no other `.fs` file.
  (assumed)
- Given the app project, when the checks of the engine run over its models, rules, and
  overrides, then there are no error findings.

## Specs it relies on

### CAP-models

````
# CAP-models · Describes a model from its F# record type

Status: active

## What it is for

This capability reads an F# record type and gives each engine a plan of the model. The
plan holds each field, its kind, its semantic type, its label, and the system fields. Each
engine takes its behaviour from this plan. The plan also holds the typed overrides and the
override catalogue.

## Code map

- src/Typed.Engine/Model.fs
- src/Typed.Engine/Overrides.fs

## Requirements

### CAP-models.R1 · Each field gets a kind from its type

Status: verified · Intents: IN-0001

The kinds are text, whole number, decimal, boolean, enum, date, timestamp, id, reference,
and optional. A field gets its kind from its type. A semantic type gets its kind from a
default in a list of type defaults. A plain union with no fields is an enum.

- Given the type defaults of the todo app, when the engine describes `Todo`, then `Title`
  has the kind text with the length 200.
- Given the type defaults of the todo app, when the engine describes `Todo`, then `Notes`
  has the kind optional of text with the length 5000.
- Given the type defaults of the todo app. When the engine describes `Todo`, then
  `Priority` has the kind enum with the cases `Low`, `Normal`, and `High`.
- Given the type defaults of the todo app, when the engine describes `Todo`, then `Due`
  has the kind optional of date.
- Given the type defaults of the todo app, when the engine describes `Todo`, then `List`
  has the kind reference to `TodoList`.
- Given the type defaults of the todo app. When the engine describes `Todo`, then `Done`
  has the kind boolean, and `CompletedAt` has the kind optional of timestamp.
- Given the union type `Priority` as a model. When the engine describes it, then the result
  is the error `WRONG_CONFIG_TYPE` with the message "Priority is not a record type."
  (assumed)

### CAP-models.R2 · A type with no default is an error that names each engine

Status: verified · Intents: IN-0001

When a field has a type that no default covers, the description fails. The error names
the model, the field, the type, and each engine.

- Given a model `Tagged` with a field `Tags: Title list`. When the engine describes
  `Tagged`, then the result is the error `MISSING_DEFAULT`. The message is
  "Tagged.Tags has type 'Title list', which has no default in Persist, Validate,
  Display, API."
- Given a model with two fields that have no default. When the engine describes the
  model, then the result holds one `MISSING_DEFAULT` error for each field.

### CAP-models.R3 · The system fields are known and never written from a model

Status: verified · Intents: IN-0001

The system fields are `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`. A model
does not declare them. The plan knows their names.

- Given any model, when a caller asks for the system field names, then the names are
  `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`.
- Given the model `Todo`. When the engine describes `Todo`, then the fields of the plan
  are the seven fields of the record, and no system field.

### CAP-models.R4 · The label comes from the field name

Status: verified · Intents: IN-0001

The field name is split into words. The first word starts with a capital letter. The other
words are lower case.

- Given the field name `CompletedAt`, when the engine makes the label, then the label is
  "Completed at".
- Given the field name `Due`, when the engine makes the label, then the label is "Due".
- Given the field name `TimeZone`, when the engine makes the label, then the label is
  "Time zone".
- Given the field name `ExternalId`, when the engine makes the label, then the label is
  "External id".
- Given the field name `HasDue`, when the engine makes the label, then the label is "Has
  due".

### CAP-models.R5 · Wide text is text longer than 400 characters

Status: verified · Intents: IN-0001

- Given the field `Notes` with the kind optional of text with the length 5000. When a
  caller asks if the field is wide, then the answer is yes.
- Given the field `Title` with the kind text with the length 200. When a caller asks if
  the field is wide, then the answer is no.
- Given a text type with the length 400, when a caller asks if the field is wide, then the
  answer is no.
- Given a text type with the length 401, when a caller asks if the field is wide, then the
  answer is yes.

### CAP-models.R6 · The title field is marked or named

Status: verified · Intents: IN-0001

The title field names a record in pickers and links. The title field is the first field
whose type carries `[<TitleType>]`. A `titleField` override replaces that choice.

- Given the model `TodoList` and no overrides, when a caller asks for the title field,
  then the field is `Name`.
- Given the model `User` and no overrides, when a caller asks for the title field, then
  the field is `DisplayName`.
- Given a model `NoTitle` with one field `Count: int` and no overrides, when a caller
  asks for the title field, then there is none.
- Given the model `NoTitle` and the override `titleField <@ fun n -> n.Count @>`, when a
  caller asks for the title field, then the field is `Count`.

### CAP-models.R7 · Overrides take typed field selectors

Status: verified · Intents: IN-0001

Each override in the catalogue takes a field selector such as `<@ fun t -> t.Due @>`. The
catalogue is `label`, `hiddenInForm`, `readOnly`, `listColumns`, `listEdit`, `listSort`,
`listView`, `titleField`, `rule`, `sensitive`, `column`, and `unsafe`. An override set is
typed by its model, so a selector of another model does not compile.

- Given the selector `<@ fun (t: Todo) -> t.Due @>`, when the engine reads the field name,
  then the name is "Due".
- Given `overrides<Todo> [ listSort <@ fun t -> t.Due @> Ascending ]`, when the engine
  reads the override set, then its model name is "Todo" and it holds one `ListSort`
  item for the field "Due".
- Given `listColumns [ col <@ fun t -> t.Title @>; col <@ fun t -> t.Due @> ]`, when the
  engine reads the item, then the item holds the fields "Title" and "Due", in that
  order.
- Given `listView "Active" <@ fun l -> not l.Archived @>`, when the engine reads the
  fields that the view uses, then the fields are "Archived" only.
````

### CAP-access

````
# CAP-access · Decides who can see and change each record

Status: active

## What it is for

This capability holds the access rule of each model and builds the scope that every query
takes. The four rules are `owned`, `ownedVia`, `self`, and `system`. A rule can carry a
read-only condition on the parent. Access is denied by default. A record outside the
scope of a caller is not found.

## Code map

- src/Typed.Engine/Access.fs

## Requirements

### CAP-access.R1 · Each model declares one of four rules

Status: verified · Intents: IN-0001

- Given `rule<TodoList> owned`, when a caller reads the rule, then the model name is
  "TodoList" and the access is `Owned`.
- Given `rule<Todo> (ownedVia <@ fun t -> t.List @>)`, when a caller reads the rule, then
  the access is `OwnedVia` with the reference field "List" and the parent "TodoList".
- Given `rule<User> self`, when a caller reads the rule, then the access is `Self`.
- Given `rule<Audit> system`, when a caller reads the rule, then the access is
  `SystemOnly`.

### CAP-access.R2 · A read-only condition names a bool field of the parent

Status: verified · Intents: IN-0001

- Given `ownedVia <@ fun t -> t.List @> |> readOnlyWhenParent <@ fun l -> l.Archived @>`,
  when a caller reads the rule, then the read-only condition names the model "TodoList"
  and the field "Archived".
- Given `owned |> readOnlyWhenParent <@ fun l -> l.Archived @>`, when a caller reads the
  rule, then the call fails with the message "readOnlyWhenParent only applies to
  ownedVia".

### CAP-access.R3 · Only this capability can make a scope

Status: verified · Intents: IN-0001

A scope is an owner filter, a self scope, or the engine-only scope. The scope type has a private
constructor. Other code cannot make one.

- Given a user with the id `u1`, when the engine makes the scope for `TodoList`, then the
  scope is the owner filter for `u1`.
- Given the model `User` with the rule `self`. When the engine makes the scope for a user
  `u1`, then the scope is the self scope for `u1`. It matches only the record whose `Id` is
  `u1`. (assumed)
- Given the model `Todo` with its `ownedVia` rule. When the engine makes the scope for `u1`,
  then the scope is the owner filter for `u1`, because the owner is copied to each todo.
- Given the model `Audit` with the rule `system`, when the engine makes a user scope, then
  the result is the error `NOT_FOUND`.
- Given the engine itself, when the engine makes the build scope, then the scope is the
  engine-only scope.

### CAP-access.R4 · A change to a read-only record is refused with its reason

Status: verified · Intents: IN-0001

- Given the list "Old" with `Archived = true`. When the engine checks a change to a todo of
  that list, then the result is the error `READ_ONLY_RECORD`. The reason is "This list is
  archived".
- Given the list "Groceries" with `Archived = false`, when the engine checks a change to a
  todo of that list, then the check passes.
- Given the list "Old" with `Archived = true`, when the engine checks a delete of the list
  itself, then the check passes.
- Given a todo whose list does not exist. When the engine checks a change to the todo,
  then the result is the error `NOT_FOUND`.
- Given the list "Old" with `Archived = true`. When the engine checks the creation of a
  todo in that list, then the result is the error `READ_ONLY_RECORD`. The reason is "This
  list is archived". (assumed)

### CAP-access.R5 · The owner of a child is copied from its parent

Status: verified · Intents: IN-0001

- Given a user `u1` and a list owned by `u1`. When the engine decides the owner of a new
  todo in that list, then the owner is `u1`.
- Given a user `u1` and a list owned by `u2`. When the engine decides the owner of a new
  todo in that list, then the result is the error `NOT_FOUND`.
````

### CAP-checks

````
# CAP-checks · Fails the build when a model, rule, or override is wrong

Status: active

## What it is for

This capability runs every build check over the models, the access rules, and the
override sets. Each check gives findings. An error finding fails the build. A warning
finding does not. The generator job prints the findings.

## Code map

- src/Typed.Engine/Checks.fs

## Requirements

### CAP-checks.R1 · A model with no access rule fails the build

Status: verified · Intents: IN-0001

- Given the model `TodoList` and no access rules. When the checks run, then the findings
  hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList has no access
  rule."
- Given the model `TodoList` and two access rules for `TodoList`. When the checks run,
  then the findings hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList
  has more than one access rule."
- Given the three models of the todo app and the three access rules of the todo app.
  When the checks run, then there is no `MISSING_ACCESS_RULE` finding.

### CAP-checks.R2 · An override that names a missing field fails the build

Status: verified · Intents: IN-0001

The F# compiler refuses a selector of a field that does not exist. This check covers an
override set that names a model with no description. It also covers an override item that names a
field that the plan does not hold.

- Given an override set for a model `Ghost` that is not in the model list. When the checks
  run, then the findings hold the error `MISSING_FIELD` with the message "Overrides name
  Ghost, which is not a model."
- Given a `Label` item for the field "Dew" of `Todo`. When the checks run, then the
  findings hold the error `MISSING_FIELD` with the message "Override names missing field
  Todo.Dew."

### CAP-checks.R3 · An override with config for the wrong type fails the build

Status: verified · Intents: IN-0001

- Given the override `rule <@ fun t -> t.Due @>` whose predicate takes a `string`, when
  the checks run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message
  names `Todo.Due`, the expected type, and the type of the field.
- Given the override `rule <@ fun t -> t.Title @>` whose predicate takes a `Title`, when
  the checks run, then there is no `WRONG_CONFIG_TYPE` finding.

### CAP-checks.R4 · An override that targets a system field fails the build

Status: verified · Intents: IN-0001

- Given a `Column` item for the field "Version" of `Todo`. When the checks run, then the
  findings hold the error `SYSTEM_FIELD_TARGET` with the message "Override targets system
  field Todo.Version."

### CAP-checks.R5 · A reference to a model that does not exist fails the build

Status: verified · Intents: IN-0001

- Given the model `Todo` alone, with its field `List: Ref<TodoList>`. When the checks run,
  then the findings hold the error `DANGLING_REFERENCE` with the message "Field Todo.List
  refers to TodoList, which is not a model."

### CAP-checks.R6 · A referenced model with no title field fails the build

Status: verified · Intents: IN-0001

- Given a model `NoTitle` with one field `Count: int`, and a model `Child` with the field
  `Parent: Ref<NoTitle>`, and no `titleField` override. When the checks run, then the
  findings hold the error `NO_TITLE_FIELD` with the message "NoTitle has no title field,
  but Child.Parent refers to it."
- Given the same two models and the override `titleField <@ fun n -> n.Count @>` for
  `NoTitle`, when the checks run, then there is no `NO_TITLE_FIELD` finding.

### CAP-checks.R7 · An `ownedVia` rule must name a reference to its parent

Status: verified · Intents: IN-0001

- Given the access rule `ownedVia <@ fun t -> t.Title @>` for `Todo`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE` with the message "ownedVia
  Todo.Title must be a reference to TodoList."
- Given the rule `readOnlyWhenParent <@ fun l -> l.Name @>` on the `ownedVia` rule of
  `Todo`. When the checks run, then the findings hold the error `WRONG_CONFIG_TYPE` with
  the message "readOnlyWhenParent names TodoList.Name, which is not a bool field."

### CAP-checks.R8 · Each `unsafe` escape hatch is a warning that gives its reason

Status: verified · Intents: IN-0001

- Given the override `unsafe "rawName" "uses raw SQL"` for `TodoList`. When the checks
  run, then the findings hold no error, and one warning `UNSAFE_ESCAPE_HATCH` with the message
  "Unsafe escape hatch TodoList.rawName: uses raw SQL".

### CAP-checks.R9 · An override that matches the default is a warning

Status: verified · Intents: IN-0001

- Given the override `label <@ fun t -> t.Due @> "Due"` for `Todo`. When the checks run,
  then the findings hold one warning `REDUNDANT_OVERRIDE` with the message "Override
  Todo.Due label matches the default." (assumed)
- Given the override `label <@ fun t -> t.Due @> "Due date"` for `Todo`, when the checks
  run, then there is no such warning.

### CAP-checks.R10 · The todo app passes every check

Status: verified · Intents: IN-0001

- Given the three models, the three access rules, and the two override sets of the todo
  app. When the checks run, then there are no error findings.

### CAP-checks.R11 · Two overrides of one kind on one field fail the build

Status: verified · Intents: IN-0001

- Given two `label` items for the field `Due` of `Todo`. When the checks run, then the
  findings hold the error `WRONG_CONFIG_TYPE` with the message "Todo.Due has two label
  overrides." (assumed)
- Given one `label` item and one `readOnly` item for `Due`, when the checks run, then there
  is no such finding.

### CAP-checks.R12 · Findings come in model order, then field order

Status: verified · Intents: IN-0001

- Given the models `TodoList` then `Todo`, each with one error. When the checks run, then
  the `TodoList` finding comes before the `Todo` finding. (assumed)
- Given no models and no rules, when the checks run, then there are no findings.

### CAP-checks.R13 · A sort or a view that the store cannot serve fails the build

Status: verified · Intents: IN-0001

Wide text is never indexed, so a list cannot sort by it. A list view must be a predicate
that the store can turn into a filter: a bool field, `not` of one, or an equality.

- Given the override `listSort <@ fun t -> t.Notes @> Ascending`. When the checks run,
  then the findings hold the error `WRONG_CONFIG_TYPE`. The message is "Cannot sort Todo
  by Notes: wide text is never indexed."
- Given the view `listView "Long" <@ fun t -> t.Title.Length > 3 @>`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message starts with
  "Unsupported list view for Todo:". (assumed)
- Given the view `listView "High" <@ fun t -> t.Priority = High @>`, when the checks run,
  then there is no such finding.
````

### DES-names

````
# DES-names · The names in code follow the spec

Status: agreed · Default: yes · Ask at init: no

## Rule

Code, folders, and check names use the words of the spec: Access, Persist, Validate,
Display, API, Generator, model, semantic type, kind, override, access rule, hook, and
system field.

The design (`docs/design/typed-layers-engine.md`) and the blueprint
(`docs/design/typed-layers-engine.blueprint.md`) give advice on where the walls go. Their
component names are not the names in code. This table maps them:

| Design component | Code module |
|---|---|
| `AuthorizingEngine` | `Access` |
| `RecordsAccess` | `Persist` |
| `ValidatingEngine` | `Validate` |
| `PresentingEngine`, `PageClient` | `Display` |
| `ApiClient` | `Api` |
| `BuildTool`, `BuildManager`, `CheckingEngine`, `ModelsAccess` | `Generator` |
| `RecordsManager`, `SignInManager`, `JobsManager` | `Runtime` |
| `SessionAccess`, `IdentityAccess`, `LeaseAccess` | `Runtime` |

## Why

The spec's acceptance test says that the todo app has no hand-written code outside
models, access rules, overrides, and hooks. That test reads the code by the spec's names.
Two sets of names would make the test hard to read and easy to fail.
````

### DES-errors

````
# DES-errors · The project's error codes

Status: agreed · Default: yes · Ask at init: no

## Rule

Each error that the system can give has a code here, in `UPPER_SNAKE_CASE`. Acceptance
lines, Interface entries, and checks all use these codes. Thus, a check can expect the
exact error.

| Code | Meaning | Whose fault | Retry helps? |
|---|---|---|---|
| `NOT_FOUND` | The record does not exist, or is outside the scope of the caller | caller | no |
| `STALE_VERSION` | The save used an old `Version` | caller | no |
| `READ_ONLY_RECORD` | The record is read-only. The reason is in the error | caller | no |
| `VALIDATION_FAILED` | A field broke a rule. The errors are listed for each field | caller | no |
| `MISSING_DEFAULT` | A type has no default in an engine | caller | no |
| `MISSING_ACCESS_RULE` | A model has no access rule, or more than one | caller | no |
| `MISSING_FIELD` | An override names a field or a model that does not exist | caller | no |
| `WRONG_CONFIG_TYPE` | An override or a rule has config that does not fit the field | caller | no |
| `SYSTEM_FIELD_TARGET` | An override targets a system field | caller | no |
| `DANGLING_REFERENCE` | A reference points at a model that does not exist | caller | no |
| `NO_TITLE_FIELD` | A referenced model has no title field | caller | no |
| `UNPLANNED_DROP` | A migration would drop a column with no `renamedFrom` | caller | no |
| `UNSAFE_ESCAPE_HATCH` | Warning: an `unsafe` override, listed with its reason | caller | no |
| `REDUNDANT_OVERRIDE` | Warning: an override that equals the default | caller | no |
| `UNSAFE_MIGRATION` | A new required column has no default, so existing rows would fail | caller | no |
| `SHAPE_UNREADABLE` | The stored shape file cannot be read | caller | no |

When a requirement needs a new error, add a row. When the meaning is the same, use the
existing code again.

Add rows at intake and ready. Do not add a row while a change is in tests, build, or
verify. When you add a row, leave `Default:` as it is. Write each row the same way:

- The code in backticks.
- "Whose fault": `caller`, `system`, or `outside service`.
- "Retry helps?": `yes` or `no`.

Checks and the code of the app match on the error code. Each requirement decides what a
person sees, for example a message or a status. The message or the status can omit the
error code. If the caller cannot see the error code, a check expects the exact message and
status instead.

## Why

A check that expects "an error" passes for any error, also for a typo. A named code makes
the check exact. A named code also gives callers a stable value to handle.
````

### DES-writing-style

````
# DES-writing-style · How pdlc writes

Status: agreed · Default: yes · Ask at init: yes

## Rule

This rule is a copy of `WRITING-GUIDELINES.md` from the pskills repository.

These guidelines apply to all writing in this repository. They also apply to all text that
an agent writes when it uses the skills in this repository. They have four parts:

1. Part 1 gives the rules of ASD-STE100 Simplified Technical English (STE) in short form.
2. Part 2 gives two more rules: the local-references rule and the described-IDs rule.
3. Part 3 tells you how to apply the rules to each type of text.
4. Part 4 tells you how to examine your text against the rules.

### Where these guidelines apply

Use these guidelines for all of these types of text:

- The replies that an agent writes to the user in a conversation.
- Skills: the `SKILL.md` file, its reference files, and its templates.
- Agent definitions, system prompts, and the prompts that an agent writes for other agents.
- The documents that a skill or an agent writes: specs, designs, plans, reports, and web
  pages.
- Commit messages, pull request text, and code comments.

These items are not prose. Do not change them to obey the rules:

- Code, commands, file paths, and identifiers.
- Error codes, field names, and values.
- Quoted text. For example, the words of a user, or the trigger phrases in the description
  of a skill.
- The names of products, organisations, and published works.

### Part 1: STE rules

STE is a controlled language for technical text. The standard has 53 rules in nine
sections, and a dictionary of approved words. This part gives the rules in short form,
with the rule numbers of the standard. This repository does not contain the dictionary.
Part 4 tells you how to get the dictionary and how to use it.

#### Words (STE section 1)

- **1.1** Use approved words, technical nouns, and technical verbs.
- **1.2 to 1.4** Use an approved word only as its approved part of speech. Use the word
  only with its approved meaning and in its approved forms. For example, "check" is
  approved as a noun. As a verb, write "make sure that".
- **1.5, 1.6, 1.8 to 1.10** Technical nouns are the names of items in your subject.
  Examples in software are "commit", "branch", "file", "schema", and "pull request". Use
  the terms that your project uses. Select short terms. Do not use slang or jargon.
- **1.7** Do not use a technical noun as a verb. For example, do not write "action the
  ticket". Write "close the ticket".
- **1.11** Use one technical noun for one item. Do not use a different noun for the same
  item in a different part of the text.
- **1.12, 1.13** Technical verbs are the actions in your subject. In software, "commit",
  "merge", "deploy", "parse", and "click" are technical verbs. Do not use a technical verb
  as a noun. Write "merge the branch", not "do a merge of the branch".
- **1.14** Use New Zealand English spelling, for example "behaviour", "colour", and
  "organise". STE uses American English unless your own rules give a different standard.
  These guidelines give New Zealand English. Do not change the spelling of code, field
  names, or quoted text.

#### Multi-word nouns (STE section 2)

- **2.1** Do not write a multi-word noun of more than three words. For example, do not
  write "test result cache file path". Write "the path of the cache file for test
  results".
- **2.2** If a technical noun has more than three words, write it in full one time. Then
  use a shorter form, or join the words with hyphens.

#### Verbs (STE section 3)

- **3.1, 3.2** Use only these verb forms:
  - The infinitive: "to run".
  - The imperative: "Run the tests."
  - The simple present, the simple past, and the simple future: "runs", "ran", "will run".
  - The past participle as an adjective: "the merged branch".
- **3.3** Use the past participle as an adjective, for example "the merged branch".
- **3.4** Do not use auxiliary verbs to make complex verb constructions. For example, do
  not write "the file should have been updated". Write "update the file".
- **3.5** Use the "-ing" form of a verb only in a technical noun, for example "load
  balancing". Do not write "When running the tests, read the log". Write "When you run
  the tests, read the log".
- **3.6** Use the active voice. In descriptive text, you can use the passive voice only
  when you do not know who or what does the action.
- **Modal verbs (3.2, 3.4).** STE has "must" and "can". It does not have "should" or "may".
  Keep the strength of the original text:
  - A rule that always holds: write "must" or "must not".
  - Advice or a default: write "Try to …" or "Usually, …". Do not write "must".
  - A permission: write "is permitted to …". Write "can" for an ability.
  - The word "never": write "Do not …".
- **3.7** Use a verb to show an action, not a noun. Write "install the package", not "do
  the installation of the package".

#### Sentences (STE section 4)

- **4.1** Write short and clear sentences. Give one topic in each sentence. Give accurate
  facts, not abstract statements. For example, do not write "Large inputs change the run
  time". Write "When the input has more than 1,000 rows, the run time increases".
- **4.2** Do not omit words to make a sentence shorter. Do not omit articles, verbs, or
  subjects. Do not use contractions. Write "do not", not "don't".
- **4.3** Use a vertical list for complex text.
- **4.4** Use connecting words to connect related sentences, for example "and", "but",
  "then", "thus", and "as a result".
- **4.5** When applicable, put "the", "a", "an", "this", or "these" before a noun.

#### Instructions (STE section 5)

STE calls this type of text a procedure.

- **5.1** Write a maximum of 20 words in each instruction.
- **5.2** Write only one instruction in each sentence, unless the actions occur at the
  same time.
- **5.3** Write instructions in the imperative: "Run the tests."
- **5.4** If the reader must know a condition first, start with the condition. Put a comma
  after the condition: "If the tests fail, stop."
- **5.5** Use a note only to give information, not to give an instruction.

#### Descriptive text (STE section 6)

Descriptive text gives information, not instructions. Do not use the imperative in
descriptive text.

- **6.1** Give information gradually. Give the primary point first, then the details.
- **6.2** Use key words and headings to show the structure of the text.
- **6.3** Write a maximum of 25 words in each descriptive sentence.
- **6.4 to 6.6** Use paragraphs to show related information. Give each paragraph only one
  topic. Write a maximum of six sentences in each paragraph.

#### Safety instructions (STE section 7)

In software, a safety instruction tells the reader about an action that can cause loss of
data or other damage. Examples are a force push, a deleted table, and a message to other
persons.

- **7.1** Start with a word that shows the level of risk. Write "Warning" for a risk to
  persons. Write "Caution" for a risk to data or equipment.
- **7.2** Then give a clear command or condition.
- **7.3** Then give the risk or the possible result.

For example: "Caution: Do not force push to a shared branch. A force push can erase the
commits of other persons."

#### Punctuation and word count (STE section 8)

- **8.1** Do not use the semicolon. Write two sentences.
- **8.2** Use a hyphen to join words that are directly related, for example "read-only".
- **8.3** Use parentheses for references, identifiers, abbreviations, short explanations,
  and alternatives.
- **8.4** In a vertical list, a colon ends a sentence for the word count.
- **8.5 to 8.7** For the word count, count each of these items as one word:
  - Text in parentheses.
  - A number, or a number with its unit.
  - An abbreviation or an identifier.
  - Quoted text.
  - A title or a heading.
  - A proper noun.
  - A hyphenated word.

#### Writing practices (STE section 9)

- **9.1** If you cannot replace a word with an approved word, write the sentence again
  with a different structure.
- **9.2** Use each word correctly.
- **9.3** Do not use phrasal verbs. For example, write "prepare" or "configure", not "set
  up". Write "find", not "find out".
- **9.4** Use the same terms and the same style in all of the text.

#### General recommendations (STE)

- **GR-1** Write "that" after verbs such as "make sure", "show", and "recommend". For
  example: "Make sure that the tests pass."
- **GR-2** When you use "with", read the sentence again. Make sure that the sentence has
  only one meaning.
- **GR-3, GR-4** If a pronoun can refer to more than one noun, write the noun. The
  local-references rule in Part 2 makes this rule stricter.
- **GR-6** Do not use Latin abbreviations. Write "for example", not "e.g.". Write "that
  is", not "i.e.". Do not write "etc.".
- **GR-7** Use gender-neutral language. Use "they" for a person whose pronouns you do not
  know.
- **GR-8** Use the possessive form, for example "the user's file", only when you are sure
  that it is correct.

### Part 2: Two more rules

These two rules apply together with the STE rules.

#### The local-references rule

The reader must not look back far to find what a word refers to.

- A pronoun ("it", "they", "this", "that", "these", "those") must refer to a noun in the
  same paragraph. A short noun, for example "the file" or "the change", must also refer
  to a noun in the same paragraph.
- Only one noun in that paragraph can match the reference. If two nouns can match, write
  the full noun.
- At the start of a paragraph, a list item, a table cell, or a reply, write the full noun.
  Do not use "It", "This", or "They" to refer to an earlier paragraph.
- A heading does not give a noun to the text below the heading. Under the heading
  "OutboxAccess", do not start with "It stores". Write "OutboxAccess stores".
- In a conversation, each reply must make sense alone. Do not write "the PR" or "the
  second option" to refer to an earlier reply. Write the name or the full description.

For example, do not write: "The run found a problem in the template and in the checker.
It is now correct." Write: "The run found a problem in the template and in the checker. I
corrected the template."

#### The described-IDs rule

An ID is a short code that names an item, for example "B2", "R3", "V1", "UC2", "PR #6",
"task #4", or a commit hash. An ID without a description makes the reader look back.

- Each time that you write an ID, write a short description of the item in the same
  sentence. For example: "B2 (cache the results)".
- When the reader must select an alternative, give each alternative its ID and its
  description in the question. Do not ask "B1, B2, or B3?".
- In a table, a row that gives the ID and the description is sufficient for that row.
- Make each description short and simple: two to eight words.
- If the ID is not necessary for the reader, do not use the ID. Use the description only.

For example, do not write: "Do you want B1 or B3?" Write: "Do you want B1 (keep the
cache) or B3 (remove the cache)?"

### Part 3: How to apply the rules

#### Replies to the user

- Give the answer or the result first. Then give the details.
- Use plain words, and give the context that the reader must have.
- Use short sentences. Use a list for three or more items. Do not write a paragraph when
  sentences in a list are sufficient.
- Write only about the topic. Do not add text that does not relate to the topic.
- Make each reply clear alone. Obey the local-references rule.
- When you ask a question with alternatives, obey the described-IDs rule.

#### Skills, agent definitions, and prompts

- If a skill gives an exact output format, the format has priority over these
  guidelines. For example, a skill can tell an agent to write one sentence for each
  option. Then the agent writes one sentence, also when the sentence has two topics.
- Write each step in the imperative. Write one instruction in each sentence.
- Put the condition first: "If the suite exists, load the suite."
- Give numbers to the steps of a procedure.
- Write the purpose of a skill, and other descriptive text, with the descriptive rules.
- In the `description` of a skill, keep the name and the trigger phrases of the skill.
  Do not change these quoted phrases. Write the other text of the description with the
  rules.
- A prompt for a different agent must make sense alone, because that agent does not see
  your conversation.

#### Documents and artifacts

- Start each section with its primary point.
- Use headings as key words.
- Give each technical noun that a reader possibly does not know in a "Words used here"
  list. Use one line for each word.
- Use a table to compare items. Use a list for steps and alternatives.

#### Commit messages and pull requests

- Write the subject line of a commit in the imperative, for example "Add the linter".
- Write the body as descriptive text.

### Part 4: Examine your text

1. Run the linter: `python check_writing.py FILE`. The linter is in the `scripts` folder of
   the `improve-writing` skill. The linter finds the problems that a script can measure.
2. To examine words against the STE dictionary, get the standard. The standard is free
   from <https://www.asd-ste100.org>. Then give the linter your copy:
   `--dictionary PATH`. The path can be the PDF file or a text file of the PDF.
3. Read the text again for the rules that a script cannot measure. Examples are one topic
   in each sentence, the local-references rule, and accurate facts.

The `/improve-writing` skill does these three steps and writes the text again.

### Source

- ASD-STE100 Simplified Technical English, Issue 9, 2025-01-15, by ASD (the Aerospace,
  Security and Defence Industries Association of Europe). Part 1 gives the rules of the
  standard in short form, with the rule numbers of the standard.
- The standard is free from <https://www.asd-ste100.org>. ASD does not let other persons
  distribute the standard. Thus, this file does not contain the text of the standard or
  its dictionary.
- ASD-STE100 is a registered trademark of ASD.
````

### DES-pdlc-check-tagging

````
# DES-pdlc-check-tagging · How checks name their requirement

Status: agreed · Default: yes · Ask at init: yes

## Rule

The requirement ID starts the name of the check, for example `CAP-email.R3 sends from a
template`. A check that proves two requirements names the two requirements.

If the check runner does not accept `.` or `-` in names (Python's `unittest`, for
example), write the ID with underscores instead: `test_CAP_email_R3_sends_from_a_template`.
The two forms count as the same ID. Use only one form in each project.

Lay checks out the same way everywhere:

- Put new check files in the check folder (`tests/` in a project that had none). Name
  each file after the spec it proves, in a form the runner finds: `test_CAP_email.py`,
  `CAP-email.test.js`.
- Inside a file, make one group per requirement (a test class or a `describe` block).
  Name the group with the requirement ID: `class CAP_email_R3(unittest.TestCase)`,
  `describe("CAP-email.R3", ...)`.
- Each check's name still starts with the ID, then says what it proves.

## Why

The trace from a requirement to its checks is only as good as this naming. Thus, the
naming must be simple and the same everywhere.
````

## The code

### Diff against pdlc/CH-0005-checks

````
diff --git a/src/Todo.App/Defaults.fs b/src/Todo.App/Defaults.fs
new file mode 100644
index 0000000..66b955e
--- /dev/null
+++ b/src/Todo.App/Defaults.fs
@@ -0,0 +1,41 @@
+module Todo.App.Defaults
+
+open System
+open Typed.Engine.Model
+open Todo.App.Model
+
+let private oneLine (s: string) : string list =
+    if s.IndexOfAny [| '\r'; '\n' |] >= 0 then
+        [ "must be on one line" ]
+    else
+        []
+
+let private knownZone (s: string) : string list =
+    if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then
+        []
+    else
+        [ "must be a known time zone, such as Pacific/Auckland" ]
+
+let typeDefaults: TypeDefault list =
+    [ { Clr = typeof<Title>
+        Kind = Text 200
+        Check = oneLine
+        Hidden = false
+        Unique = false }
+      { Clr = typeof<Notes>
+        Kind = Text 5000
+        Check = (fun _ -> [])
+        Hidden = false
+        Unique = false }
+      { Clr = typeof<TimeZoneId>
+        Kind = Text 64
+        Check = knownZone
+        Hidden = false
+        Unique = false }
+      { Clr = typeof<ExternalId>
+        Kind = Text 255
+        Check = (fun _ -> [])
+        Hidden = true
+        Unique = true } ]
+
+let models: Type list = [ typeof<User>; typeof<TodoList>; typeof<Todo> ]
diff --git a/src/Todo.App/Hooks.fs b/src/Todo.App/Hooks.fs
new file mode 100644
index 0000000..4f0d6c1
--- /dev/null
+++ b/src/Todo.App/Hooks.fs
@@ -0,0 +1,22 @@
+module Todo.App.Hooks
+
+open Typed.Engine.Model
+open Todo.App.Model
+
+/// Before save: set CompletedAt when Done turns true, clear it when Done turns false.
+let completionTime (ctx: HookContext) (previous: Todo option) (saved: Todo) : Todo =
+    let wasDone =
+        match previous with
+        | Some p -> p.Done
+        | None -> false
+
+    match wasDone, saved.Done with
+    | false, true -> { saved with CompletedAt = Some ctx.Now }
+    | true, false -> { saved with CompletedAt = None }
+    | _ -> saved
+
+/// Computed value: the todo is open and due before today in the user's time zone.
+let isOverdue (ctx: HookContext) (todo: Todo) : bool =
+    match todo.Due with
+    | Some due -> not todo.Done && due < localToday ctx
+    | None -> false
diff --git a/src/Todo.App/Model.fs b/src/Todo.App/Model.fs
new file mode 100644
index 0000000..7526831
--- /dev/null
+++ b/src/Todo.App/Model.fs
@@ -0,0 +1,34 @@
+module Todo.App.Model
+
+open System
+open Typed.Engine.Model
+
+[<TitleType>]
+type Title = Title of string
+
+type Notes = Notes of string
+
+type Priority =
+    | Low
+    | Normal
+    | High
+
+type TimeZoneId = TimeZoneId of string
+
+type ExternalId = ExternalId of string
+
+type User =
+    { Subject: ExternalId
+      DisplayName: Title
+      TimeZone: TimeZoneId }
+
+type TodoList = { Name: Title; Archived: bool }
+
+type Todo =
+    { List: Ref<TodoList>
+      Title: Title
+      Notes: Notes option
+      Priority: Priority
+      Due: DateOnly option
+      Done: bool
+      CompletedAt: DateTimeOffset option }
diff --git a/src/Todo.App/Schema.fs b/src/Todo.App/Schema.fs
new file mode 100644
index 0000000..c032a2d
--- /dev/null
+++ b/src/Todo.App/Schema.fs
@@ -0,0 +1,28 @@
+module Todo.App.Schema
+
+open Typed.Engine.Overrides
+open Typed.Engine.Access
+open Todo.App.Model
+
+let access: AccessRule list =
+    [ rule<User> self
+      rule<TodoList> owned
+      rule<Todo> (ownedVia <@ fun (t: Todo) -> t.List @> |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>) ]
+
+let todoDisplay: ModelOverrides =
+    overrides<Todo>
+        [ readOnly <@ fun (t: Todo) -> t.CompletedAt @>
+          listEdit <@ fun (t: Todo) -> t.Done @>
+          listSort <@ fun (t: Todo) -> t.Due @> Ascending
+          listColumns
+              [ col <@ fun (t: Todo) -> t.Title @>
+                col <@ fun (t: Todo) -> t.Due @>
+                col <@ fun (t: Todo) -> t.Priority @>
+                col <@ fun (t: Todo) -> t.Done @> ] ]
+
+let todoListDisplay: ModelOverrides =
+    overrides<TodoList>
+        [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
+          listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ]
+
+let overrideSets: ModelOverrides list = [ todoDisplay; todoListDisplay ]
diff --git a/src/Todo.App/Todo.App.fsproj b/src/Todo.App/Todo.App.fsproj
new file mode 100644
index 0000000..7ae3e71
--- /dev/null
+++ b/src/Todo.App/Todo.App.fsproj
@@ -0,0 +1,18 @@
+<Project Sdk="Microsoft.NET.Sdk">
+
+  <PropertyGroup>
+    <TargetFramework>net8.0</TargetFramework>
+  </PropertyGroup>
+
+  <ItemGroup>
+    <Compile Include="Model.fs" />
+    <Compile Include="Defaults.fs" />
+    <Compile Include="Schema.fs" />
+    <Compile Include="Hooks.fs" />
+  </ItemGroup>
+
+  <ItemGroup>
+    <ProjectReference Include="../Typed.Engine/Typed.Engine.fsproj" />
+  </ItemGroup>
+
+</Project>
````

### src/Todo.App/Defaults.fs

````
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
````

### src/Todo.App/Hooks.fs

````
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
````

### src/Todo.App/Model.fs

````
module Todo.App.Model

open System
open Typed.Engine.Model

[<TitleType>]
type Title = Title of string

type Notes = Notes of string

type Priority =
    | Low
    | Normal
    | High

type TimeZoneId = TimeZoneId of string

type ExternalId = ExternalId of string

type User =
    { Subject: ExternalId
      DisplayName: Title
      TimeZone: TimeZoneId }

type TodoList = { Name: Title; Archived: bool }

type Todo =
    { List: Ref<TodoList>
      Title: Title
      Notes: Notes option
      Priority: Priority
      Due: DateOnly option
      Done: bool
      CompletedAt: DateTimeOffset option }
````

### src/Todo.App/Schema.fs

````
module Todo.App.Schema

open Typed.Engine.Overrides
open Typed.Engine.Access
open Todo.App.Model

let access: AccessRule list =
    [ rule<User> self
      rule<TodoList> owned
      rule<Todo> (ownedVia <@ fun (t: Todo) -> t.List @> |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>) ]

let todoDisplay: ModelOverrides =
    overrides<Todo>
        [ readOnly <@ fun (t: Todo) -> t.CompletedAt @>
          listEdit <@ fun (t: Todo) -> t.Done @>
          listSort <@ fun (t: Todo) -> t.Due @> Ascending
          listColumns
              [ col <@ fun (t: Todo) -> t.Title @>
                col <@ fun (t: Todo) -> t.Due @>
                col <@ fun (t: Todo) -> t.Priority @>
                col <@ fun (t: Todo) -> t.Done @> ] ]

let todoListDisplay: ModelOverrides =
    overrides<TodoList>
        [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
          listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ]

let overrideSets: ModelOverrides list = [ todoDisplay; todoListDisplay ]
````

### src/Todo.App/Todo.App.fsproj

````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Model.fs" />
    <Compile Include="Defaults.fs" />
    <Compile Include="Schema.fs" />
    <Compile Include="Hooks.fs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Typed.Engine/Typed.Engine.fsproj" />
  </ItemGroup>

</Project>
````

## Writing check

`pdlc/bin/check_writing.py --since pdlc/CH-0005-checks` on pdlc/changes/CH-0006-todo-app/change.md, pdlc/specs/capabilities/CAP-todo-app.md:

````
No findings.
````

## Visual review frames

Read each frame to see the change working:

- pdlc/changes/CH-0006-todo-app/frames/01.png
- pdlc/changes/CH-0006-todo-app/frames/02.png
- pdlc/changes/CH-0006-todo-app/frames/03.png
- pdlc/changes/CH-0006-todo-app/frames/04.png
- pdlc/changes/CH-0006-todo-app/frames/05.png
- pdlc/changes/CH-0006-todo-app/frames/06.png
- pdlc/changes/CH-0006-todo-app/frames/07.png
- pdlc/changes/CH-0006-todo-app/frames/08.png
- pdlc/changes/CH-0006-todo-app/frames/09.png
- pdlc/changes/CH-0006-todo-app/frames/10.png
- pdlc/changes/CH-0006-todo-app/frames/11.png
- pdlc/changes/CH-0006-todo-app/frames/12.png
- pdlc/changes/CH-0006-todo-app/frames/13.png
- pdlc/changes/CH-0006-todo-app/frames/14.png
- pdlc/changes/CH-0006-todo-app/frames/15.png
- pdlc/changes/CH-0006-todo-app/frames/16.png
