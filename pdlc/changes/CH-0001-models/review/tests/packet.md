# Review packet · CH-0001-models · tests

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
# CH-0001 · Models

Intent: IN-0001 · Scope: CAP-models · Status: building · Ticket: none · Branch: pdlc/CH-0001-models · Base: ccr-1ef8bdfa-1cxxi7
Tests-Locked: c61b18d

## Delivers

The engine can describe an F# record type as a model plan. The plan gives kinds, type defaults, labels, wide text, the title field, system fields, and typed overrides. The shared contracts that the other changes use exist.

## Requirements

### CAP-models.R1 · Each field gets a kind from its type

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

- Given a model `Tagged` with a field `Tags: Title list`. When the engine describes
  `Tagged`, then the result is the error `MISSING_DEFAULT`. The message is
  "Tagged.Tags has type 'Title list', which has no default in Persist, Validate,
  Display, API."
- Given a model with two fields that have no default. When the engine describes the
  model, then the result holds one `MISSING_DEFAULT` error for each field.

### CAP-models.R3 · The system fields are known and never written from a model

- Given any model, when a caller asks for the system field names, then the names are
  `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`.
- Given the model `Todo`. When the engine describes `Todo`, then the fields of the plan
  are the seven fields of the record, and no system field.

### CAP-models.R4 · The label comes from the field name

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

- Given the field `Notes` with the kind optional of text with the length 5000. When a
  caller asks if the field is wide, then the answer is yes.
- Given the field `Title` with the kind text with the length 200. When a caller asks if
  the field is wide, then the answer is no.
- Given a text type with the length 400, when a caller asks if the field is wide, then the
  answer is no.
- Given a text type with the length 401, when a caller asks if the field is wide, then the
  answer is yes.

### CAP-models.R6 · The title field is marked or named

- Given the model `TodoList` and no overrides, when a caller asks for the title field,
  then the field is `Name`.
- Given the model `User` and no overrides, when a caller asks for the title field, then
  the field is `DisplayName`.
- Given a model `NoTitle` with one field `Count: int` and no overrides, when a caller
  asks for the title field, then there is none.
- Given the model `NoTitle` and the override `titleField <@ fun n -> n.Count @>`, when a
  caller asks for the title field, then the field is `Count`.

### CAP-models.R7 · Overrides take typed field selectors

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

## Relies on

- DES-names (names follow the spec)
- DES-errors (error codes)
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

In `src/Typed.Engine/Model.fs`, module `Typed.Engine.Model`:

- `[<TitleType>]` attribute (`TitleTypeAttribute`), and `type Ref<'T> = Ref of Guid`.
- `type Kind = Text of maxLength: int | WholeNumber | Decimal | Boolean | Enum of cases: string list | Date | Timestamp | Id | Reference of target: string | Optional of Kind`
- `type ErrorCode` is a union with one case for each code in `DES-errors`, such as `MISSING_DEFAULT`. (assumed)
- `type Severity = Error | Warning`
- `type Finding = { Severity: Severity; Code: ErrorCode; Model: string; Field: string option; Message: string }`
- `type TypeDefault = { Clr: Type; Kind: Kind; Check: string -> string list; Hidden: bool; Unique: bool }`
- `type Field = { Name: string; Label: string; Kind: Kind; Prop: PropertyInfo; Core: Type; Check: string -> string list; Hidden: bool; Unique: bool; IsTitle: bool }`
- `type Model = { Name: string; Clr: Type; Fields: Field list }`
- `type Record = { Model: string; Id: Guid; Owner: Guid option; Version: int64; Values: Map<string, obj> }`: a stored record as the store and the access rules see it. (assumed)
- `type HookContext = { User: Guid; TimeZone: TimeZoneInfo; Now: DateTimeOffset }` and `localToday: HookContext -> DateOnly`: the context that hooks get. (assumed)
- `engines: string list` = `["Persist"; "Validate"; "Display"; "API"]`
- `systemFields: string list` = `["Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt"]`
- `describe: TypeDefault list -> Type -> Result<Model, Finding list>`
  - errors: `MISSING_DEFAULT` (one for each field with no default), `WRONG_CONFIG_TYPE` (the type is not a record)
- `labelOf: string -> string`
- `isWide: Kind -> bool`, `innerKind: Kind -> Kind`, `isOptional: Kind -> bool`, `refTarget: Kind -> string option`
- `displayType: Type -> string` gives `Title list` for `FSharpList<Title>`.

In `src/Typed.Engine/Overrides.fs`, module `Typed.Engine.Overrides`:

- `type Direction = Ascending | Descending`
- `type OverrideItem = Label of field: string * text: string | HiddenInForm of string | ReadOnly of string | ListColumns of string list | ListEdit of string | ListSort of string * Direction | ListView of name: string * filter: Expr | TitleFieldOf of string | Rule of field: string * fieldType: Type * predicate: (obj -> bool) * message: string | Sensitive of string | Column of field: string * name: string | Unsafe of name: string * reason: string`
- `type Override<'T>`, `type ColumnRef<'T>`, `type ModelOverrides = { ModelName: string; Items: OverrideItem list }`
- `fieldName: Expr -> string`: the property name in `<@ fun t -> t.Due @>`.
- `label`, `hiddenInForm`, `readOnly`, `listEdit`, `listSort`, `col`, `listColumns`, `listView`, `titleField`, `rule`, `sensitive`, `column`, `unsafe`: each takes an `Expr<'T -> 'F>` selector (and its config) and gives an `Override<'T>`.
- `overrides<'T>: Override<'T> list -> ModelOverrides`
- `referencedFields: Expr -> string list`, `fieldsOf: OverrideItem -> string list`
- `titleFieldName: Field list -> OverrideItem list -> string option`

Types for the checks. The acceptance lines name the todo app. That app is a later change, so
the checks declare these types themselves, exactly as written here, in the check file:

```fsharp
[<TitleType>]
type Title      = Title of string
type Notes      = Notes of string
type Priority   = Low | Normal | High
type TimeZoneId = TimeZoneId of string
type ExternalId = ExternalId of string

type User     = { Subject: ExternalId; DisplayName: Title; TimeZone: TimeZoneId }
type TodoList = { Name: Title; Archived: bool }
type Todo     = { List: Ref<TodoList>; Title: Title; Notes: Notes option; Priority: Priority
                  Due: DateOnly option; Done: bool; CompletedAt: DateTimeOffset option }
```

The type defaults of the todo app are these four entries, and no others. `Priority` and
`Ref<TodoList>` get their kinds from the engine, not from the list.

| Clr | Kind | Check | Hidden | Unique |
|---|---|---|---|---|
| `Title` | `Text 200` | no line break | false | false |
| `Notes` | `Text 5000` | none | false | false |
| `TimeZoneId` | `Text 64` | known IANA zone | false | false |
| `ExternalId` | `Text 255` | none | true | true |

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement, named with the requirement ID with a hyphen in place of the dot, in double backticks, such as ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

none

## Files

- src/Typed.Engine/Model.fs
- src/Typed.Engine/Overrides.fs
- src/Typed.Engine/Typed.Engine.fsproj: the project file and the Compile entries of this spec (assumed)
- .gitignore: `bin/`, `obj/`, and `build/` (assumed)

## Check files

- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
- tests/Typed.Engine.Tests/CAP-models.Tests.fs

## Steps

### Step 1 · Each field gets a kind from its type · [ ]

- Requirement: CAP-models.R1
- Check: the checks named `CAP-models.R1 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R1 (each field gets a kind from its type) passes

### Step 2 · A type with no default is an error that names each engine · [ ]

- Requirement: CAP-models.R2
- Check: the checks named `CAP-models.R2 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R2 (a type with no default is an error that names each engine) passes

### Step 3 · The system fields are known and never written from a model · [ ]

- Requirement: CAP-models.R3
- Check: the checks named `CAP-models.R3 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R3 (the system fields are known and never written from a model) passes

### Step 4 · The label comes from the field name · [ ]

- Requirement: CAP-models.R4
- Check: the checks named `CAP-models.R4 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R4 (the label comes from the field name) passes

### Step 5 · Wide text is text longer than 400 characters · [ ]

- Requirement: CAP-models.R5
- Check: the checks named `CAP-models.R5 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R5 (wide text is text longer than 400 characters) passes

### Step 6 · The title field is marked or named · [ ]

- Requirement: CAP-models.R6
- Check: the checks named `CAP-models.R6 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R6 (the title field is marked or named) passes

### Step 7 · Overrides take typed field selectors · [ ]

- Requirement: CAP-models.R7
- Check: the checks named `CAP-models.R7 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R7 (overrides take typed field selectors) passes

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `CAP-models` as the ready stage left them.
- 2026-10-19T05:43:55+00:00 · Tests: the test writer wrote 30 checks in `CAP-models.Tests.fs` and the test
  project. Red: `dotnet test` fails to compile, because `Typed.Engine` does not exist yet.
  Every error names a missing symbol of the Interface. No error is a fault in a check.
  Locked at 5dba628.
- 2026-10-19T05:47:07+00:00 · Build round 1: the builder found that F# rejects a dot in a module name. The
  test writer renamed the seven modules with a hyphen. The check names did not change.
  The builder's engine code is in the working tree, so the checks now run. Locked again
  at c61b18d.
````

## The spec it covers: title and summary

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
````

## The requirements, in full

### CAP-models.R1 · Each field gets a kind from its type

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

When a field has a type that no default covers, the description fails. The error names
the model, the field, the type, and each engine.

- Given a model `Tagged` with a field `Tags: Title list`. When the engine describes
  `Tagged`, then the result is the error `MISSING_DEFAULT`. The message is
  "Tagged.Tags has type 'Title list', which has no default in Persist, Validate,
  Display, API."
- Given a model with two fields that have no default. When the engine describes the
  model, then the result holds one `MISSING_DEFAULT` error for each field.

### CAP-models.R3 · The system fields are known and never written from a model

Status: ready · Intents: IN-0001

The system fields are `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`. A model
does not declare them. The plan knows their names.

- Given any model, when a caller asks for the system field names, then the names are
  `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`.
- Given the model `Todo`. When the engine describes `Todo`, then the fields of the plan
  are the seven fields of the record, and no system field.

### CAP-models.R4 · The label comes from the field name

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

- Given the field `Notes` with the kind optional of text with the length 5000. When a
  caller asks if the field is wide, then the answer is yes.
- Given the field `Title` with the kind text with the length 200. When a caller asks if
  the field is wide, then the answer is no.
- Given a text type with the length 400, when a caller asks if the field is wide, then the
  answer is no.
- Given a text type with the length 401, when a caller asks if the field is wide, then the
  answer is yes.

### CAP-models.R6 · The title field is marked or named

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

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

## Specs it relies on

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

### Diff against ccr-1ef8bdfa-1cxxi7

````
diff --git a/tests/Typed.Engine.Tests/CAP-models.Tests.fs b/tests/Typed.Engine.Tests/CAP-models.Tests.fs
new file mode 100644
index 0000000..247f972
--- /dev/null
+++ b/tests/Typed.Engine.Tests/CAP-models.Tests.fs
@@ -0,0 +1,271 @@
+namespace Typed.Engine.Tests
+
+open System
+open Microsoft.FSharp.Quotations
+open Xunit
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+
+/// Models, semantic types, and type defaults of the todo app. The checks use only these.
+module ModelsFixtures =
+
+    [<TitleType>]
+    type Title = Title of string
+
+    type Notes = Notes of string
+
+    type Priority =
+        | Low
+        | Normal
+        | High
+
+    type TimeZoneId = TimeZoneId of string
+
+    type ExternalId = ExternalId of string
+
+    type User =
+        { Subject: ExternalId
+          DisplayName: Title
+          TimeZone: TimeZoneId }
+
+    type TodoList = { Name: Title; Archived: bool }
+
+    type Todo =
+        { List: Ref<TodoList>
+          Title: Title
+          Notes: Notes option
+          Priority: Priority
+          Due: DateOnly option
+          Done: bool
+          CompletedAt: DateTimeOffset option }
+
+    type NoTitle = { Count: int }
+
+    type Tagged = { Tags: Title list }
+
+    type TwoMissing = { Tags: Title list; Aliases: Notes list }
+
+    let private def (clr: Type) kind : TypeDefault =
+        { Clr = clr
+          Kind = kind
+          Check = (fun _ -> [])
+          Hidden = false
+          Unique = false }
+
+    /// Exactly the four entries of the spec. The engine gives Priority and Ref kinds itself.
+    let defaults: TypeDefault list =
+        [ def typeof<Title> (Text 200)
+          def typeof<Notes> (Text 5000)
+          def typeof<TimeZoneId> (Text 64)
+          { def typeof<ExternalId> (Text 255) with
+              Hidden = true
+              Unique = true } ]
+
+    let describeOk (t: Type) : Model =
+        match describe defaults t with
+        | Ok m -> m
+        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs
+
+    let describeErrors (t: Type) : Finding list =
+        match describe defaults t with
+        | Ok m -> failwithf "Expected errors, got a plan for %s" m.Name
+        | Result.Error fs -> fs
+
+    let field (m: Model) (name: string) : Field =
+        m.Fields |> List.find (fun f -> f.Name = name)
+
+open ModelsFixtures
+
+module ``CAP-models-R1`` =
+
+    [<Fact>]
+    let ``CAP-models.R1 Title has kind text 200`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal(Text 200, (field m "Title").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R1 Notes has kind optional of text 5000`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal(Optional(Text 5000), (field m "Notes").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R1 Priority has kind enum with Low Normal High`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal(Enum [ "Low"; "Normal"; "High" ], (field m "Priority").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R1 Due has kind optional of date`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal(Optional Date, (field m "Due").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R1 List has kind reference to TodoList`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal(Reference "TodoList", (field m "List").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R1 Done is boolean and CompletedAt is optional of timestamp`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal(Boolean, (field m "Done").Kind)
+        Assert.Equal(Optional Timestamp, (field m "CompletedAt").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R1 a union type as a model is WRONG_CONFIG_TYPE`` () =
+        let fs = describeErrors typeof<Priority>
+        Assert.Equal(1, fs.Length)
+        Assert.Equal(ErrorCode.WRONG_CONFIG_TYPE, fs.Head.Code)
+        Assert.Equal("Priority is not a record type.", fs.Head.Message)
+
+module ``CAP-models-R2`` =
+
+    [<Fact>]
+    let ``CAP-models.R2 a list of Title has no default and names each engine`` () =
+        let fs = describeErrors typeof<Tagged>
+        Assert.Equal(1, fs.Length)
+        let f = fs.Head
+        Assert.Equal(ErrorCode.MISSING_DEFAULT, f.Code)
+        Assert.Equal("Tagged", f.Model)
+        Assert.Equal(Some "Tags", f.Field)
+        Assert.Equal(
+            "Tagged.Tags has type 'Title list', which has no default in Persist, Validate, Display, API.",
+            f.Message
+        )
+
+    [<Fact>]
+    let ``CAP-models.R2 two fields with no default give two MISSING_DEFAULT errors`` () =
+        let fs = describeErrors typeof<TwoMissing>
+        Assert.Equal(2, fs.Length)
+        Assert.All(fs, fun f -> Assert.Equal(ErrorCode.MISSING_DEFAULT, f.Code))
+        let names = fs |> List.map (fun f -> f.Field) |> List.sort
+        Assert.Equal<string option list>([ Some "Aliases"; Some "Tags" ], names)
+
+module ``CAP-models-R3`` =
+
+    [<Fact>]
+    let ``CAP-models.R3 the system field names are known`` () =
+        Assert.Equal<string list>([ "Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt" ], systemFields)
+
+    [<Fact>]
+    let ``CAP-models.R3 the plan of Todo holds its seven fields and no system field`` () =
+        let m = describeOk typeof<Todo>
+        let names = m.Fields |> List.map (fun f -> f.Name)
+        Assert.Equal<string list>(
+            [ "List"; "Title"; "Notes"; "Priority"; "Due"; "Done"; "CompletedAt" ],
+            names
+        )
+        for s in systemFields do
+            Assert.DoesNotContain(s, names)
+
+module ``CAP-models-R4`` =
+
+    [<Fact>]
+    let ``CAP-models.R4 CompletedAt gives Completed at`` () =
+        Assert.Equal("Completed at", labelOf "CompletedAt")
+
+    [<Fact>]
+    let ``CAP-models.R4 Due gives Due`` () = Assert.Equal("Due", labelOf "Due")
+
+    [<Fact>]
+    let ``CAP-models.R4 TimeZone gives Time zone`` () =
+        Assert.Equal("Time zone", labelOf "TimeZone")
+
+    [<Fact>]
+    let ``CAP-models.R4 ExternalId gives External id`` () =
+        Assert.Equal("External id", labelOf "ExternalId")
+
+    [<Fact>]
+    let ``CAP-models.R4 HasDue gives Has due`` () =
+        Assert.Equal("Has due", labelOf "HasDue")
+
+    [<Fact>]
+    let ``CAP-models.R4 the label of a described field comes from its name`` () =
+        let m = describeOk typeof<Todo>
+        Assert.Equal("Completed at", (field m "CompletedAt").Label)
+
+module ``CAP-models-R5`` =
+
+    [<Fact>]
+    let ``CAP-models.R5 Notes is wide`` () =
+        let m = describeOk typeof<Todo>
+        Assert.True(isWide (field m "Notes").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R5 Title is not wide`` () =
+        let m = describeOk typeof<Todo>
+        Assert.False(isWide (field m "Title").Kind)
+
+    [<Fact>]
+    let ``CAP-models.R5 text of length 400 is not wide`` () =
+        Assert.False(isWide (Text 400))
+
+    [<Fact>]
+    let ``CAP-models.R5 text of length 401 is wide`` () =
+        Assert.True(isWide (Text 401))
+
+    [<Fact>]
+    let ``CAP-models.R5 a whole number is not wide`` () =
+        Assert.False(isWide WholeNumber)
+
+module ``CAP-models-R6`` =
+
+    let private titleOf (t: Type) (items: OverrideItem list) =
+        let m = describeOk t
+        titleFieldName m.Fields items
+
+    [<Fact>]
+    let ``CAP-models.R6 TodoList has the title field Name`` () =
+        Assert.Equal(Some "Name", titleOf typeof<TodoList> [])
+
+    [<Fact>]
+    let ``CAP-models.R6 User has the title field DisplayName`` () =
+        Assert.Equal(Some "DisplayName", titleOf typeof<User> [])
+
+    [<Fact>]
+    let ``CAP-models.R6 NoTitle has no title field`` () =
+        Assert.Equal(None, titleOf typeof<NoTitle> [])
+
+    [<Fact>]
+    let ``CAP-models.R6 the titleField override names Count`` () =
+        let mo = overrides<NoTitle> [ titleField <@ fun (n: NoTitle) -> n.Count @> ]
+        Assert.Equal(Some "Count", titleOf typeof<NoTitle> mo.Items)
+
+module ``CAP-models-R7`` =
+
+    [<Fact>]
+    let ``CAP-models.R7 the selector for Due reads the name Due`` () =
+        let q = <@ fun (t: Todo) -> t.Due @> :> Expr
+        Assert.Equal("Due", fieldName q)
+
+    [<Fact>]
+    let ``CAP-models.R7 listSort gives one ListSort item for Due`` () =
+        let mo = overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Due @> Ascending ]
+        Assert.Equal("Todo", mo.ModelName)
+        match mo.Items with
+        | [ ListSort(f, d) ] ->
+            Assert.Equal("Due", f)
+            Assert.Equal(Ascending, d)
+        | other -> failwithf "Expected one ListSort item, got %d items" other.Length
+
+    [<Fact>]
+    let ``CAP-models.R7 listColumns holds Title then Due in order`` () =
+        let mo =
+            overrides<Todo>
+                [ listColumns
+                      [ col <@ fun (t: Todo) -> t.Title @>
+                        col <@ fun (t: Todo) -> t.Due @> ] ]
+        match mo.Items with
+        | [ ListColumns fs as item ] ->
+            Assert.Equal<string list>([ "Title"; "Due" ], fs)
+            Assert.Equal<string list>([ "Title"; "Due" ], fieldsOf item)
+        | other -> failwithf "Expected one ListColumns item, got %d items" other.Length
+
+    [<Fact>]
+    let ``CAP-models.R7 listView uses the field Archived only`` () =
+        let mo =
+            overrides<TodoList> [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @> ]
+        match mo.Items with
+        | [ ListView(name, filter) as item ] ->
+            Assert.Equal("Active", name)
+            Assert.Equal<string list>([ "Archived" ], referencedFields filter)
+            Assert.Equal<string list>([ "Archived" ], fieldsOf item)
+        | other -> failwithf "Expected one ListView item, got %d items" other.Length
diff --git a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
new file mode 100644
index 0000000..f3ba2c6
--- /dev/null
+++ b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
@@ -0,0 +1,22 @@
+<Project Sdk="Microsoft.NET.Sdk">
+
+  <PropertyGroup>
+    <TargetFramework>net8.0</TargetFramework>
+    <IsPackable>false</IsPackable>
+  </PropertyGroup>
+
+  <ItemGroup>
+    <Compile Include="CAP-models.Tests.fs" />
+  </ItemGroup>
+
+  <ItemGroup>
+    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
+    <PackageReference Include="xunit" Version="2.6.2" />
+    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
+  </ItemGroup>
+
+  <ItemGroup>
+    <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
+  </ItemGroup>
+
+</Project>
````

### tests/Typed.Engine.Tests/CAP-models.Tests.fs

````
namespace Typed.Engine.Tests

open System
open Microsoft.FSharp.Quotations
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides

/// Models, semantic types, and type defaults of the todo app. The checks use only these.
module ModelsFixtures =

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

    type NoTitle = { Count: int }

    type Tagged = { Tags: Title list }

    type TwoMissing = { Tags: Title list; Aliases: Notes list }

    let private def (clr: Type) kind : TypeDefault =
        { Clr = clr
          Kind = kind
          Check = (fun _ -> [])
          Hidden = false
          Unique = false }

    /// Exactly the four entries of the spec. The engine gives Priority and Ref kinds itself.
    let defaults: TypeDefault list =
        [ def typeof<Title> (Text 200)
          def typeof<Notes> (Text 5000)
          def typeof<TimeZoneId> (Text 64)
          { def typeof<ExternalId> (Text 255) with
              Hidden = true
              Unique = true } ]

    let describeOk (t: Type) : Model =
        match describe defaults t with
        | Ok m -> m
        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs

    let describeErrors (t: Type) : Finding list =
        match describe defaults t with
        | Ok m -> failwithf "Expected errors, got a plan for %s" m.Name
        | Result.Error fs -> fs

    let field (m: Model) (name: string) : Field =
        m.Fields |> List.find (fun f -> f.Name = name)

open ModelsFixtures

module ``CAP-models-R1`` =

    [<Fact>]
    let ``CAP-models.R1 Title has kind text 200`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Text 200, (field m "Title").Kind)

    [<Fact>]
    let ``CAP-models.R1 Notes has kind optional of text 5000`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Optional(Text 5000), (field m "Notes").Kind)

    [<Fact>]
    let ``CAP-models.R1 Priority has kind enum with Low Normal High`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Enum [ "Low"; "Normal"; "High" ], (field m "Priority").Kind)

    [<Fact>]
    let ``CAP-models.R1 Due has kind optional of date`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Optional Date, (field m "Due").Kind)

    [<Fact>]
    let ``CAP-models.R1 List has kind reference to TodoList`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Reference "TodoList", (field m "List").Kind)

    [<Fact>]
    let ``CAP-models.R1 Done is boolean and CompletedAt is optional of timestamp`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal(Boolean, (field m "Done").Kind)
        Assert.Equal(Optional Timestamp, (field m "CompletedAt").Kind)

    [<Fact>]
    let ``CAP-models.R1 a union type as a model is WRONG_CONFIG_TYPE`` () =
        let fs = describeErrors typeof<Priority>
        Assert.Equal(1, fs.Length)
        Assert.Equal(ErrorCode.WRONG_CONFIG_TYPE, fs.Head.Code)
        Assert.Equal("Priority is not a record type.", fs.Head.Message)

module ``CAP-models-R2`` =

    [<Fact>]
    let ``CAP-models.R2 a list of Title has no default and names each engine`` () =
        let fs = describeErrors typeof<Tagged>
        Assert.Equal(1, fs.Length)
        let f = fs.Head
        Assert.Equal(ErrorCode.MISSING_DEFAULT, f.Code)
        Assert.Equal("Tagged", f.Model)
        Assert.Equal(Some "Tags", f.Field)
        Assert.Equal(
            "Tagged.Tags has type 'Title list', which has no default in Persist, Validate, Display, API.",
            f.Message
        )

    [<Fact>]
    let ``CAP-models.R2 two fields with no default give two MISSING_DEFAULT errors`` () =
        let fs = describeErrors typeof<TwoMissing>
        Assert.Equal(2, fs.Length)
        Assert.All(fs, fun f -> Assert.Equal(ErrorCode.MISSING_DEFAULT, f.Code))
        let names = fs |> List.map (fun f -> f.Field) |> List.sort
        Assert.Equal<string option list>([ Some "Aliases"; Some "Tags" ], names)

module ``CAP-models-R3`` =

    [<Fact>]
    let ``CAP-models.R3 the system field names are known`` () =
        Assert.Equal<string list>([ "Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt" ], systemFields)

    [<Fact>]
    let ``CAP-models.R3 the plan of Todo holds its seven fields and no system field`` () =
        let m = describeOk typeof<Todo>
        let names = m.Fields |> List.map (fun f -> f.Name)
        Assert.Equal<string list>(
            [ "List"; "Title"; "Notes"; "Priority"; "Due"; "Done"; "CompletedAt" ],
            names
        )
        for s in systemFields do
            Assert.DoesNotContain(s, names)

module ``CAP-models-R4`` =

    [<Fact>]
    let ``CAP-models.R4 CompletedAt gives Completed at`` () =
        Assert.Equal("Completed at", labelOf "CompletedAt")

    [<Fact>]
    let ``CAP-models.R4 Due gives Due`` () = Assert.Equal("Due", labelOf "Due")

    [<Fact>]
    let ``CAP-models.R4 TimeZone gives Time zone`` () =
        Assert.Equal("Time zone", labelOf "TimeZone")

    [<Fact>]
    let ``CAP-models.R4 ExternalId gives External id`` () =
        Assert.Equal("External id", labelOf "ExternalId")

    [<Fact>]
    let ``CAP-models.R4 HasDue gives Has due`` () =
        Assert.Equal("Has due", labelOf "HasDue")

    [<Fact>]
    let ``CAP-models.R4 the label of a described field comes from its name`` () =
        let m = describeOk typeof<Todo>
        Assert.Equal("Completed at", (field m "CompletedAt").Label)

module ``CAP-models-R5`` =

    [<Fact>]
    let ``CAP-models.R5 Notes is wide`` () =
        let m = describeOk typeof<Todo>
        Assert.True(isWide (field m "Notes").Kind)

    [<Fact>]
    let ``CAP-models.R5 Title is not wide`` () =
        let m = describeOk typeof<Todo>
        Assert.False(isWide (field m "Title").Kind)

    [<Fact>]
    let ``CAP-models.R5 text of length 400 is not wide`` () =
        Assert.False(isWide (Text 400))

    [<Fact>]
    let ``CAP-models.R5 text of length 401 is wide`` () =
        Assert.True(isWide (Text 401))

    [<Fact>]
    let ``CAP-models.R5 a whole number is not wide`` () =
        Assert.False(isWide WholeNumber)

module ``CAP-models-R6`` =

    let private titleOf (t: Type) (items: OverrideItem list) =
        let m = describeOk t
        titleFieldName m.Fields items

    [<Fact>]
    let ``CAP-models.R6 TodoList has the title field Name`` () =
        Assert.Equal(Some "Name", titleOf typeof<TodoList> [])

    [<Fact>]
    let ``CAP-models.R6 User has the title field DisplayName`` () =
        Assert.Equal(Some "DisplayName", titleOf typeof<User> [])

    [<Fact>]
    let ``CAP-models.R6 NoTitle has no title field`` () =
        Assert.Equal(None, titleOf typeof<NoTitle> [])

    [<Fact>]
    let ``CAP-models.R6 the titleField override names Count`` () =
        let mo = overrides<NoTitle> [ titleField <@ fun (n: NoTitle) -> n.Count @> ]
        Assert.Equal(Some "Count", titleOf typeof<NoTitle> mo.Items)

module ``CAP-models-R7`` =

    [<Fact>]
    let ``CAP-models.R7 the selector for Due reads the name Due`` () =
        let q = <@ fun (t: Todo) -> t.Due @> :> Expr
        Assert.Equal("Due", fieldName q)

    [<Fact>]
    let ``CAP-models.R7 listSort gives one ListSort item for Due`` () =
        let mo = overrides<Todo> [ listSort <@ fun (t: Todo) -> t.Due @> Ascending ]
        Assert.Equal("Todo", mo.ModelName)
        match mo.Items with
        | [ ListSort(f, d) ] ->
            Assert.Equal("Due", f)
            Assert.Equal(Ascending, d)
        | other -> failwithf "Expected one ListSort item, got %d items" other.Length

    [<Fact>]
    let ``CAP-models.R7 listColumns holds Title then Due in order`` () =
        let mo =
            overrides<Todo>
                [ listColumns
                      [ col <@ fun (t: Todo) -> t.Title @>
                        col <@ fun (t: Todo) -> t.Due @> ] ]
        match mo.Items with
        | [ ListColumns fs as item ] ->
            Assert.Equal<string list>([ "Title"; "Due" ], fs)
            Assert.Equal<string list>([ "Title"; "Due" ], fieldsOf item)
        | other -> failwithf "Expected one ListColumns item, got %d items" other.Length

    [<Fact>]
    let ``CAP-models.R7 listView uses the field Archived only`` () =
        let mo =
            overrides<TodoList> [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @> ]
        match mo.Items with
        | [ ListView(name, filter) as item ] ->
            Assert.Equal("Active", name)
            Assert.Equal<string list>([ "Archived" ], referencedFields filter)
            Assert.Equal<string list>([ "Archived" ], fieldsOf item)
        | other -> failwithf "Expected one ListView item, got %d items" other.Length
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
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
  </ItemGroup>

</Project>
````

