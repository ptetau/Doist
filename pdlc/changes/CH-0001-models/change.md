# CH-0001 · Models

Intent: IN-0001 · Scope: CAP-models · Status: testing · Ticket: none · Branch: pdlc/CH-0001-models · Base: ccr-1ef8bdfa-1cxxi7
Tests-Locked: 5dba628

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

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement, named with the requirement ID in double backticks, such as ``module ``CAP-models.R1`` ``. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

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
