# Review packet · CH-0001-models · security

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: security

Default: yes

You judge if the change is safe.

- The code validates input from users, files, or the network as the requirements say,
  before it uses the input.
- There is no injection: the code builds commands, queries, paths, and HTML safely.
- There are no secrets in code, logs, or errors.
- Permissions and access checks are correct, and they fail closed.
- Each new dependency is necessary and trustworthy.

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

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

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

### Step 1 · Each field gets a kind from its type · [x]

- Requirement: CAP-models.R1
- Check: the checks named `CAP-models.R1 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R1 (each field gets a kind from its type) passes

### Step 2 · A type with no default is an error that names each engine · [x]

- Requirement: CAP-models.R2
- Check: the checks named `CAP-models.R2 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R2 (a type with no default is an error that names each engine) passes

### Step 3 · The system fields are known and never written from a model · [x]

- Requirement: CAP-models.R3
- Check: the checks named `CAP-models.R3 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R3 (the system fields are known and never written from a model) passes

### Step 4 · The label comes from the field name · [x]

- Requirement: CAP-models.R4
- Check: the checks named `CAP-models.R4 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R4 (the label comes from the field name) passes

### Step 5 · Wide text is text longer than 400 characters · [x]

- Requirement: CAP-models.R5
- Check: the checks named `CAP-models.R5 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R5 (wide text is text longer than 400 characters) passes

### Step 6 · The title field is marked or named · [x]

- Requirement: CAP-models.R6
- Check: the checks named `CAP-models.R6 ...` in `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj`
- Done when: every check of CAP-models.R6 (the title field is marked or named) passes

### Step 7 · Overrides take typed field selectors · [x]

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
- 2026-10-19T05:48:21+00:00 · Build: the builder wrote `Model.fs`, `Overrides.fs`, the project file, and
  `.gitignore`. All 30 checks pass. The check files did not change after the lock. Commit
  `367dc43`.
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

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

When a field has a type that no default covers, the description fails. The error names
the model, the field, the type, and each engine.

- Given a model `Tagged` with a field `Tags: Title list`. When the engine describes
  `Tagged`, then the result is the error `MISSING_DEFAULT`. The message is
  "Tagged.Tags has type 'Title list', which has no default in Persist, Validate,
  Display, API."
- Given a model with two fields that have no default. When the engine describes the
  model, then the result holds one `MISSING_DEFAULT` error for each field.

### CAP-models.R3 · The system fields are known and never written from a model

Status: built · Intents: IN-0001

The system fields are `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`. A model
does not declare them. The plan knows their names.

- Given any model, when a caller asks for the system field names, then the names are
  `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`.
- Given the model `Todo`. When the engine describes `Todo`, then the fields of the plan
  are the seven fields of the record, and no system field.

### CAP-models.R4 · The label comes from the field name

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

- Given the field `Notes` with the kind optional of text with the length 5000. When a
  caller asks if the field is wide, then the answer is yes.
- Given the field `Title` with the kind text with the length 200. When a caller asks if
  the field is wide, then the answer is no.
- Given a text type with the length 400, when a caller asks if the field is wide, then the
  answer is no.
- Given a text type with the length 401, when a caller asks if the field is wide, then the
  answer is yes.

### CAP-models.R6 · The title field is marked or named

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

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

## The code

### Diff against ccr-1ef8bdfa-1cxxi7

````
diff --git a/.gitignore b/.gitignore
index e73bd8f..bb486d4 100644
--- a/.gitignore
+++ b/.gitignore
@@ -1 +1,4 @@
 pdlc/.tools/
+bin/
+obj/
+build/
diff --git a/src/Typed.Engine/Model.fs b/src/Typed.Engine/Model.fs
new file mode 100644
index 0000000..64927d0
--- /dev/null
+++ b/src/Typed.Engine/Model.fs
@@ -0,0 +1,212 @@
+module Typed.Engine.Model
+
+open System
+open System.Reflection
+open Microsoft.FSharp.Reflection
+
+/// Marks a semantic type whose fields are the title of a record.
+[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
+type TitleTypeAttribute() =
+    inherit Attribute()
+
+/// A reference to a record of the model 'T.
+type Ref<'T> = Ref of Guid
+
+type Kind =
+    | Text of maxLength: int
+    | WholeNumber
+    | Decimal
+    | Boolean
+    | Enum of cases: string list
+    | Date
+    | Timestamp
+    | Id
+    | Reference of target: string
+    | Optional of Kind
+
+type ErrorCode =
+    | NOT_FOUND
+    | STALE_VERSION
+    | READ_ONLY_RECORD
+    | VALIDATION_FAILED
+    | MISSING_DEFAULT
+    | MISSING_ACCESS_RULE
+    | MISSING_FIELD
+    | WRONG_CONFIG_TYPE
+    | SYSTEM_FIELD_TARGET
+    | DANGLING_REFERENCE
+    | NO_TITLE_FIELD
+    | UNPLANNED_DROP
+    | UNSAFE_ESCAPE_HATCH
+    | REDUNDANT_OVERRIDE
+    | UNSAFE_MIGRATION
+    | SHAPE_UNREADABLE
+
+type Severity =
+    | Error
+    | Warning
+
+type Finding =
+    { Severity: Severity
+      Code: ErrorCode
+      Model: string
+      Field: string option
+      Message: string }
+
+type TypeDefault =
+    { Clr: Type
+      Kind: Kind
+      Check: string -> string list
+      Hidden: bool
+      Unique: bool }
+
+type Field =
+    { Name: string
+      Label: string
+      Kind: Kind
+      Prop: PropertyInfo
+      Core: Type
+      Check: string -> string list
+      Hidden: bool
+      Unique: bool
+      IsTitle: bool }
+
+type Model =
+    { Name: string
+      Clr: Type
+      Fields: Field list }
+
+/// A stored record as the store and the access rules see it.
+type Record =
+    { Model: string
+      Id: Guid
+      Owner: Guid option
+      Version: int64
+      Values: Map<string, obj> }
+
+/// The context that hooks get.
+type HookContext =
+    { User: Guid
+      TimeZone: TimeZoneInfo
+      Now: DateTimeOffset }
+
+let localToday (ctx: HookContext) : DateOnly =
+    DateOnly.FromDateTime((TimeZoneInfo.ConvertTime(ctx.Now, ctx.TimeZone)).DateTime)
+
+let engines: string list = [ "Persist"; "Validate"; "Display"; "API" ]
+
+let systemFields: string list = [ "Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt" ]
+
+/// The label of a field name: words split at capitals, only the first word capitalised.
+let labelOf (name: string) : string =
+    let spaced =
+        name
+        |> Seq.mapi (fun i c -> if i > 0 && Char.IsUpper c then " " + string (Char.ToLower c) else string (Char.ToLower c))
+        |> String.concat ""
+    if spaced = "" then spaced else string (Char.ToUpper spaced[0]) + spaced.Substring 1
+
+let rec innerKind (kind: Kind) : Kind =
+    match kind with
+    | Optional k -> innerKind k
+    | k -> k
+
+let isOptional (kind: Kind) : bool =
+    match kind with
+    | Optional _ -> true
+    | _ -> false
+
+let isWide (kind: Kind) : bool =
+    match innerKind kind with
+    | Text n -> n > 400
+    | _ -> false
+
+let rec refTarget (kind: Kind) : string option =
+    match kind with
+    | Reference t -> Some t
+    | Optional k -> refTarget k
+    | _ -> None
+
+/// The name of a type as a person reads it, such as `Title list`.
+let rec displayType (t: Type) : string =
+    if t.IsGenericType then
+        let arg = displayType (t.GetGenericArguments()[0])
+        match t.GetGenericTypeDefinition().Name with
+        | "FSharpList`1" -> arg + " list"
+        | "FSharpOption`1" -> arg + " option"
+        | _ -> t.Name.Substring(0, t.Name.IndexOf '`') + "<" + arg + ">"
+    else
+        t.Name
+
+let private isGeneric (def: Type) (t: Type) =
+    t.IsGenericType && t.GetGenericTypeDefinition() = def
+
+let private isEnumUnion (t: Type) =
+    FSharpType.IsUnion(t, true)
+    && not (isGeneric typedefof<option<_>> t)
+    && FSharpType.GetUnionCases(t, true) |> Array.forall (fun c -> c.GetFields().Length = 0)
+
+let private builtinKind (t: Type) : Kind option =
+    if t = typeof<bool> then Some Boolean
+    elif t = typeof<int> || t = typeof<int64> then Some WholeNumber
+    elif t = typeof<decimal> then Some Decimal
+    elif t = typeof<DateOnly> then Some Date
+    elif t = typeof<DateTimeOffset> then Some Timestamp
+    elif t = typeof<Guid> then Some Id
+    elif isGeneric typedefof<Ref<_>> t then Some(Reference((t.GetGenericArguments()[0]).Name))
+    elif isEnumUnion t then Some(Enum(FSharpType.GetUnionCases(t, true) |> Array.map (fun c -> c.Name) |> List.ofArray))
+    else None
+
+let private isTitleType (t: Type) =
+    t.GetCustomAttributes(typeof<TitleTypeAttribute>, false).Length > 0
+
+let describe (defaults: TypeDefault list) (t: Type) : Result<Model, Finding list> =
+    if not (FSharpType.IsRecord(t, true)) then
+        Result.Error
+            [ { Severity = Error
+                Code = WRONG_CONFIG_TYPE
+                Model = t.Name
+                Field = None
+                Message = sprintf "%s is not a record type." t.Name } ]
+    else
+        let describeField (prop: PropertyInfo) : Result<Field, Finding> =
+            let isOpt = isGeneric typedefof<option<_>> prop.PropertyType
+            let core = if isOpt then prop.PropertyType.GetGenericArguments()[0] else prop.PropertyType
+            let found =
+                match defaults |> List.tryFind (fun d -> d.Clr = core) with
+                | Some d -> Some(d.Kind, d.Check, d.Hidden, d.Unique)
+                | None -> builtinKind core |> Option.map (fun k -> k, (fun _ -> []), false, false)
+            match found with
+            | Some(kind, check, hidden, unique) ->
+                Ok
+                    { Name = prop.Name
+                      Label = labelOf prop.Name
+                      Kind = if isOpt then Optional kind else kind
+                      Prop = prop
+                      Core = core
+                      Check = check
+                      Hidden = hidden
+                      Unique = unique
+                      IsTitle = isTitleType core }
+            | None ->
+                Result.Error
+                    { Severity = Error
+                      Code = MISSING_DEFAULT
+                      Model = t.Name
+                      Field = Some prop.Name
+                      Message =
+                        sprintf
+                            "%s.%s has type '%s', which has no default in %s."
+                            t.Name
+                            prop.Name
+                            (displayType prop.PropertyType)
+                            (String.Join(", ", engines)) }
+
+        let results = FSharpType.GetRecordFields(t, true) |> List.ofArray |> List.map describeField
+        let errors = results |> List.choose (function Result.Error f -> Some f | Ok _ -> None)
+        if errors.IsEmpty then
+            Ok
+                { Name = t.Name
+                  Clr = t
+                  Fields = results |> List.choose (function Ok f -> Some f | Result.Error _ -> None) }
+        else
+            Result.Error errors
diff --git a/src/Typed.Engine/Overrides.fs b/src/Typed.Engine/Overrides.fs
new file mode 100644
index 0000000..9567cac
--- /dev/null
+++ b/src/Typed.Engine/Overrides.fs
@@ -0,0 +1,100 @@
+module Typed.Engine.Overrides
+
+open System
+open Microsoft.FSharp.Quotations
+open Microsoft.FSharp.Quotations.Patterns
+open Typed.Engine.Model
+
+type Direction =
+    | Ascending
+    | Descending
+
+type OverrideItem =
+    | Label of field: string * text: string
+    | HiddenInForm of string
+    | ReadOnly of string
+    | ListColumns of string list
+    | ListEdit of string
+    | ListSort of string * Direction
+    | ListView of name: string * filter: Expr
+    | TitleFieldOf of string
+    | Rule of field: string * fieldType: Type * predicate: (obj -> bool) * message: string
+    | Sensitive of string
+    | Column of field: string * name: string
+    | Unsafe of name: string * reason: string
+
+/// One override of the model 'T.
+type Override<'T> = Override of OverrideItem
+
+/// One column of a list, named by a field selector of the model 'T.
+type ColumnRef<'T> = ColumnRef of string
+
+type ModelOverrides =
+    { ModelName: string
+      Items: OverrideItem list }
+
+/// The property name in `<@ fun t -> t.Due @>`.
+let rec fieldName (selector: Expr) : string =
+    match selector with
+    | Lambda(_, body) -> fieldName body
+    | Coerce(e, _) -> fieldName e
+    | PropertyGet(Some _, prop, _) -> prop.Name
+    | _ -> failwithf "Not a field selector: %A" selector
+
+let label (sel: Expr<'T -> 'F>) (text: string) : Override<'T> = Override(Label(fieldName sel, text))
+let hiddenInForm (sel: Expr<'T -> 'F>) : Override<'T> = Override(HiddenInForm(fieldName sel))
+let readOnly (sel: Expr<'T -> 'F>) : Override<'T> = Override(ReadOnly(fieldName sel))
+let listEdit (sel: Expr<'T -> 'F>) : Override<'T> = Override(ListEdit(fieldName sel))
+
+let listSort (sel: Expr<'T -> 'F>) (direction: Direction) : Override<'T> =
+    Override(ListSort(fieldName sel, direction))
+
+let col (sel: Expr<'T -> 'F>) : ColumnRef<'T> = ColumnRef(fieldName sel)
+
+let listColumns (cols: ColumnRef<'T> list) : Override<'T> =
+    Override(ListColumns(cols |> List.map (fun (ColumnRef f) -> f)))
+
+let listView (name: string) (filter: Expr<'T -> bool>) : Override<'T> = Override(ListView(name, filter))
+let titleField (sel: Expr<'T -> 'F>) : Override<'T> = Override(TitleFieldOf(fieldName sel))
+
+let rule (sel: Expr<'T -> 'F>) (predicate: 'F -> bool) (message: string) : Override<'T> =
+    Override(Rule(fieldName sel, typeof<'F>, (fun o -> predicate (unbox<'F> o)), message))
+
+let sensitive (sel: Expr<'T -> 'F>) : Override<'T> = Override(Sensitive(fieldName sel))
+let column (sel: Expr<'T -> 'F>) (name: string) : Override<'T> = Override(Column(fieldName sel, name))
+let unsafe (name: string) (reason: string) : Override<'T> = Override(Unsafe(name, reason))
+
+let overrides<'T> (items: Override<'T> list) : ModelOverrides =
+    { ModelName = typeof<'T>.Name
+      Items = items |> List.map (fun (Override i) -> i) }
+
+/// The names of the properties that an expression reads, in order, with no repeats.
+let referencedFields (expr: Expr) : string list =
+    let rec walk (e: Expr) : string list =
+        match e with
+        | PropertyGet(Some inst, prop, _) -> walk inst @ [ prop.Name ]
+        | ExprShape.ShapeVar _ -> []
+        | ExprShape.ShapeLambda(_, body) -> walk body
+        | ExprShape.ShapeCombination(_, args) -> args |> List.collect walk
+    walk expr |> List.distinct
+
+let fieldsOf (item: OverrideItem) : string list =
+    match item with
+    | Label(f, _)
+    | HiddenInForm f
+    | ReadOnly f
+    | ListEdit f
+    | ListSort(f, _)
+    | TitleFieldOf f
+    | Rule(f, _, _, _)
+    | Sensitive f
+    | Column(f, _) -> [ f ]
+    | ListColumns fs -> fs
+    | ListView(_, filter) -> referencedFields filter
+    | Unsafe _ -> []
+
+/// The title field: an override wins, else the field of a title type.
+let titleFieldName (fields: Field list) (items: OverrideItem list) : string option =
+    match items |> List.tryPick (function TitleFieldOf f -> Some f | _ -> None) with
+    | Some f -> Some f
+    | None -> fields |> List.tryFind (fun f -> f.IsTitle) |> Option.map (fun f -> f.Name)
diff --git a/src/Typed.Engine/Typed.Engine.fsproj b/src/Typed.Engine/Typed.Engine.fsproj
new file mode 100644
index 0000000..fe07324
--- /dev/null
+++ b/src/Typed.Engine/Typed.Engine.fsproj
@@ -0,0 +1,12 @@
+<Project Sdk="Microsoft.NET.Sdk">
+
+  <PropertyGroup>
+    <TargetFramework>net8.0</TargetFramework>
+  </PropertyGroup>
+
+  <ItemGroup>
+    <Compile Include="Model.fs" />
+    <Compile Include="Overrides.fs" />
+  </ItemGroup>
+
+</Project>
````

### .gitignore

````
pdlc/.tools/
bin/
obj/
build/
````

### src/Typed.Engine/Model.fs

````
module Typed.Engine.Model

open System
open System.Reflection
open Microsoft.FSharp.Reflection

/// Marks a semantic type whose fields are the title of a record.
[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type TitleTypeAttribute() =
    inherit Attribute()

/// A reference to a record of the model 'T.
type Ref<'T> = Ref of Guid

type Kind =
    | Text of maxLength: int
    | WholeNumber
    | Decimal
    | Boolean
    | Enum of cases: string list
    | Date
    | Timestamp
    | Id
    | Reference of target: string
    | Optional of Kind

type ErrorCode =
    | NOT_FOUND
    | STALE_VERSION
    | READ_ONLY_RECORD
    | VALIDATION_FAILED
    | MISSING_DEFAULT
    | MISSING_ACCESS_RULE
    | MISSING_FIELD
    | WRONG_CONFIG_TYPE
    | SYSTEM_FIELD_TARGET
    | DANGLING_REFERENCE
    | NO_TITLE_FIELD
    | UNPLANNED_DROP
    | UNSAFE_ESCAPE_HATCH
    | REDUNDANT_OVERRIDE
    | UNSAFE_MIGRATION
    | SHAPE_UNREADABLE

type Severity =
    | Error
    | Warning

type Finding =
    { Severity: Severity
      Code: ErrorCode
      Model: string
      Field: string option
      Message: string }

type TypeDefault =
    { Clr: Type
      Kind: Kind
      Check: string -> string list
      Hidden: bool
      Unique: bool }

type Field =
    { Name: string
      Label: string
      Kind: Kind
      Prop: PropertyInfo
      Core: Type
      Check: string -> string list
      Hidden: bool
      Unique: bool
      IsTitle: bool }

type Model =
    { Name: string
      Clr: Type
      Fields: Field list }

/// A stored record as the store and the access rules see it.
type Record =
    { Model: string
      Id: Guid
      Owner: Guid option
      Version: int64
      Values: Map<string, obj> }

/// The context that hooks get.
type HookContext =
    { User: Guid
      TimeZone: TimeZoneInfo
      Now: DateTimeOffset }

let localToday (ctx: HookContext) : DateOnly =
    DateOnly.FromDateTime((TimeZoneInfo.ConvertTime(ctx.Now, ctx.TimeZone)).DateTime)

let engines: string list = [ "Persist"; "Validate"; "Display"; "API" ]

let systemFields: string list = [ "Id"; "Owner"; "Version"; "CreatedAt"; "UpdatedAt" ]

/// The label of a field name: words split at capitals, only the first word capitalised.
let labelOf (name: string) : string =
    let spaced =
        name
        |> Seq.mapi (fun i c -> if i > 0 && Char.IsUpper c then " " + string (Char.ToLower c) else string (Char.ToLower c))
        |> String.concat ""
    if spaced = "" then spaced else string (Char.ToUpper spaced[0]) + spaced.Substring 1

let rec innerKind (kind: Kind) : Kind =
    match kind with
    | Optional k -> innerKind k
    | k -> k

let isOptional (kind: Kind) : bool =
    match kind with
    | Optional _ -> true
    | _ -> false

let isWide (kind: Kind) : bool =
    match innerKind kind with
    | Text n -> n > 400
    | _ -> false

let rec refTarget (kind: Kind) : string option =
    match kind with
    | Reference t -> Some t
    | Optional k -> refTarget k
    | _ -> None

/// The name of a type as a person reads it, such as `Title list`.
let rec displayType (t: Type) : string =
    if t.IsGenericType then
        let arg = displayType (t.GetGenericArguments()[0])
        match t.GetGenericTypeDefinition().Name with
        | "FSharpList`1" -> arg + " list"
        | "FSharpOption`1" -> arg + " option"
        | _ -> t.Name.Substring(0, t.Name.IndexOf '`') + "<" + arg + ">"
    else
        t.Name

let private isGeneric (def: Type) (t: Type) =
    t.IsGenericType && t.GetGenericTypeDefinition() = def

let private isEnumUnion (t: Type) =
    FSharpType.IsUnion(t, true)
    && not (isGeneric typedefof<option<_>> t)
    && FSharpType.GetUnionCases(t, true) |> Array.forall (fun c -> c.GetFields().Length = 0)

let private builtinKind (t: Type) : Kind option =
    if t = typeof<bool> then Some Boolean
    elif t = typeof<int> || t = typeof<int64> then Some WholeNumber
    elif t = typeof<decimal> then Some Decimal
    elif t = typeof<DateOnly> then Some Date
    elif t = typeof<DateTimeOffset> then Some Timestamp
    elif t = typeof<Guid> then Some Id
    elif isGeneric typedefof<Ref<_>> t then Some(Reference((t.GetGenericArguments()[0]).Name))
    elif isEnumUnion t then Some(Enum(FSharpType.GetUnionCases(t, true) |> Array.map (fun c -> c.Name) |> List.ofArray))
    else None

let private isTitleType (t: Type) =
    t.GetCustomAttributes(typeof<TitleTypeAttribute>, false).Length > 0

let describe (defaults: TypeDefault list) (t: Type) : Result<Model, Finding list> =
    if not (FSharpType.IsRecord(t, true)) then
        Result.Error
            [ { Severity = Error
                Code = WRONG_CONFIG_TYPE
                Model = t.Name
                Field = None
                Message = sprintf "%s is not a record type." t.Name } ]
    else
        let describeField (prop: PropertyInfo) : Result<Field, Finding> =
            let isOpt = isGeneric typedefof<option<_>> prop.PropertyType
            let core = if isOpt then prop.PropertyType.GetGenericArguments()[0] else prop.PropertyType
            let found =
                match defaults |> List.tryFind (fun d -> d.Clr = core) with
                | Some d -> Some(d.Kind, d.Check, d.Hidden, d.Unique)
                | None -> builtinKind core |> Option.map (fun k -> k, (fun _ -> []), false, false)
            match found with
            | Some(kind, check, hidden, unique) ->
                Ok
                    { Name = prop.Name
                      Label = labelOf prop.Name
                      Kind = if isOpt then Optional kind else kind
                      Prop = prop
                      Core = core
                      Check = check
                      Hidden = hidden
                      Unique = unique
                      IsTitle = isTitleType core }
            | None ->
                Result.Error
                    { Severity = Error
                      Code = MISSING_DEFAULT
                      Model = t.Name
                      Field = Some prop.Name
                      Message =
                        sprintf
                            "%s.%s has type '%s', which has no default in %s."
                            t.Name
                            prop.Name
                            (displayType prop.PropertyType)
                            (String.Join(", ", engines)) }

        let results = FSharpType.GetRecordFields(t, true) |> List.ofArray |> List.map describeField
        let errors = results |> List.choose (function Result.Error f -> Some f | Ok _ -> None)
        if errors.IsEmpty then
            Ok
                { Name = t.Name
                  Clr = t
                  Fields = results |> List.choose (function Ok f -> Some f | Result.Error _ -> None) }
        else
            Result.Error errors
````

### src/Typed.Engine/Overrides.fs

````
module Typed.Engine.Overrides

open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Typed.Engine.Model

type Direction =
    | Ascending
    | Descending

type OverrideItem =
    | Label of field: string * text: string
    | HiddenInForm of string
    | ReadOnly of string
    | ListColumns of string list
    | ListEdit of string
    | ListSort of string * Direction
    | ListView of name: string * filter: Expr
    | TitleFieldOf of string
    | Rule of field: string * fieldType: Type * predicate: (obj -> bool) * message: string
    | Sensitive of string
    | Column of field: string * name: string
    | Unsafe of name: string * reason: string

/// One override of the model 'T.
type Override<'T> = Override of OverrideItem

/// One column of a list, named by a field selector of the model 'T.
type ColumnRef<'T> = ColumnRef of string

type ModelOverrides =
    { ModelName: string
      Items: OverrideItem list }

/// The property name in `<@ fun t -> t.Due @>`.
let rec fieldName (selector: Expr) : string =
    match selector with
    | Lambda(_, body) -> fieldName body
    | Coerce(e, _) -> fieldName e
    | PropertyGet(Some _, prop, _) -> prop.Name
    | _ -> failwithf "Not a field selector: %A" selector

let label (sel: Expr<'T -> 'F>) (text: string) : Override<'T> = Override(Label(fieldName sel, text))
let hiddenInForm (sel: Expr<'T -> 'F>) : Override<'T> = Override(HiddenInForm(fieldName sel))
let readOnly (sel: Expr<'T -> 'F>) : Override<'T> = Override(ReadOnly(fieldName sel))
let listEdit (sel: Expr<'T -> 'F>) : Override<'T> = Override(ListEdit(fieldName sel))

let listSort (sel: Expr<'T -> 'F>) (direction: Direction) : Override<'T> =
    Override(ListSort(fieldName sel, direction))

let col (sel: Expr<'T -> 'F>) : ColumnRef<'T> = ColumnRef(fieldName sel)

let listColumns (cols: ColumnRef<'T> list) : Override<'T> =
    Override(ListColumns(cols |> List.map (fun (ColumnRef f) -> f)))

let listView (name: string) (filter: Expr<'T -> bool>) : Override<'T> = Override(ListView(name, filter))
let titleField (sel: Expr<'T -> 'F>) : Override<'T> = Override(TitleFieldOf(fieldName sel))

let rule (sel: Expr<'T -> 'F>) (predicate: 'F -> bool) (message: string) : Override<'T> =
    Override(Rule(fieldName sel, typeof<'F>, (fun o -> predicate (unbox<'F> o)), message))

let sensitive (sel: Expr<'T -> 'F>) : Override<'T> = Override(Sensitive(fieldName sel))
let column (sel: Expr<'T -> 'F>) (name: string) : Override<'T> = Override(Column(fieldName sel, name))
let unsafe (name: string) (reason: string) : Override<'T> = Override(Unsafe(name, reason))

let overrides<'T> (items: Override<'T> list) : ModelOverrides =
    { ModelName = typeof<'T>.Name
      Items = items |> List.map (fun (Override i) -> i) }

/// The names of the properties that an expression reads, in order, with no repeats.
let referencedFields (expr: Expr) : string list =
    let rec walk (e: Expr) : string list =
        match e with
        | PropertyGet(Some inst, prop, _) -> walk inst @ [ prop.Name ]
        | ExprShape.ShapeVar _ -> []
        | ExprShape.ShapeLambda(_, body) -> walk body
        | ExprShape.ShapeCombination(_, args) -> args |> List.collect walk
    walk expr |> List.distinct

let fieldsOf (item: OverrideItem) : string list =
    match item with
    | Label(f, _)
    | HiddenInForm f
    | ReadOnly f
    | ListEdit f
    | ListSort(f, _)
    | TitleFieldOf f
    | Rule(f, _, _, _)
    | Sensitive f
    | Column(f, _) -> [ f ]
    | ListColumns fs -> fs
    | ListView(_, filter) -> referencedFields filter
    | Unsafe _ -> []

/// The title field: an override wins, else the field of a title type.
let titleFieldName (fields: Field list) (items: OverrideItem list) : string option =
    match items |> List.tryPick (function TitleFieldOf f -> Some f | _ -> None) with
    | Some f -> Some f
    | None -> fields |> List.tryFind (fun f -> f.IsTitle) |> Option.map (fun f -> f.Name)
````

### src/Typed.Engine/Typed.Engine.fsproj

````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Model.fs" />
    <Compile Include="Overrides.fs" />
  </ItemGroup>

</Project>
````

