# Review packet · CH-0006-todo-app · tests

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: tests

Default: yes

You judge the checks, not the code. You do not see the code.

- Each acceptance line in the change spec has at least one check. The exception is a
  requirement marked `(retire)`: its acceptance lines get no check, and each check that
  only proved them is gone.
- The name of each check contains its requirement ID.
- Each check tests what the acceptance line says, through the "Interface" in the change
  spec. The check does not depend on the internal structure of the code.
- A check proves each line at the surface that the line names. A line where the person
  clicks needs a check that clicks and reads what shows. A check of a helper below that
  surface is not sufficient alone.
- If a line depends on the current date or time, its check sets the clock through a seam
  at that same surface. A check that runs a command sets the clock with the environment
  variable or flag of the command. Each of these is a problem:
  - Using the real clock, or setting the time only on a helper below the surface.
  - Leaving a line to manual checks when a stand-in seam could reach the surface.
- Each check can fail. It fails on a plausible wrong version of the code (see "Wrong
  versions"). A check that passes, whatever the code does, does not count.
- A correct app passes each check. A fake that stands in for something real must use the
  names and the behaviour of the real thing. Then the check does not prove only the fake.
- No check can hang. Anything that waits for the app has a time limit.
- Each error code that the Interface lists has a check that expects that exact code.
- The checks control time, randomness, and vendors only through the "Test seams" of the
  Interface.
- The checks are sufficiently clear that a failure tells you what broke.

## Wrong versions

For each check, name one plausible wrong version of the code. Decide whether the check
fails on that version. Write one line for each check under "Wrong versions" in your
result: the check, the wrong version, and `fails` or `passes`. A check that passes a
plausible wrong version is a problem. Name the value or line that catches the wrong
version.

Look for these traps:

- Examples that also fit a wrong rule. These are common cases:
  - A rule about order, tested with items that are already in that order or in
    alphabetical order.
  - Values that come out the same if the code rounds up, rounds down, or rounds to the
    nearest value.
  - Text that is already in the case that the rule asks for.
- A limit that the check does not test exactly at the edge and one past the edge. For "at
  most 20", the value 20 passes and the value 21 fails. Values near the edge prove nothing
  about the edge.
- A rule about every member of a class that the check tests without a non-member.
  "Any other value is an error" needs a value close to an allowed value, not only a few
  far-off values. A close value differs by its case, a space, or a prefix. "Only the
  owner may" needs a person who is not the owner.

## The change spec

````
# CH-0006 · Todo app

Intent: IN-0001 · Scope: CAP-todo-app · Status: building · Ticket: none · Branch: pdlc/CH-0006-todo-app · Base: pdlc/CH-0005-checks
Tests-Locked: dfc59556260396720b743eb4ff82ef074705f7b9

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

Status: ready · Intents: IN-0001

- Given the todo app, when the engine describes its models, then the models are `User`,
  `TodoList`, and `Todo`, with the fields of the spec.
- Given the todo app. When the engine reads its type defaults, then `Title` is text 200,
  `Notes` is text 5000, `TimeZoneId` is text 64. Also `ExternalId` is text 255, unique,
  and hidden.

### CAP-todo-app.R2 · The three access rules of the spec

Status: ready · Intents: IN-0001

- Given the todo app. When a caller reads its access rules, then `User` is `self`,
  `TodoList` is `owned`, and `Todo` is `ownedVia List`, read-only when the list is
  `Archived`.

### CAP-todo-app.R3 · The overrides of the spec

Status: ready · Intents: IN-0001

- Given the todo app, when a caller reads the overrides of `Todo`, then they are
  `readOnly CompletedAt`, `listEdit Done`, `listSort Due Ascending`, and `listColumns
  [Title; Due; Priority; Done]`.
- Given the todo app. When a caller reads the overrides of `TodoList`, then they are the
  views "Active" (not `Archived`) and "Archived" (`Archived`), in that order.

### CAP-todo-app.R4 · Ticking a todo sets its completion time

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

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

## The checks

### Diff against pdlc/CH-0005-checks

````
diff --git a/tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs b/tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs
new file mode 100644
index 0000000..8aac4f6
--- /dev/null
+++ b/tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs
@@ -0,0 +1,336 @@
+namespace Typed.Engine.Tests
+
+open System
+open System.IO
+open Xunit
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+open Typed.Engine.Access
+open Typed.Engine.Checks
+
+// Short names for the app modules. The app type names (Todo, User) must not hide the namespace.
+module AppDefaults = Todo.App.Defaults
+module AppSchema = Todo.App.Schema
+
+module TodoAppFixtures =
+
+    open Todo.App.Model
+
+    let describeApp (t: Type) : Model =
+        match describe AppDefaults.typeDefaults t with
+        | Ok m -> m
+        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs
+
+    let fieldOf (m: Model) (name: string) : Field =
+        m.Fields |> List.find (fun f -> f.Name = name)
+
+    let defaultFor (t: Type) : TypeDefault =
+        AppDefaults.typeDefaults |> List.find (fun d -> d.Clr = t)
+
+    let typeDefaultsFor () =
+        defaultFor typeof<Title>, defaultFor typeof<Notes>, defaultFor typeof<TimeZoneId>, defaultFor typeof<ExternalId>
+
+    let ruleFor (name: string) : AccessRule =
+        AppSchema.access |> List.find (fun r -> r.ModelName = name)
+
+    let setFor (name: string) : ModelOverrides =
+        AppSchema.overrideSets |> List.find (fun s -> s.ModelName = name)
+
+open TodoAppFixtures
+
+module ``CAP-todo-app-R1`` =
+
+    open Todo.App.Model
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 the models are User then TodoList then Todo`` () =
+        Assert.Equal<string list>([ "User"; "TodoList"; "Todo" ], AppDefaults.models |> List.map (fun t -> t.Name))
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 User has the fields Subject, DisplayName, and TimeZone`` () =
+        let m = describeApp typeof<User>
+        Assert.Equal<string list>([ "Subject"; "DisplayName"; "TimeZone" ], m.Fields |> List.map (fun f -> f.Name))
+        Assert.Equal(Text 255, (fieldOf m "Subject").Kind)
+        Assert.Equal(Text 200, (fieldOf m "DisplayName").Kind)
+        Assert.Equal(Text 64, (fieldOf m "TimeZone").Kind)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 TodoList has the fields Name and Archived`` () =
+        let m = describeApp typeof<TodoList>
+        Assert.Equal<string list>([ "Name"; "Archived" ], m.Fields |> List.map (fun f -> f.Name))
+        Assert.Equal(Text 200, (fieldOf m "Name").Kind)
+        Assert.Equal(Boolean, (fieldOf m "Archived").Kind)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 Todo has its seven fields with their kinds`` () =
+        let m = describeApp typeof<Todo>
+        Assert.Equal<string list>(
+            [ "List"; "Title"; "Notes"; "Priority"; "Due"; "Done"; "CompletedAt" ],
+            m.Fields |> List.map (fun f -> f.Name)
+        )
+        Assert.Equal(Reference "TodoList", (fieldOf m "List").Kind)
+        Assert.Equal(Text 200, (fieldOf m "Title").Kind)
+        Assert.Equal(Optional(Text 5000), (fieldOf m "Notes").Kind)
+        Assert.Equal(Enum [ "Low"; "Normal"; "High" ], (fieldOf m "Priority").Kind)
+        Assert.Equal(Optional Date, (fieldOf m "Due").Kind)
+        Assert.Equal(Boolean, (fieldOf m "Done").Kind)
+        Assert.Equal(Optional Timestamp, (fieldOf m "CompletedAt").Kind)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 Title is text 200, not hidden, not unique`` () =
+        let d, _, _, _ = typeDefaultsFor ()
+        Assert.Equal(Text 200, d.Kind)
+        Assert.False(d.Hidden)
+        Assert.False(d.Unique)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 Notes is text 5000, not hidden, not unique`` () =
+        let _, d, _, _ = typeDefaultsFor ()
+        Assert.Equal(Text 5000, d.Kind)
+        Assert.False(d.Hidden)
+        Assert.False(d.Unique)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 TimeZoneId is text 64, not hidden, not unique`` () =
+        let _, _, d, _ = typeDefaultsFor ()
+        Assert.Equal(Text 64, d.Kind)
+        Assert.False(d.Hidden)
+        Assert.False(d.Unique)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 ExternalId is text 255, unique, and hidden`` () =
+        let _, _, _, d = typeDefaultsFor ()
+        Assert.Equal(Text 255, d.Kind)
+        Assert.True(d.Hidden)
+        Assert.True(d.Unique)
+
+    [<Fact>]
+    let ``CAP-todo-app.R1 the type defaults hold exactly the four types with a default`` () =
+        let clrs = AppDefaults.typeDefaults |> List.map (fun d -> d.Clr.Name) |> List.sort
+        Assert.Equal<string list>([ "ExternalId"; "Notes"; "TimeZoneId"; "Title" ], clrs)
+
+module ``CAP-todo-app-R2`` =
+
+    open Todo.App.Model
+
+    [<Fact>]
+    let ``CAP-todo-app.R2 there are exactly three access rules`` () =
+        Assert.Equal<string list>(
+            [ "Todo"; "TodoList"; "User" ],
+            AppSchema.access |> List.map (fun r -> r.ModelName) |> List.sort
+        )
+
+    [<Fact>]
+    let ``CAP-todo-app.R2 User is self`` () =
+        Assert.Equal(Self, (ruleFor "User").Access)
+
+    [<Fact>]
+    let ``CAP-todo-app.R2 TodoList is owned`` () =
+        Assert.Equal(Owned, (ruleFor "TodoList").Access)
+
+    [<Fact>]
+    let ``CAP-todo-app.R2 Todo is ownedVia List and read-only when the list is Archived`` () =
+        Assert.Equal(OwnedVia("List", "TodoList", Some("TodoList", "Archived")), (ruleFor "Todo").Access)
+
+module ``CAP-todo-app-R3`` =
+
+    [<Fact>]
+    let ``CAP-todo-app.R3 the overrides of Todo are the four of the spec`` () =
+        let items = (setFor "Todo").Items
+        Assert.Equal(4, items.Length)
+        // Compare names, directions, and column lists. Never compare OverrideItem values: they hold quotations.
+        let sorts =
+            items
+            |> List.choose (fun i ->
+                match i with
+                | ListSort(f, d) -> Some(f, d)
+                | _ -> None)
+        Assert.Equal<(string * Direction) list>([ ("Due", Ascending) ], sorts)
+        let columns =
+            items
+            |> List.choose (fun i ->
+                match i with
+                | ListColumns fs -> Some fs
+                | _ -> None)
+        Assert.Equal<string list list>([ [ "Title"; "Due"; "Priority"; "Done" ] ], columns)
+        // The two other items are readOnly CompletedAt and listEdit Done. Match on the case, so a swap fails.
+        let readOnlys =
+            items
+            |> List.choose (fun i ->
+                match i with
+                | ReadOnly f -> Some f
+                | _ -> None)
+        Assert.Equal<string list>([ "CompletedAt" ], readOnlys)
+        let listEdits =
+            items
+            |> List.choose (fun i ->
+                match i with
+                | ListEdit f -> Some f
+                | _ -> None)
+        Assert.Equal<string list>([ "Done" ], listEdits)
+
+    [<Fact>]
+    let ``CAP-todo-app.R3 listSort of Todo is Due ascending`` () =
+        let sorts =
+            (setFor "Todo").Items
+            |> List.filter (fun i ->
+                match i with
+                | ListSort _ -> true
+                | _ -> false)
+        match sorts with
+        | [ ListSort(f, d) ] ->
+            Assert.Equal("Due", f)
+            Assert.Equal(Ascending, d)
+        | other -> failwithf "Expected one ListSort item, got %d" other.Length
+
+    [<Fact>]
+    let ``CAP-todo-app.R3 listColumns of Todo are Title, Due, Priority, Done in that order`` () =
+        let cols =
+            (setFor "Todo").Items
+            |> List.choose (fun i ->
+                match i with
+                | ListColumns fs -> Some fs
+                | _ -> None)
+        Assert.Equal<string list list>([ [ "Title"; "Due"; "Priority"; "Done" ] ], cols)
+
+    [<Fact>]
+    let ``CAP-todo-app.R3 the overrides of TodoList are the views Active then Archived`` () =
+        let items = (setFor "TodoList").Items
+        let names =
+            items
+            |> List.map (fun i ->
+                match i with
+                | ListView(n, _) -> n
+                | other -> failwithf "Expected a view, got %A" other)
+        Assert.Equal<string list>([ "Active"; "Archived" ], names)
+        for i in items do
+            Assert.Equal<string list>([ "Archived" ], fieldsOf i)
+
+    [<Fact>]
+    let ``CAP-todo-app.R3 the Active view is not Archived and the Archived view is Archived`` () =
+        let views =
+            (setFor "TodoList").Items
+            |> List.map (fun i ->
+                match i with
+                | ListView(n, q) -> n, (sprintf "%A" q)
+                | other -> failwithf "Expected a view, got %A" other)
+        let (activeName, activeText), (archivedName, archivedText) =
+            match views with
+            | [ a; b ] -> a, b
+            | other -> failwithf "Expected two views, got %d" other.Length
+        Assert.Equal("Active", activeName)
+        Assert.Equal("Archived", archivedName)
+        // Both views read only Archived. The Active quotation also applies not, the Archived one does not.
+        Assert.Contains("Archived", activeText)
+        Assert.Contains("Archived", archivedText)
+        Assert.Contains("Not", activeText)
+        Assert.DoesNotContain("Not", archivedText)
+        Assert.NotEqual<string>(activeText, archivedText)
+
+module HookFixtures =
+
+    open Todo.App.Model
+
+    let at (h: int) : DateTimeOffset = DateTimeOffset(2026, 10, 18, h, 0, 0, TimeSpan.Zero)
+
+    let contextAt (zone: string) (now: DateTimeOffset) : HookContext =
+        { User = Guid.NewGuid()
+          TimeZone = TimeZoneInfo.FindSystemTimeZoneById zone
+          Now = now }
+
+    let todo (done': bool) (completedAt: DateTimeOffset option) (due: DateOnly option) : Todo =
+        { List = Ref(Guid.NewGuid())
+          Title = Title "Buy milk"
+          Notes = None
+          Priority = Normal
+          Due = due
+          Done = done'
+          CompletedAt = completedAt }
+
+open HookFixtures
+
+module ``CAP-todo-app-R4`` =
+
+    open Todo.App.Model
+
+    [<Fact>]
+    let ``CAP-todo-app.R4 ticking a todo sets CompletedAt to now`` () =
+        let ctx = contextAt "UTC" (at 9)
+        let previous = todo false None None
+        let saved = todo true None None
+        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
+        Assert.Equal(Some(at 9), result.CompletedAt)
+        Assert.True(result.Done)
+
+    [<Fact>]
+    let ``CAP-todo-app.R4 unticking a todo empties CompletedAt`` () =
+        let ctx = contextAt "UTC" (at 12)
+        let previous = todo true (Some(at 9)) None
+        // The saved todo still carries the old time, so the hook must clear it.
+        let saved = todo false (Some(at 9)) None
+        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
+        Assert.Equal(None, result.CompletedAt)
+        Assert.False(result.Done)
+
+    [<Fact>]
+    let ``CAP-todo-app.R4 saving a todo that stays done keeps the first CompletedAt`` () =
+        let ctx = contextAt "UTC" (at 12)
+        let previous = todo true (Some(at 9)) None
+        let saved = todo true (Some(at 9)) None
+        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
+        Assert.Equal(Some(at 9), result.CompletedAt)
+
+module ``CAP-todo-app-R5`` =
+
+    open Todo.App.Model
+
+    // 2026-10-18T14:00:00Z is 2026-10-19 03:00 in Auckland (UTC+13). It is still 2026-10-18 in UTC.
+    let private ctx () = contextAt "Pacific/Auckland" (at 14)
+
+    [<Fact>]
+    let ``CAP-todo-app.R5 an open todo due on the Auckland yesterday is overdue`` () =
+        let t = todo false None (Some(DateOnly(2026, 10, 18)))
+        Assert.True(Todo.App.Hooks.isOverdue (ctx ()) t)
+
+    [<Fact>]
+    let ``CAP-todo-app.R5 a done todo due on the Auckland yesterday is not overdue`` () =
+        let t = todo true (Some(at 9)) (Some(DateOnly(2026, 10, 18)))
+        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)
+
+    [<Fact>]
+    let ``CAP-todo-app.R5 an open todo due on the Auckland today is not overdue`` () =
+        let t = todo false None (Some(DateOnly(2026, 10, 19)))
+        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)
+
+    [<Fact>]
+    let ``CAP-todo-app.R5 an open todo with no due date is not overdue`` () =
+        let t = todo false None None
+        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)
+
+module ``CAP-todo-app-R6`` =
+
+    let private appFolder () : string =
+        let rec up (dir: DirectoryInfo) =
+            if isNull dir then
+                failwith "Folder src/Todo.App not found above the test run folder."
+            else
+                let candidate = Path.Combine(dir.FullName, "src", "Todo.App")
+                if Directory.Exists candidate then candidate else up dir.Parent
+        up (DirectoryInfo AppContext.BaseDirectory)
+
+    [<Fact>]
+    let ``CAP-todo-app.R6 the source files are Model, Defaults, Schema, and Hooks`` () =
+        let root = appFolder ()
+        let files =
+            Directory.GetFiles(root, "*.fs", SearchOption.AllDirectories)
+            |> Array.map (fun p -> Path.GetRelativePath(root, p))
+            // Build output is not source.
+            |> Array.filter (fun p -> not (p.StartsWith "obj" || p.StartsWith "bin"))
+            |> Array.sort
+            |> List.ofArray
+        Assert.Equal<string list>([ "Defaults.fs"; "Hooks.fs"; "Model.fs"; "Schema.fs" ], files)
+
+    [<Fact>]
+    let ``CAP-todo-app.R6 the engine checks find no error in the models, rules, and overrides`` () =
+        let fs = run AppDefaults.typeDefaults AppDefaults.models AppSchema.access AppSchema.overrideSets
+        Assert.Empty(errors fs)
diff --git a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
index 8cf3c47..9f38d5e 100644
--- a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
+++ b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
@@ -11,6 +11,7 @@
     <Compile Include="CAP-validate.Tests.fs" />
     <Compile Include="CAP-persist.Tests.fs" />
     <Compile Include="CAP-checks.Tests.fs" />
+    <Compile Include="CAP-todo-app.Tests.fs" />
   </ItemGroup>
 
   <ItemGroup>
@@ -22,6 +23,7 @@
 
   <ItemGroup>
     <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
+    <ProjectReference Include="../../src/Todo.App/Todo.App.fsproj" />
   </ItemGroup>
 
 </Project>
````

### tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs

````
namespace Typed.Engine.Tests

open System
open System.IO
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Checks

// Short names for the app modules. The app type names (Todo, User) must not hide the namespace.
module AppDefaults = Todo.App.Defaults
module AppSchema = Todo.App.Schema

module TodoAppFixtures =

    open Todo.App.Model

    let describeApp (t: Type) : Model =
        match describe AppDefaults.typeDefaults t with
        | Ok m -> m
        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs

    let fieldOf (m: Model) (name: string) : Field =
        m.Fields |> List.find (fun f -> f.Name = name)

    let defaultFor (t: Type) : TypeDefault =
        AppDefaults.typeDefaults |> List.find (fun d -> d.Clr = t)

    let typeDefaultsFor () =
        defaultFor typeof<Title>, defaultFor typeof<Notes>, defaultFor typeof<TimeZoneId>, defaultFor typeof<ExternalId>

    let ruleFor (name: string) : AccessRule =
        AppSchema.access |> List.find (fun r -> r.ModelName = name)

    let setFor (name: string) : ModelOverrides =
        AppSchema.overrideSets |> List.find (fun s -> s.ModelName = name)

open TodoAppFixtures

module ``CAP-todo-app-R1`` =

    open Todo.App.Model

    [<Fact>]
    let ``CAP-todo-app.R1 the models are User then TodoList then Todo`` () =
        Assert.Equal<string list>([ "User"; "TodoList"; "Todo" ], AppDefaults.models |> List.map (fun t -> t.Name))

    [<Fact>]
    let ``CAP-todo-app.R1 User has the fields Subject, DisplayName, and TimeZone`` () =
        let m = describeApp typeof<User>
        Assert.Equal<string list>([ "Subject"; "DisplayName"; "TimeZone" ], m.Fields |> List.map (fun f -> f.Name))
        Assert.Equal(Text 255, (fieldOf m "Subject").Kind)
        Assert.Equal(Text 200, (fieldOf m "DisplayName").Kind)
        Assert.Equal(Text 64, (fieldOf m "TimeZone").Kind)

    [<Fact>]
    let ``CAP-todo-app.R1 TodoList has the fields Name and Archived`` () =
        let m = describeApp typeof<TodoList>
        Assert.Equal<string list>([ "Name"; "Archived" ], m.Fields |> List.map (fun f -> f.Name))
        Assert.Equal(Text 200, (fieldOf m "Name").Kind)
        Assert.Equal(Boolean, (fieldOf m "Archived").Kind)

    [<Fact>]
    let ``CAP-todo-app.R1 Todo has its seven fields with their kinds`` () =
        let m = describeApp typeof<Todo>
        Assert.Equal<string list>(
            [ "List"; "Title"; "Notes"; "Priority"; "Due"; "Done"; "CompletedAt" ],
            m.Fields |> List.map (fun f -> f.Name)
        )
        Assert.Equal(Reference "TodoList", (fieldOf m "List").Kind)
        Assert.Equal(Text 200, (fieldOf m "Title").Kind)
        Assert.Equal(Optional(Text 5000), (fieldOf m "Notes").Kind)
        Assert.Equal(Enum [ "Low"; "Normal"; "High" ], (fieldOf m "Priority").Kind)
        Assert.Equal(Optional Date, (fieldOf m "Due").Kind)
        Assert.Equal(Boolean, (fieldOf m "Done").Kind)
        Assert.Equal(Optional Timestamp, (fieldOf m "CompletedAt").Kind)

    [<Fact>]
    let ``CAP-todo-app.R1 Title is text 200, not hidden, not unique`` () =
        let d, _, _, _ = typeDefaultsFor ()
        Assert.Equal(Text 200, d.Kind)
        Assert.False(d.Hidden)
        Assert.False(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 Notes is text 5000, not hidden, not unique`` () =
        let _, d, _, _ = typeDefaultsFor ()
        Assert.Equal(Text 5000, d.Kind)
        Assert.False(d.Hidden)
        Assert.False(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 TimeZoneId is text 64, not hidden, not unique`` () =
        let _, _, d, _ = typeDefaultsFor ()
        Assert.Equal(Text 64, d.Kind)
        Assert.False(d.Hidden)
        Assert.False(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 ExternalId is text 255, unique, and hidden`` () =
        let _, _, _, d = typeDefaultsFor ()
        Assert.Equal(Text 255, d.Kind)
        Assert.True(d.Hidden)
        Assert.True(d.Unique)

    [<Fact>]
    let ``CAP-todo-app.R1 the type defaults hold exactly the four types with a default`` () =
        let clrs = AppDefaults.typeDefaults |> List.map (fun d -> d.Clr.Name) |> List.sort
        Assert.Equal<string list>([ "ExternalId"; "Notes"; "TimeZoneId"; "Title" ], clrs)

module ``CAP-todo-app-R2`` =

    open Todo.App.Model

    [<Fact>]
    let ``CAP-todo-app.R2 there are exactly three access rules`` () =
        Assert.Equal<string list>(
            [ "Todo"; "TodoList"; "User" ],
            AppSchema.access |> List.map (fun r -> r.ModelName) |> List.sort
        )

    [<Fact>]
    let ``CAP-todo-app.R2 User is self`` () =
        Assert.Equal(Self, (ruleFor "User").Access)

    [<Fact>]
    let ``CAP-todo-app.R2 TodoList is owned`` () =
        Assert.Equal(Owned, (ruleFor "TodoList").Access)

    [<Fact>]
    let ``CAP-todo-app.R2 Todo is ownedVia List and read-only when the list is Archived`` () =
        Assert.Equal(OwnedVia("List", "TodoList", Some("TodoList", "Archived")), (ruleFor "Todo").Access)

module ``CAP-todo-app-R3`` =

    [<Fact>]
    let ``CAP-todo-app.R3 the overrides of Todo are the four of the spec`` () =
        let items = (setFor "Todo").Items
        Assert.Equal(4, items.Length)
        // Compare names, directions, and column lists. Never compare OverrideItem values: they hold quotations.
        let sorts =
            items
            |> List.choose (fun i ->
                match i with
                | ListSort(f, d) -> Some(f, d)
                | _ -> None)
        Assert.Equal<(string * Direction) list>([ ("Due", Ascending) ], sorts)
        let columns =
            items
            |> List.choose (fun i ->
                match i with
                | ListColumns fs -> Some fs
                | _ -> None)
        Assert.Equal<string list list>([ [ "Title"; "Due"; "Priority"; "Done" ] ], columns)
        // The two other items are readOnly CompletedAt and listEdit Done. Match on the case, so a swap fails.
        let readOnlys =
            items
            |> List.choose (fun i ->
                match i with
                | ReadOnly f -> Some f
                | _ -> None)
        Assert.Equal<string list>([ "CompletedAt" ], readOnlys)
        let listEdits =
            items
            |> List.choose (fun i ->
                match i with
                | ListEdit f -> Some f
                | _ -> None)
        Assert.Equal<string list>([ "Done" ], listEdits)

    [<Fact>]
    let ``CAP-todo-app.R3 listSort of Todo is Due ascending`` () =
        let sorts =
            (setFor "Todo").Items
            |> List.filter (fun i ->
                match i with
                | ListSort _ -> true
                | _ -> false)
        match sorts with
        | [ ListSort(f, d) ] ->
            Assert.Equal("Due", f)
            Assert.Equal(Ascending, d)
        | other -> failwithf "Expected one ListSort item, got %d" other.Length

    [<Fact>]
    let ``CAP-todo-app.R3 listColumns of Todo are Title, Due, Priority, Done in that order`` () =
        let cols =
            (setFor "Todo").Items
            |> List.choose (fun i ->
                match i with
                | ListColumns fs -> Some fs
                | _ -> None)
        Assert.Equal<string list list>([ [ "Title"; "Due"; "Priority"; "Done" ] ], cols)

    [<Fact>]
    let ``CAP-todo-app.R3 the overrides of TodoList are the views Active then Archived`` () =
        let items = (setFor "TodoList").Items
        let names =
            items
            |> List.map (fun i ->
                match i with
                | ListView(n, _) -> n
                | other -> failwithf "Expected a view, got %A" other)
        Assert.Equal<string list>([ "Active"; "Archived" ], names)
        for i in items do
            Assert.Equal<string list>([ "Archived" ], fieldsOf i)

    [<Fact>]
    let ``CAP-todo-app.R3 the Active view is not Archived and the Archived view is Archived`` () =
        let views =
            (setFor "TodoList").Items
            |> List.map (fun i ->
                match i with
                | ListView(n, q) -> n, (sprintf "%A" q)
                | other -> failwithf "Expected a view, got %A" other)
        let (activeName, activeText), (archivedName, archivedText) =
            match views with
            | [ a; b ] -> a, b
            | other -> failwithf "Expected two views, got %d" other.Length
        Assert.Equal("Active", activeName)
        Assert.Equal("Archived", archivedName)
        // Both views read only Archived. The Active quotation also applies not, the Archived one does not.
        Assert.Contains("Archived", activeText)
        Assert.Contains("Archived", archivedText)
        Assert.Contains("Not", activeText)
        Assert.DoesNotContain("Not", archivedText)
        Assert.NotEqual<string>(activeText, archivedText)

module HookFixtures =

    open Todo.App.Model

    let at (h: int) : DateTimeOffset = DateTimeOffset(2026, 10, 18, h, 0, 0, TimeSpan.Zero)

    let contextAt (zone: string) (now: DateTimeOffset) : HookContext =
        { User = Guid.NewGuid()
          TimeZone = TimeZoneInfo.FindSystemTimeZoneById zone
          Now = now }

    let todo (done': bool) (completedAt: DateTimeOffset option) (due: DateOnly option) : Todo =
        { List = Ref(Guid.NewGuid())
          Title = Title "Buy milk"
          Notes = None
          Priority = Normal
          Due = due
          Done = done'
          CompletedAt = completedAt }

open HookFixtures

module ``CAP-todo-app-R4`` =

    open Todo.App.Model

    [<Fact>]
    let ``CAP-todo-app.R4 ticking a todo sets CompletedAt to now`` () =
        let ctx = contextAt "UTC" (at 9)
        let previous = todo false None None
        let saved = todo true None None
        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
        Assert.Equal(Some(at 9), result.CompletedAt)
        Assert.True(result.Done)

    [<Fact>]
    let ``CAP-todo-app.R4 unticking a todo empties CompletedAt`` () =
        let ctx = contextAt "UTC" (at 12)
        let previous = todo true (Some(at 9)) None
        // The saved todo still carries the old time, so the hook must clear it.
        let saved = todo false (Some(at 9)) None
        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
        Assert.Equal(None, result.CompletedAt)
        Assert.False(result.Done)

    [<Fact>]
    let ``CAP-todo-app.R4 saving a todo that stays done keeps the first CompletedAt`` () =
        let ctx = contextAt "UTC" (at 12)
        let previous = todo true (Some(at 9)) None
        let saved = todo true (Some(at 9)) None
        let result = Todo.App.Hooks.completionTime ctx (Some previous) saved
        Assert.Equal(Some(at 9), result.CompletedAt)

module ``CAP-todo-app-R5`` =

    open Todo.App.Model

    // 2026-10-18T14:00:00Z is 2026-10-19 03:00 in Auckland (UTC+13). It is still 2026-10-18 in UTC.
    let private ctx () = contextAt "Pacific/Auckland" (at 14)

    [<Fact>]
    let ``CAP-todo-app.R5 an open todo due on the Auckland yesterday is overdue`` () =
        let t = todo false None (Some(DateOnly(2026, 10, 18)))
        Assert.True(Todo.App.Hooks.isOverdue (ctx ()) t)

    [<Fact>]
    let ``CAP-todo-app.R5 a done todo due on the Auckland yesterday is not overdue`` () =
        let t = todo true (Some(at 9)) (Some(DateOnly(2026, 10, 18)))
        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)

    [<Fact>]
    let ``CAP-todo-app.R5 an open todo due on the Auckland today is not overdue`` () =
        let t = todo false None (Some(DateOnly(2026, 10, 19)))
        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)

    [<Fact>]
    let ``CAP-todo-app.R5 an open todo with no due date is not overdue`` () =
        let t = todo false None None
        Assert.False(Todo.App.Hooks.isOverdue (ctx ()) t)

module ``CAP-todo-app-R6`` =

    let private appFolder () : string =
        let rec up (dir: DirectoryInfo) =
            if isNull dir then
                failwith "Folder src/Todo.App not found above the test run folder."
            else
                let candidate = Path.Combine(dir.FullName, "src", "Todo.App")
                if Directory.Exists candidate then candidate else up dir.Parent
        up (DirectoryInfo AppContext.BaseDirectory)

    [<Fact>]
    let ``CAP-todo-app.R6 the source files are Model, Defaults, Schema, and Hooks`` () =
        let root = appFolder ()
        let files =
            Directory.GetFiles(root, "*.fs", SearchOption.AllDirectories)
            |> Array.map (fun p -> Path.GetRelativePath(root, p))
            // Build output is not source.
            |> Array.filter (fun p -> not (p.StartsWith "obj" || p.StartsWith "bin"))
            |> Array.sort
            |> List.ofArray
        Assert.Equal<string list>([ "Defaults.fs"; "Hooks.fs"; "Model.fs"; "Schema.fs" ], files)

    [<Fact>]
    let ``CAP-todo-app.R6 the engine checks find no error in the models, rules, and overrides`` () =
        let fs = run AppDefaults.typeDefaults AppDefaults.models AppSchema.access AppSchema.overrideSets
        Assert.Empty(errors fs)
````

### tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="CAP-models.Tests.fs" />
    <Compile Include="CAP-access.Tests.fs" />
    <Compile Include="CAP-validate.Tests.fs" />
    <Compile Include="CAP-persist.Tests.fs" />
    <Compile Include="CAP-checks.Tests.fs" />
    <Compile Include="CAP-todo-app.Tests.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.11" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
    <ProjectReference Include="../../src/Todo.App/Todo.App.fsproj" />
  </ItemGroup>

</Project>
````

