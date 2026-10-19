# Review packet · CH-0007-generate · tests

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
# CH-0007 · Generate

Intent: IN-0001 · Scope: JOB-generate · Status: building · Ticket: none · Branch: pdlc/CH-0007-generate · Base: pdlc/CH-0006-todo-app
Tests-Locked: 17b1275323714dd91de1497c6b2c520b5cff3f7c

## Delivers

An app builder runs one command that prints every finding, sets the exit code, and writes the schema. The README tells how to run the generator and the checks.

## Requirements

### JOB-generate.R1 · The command prints each finding and sets the exit code

- Given the todo app, when the builder runs `generate`, then the output holds no `error`
  line and the exit code is 0.
- Given the todo app with the access rule of `Todo` removed. When the builder runs
  `generate`, then the output holds the line "error MISSING_ACCESS_RULE Model Todo has no
  access rule." and the exit code is 1.
- Given the todo app with the override `unsafe "rawName" "uses raw SQL"` on `TodoList`.
  When the builder runs `generate`, then the output holds the line "warning
  UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL". The exit
  code is 0.
- Given the command `frobnicate`. When the builder runs it, then the output holds "usage:
  generate" and the exit code is 2. (assumed)
- Given the todo app, when the builder runs `generate`, then each finding line goes to
  standard output. (assumed)

### JOB-generate.R2 · The command writes the schema when there is no error

- Given the todo app. When the builder runs `generate`, then the file `build/schema.sql`
  holds the tables, the foreign keys, and the indexes of the three models. (assumed)
- Given the todo app with the access rule of `Todo` removed, when the builder runs
  `generate`, then `build/schema.sql` is not written.
- Given no `build/` folder, when the builder runs `generate`, then the command creates the
  folder. (assumed)

### JOB-generate.R3 · The README tells how to run the generator and the checks

- Given the README, when a reader looks for the generator, then the README gives the
  `generate` command and the meaning of its exit codes.
- Given the README, when a reader looks for the checks, then the README gives the command
  `dotnet test tests/`.

## Relies on

- CAP-models (used, not changed)
- CAP-checks (used, not changed)
- CAP-access (used, not changed)
- CAP-persist (used, not changed)
- CAP-todo-app (used, not changed)
- DES-names
- DES-errors
- DES-test-database
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

The command, in `src/Typed.Engine.Generator/Program.fs`:

- `dotnet run --project src/Typed.Engine.Generator -- generate [--out <folder>]`. The default folder is `build/`. (assumed)
- Output: one line for each finding on standard output, as `<severity> <code> <message>`, where severity is `error` or `warning`.
- Exit codes: 0 (no error finding), 1 (at least one error finding), 2 (unknown command, after the line "usage: generate [--out <folder>]").
- Files written on exit 0: `<folder>/schema.sql` (the SQL Server schema) and `<folder>/shape.json` (the stored shape).

The library entry that the command calls, in `src/Typed.Engine.Generator/Generator.fs`, module `Typed.Engine.Generator`:

- `type Config = { Defaults: TypeDefault list; Models: Type list; Rules: AccessRule list; Overrides: ModelOverrides list; OutDir: string }`
- `run: Config -> TextWriter -> int`: runs the checks, writes the lines, writes the files, and gives the exit code. The checks call `run` with a changed `Config` to prove the lines about a removed rule or an added `unsafe` override. (assumed)
- `todoConfig: outDir: string -> Config`: the config of the todo app, which `Program.main` uses.
- `execute: args: string[] -> config: Config -> stdout: TextWriter -> stderr: TextWriter -> int`, in `Generator.fs`. It parses the arguments, runs `run` with `stdout` for the finding lines and for the usage line, and gives the exit code. `stderr` stays empty. `Program.main` calls `execute argv (todoConfig out) Console.Out Console.Error`. (assumed)

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

- output folder: the `--out` flag, or `Config.OutDir`. The checks use a temporary folder.
- the app under test: `Generator.run` takes a `Config`. A check can remove a rule or add an override without a change to the app code. The README names `--out`. (assumed)

## Files

- src/Typed.Engine.Generator/Program.fs
- src/Typed.Engine.Generator/Generator.fs (assumed)
- src/Typed.Engine.Generator/Typed.Engine.Generator.fsproj (assumed)
- README.md

## Check files

- tests/Typed.Engine.Tests/JOB-generate.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

## Steps

### Step 1 · The command prints each finding and sets the exit code · [ ]

- Requirement: JOB-generate.R1
- Check: the checks named `JOB-generate.R1 ...` in `tests/Typed.Engine.Tests/JOB-generate.Tests.fs`
- Done when: every check of JOB-generate.R1 (the command prints each finding and sets the exit code) passes

### Step 2 · The command writes the schema when there is no error · [ ]

- Requirement: JOB-generate.R2
- Check: the checks named `JOB-generate.R2 ...` in `tests/Typed.Engine.Tests/JOB-generate.Tests.fs`
- Done when: every check of JOB-generate.R2 (the command writes the schema when there is no error) passes

### Step 3 · The README tells how to run the generator and the checks · [ ]

- Requirement: JOB-generate.R3
- Check: the checks named `JOB-generate.R3 ...` in `tests/Typed.Engine.Tests/JOB-generate.Tests.fs`
- Done when: every check of JOB-generate.R3 (the readme tells how to run the generator and the checks) passes

### Step 4 · README · [ ]

- Requirement: JOB-generate.R3
- Check: the checks named `JOB-generate.R3 ...`
- Done when: the README tells what this change adds, names the `--out` seam, and gives the commands `dotnet run --project src/Typed.Engine.Generator -- generate` and `dotnet test tests/Typed.Engine.Tests`

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `JOB-generate` as the ready stage left them.
- Sent back to tests: the checks hung on dotnet run. They now start the built dll.
````

## The spec it covers: title and summary

````
# JOB-generate · Run the build checks and write the schema from the command line

Status: active

## Who and why

An app builder, or CI, runs the generator after a change to the models. The generator
loads the models, the access rules, and the overrides. Then it runs every build check,
prints each finding, and writes the schema. The build fails when any finding is an error.

## Relies on

- Capabilities: CAP-models, CAP-checks, CAP-access, CAP-persist, CAP-todo-app
- Design: DES-errors, DES-names, DES-test-database

## Code map

- src/Typed.Engine.Generator/Program.fs: the `generate` command
- README.md: how to run the generator and the checks
````

## The requirements, in full

### JOB-generate.R1 · The command prints each finding and sets the exit code

Status: ready · Intents: IN-0001

The command is `dotnet run --project src/Typed.Engine.Generator -- generate` (assumed).
It prints one line for each finding, in the form `<severity> <code> <message>`
(assumed). The exit code is 1 when any finding is an error, and 0 otherwise.

- Given the todo app, when the builder runs `generate`, then the output holds no `error`
  line and the exit code is 0.
- Given the todo app with the access rule of `Todo` removed. When the builder runs
  `generate`, then the output holds the line "error MISSING_ACCESS_RULE Model Todo has no
  access rule." and the exit code is 1.
- Given the todo app with the override `unsafe "rawName" "uses raw SQL"` on `TodoList`.
  When the builder runs `generate`, then the output holds the line "warning
  UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL". The exit
  code is 0.
- Given the command `frobnicate`. When the builder runs it, then the output holds "usage:
  generate" and the exit code is 2. (assumed)
- Given the todo app, when the builder runs `generate`, then each finding line goes to
  standard output. (assumed)

### JOB-generate.R2 · The command writes the schema when there is no error

Status: ready · Intents: IN-0001

- Given the todo app. When the builder runs `generate`, then the file `build/schema.sql`
  holds the tables, the foreign keys, and the indexes of the three models. (assumed)
- Given the todo app with the access rule of `Todo` removed, when the builder runs
  `generate`, then `build/schema.sql` is not written.
- Given no `build/` folder, when the builder runs `generate`, then the command creates the
  folder. (assumed)

### JOB-generate.R3 · The README tells how to run the generator and the checks

Status: ready · Intents: IN-0001

- Given the README, when a reader looks for the generator, then the README gives the
  `generate` command and the meaning of its exit codes.
- Given the README, when a reader looks for the checks, then the README gives the command
  `dotnet test tests/`.

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

### CAP-persist

````
# CAP-persist · Stores and loads records

Status: active

## What it is for

This capability writes the schema, the indexes, and the queries for each model. It stores
and loads records with an owner filter on every query. It pages lists by keyset. It
deletes children with their parent in one transaction. It refuses a stale save. It emits
a migration when the stored shape changes. The stored shape lives in `build/shape.json`
(assumed).

The checks of this capability run on SQLite in memory, as `DES-test-database` says. The
SQL that the capability writes targets SQL Server.

## Code map

- src/Typed.Engine/Persist.fs

## Requirements

### CAP-persist.R1 · Each model gets a table with its system columns

Status: verified · Intents: IN-0001

- Given the model `Todo` and its access rule. When the engine writes the schema, then the
  schema holds `CREATE TABLE [Todo]` with the columns `[Id] uniqueidentifier NOT NULL
  PRIMARY KEY`, `[Owner] uniqueidentifier NOT NULL`, `[Version] rowversion NOT NULL`,
  `[CreatedAt] datetimeoffset NOT NULL`, and `[UpdatedAt] datetimeoffset NOT NULL`.
- Given the model `User` with the rule `self`, when the engine writes the schema, then
  the table `[User]` has no `[Owner]` column.
- Given the model `Todo`. When the engine writes the schema, then `[Title]` is
  `nvarchar(200) NOT NULL`, `[Notes]` is `nvarchar(max) NULL`, `[Priority]` is
  `varchar(16) NOT NULL`, `[Due]` is `date NULL`, and `[CompletedAt]` is `datetimeoffset
  NULL`.
- Given the model `Todo` with `listSort` on `Due`, when the engine writes the schema, then
  the table has the column `[HasDue] AS (CASE WHEN [Due] IS NULL THEN 0 ELSE 1 END)
  PERSISTED`.
- Given the override `column <@ fun u -> u.Subject @> "ExternalSubject"`. When the engine
  writes the schema, then the column is `[ExternalSubject] nvarchar(255) NOT NULL`.
- Given the override `sensitive <@ fun u -> u.Subject @>`, when the engine writes the
  schema, then the schema does not change.

### CAP-persist.R2 · Foreign keys are indexed and never cascade

Status: verified · Intents: IN-0001

- Given the model `Todo`, when the engine writes the schema, then the schema holds
  `ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES
  [TodoList] ([Id]);`.
- Given the three models of the todo app, when the engine writes the schema, then the
  text "CASCADE" does not appear.
- Given the model `Todo`, when the engine derives the indexes, then one index starts with
  the column `List`.

### CAP-persist.R3 · Each list query gets one index

Status: verified · Intents: IN-0001

The index columns are the owner, then the filter fields of the view, then the sort fields,
then `Id`. An optional sort field is led by its `Has` column. Wide text is never indexed.
A unique semantic type gets a unique index.

- Given the model `TodoList` with its two list views, when the engine derives the indexes,
  then one index has the columns `Owner`, `Archived`, `Name`, `Id`.
- Given the model `Todo` with `listSort` on `Due`, when the engine derives the indexes,
  then one index has the columns `Owner`, `List`, `HasDue`, `Due`, `Id`.
- Given the model `User`, when the engine derives the indexes, then there is a unique
  index on `Subject`, and no list index.
- Given an owned model with a title field, no `listSort`, and no `listView`. When the
  engine derives the indexes, then one index has the columns `Owner`, the title field,
  `Id`. (assumed)

### CAP-persist.R4 · Every list query filters by owner and names its columns

Status: verified · Intents: IN-0001

- Given the model `Todo` and its overrides, when the engine writes the list query, then
  the query starts with `SELECT TOP (@limit) [Id], [Title], [Due], [Priority], [Done]
  FROM [Todo]`.
- Given the model `Todo`, when the engine writes the list query, then the `WHERE` clause
  holds `[Owner] = @owner` and `[List] = @parent`.
- Given the model `Todo`, when the engine writes the list query, then the query ends with
  `ORDER BY [HasDue] DESC, [Due] ASC, [Id] ASC`.
- Given the model `TodoList` and the view "Archived", when the engine writes the list
  query, then the `WHERE` clause holds `[Archived] = 1`.
- Given the model `TodoList` and no `listColumns`, when the engine writes the list query,
  then the columns are `[Id], [Name], [Archived]`, and no wide text column.

### CAP-persist.R5 · Keyset paging returns each record one time

Status: verified · Intents: IN-0001

- Given 25 todos of one user in one list, with 6 empty due dates and the rest spread over
  3 dates. When a caller pages through the list 4 at a time, then the caller gets each of
  the 25 todos one time. The caller gets no other todo.
- Given the same 25 todos. When the caller asks for a page of size 1 and a page of size
  200, then each page runs one query.
- Given a page size of 300. When a caller asks for a page, then the result is the error
  `VALIDATION_FAILED` on `limit` with the message "must be between 1 and 200". (assumed)
- Given a page size of 0, when a caller asks for a page, then the result is the same error.
- Given a page size of 200, when a caller asks for a page, then the page holds up to 200.
- Given no page size, when a caller asks for a page, then the engine uses the size 50.
  (assumed)
- Given the cursor "not-a-cursor". When a caller asks for the next page, then the result
  is the error `VALIDATION_FAILED` on `cursor` with the message "is not valid". (assumed)

### CAP-persist.R6 · Deleting a parent deletes its children in one transaction

Status: verified · Intents: IN-0001

- Given a list with 3 todos, when a caller removes the list, then the list and its 3
  todos are gone.
- Given a list with 3 todos, and a failure after the second todo is deleted. When a caller
  removes the list, then the list and all 3 todos still exist.
- Given a list owned by `u2`, when `u1` removes the list, then the result is the error
  `NOT_FOUND`, and the list still exists.

### CAP-persist.R7 · A stale save fails with a conflict

Status: verified · Intents: IN-0001

- Given a todo at version 3. When a caller saves the todo with version 3, then the save
  passes and the todo is at version 4.
- Given a todo at version 4. When a caller saves the todo with version 3, then the result
  is the error `STALE_VERSION`, and the stored todo does not change.
- Given a new todo, when a caller adds it, then the stored todo is at version 1. (assumed)
- Given a todo at version 3. When a caller saves the todo with no version, then the result
  is the error `VALIDATION_FAILED` on `Version` with the message "is required". (assumed)

### CAP-persist.R8 · A migration never drops a column without a note

Status: verified · Intents: IN-0001

- Given the last shape of `Todo` with the column `Due`, and a new shape with the column
  `DueDate` and no `renamedFrom`. When the engine writes the migration, then the result is
  the error `UNPLANNED_DROP` with the message "Todo.Due would be dropped. Add renamedFrom
  or an explicit drop."
- Given the same shapes and the note `renamedFrom "Due"` on `DueDate`, when the engine
  writes the migration, then the migration holds `EXEC sp_rename 'Todo.Due', 'DueDate',
  'COLUMN';`.
- Given the last shape of `Todo` and a new shape with one more optional column `Tags`.
  When the engine writes the migration, then the migration holds `ALTER TABLE [Todo] ADD
  [Tags] nvarchar(200) NULL`.
- Given a new shape with one more required text column `Code`. When the engine writes the
  migration, then the result is the error `UNSAFE_MIGRATION`. The message is "Todo.Code is
  required and has no default, so existing rows would fail." (assumed)
- Given no stored shape. When the engine writes the migration, then the migration is the
  full schema, and the engine stores the new shape. (assumed)
- Given a stored shape that is not valid JSON. When the engine reads the shape, then the
  result is the error `SHAPE_UNREADABLE`. The message is "build/shape.json cannot be read.
  Delete it to start from an empty shape." (assumed)
````

### CAP-todo-app

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

### DES-test-database

````
# DES-test-database · Which database the checks use

Status: agreed · Default: yes · Ask at init: no

## Rule

The checks of the engine library use SQLite in memory as a test double. SQL Server is
the target database, and the checks that need SQL Server run only in CI, in a SQL Server
container.

- A check of the generator, the build checks, validation, or the SQL that the engine
  writes runs in SQLite in memory. The check swaps `TOP (@n)` for `LIMIT @n` and uses a
  generated column for each `Has` column.
- A check of the web app, sign-in, sessions, the audit log, or a security rule needs SQL
  Server and a sign-in provider. Mark the check `[<RequiresSqlServer>]`. The checks
  command skips it when no `SQLSERVER_CONNECTION` setting is set.
- A change that only adds such checks is not verified until CI runs them.

## Why

This sandbox has no SQL Server and no sign-in provider. Without a test double, no check of
the engine library can run here. Without the SQL Server marker, a skipped check would look
like a pass.
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

### Diff against pdlc/CH-0006-todo-app

````
diff --git a/tests/Typed.Engine.Tests/JOB-generate.Tests.fs b/tests/Typed.Engine.Tests/JOB-generate.Tests.fs
new file mode 100644
index 0000000..166f2cf
--- /dev/null
+++ b/tests/Typed.Engine.Tests/JOB-generate.Tests.fs
@@ -0,0 +1,228 @@
+namespace Typed.Engine.Tests
+
+open System
+open System.Diagnostics
+open System.IO
+open System.Text.RegularExpressions
+open Xunit
+open Typed.Engine.Overrides
+open Typed.Engine.Generator
+
+module GenerateFixtures =
+
+    /// The time limit for one run of the command, which may build first.
+    let commandLimitMs = 120000
+
+    let tempFolder () : string =
+        Path.Combine(Path.GetTempPath(), "generate-check-" + Guid.NewGuid().ToString("N"))
+
+    /// The repository root: the first folder above the test run folder that holds README.md and src.
+    let repoRoot () : string =
+        let rec up (dir: DirectoryInfo) =
+            if isNull dir then
+                failwith "Repository root not found above the test run folder."
+            elif File.Exists(Path.Combine(dir.FullName, "README.md")) && Directory.Exists(Path.Combine(dir.FullName, "src")) then
+                dir.FullName
+            else
+                up dir.Parent
+        up (DirectoryInfo AppContext.BaseDirectory)
+
+    let readmeText () : string = File.ReadAllText(Path.Combine(repoRoot (), "README.md"))
+
+    let linesOf (text: string) : string list =
+        text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
+        |> Array.map (fun l -> l.Trim())
+        |> List.ofArray
+
+    /// Runs the library entry with a changed config. Gives the exit code and the output lines.
+    let runConfig (cfg: Config) : int * string list =
+        use w = new StringWriter()
+        let code = run cfg w
+        code, linesOf (w.ToString())
+
+    /// Runs the entry that the command calls, with two separate writers.
+    /// Gives the exit code, the standard output text, and the standard error text.
+    let executeConfig (args: string[]) (cfg: Config) : int * string * string =
+        use out = new StringWriter()
+        use err = new StringWriter()
+        let code = execute args cfg out err
+        code, out.ToString(), err.ToString()
+
+    /// The todo app with the access rule of Todo removed.
+    let withoutTodoRule (outDir: string) : Config =
+        let cfg = todoConfig outDir
+        { cfg with Rules = cfg.Rules |> List.filter (fun r -> r.ModelName <> "Todo") }
+
+    /// The todo app with the override unsafe "rawName" "uses raw SQL" added on TodoList.
+    let withUnsafe (outDir: string) : Config =
+        let cfg = todoConfig outDir
+        let extra = (overrides<Todo.App.Model.TodoList> [ unsafe "rawName" "uses raw SQL" ]).Items
+        let sets =
+            cfg.Overrides
+            |> List.map (fun s -> if s.ModelName = "TodoList" then { s with Items = s.Items @ extra } else s)
+        { cfg with Overrides = sets }
+
+    /// Runs the real command with a time limit. Gives the exit code, standard output, and standard error.
+    let runCommand (args: string list) : int * string * string =
+        let psi = ProcessStartInfo("dotnet")
+        psi.WorkingDirectory <- repoRoot ()
+        psi.RedirectStandardOutput <- true
+        psi.RedirectStandardError <- true
+        psi.UseShellExecute <- false
+        // Start the built dll directly. `dotnet run` leaves an MSBuild node that holds the pipes open.
+        let dll = Path.Combine(AppContext.BaseDirectory, "Typed.Engine.Generator.dll")
+        let front =
+            if File.Exists dll then
+                [ dll ]
+            else
+                psi.Environment.["DOTNET_CLI_USE_MSBUILD_SERVER"] <- "0"
+                psi.Environment.["MSBUILDDISABLENODEREUSE"] <- "1"
+                psi.Environment.["DOTNET_CLI_TELEMETRY_OPTOUT"] <- "1"
+                [ "run"; "--project"; "src/Typed.Engine.Generator"; "--" ]
+        for a in front @ args do
+            psi.ArgumentList.Add a
+        use p = Process.Start psi
+        let out = p.StandardOutput.ReadToEndAsync()
+        let err = p.StandardError.ReadToEndAsync()
+        if not (p.WaitForExit commandLimitMs) then
+            p.Kill true
+            failwithf "The command did not finish in %d ms." commandLimitMs
+        p.WaitForExit()
+        p.ExitCode, out.Result, err.Result
+
+open GenerateFixtures
+
+module ``JOB-generate-R1`` =
+
+    [<Fact>]
+    let ``JOB-generate.R1 the todo app prints no error line and exits 0`` () =
+        let dir = tempFolder ()
+        let code, stdout, _ = runCommand [ "generate"; "--out"; dir ]
+        Assert.Equal(0, code)
+        Assert.DoesNotContain(linesOf stdout, fun l -> l.StartsWith "error ")
+
+    [<Fact>]
+    let ``JOB-generate.R1 the todo app without the Todo access rule prints MISSING_ACCESS_RULE and exits 1`` () =
+        let code, lines = runConfig (withoutTodoRule (tempFolder ()))
+        Assert.Equal(1, code)
+        Assert.Contains("error MISSING_ACCESS_RULE Model Todo has no access rule.", lines)
+
+    [<Fact>]
+    let ``JOB-generate.R1 an unsafe override on TodoList prints one warning and exits 0`` () =
+        let code, lines = runConfig (withUnsafe (tempFolder ()))
+        Assert.Equal(0, code)
+        let warning = "warning UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL"
+        Assert.Equal(1, lines |> List.filter (fun l -> l = warning) |> List.length)
+        Assert.DoesNotContain(lines, fun l -> l.StartsWith "error ")
+
+    [<Fact>]
+    let ``JOB-generate.R1 the command frobnicate prints usage and exits 2`` () =
+        let code, stdout, _ = runCommand [ "frobnicate" ]
+        Assert.Equal(2, code)
+        Assert.Contains("usage: generate", stdout)
+
+    [<Fact>]
+    let ``JOB-generate.R1 the command prints the usage line to standard output and not to standard error`` () =
+        // The real todo app has no finding, so the usage line is the real output that shows the stream.
+        let code, stdout, stderr = runCommand [ "frobnicate" ]
+        Assert.Equal(2, code)
+        Assert.Contains(linesOf stdout, fun l -> l.StartsWith "usage: generate")
+        Assert.DoesNotContain("usage: generate", stderr)
+
+    [<Fact>]
+    let ``JOB-generate.R1 the error finding line goes to standard output and standard error stays empty`` () =
+        let dir = tempFolder ()
+        let code, stdout, stderr = executeConfig [| "generate"; "--out"; dir |] (withoutTodoRule dir)
+        Assert.Equal(1, code)
+        Assert.Contains("error MISSING_ACCESS_RULE Model Todo has no access rule.", linesOf stdout)
+        Assert.Equal("", stderr)
+
+    [<Fact>]
+    let ``JOB-generate.R1 the unsafe override warning goes to standard output and standard error stays empty`` () =
+        let dir = tempFolder ()
+        let code, stdout, stderr = executeConfig [| "generate"; "--out"; dir |] (withUnsafe dir)
+        Assert.Equal(0, code)
+        Assert.Contains("warning UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL", linesOf stdout)
+        Assert.Equal("", stderr)
+
+module ``JOB-generate-R2`` =
+
+    let private schemaOf (dir: string) = Path.Combine(dir, "schema.sql")
+
+    [<Fact>]
+    let ``JOB-generate.R2 the todo app writes schema.sql with the tables, foreign keys, and indexes`` () =
+        let dir = tempFolder ()
+        let code, _, _ = runCommand [ "generate"; "--out"; dir ]
+        Assert.Equal(0, code)
+        let sql = File.ReadAllText(schemaOf dir)
+        Assert.Equal(3, Regex.Matches(sql, "CREATE TABLE", RegexOptions.IgnoreCase).Count)
+        for name in [ "User"; "TodoList"; "Todo" ] do
+            Assert.Matches(Regex("CREATE TABLE\\s+\\S*" + name + "\\b", RegexOptions.IgnoreCase), sql)
+        Assert.Matches(Regex("CREATE\\s+(UNIQUE\\s+)?(NONCLUSTERED\\s+)?INDEX", RegexOptions.IgnoreCase), sql)
+        // Some statement that names the table Todo (not TodoList) holds a FOREIGN KEY.
+        let statements = Regex.Split(sql, @";|^\s*GO\s*$", RegexOptions.Multiline)
+        let namesTodo (s: string) = Regex.IsMatch(s, @"\bTodo\b")
+        Assert.Contains(statements, fun s -> namesTodo s && s.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
+        // Some index statement names the table Todo.
+        Assert.Contains(statements, fun s -> namesTodo s && Regex.IsMatch(s, @"CREATE\s+(UNIQUE\s+)?(NONCLUSTERED\s+)?INDEX", RegexOptions.IgnoreCase))
+
+    [<Fact>]
+    let ``JOB-generate.R2 the todo app also writes shape.json`` () =
+        let dir = tempFolder ()
+        let code, _ = runConfig (todoConfig dir)
+        Assert.Equal(0, code)
+        Assert.True(File.Exists(Path.Combine(dir, "shape.json")))
+
+    [<Fact>]
+    let ``JOB-generate.R2 an error finding leaves schema.sql unwritten`` () =
+        let dir = tempFolder ()
+        let code, _ = runConfig (withoutTodoRule dir)
+        Assert.Equal(1, code)
+        Assert.False(File.Exists(schemaOf dir))
+
+    [<Fact>]
+    let ``JOB-generate.R2 a missing build folder is created`` () =
+        // The folder and its parent do not exist before the run.
+        let dir = Path.Combine(tempFolder (), "build")
+        Assert.False(Directory.Exists dir)
+        let code, _ = runConfig (todoConfig dir)
+        Assert.Equal(0, code)
+        Assert.True(Directory.Exists dir)
+        Assert.True(File.Exists(schemaOf dir))
+
+module ``JOB-generate-R3`` =
+
+    [<Fact>]
+    let ``JOB-generate.R3 the README gives the generate command`` () =
+        Assert.Contains("dotnet run --project src/Typed.Engine.Generator -- generate", readmeText ())
+
+    [<Fact>]
+    let ``JOB-generate.R3 the README gives the meaning of exit codes 0, 1, and 2`` () =
+        let lines = linesOf (readmeText ())
+        let afterExit = lines |> List.skipWhile (fun l -> not (l.ToLowerInvariant().Contains "exit"))
+        Assert.NotEmpty afterExit
+        // The code is its own token: not inside a word, a version such as net8.0, or a longer number.
+        let hasToken (code: string) (l: string) =
+            Regex.IsMatch(l, "(?<![A-Za-z0-9.])" + code + "(?![A-Za-z0-9]|\\.[0-9])")
+        // A line for one code holds no token of the other two codes, so each code needs its own line.
+        // The meaning is a regex on the lower-case line.
+        let has (code: string) (meaning: string) =
+            let others = [ "0"; "1"; "2" ] |> List.filter (fun c -> c <> code)
+            afterExit
+            |> List.exists (fun l ->
+                hasToken code l
+                && others |> List.forall (fun o -> not (hasToken o l))
+                && Regex.IsMatch(l.ToLowerInvariant(), meaning))
+        // Code 0 means no error, success, or ok. The bare word "error" does not count.
+        Assert.True(has "0" @"\b(no errors?|success|ok)\b", "exit code 0 has no meaning in the README")
+        // Code 1 means error. The phrase "no error" does not count.
+        Assert.True(has "1" @"(?<!\bno )\berrors?\b", "exit code 1 has no meaning in the README")
+        Assert.True(has "2" @"usage|unknown", "exit code 2 has no meaning in the README")
+
+    [<Fact>]
+    let ``JOB-generate.R3 the README gives the command dotnet test tests/`` () =
+        Assert.Contains("dotnet test tests/Typed.Engine.Tests", readmeText ())
+
+    [<Fact>]
+    let ``JOB-generate.R3 the README names the --out seam`` () =
+        Assert.Contains("--out", readmeText ())
diff --git a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
index 9f38d5e..d90f29d 100644
--- a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
+++ b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
@@ -12,6 +12,7 @@
     <Compile Include="CAP-persist.Tests.fs" />
     <Compile Include="CAP-checks.Tests.fs" />
     <Compile Include="CAP-todo-app.Tests.fs" />
+    <Compile Include="JOB-generate.Tests.fs" />
   </ItemGroup>
 
   <ItemGroup>
@@ -24,6 +25,7 @@
   <ItemGroup>
     <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
     <ProjectReference Include="../../src/Todo.App/Todo.App.fsproj" />
+    <ProjectReference Include="../../src/Typed.Engine.Generator/Typed.Engine.Generator.fsproj" />
   </ItemGroup>
 
 </Project>
````

### tests/Typed.Engine.Tests/JOB-generate.Tests.fs

````
namespace Typed.Engine.Tests

open System
open System.Diagnostics
open System.IO
open System.Text.RegularExpressions
open Xunit
open Typed.Engine.Overrides
open Typed.Engine.Generator

module GenerateFixtures =

    /// The time limit for one run of the command, which may build first.
    let commandLimitMs = 120000

    let tempFolder () : string =
        Path.Combine(Path.GetTempPath(), "generate-check-" + Guid.NewGuid().ToString("N"))

    /// The repository root: the first folder above the test run folder that holds README.md and src.
    let repoRoot () : string =
        let rec up (dir: DirectoryInfo) =
            if isNull dir then
                failwith "Repository root not found above the test run folder."
            elif File.Exists(Path.Combine(dir.FullName, "README.md")) && Directory.Exists(Path.Combine(dir.FullName, "src")) then
                dir.FullName
            else
                up dir.Parent
        up (DirectoryInfo AppContext.BaseDirectory)

    let readmeText () : string = File.ReadAllText(Path.Combine(repoRoot (), "README.md"))

    let linesOf (text: string) : string list =
        text.Split([| '\r'; '\n' |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun l -> l.Trim())
        |> List.ofArray

    /// Runs the library entry with a changed config. Gives the exit code and the output lines.
    let runConfig (cfg: Config) : int * string list =
        use w = new StringWriter()
        let code = run cfg w
        code, linesOf (w.ToString())

    /// Runs the entry that the command calls, with two separate writers.
    /// Gives the exit code, the standard output text, and the standard error text.
    let executeConfig (args: string[]) (cfg: Config) : int * string * string =
        use out = new StringWriter()
        use err = new StringWriter()
        let code = execute args cfg out err
        code, out.ToString(), err.ToString()

    /// The todo app with the access rule of Todo removed.
    let withoutTodoRule (outDir: string) : Config =
        let cfg = todoConfig outDir
        { cfg with Rules = cfg.Rules |> List.filter (fun r -> r.ModelName <> "Todo") }

    /// The todo app with the override unsafe "rawName" "uses raw SQL" added on TodoList.
    let withUnsafe (outDir: string) : Config =
        let cfg = todoConfig outDir
        let extra = (overrides<Todo.App.Model.TodoList> [ unsafe "rawName" "uses raw SQL" ]).Items
        let sets =
            cfg.Overrides
            |> List.map (fun s -> if s.ModelName = "TodoList" then { s with Items = s.Items @ extra } else s)
        { cfg with Overrides = sets }

    /// Runs the real command with a time limit. Gives the exit code, standard output, and standard error.
    let runCommand (args: string list) : int * string * string =
        let psi = ProcessStartInfo("dotnet")
        psi.WorkingDirectory <- repoRoot ()
        psi.RedirectStandardOutput <- true
        psi.RedirectStandardError <- true
        psi.UseShellExecute <- false
        // Start the built dll directly. `dotnet run` leaves an MSBuild node that holds the pipes open.
        let dll = Path.Combine(AppContext.BaseDirectory, "Typed.Engine.Generator.dll")
        let front =
            if File.Exists dll then
                [ dll ]
            else
                psi.Environment.["DOTNET_CLI_USE_MSBUILD_SERVER"] <- "0"
                psi.Environment.["MSBUILDDISABLENODEREUSE"] <- "1"
                psi.Environment.["DOTNET_CLI_TELEMETRY_OPTOUT"] <- "1"
                [ "run"; "--project"; "src/Typed.Engine.Generator"; "--" ]
        for a in front @ args do
            psi.ArgumentList.Add a
        use p = Process.Start psi
        let out = p.StandardOutput.ReadToEndAsync()
        let err = p.StandardError.ReadToEndAsync()
        if not (p.WaitForExit commandLimitMs) then
            p.Kill true
            failwithf "The command did not finish in %d ms." commandLimitMs
        p.WaitForExit()
        p.ExitCode, out.Result, err.Result

open GenerateFixtures

module ``JOB-generate-R1`` =

    [<Fact>]
    let ``JOB-generate.R1 the todo app prints no error line and exits 0`` () =
        let dir = tempFolder ()
        let code, stdout, _ = runCommand [ "generate"; "--out"; dir ]
        Assert.Equal(0, code)
        Assert.DoesNotContain(linesOf stdout, fun l -> l.StartsWith "error ")

    [<Fact>]
    let ``JOB-generate.R1 the todo app without the Todo access rule prints MISSING_ACCESS_RULE and exits 1`` () =
        let code, lines = runConfig (withoutTodoRule (tempFolder ()))
        Assert.Equal(1, code)
        Assert.Contains("error MISSING_ACCESS_RULE Model Todo has no access rule.", lines)

    [<Fact>]
    let ``JOB-generate.R1 an unsafe override on TodoList prints one warning and exits 0`` () =
        let code, lines = runConfig (withUnsafe (tempFolder ()))
        Assert.Equal(0, code)
        let warning = "warning UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL"
        Assert.Equal(1, lines |> List.filter (fun l -> l = warning) |> List.length)
        Assert.DoesNotContain(lines, fun l -> l.StartsWith "error ")

    [<Fact>]
    let ``JOB-generate.R1 the command frobnicate prints usage and exits 2`` () =
        let code, stdout, _ = runCommand [ "frobnicate" ]
        Assert.Equal(2, code)
        Assert.Contains("usage: generate", stdout)

    [<Fact>]
    let ``JOB-generate.R1 the command prints the usage line to standard output and not to standard error`` () =
        // The real todo app has no finding, so the usage line is the real output that shows the stream.
        let code, stdout, stderr = runCommand [ "frobnicate" ]
        Assert.Equal(2, code)
        Assert.Contains(linesOf stdout, fun l -> l.StartsWith "usage: generate")
        Assert.DoesNotContain("usage: generate", stderr)

    [<Fact>]
    let ``JOB-generate.R1 the error finding line goes to standard output and standard error stays empty`` () =
        let dir = tempFolder ()
        let code, stdout, stderr = executeConfig [| "generate"; "--out"; dir |] (withoutTodoRule dir)
        Assert.Equal(1, code)
        Assert.Contains("error MISSING_ACCESS_RULE Model Todo has no access rule.", linesOf stdout)
        Assert.Equal("", stderr)

    [<Fact>]
    let ``JOB-generate.R1 the unsafe override warning goes to standard output and standard error stays empty`` () =
        let dir = tempFolder ()
        let code, stdout, stderr = executeConfig [| "generate"; "--out"; dir |] (withUnsafe dir)
        Assert.Equal(0, code)
        Assert.Contains("warning UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL", linesOf stdout)
        Assert.Equal("", stderr)

module ``JOB-generate-R2`` =

    let private schemaOf (dir: string) = Path.Combine(dir, "schema.sql")

    [<Fact>]
    let ``JOB-generate.R2 the todo app writes schema.sql with the tables, foreign keys, and indexes`` () =
        let dir = tempFolder ()
        let code, _, _ = runCommand [ "generate"; "--out"; dir ]
        Assert.Equal(0, code)
        let sql = File.ReadAllText(schemaOf dir)
        Assert.Equal(3, Regex.Matches(sql, "CREATE TABLE", RegexOptions.IgnoreCase).Count)
        for name in [ "User"; "TodoList"; "Todo" ] do
            Assert.Matches(Regex("CREATE TABLE\\s+\\S*" + name + "\\b", RegexOptions.IgnoreCase), sql)
        Assert.Matches(Regex("CREATE\\s+(UNIQUE\\s+)?(NONCLUSTERED\\s+)?INDEX", RegexOptions.IgnoreCase), sql)
        // Some statement that names the table Todo (not TodoList) holds a FOREIGN KEY.
        let statements = Regex.Split(sql, @";|^\s*GO\s*$", RegexOptions.Multiline)
        let namesTodo (s: string) = Regex.IsMatch(s, @"\bTodo\b")
        Assert.Contains(statements, fun s -> namesTodo s && s.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase))
        // Some index statement names the table Todo.
        Assert.Contains(statements, fun s -> namesTodo s && Regex.IsMatch(s, @"CREATE\s+(UNIQUE\s+)?(NONCLUSTERED\s+)?INDEX", RegexOptions.IgnoreCase))

    [<Fact>]
    let ``JOB-generate.R2 the todo app also writes shape.json`` () =
        let dir = tempFolder ()
        let code, _ = runConfig (todoConfig dir)
        Assert.Equal(0, code)
        Assert.True(File.Exists(Path.Combine(dir, "shape.json")))

    [<Fact>]
    let ``JOB-generate.R2 an error finding leaves schema.sql unwritten`` () =
        let dir = tempFolder ()
        let code, _ = runConfig (withoutTodoRule dir)
        Assert.Equal(1, code)
        Assert.False(File.Exists(schemaOf dir))

    [<Fact>]
    let ``JOB-generate.R2 a missing build folder is created`` () =
        // The folder and its parent do not exist before the run.
        let dir = Path.Combine(tempFolder (), "build")
        Assert.False(Directory.Exists dir)
        let code, _ = runConfig (todoConfig dir)
        Assert.Equal(0, code)
        Assert.True(Directory.Exists dir)
        Assert.True(File.Exists(schemaOf dir))

module ``JOB-generate-R3`` =

    [<Fact>]
    let ``JOB-generate.R3 the README gives the generate command`` () =
        Assert.Contains("dotnet run --project src/Typed.Engine.Generator -- generate", readmeText ())

    [<Fact>]
    let ``JOB-generate.R3 the README gives the meaning of exit codes 0, 1, and 2`` () =
        let lines = linesOf (readmeText ())
        let afterExit = lines |> List.skipWhile (fun l -> not (l.ToLowerInvariant().Contains "exit"))
        Assert.NotEmpty afterExit
        // The code is its own token: not inside a word, a version such as net8.0, or a longer number.
        let hasToken (code: string) (l: string) =
            Regex.IsMatch(l, "(?<![A-Za-z0-9.])" + code + "(?![A-Za-z0-9]|\\.[0-9])")
        // A line for one code holds no token of the other two codes, so each code needs its own line.
        // The meaning is a regex on the lower-case line.
        let has (code: string) (meaning: string) =
            let others = [ "0"; "1"; "2" ] |> List.filter (fun c -> c <> code)
            afterExit
            |> List.exists (fun l ->
                hasToken code l
                && others |> List.forall (fun o -> not (hasToken o l))
                && Regex.IsMatch(l.ToLowerInvariant(), meaning))
        // Code 0 means no error, success, or ok. The bare word "error" does not count.
        Assert.True(has "0" @"\b(no errors?|success|ok)\b", "exit code 0 has no meaning in the README")
        // Code 1 means error. The phrase "no error" does not count.
        Assert.True(has "1" @"(?<!\bno )\berrors?\b", "exit code 1 has no meaning in the README")
        Assert.True(has "2" @"usage|unknown", "exit code 2 has no meaning in the README")

    [<Fact>]
    let ``JOB-generate.R3 the README gives the command dotnet test tests/`` () =
        Assert.Contains("dotnet test tests/Typed.Engine.Tests", readmeText ())

    [<Fact>]
    let ``JOB-generate.R3 the README names the --out seam`` () =
        Assert.Contains("--out", readmeText ())
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
    <Compile Include="JOB-generate.Tests.fs" />
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
    <ProjectReference Include="../../src/Typed.Engine.Generator/Typed.Engine.Generator.fsproj" />
  </ItemGroup>

</Project>
````

