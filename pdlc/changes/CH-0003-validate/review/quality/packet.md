# Review packet · CH-0003-validate · quality

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: quality

Default: yes

You judge if the code is easy to read and change.

- Names tell what things are and what they do.
- There is no unnecessary repetition and no dead code.
- The code handles the errors that the requirements name, and the error messages help the
  person who sees them.
- The code follows the design specs in this packet.
- The code is as simple as the requirement lets it be.

## The change spec

````
# CH-0003 · Validate

Intent: IN-0001 · Scope: CAP-validate · Status: building · Ticket: none · Branch: pdlc/CH-0003-validate · Base: pdlc/CH-0002-access
Tests-Locked: 6f34681

## Delivers

One rule set checks a record and gives the errors for each field. Raw text parses by kind, or gives a field error.

## Requirements

### CAP-validate.R1 · Validation returns one error list for each field

- Given the list `{ Name = Title "Groceries"; Archived = false }`, when validation runs,
  then the result is an empty list.
- Given a record with two broken fields. When validation runs, then the result holds one
  error for each broken field, each with its own field name.

### CAP-validate.R2 · A `Title` is 1 to 200 characters on one line, trimmed

- Given a `TodoList` with the name "  Groceries  ", when validation runs, then the
  result is an empty list.
- Given a `TodoList` with the name "   ", when validation runs, then the result is one
  error on `Name` with the message "is required".
- Given a `TodoList` with a name of 200 characters, when validation runs, then the result
  is an empty list.
- Given a `TodoList` with a name of 201 characters. When validation runs, then the result
  is one error on `Name` with the message "must be 200 characters or fewer".
- Given a `TodoList` with the name "Shop\nlater". When validation runs, then the result is
  one error on `Name` with the message "must be on one line".
- Given a `TodoList` with the name "Shop\tlater". When validation runs, then the result
  is an empty list, because a tab is not a line break. (assumed)
- Given a `TodoList` with a name of 200 characters and three spaces at each end. When
  validation runs, then the result is an empty list.
- Given a `TodoList` with the name "<script>alert(1)</script>". When validation runs,
  then the result is an empty list. Encoding is the job of the display. (assumed)

### CAP-validate.R3 · `Notes` is up to 5,000 characters and can be empty

- Given a `Todo` with `Notes = None`, when validation runs, then there is no error on
  `Notes`.
- Given a `Todo` with notes of 5000 characters, when validation runs, then there is no
  error on `Notes`.
- Given a `Todo` with notes of 5001 characters. When validation runs, then the result is
  one error on `Notes` with the message "must be 5000 characters or fewer".
- Given a `Todo` with notes of 5000 characters and one space at the end. When validation
  runs, then the result is one error on `Notes`. The length counts as given, with no trim.
  (assumed)

### CAP-validate.R4 · A `TimeZoneId` is a known IANA zone

- Given a `User` with the time zone "Pacific/Auckland", when validation runs, then there
  is no error on `TimeZone`.
- Given a `User` with the time zone "Mars/Base". When validation runs, then the result is
  one error on `TimeZone` with the message "must be a known time zone, such as
  Pacific/Auckland".
- Given a `User` with the time zone "pacific/auckland". When validation runs, then the
  result is one error on `TimeZone`, because the name is case-sensitive. (assumed)

### CAP-validate.R5 · A reference must point at a record

- Given a `Todo` with `List = Ref Guid.Empty`, when validation runs, then the result is one
  error on `List` with the message "is required".
- Given a `Todo` with `List = Ref` of a non-empty id, when validation runs, then there is no
  error on `List`.

### CAP-validate.R6 · A `rule` override adds its message when its predicate fails

- Given the override `rule <@ fun t -> t.Title @> (fun (Title s) -> s <> "forbidden")
  "Title is reserved"`, and a `Todo` with the title "forbidden". When validation runs,
  then the result is one error on `Title` with the message "Title is reserved".
- Given the same override and a `Todo` with the title "Pay rent", when validation runs,
  then there is no error on `Title`.

### CAP-validate.R7 · An enum value must be a known case

- Given the raw value "Urgent" for the field `Priority` of `Todo`. When the engine parses
  the value, then the result is one error on `Priority` with the message "must be one of
  Low, Normal, High". (assumed)
- Given the raw value "High" for the field `Priority`, when the engine parses the value,
  then the result is the case `High`.

### CAP-validate.R8 · Raw input parses by kind, or gives a field error

- Given the raw value "abc" for a whole number field. When the engine parses the value,
  then the result is one error with the message "must be a whole number". (assumed)
- Given the raw value "2026-02-30" for the field `Due`. When the engine parses the value,
  then the result is one error with the message "must be a date in the form YYYY-MM-DD".
  (assumed)
- Given the raw value "2026-10-18" for the field `Due`, when the engine parses the value,
  then the result is `Some (DateOnly(2026, 10, 18))`.
- Given the raw value "" for the optional field `Due`, when the engine parses the value,
  then the result is `None`.
- Given the raw value "" for the required field `Title`. When the engine parses the
  value, then the result is one error with the message "is required".
- Given the raw values "on", "true", and "false" for `Done`. When the engine parses each
  value, then the results are `true`, `true`, and `false`. A missing value is `false`.
  (assumed)
- Given the raw value "yes" for the field `Done`. When the engine parses the value, then
  the result is one error with the message "must be true or false". (assumed)

## Relies on

- CAP-models (used, not changed)
- DES-names
- DES-errors
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

In `src/Typed.Engine/Validate.fs`, module `Typed.Engine.Validate`:

- `type FieldError = { Field: string; Message: string }`
- `validate: Model -> OverrideItem list -> obj -> FieldError list`: checks a typed F# record. An empty list is a pass.
- `parseField: Field -> string option -> Result<obj, FieldError>`: parses one raw value by kind. The result is the typed value, boxed. For an optional field, the result is a boxed `option`. A missing boolean is `false`.
  - messages, one for each kind:
    - "is required"
    - "must be a whole number"
    - "must be a date in the form YYYY-MM-DD"
    - "must be true or false"
    - "must be one of Low, Normal, High" (the cases of the enum, in order)
- Every check in this spec gives errors as values. No function raises.

Where the rules come from. The kind gives the length and the required rule. The semantic
type gives its own rule through `TypeDefault.Check: string -> string list`, which takes
the trimmed text and gives the messages. `validate` runs that function for a text field.
The todo app's defaults are these, and the checks declare them in the check file:

```fsharp
let oneLine (s: string) = if s.IndexOfAny([| '\r'; '\n' |]) >= 0 then [ "must be on one line" ] else []
// An exact match on the zone id. FindSystemTimeZoneById ignores case on Linux, so it is
// not used here.
let knownZone (s: string) =
    if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then []
    else [ "must be a known time zone, such as Pacific/Auckland" ]

let defaults : TypeDefault list =
    [ { Clr = typeof<Title>;      Kind = Text 200;  Check = oneLine;        Hidden = false; Unique = false }
      { Clr = typeof<Notes>;      Kind = Text 5000; Check = (fun _ -> []); Hidden = false; Unique = false }
      { Clr = typeof<TimeZoneId>; Kind = Text 64;   Check = knownZone;      Hidden = false; Unique = false }
      { Clr = typeof<ExternalId>; Kind = Text 255;  Check = (fun _ -> []); Hidden = true;  Unique = true } ]
```

The `Model` that `validate` takes comes from `describe defaults typeof<Todo>`, with these
defaults, so each `Field.Check` carries the rule of its type.

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

none

## Files

- src/Typed.Engine/Validate.fs
- src/Typed.Engine/Typed.Engine.fsproj: the Compile entry of this spec

## Check files

- tests/Typed.Engine.Tests/CAP-validate.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

## Steps

### Step 1 · Validation returns one error list for each field · [x]

- Requirement: CAP-validate.R1
- Check: the checks named `CAP-validate.R1 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R1 (validation returns one error list for each field) passes

### Step 2 · A `Title` is 1 to 200 characters on one line, trimmed · [x]

- Requirement: CAP-validate.R2
- Check: the checks named `CAP-validate.R2 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R2 (a `title` is 1 to 200 characters on one line, trimmed) passes

### Step 3 · `Notes` is up to 5,000 characters and can be empty · [x]

- Requirement: CAP-validate.R3
- Check: the checks named `CAP-validate.R3 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R3 (`notes` is up to 5,000 characters and can be empty) passes

### Step 4 · A `TimeZoneId` is a known IANA zone · [x]

- Requirement: CAP-validate.R4
- Check: the checks named `CAP-validate.R4 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R4 (a `timezoneid` is a known iana zone) passes

### Step 5 · A reference must point at a record · [x]

- Requirement: CAP-validate.R5
- Check: the checks named `CAP-validate.R5 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R5 (a reference must point at a record) passes

### Step 6 · A `rule` override adds its message when its predicate fails · [x]

- Requirement: CAP-validate.R6
- Check: the checks named `CAP-validate.R6 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R6 (a `rule` override adds its message when its predicate fails) passes

### Step 7 · An enum value must be a known case · [x]

- Requirement: CAP-validate.R7
- Check: the checks named `CAP-validate.R7 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R7 (an enum value must be a known case) passes

### Step 8 · Raw input parses by kind, or gives a field error · [x]

- Requirement: CAP-validate.R8
- Check: the checks named `CAP-validate.R8 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R8 (raw input parses by kind, or gives a field error) passes

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `CAP-validate` as the ready stage left them.
- 2026-10-19T06:12:12+00:00 · Tests: the test writer wrote 32 checks in `CAP-validate.Tests.fs`. The Interface
  gained the paragraph on where the rules come from, with the todo app defaults. Red: the
  checks fail to compile, because `Typed.Engine.Validate` does not exist yet. Locked at
  7fab997.
- 2026-10-19T06:15:23+00:00 · Build round 1: the builder found that `knownZone` in the Interface accepted
  "pacific/auckland" on Linux. One CAP-validate.R4 (time zone) check failed against correct code. The rule now
  matches the zone id exactly. The test writer mirrored it. All 87 checks pass with the
  builder's code. Locked again at 6f34681.
- 2026-10-19T06:16:19+00:00 · Build: the builder wrote `Validate.fs`. All 87 checks pass. The check files did
  not change after the lock. Commit `9f44a59`. The builder chose the message "cannot be read
  from text" for a decimal, a timestamp, an id, or a reference, which no line covers.
- 2026-10-19T06:21:54+00:00 · Verify round 1 failed. Quality named three repeats in `Validate.fs`. Sent back to
  the build stage. Specification named one long sentence, now split. Filed IN-0008
  (parse messages for all kinds), IN-0009 (parseField length), and IN-0010 (validate
  never raises).
- 2026-10-19T06:23:16+00:00 · Build round 2: the builder removed the three repeats that quality named. All 87
  checks pass. The check files did not change after the lock. Commit `40d743a`.
````

## The spec it covers: title and summary

````
# CAP-validate · Checks a record against one rule set

Status: active

## What it is for

This capability checks the values of a record and returns the errors for each field. One
rule set serves the forms and the API. The rules come from the kind, from the semantic
type, and from `rule` overrides.

## Code map

- src/Typed.Engine/Validate.fs
````

## The requirements, in full

### CAP-validate.R1 · Validation returns one error list for each field

Status: built · Intents: IN-0001

The result of a check is a list of field errors. Each field error holds the field name
and one message. An empty list means that the record passes.

- Given the list `{ Name = Title "Groceries"; Archived = false }`, when validation runs,
  then the result is an empty list.
- Given a record with two broken fields. When validation runs, then the result holds one
  error for each broken field, each with its own field name.

### CAP-validate.R2 · A `Title` is 1 to 200 characters on one line, trimmed

Status: built · Intents: IN-0001

- Given a `TodoList` with the name "  Groceries  ", when validation runs, then the
  result is an empty list.
- Given a `TodoList` with the name "   ", when validation runs, then the result is one
  error on `Name` with the message "is required".
- Given a `TodoList` with a name of 200 characters, when validation runs, then the result
  is an empty list.
- Given a `TodoList` with a name of 201 characters. When validation runs, then the result
  is one error on `Name` with the message "must be 200 characters or fewer".
- Given a `TodoList` with the name "Shop\nlater". When validation runs, then the result is
  one error on `Name` with the message "must be on one line".
- Given a `TodoList` with the name "Shop\tlater". When validation runs, then the result
  is an empty list, because a tab is not a line break. (assumed)
- Given a `TodoList` with a name of 200 characters and three spaces at each end. When
  validation runs, then the result is an empty list.
- Given a `TodoList` with the name "<script>alert(1)</script>". When validation runs,
  then the result is an empty list. Encoding is the job of the display. (assumed)

### CAP-validate.R3 · `Notes` is up to 5,000 characters and can be empty

Status: built · Intents: IN-0001

- Given a `Todo` with `Notes = None`, when validation runs, then there is no error on
  `Notes`.
- Given a `Todo` with notes of 5000 characters, when validation runs, then there is no
  error on `Notes`.
- Given a `Todo` with notes of 5001 characters. When validation runs, then the result is
  one error on `Notes` with the message "must be 5000 characters or fewer".
- Given a `Todo` with notes of 5000 characters and one space at the end. When validation
  runs, then the result is one error on `Notes`. The length counts as given, with no trim.
  (assumed)

### CAP-validate.R4 · A `TimeZoneId` is a known IANA zone

Status: built · Intents: IN-0001

- Given a `User` with the time zone "Pacific/Auckland", when validation runs, then there
  is no error on `TimeZone`.
- Given a `User` with the time zone "Mars/Base". When validation runs, then the result is
  one error on `TimeZone` with the message "must be a known time zone, such as
  Pacific/Auckland".
- Given a `User` with the time zone "pacific/auckland". When validation runs, then the
  result is one error on `TimeZone`, because the name is case-sensitive. (assumed)

### CAP-validate.R5 · A reference must point at a record

Status: built · Intents: IN-0001

- Given a `Todo` with `List = Ref Guid.Empty`, when validation runs, then the result is one
  error on `List` with the message "is required".
- Given a `Todo` with `List = Ref` of a non-empty id, when validation runs, then there is no
  error on `List`.

### CAP-validate.R6 · A `rule` override adds its message when its predicate fails

Status: built · Intents: IN-0001

- Given the override `rule <@ fun t -> t.Title @> (fun (Title s) -> s <> "forbidden")
  "Title is reserved"`, and a `Todo` with the title "forbidden". When validation runs,
  then the result is one error on `Title` with the message "Title is reserved".
- Given the same override and a `Todo` with the title "Pay rent", when validation runs,
  then there is no error on `Title`.

### CAP-validate.R7 · An enum value must be a known case

Status: built · Intents: IN-0001

The F# type makes an unknown case impossible in code. This rule applies to raw input, such
as a form value or a JSON string.

- Given the raw value "Urgent" for the field `Priority` of `Todo`. When the engine parses
  the value, then the result is one error on `Priority` with the message "must be one of
  Low, Normal, High". (assumed)
- Given the raw value "High" for the field `Priority`, when the engine parses the value,
  then the result is the case `High`.

### CAP-validate.R8 · Raw input parses by kind, or gives a field error

Status: built · Intents: IN-0001

A form or the API gives each field as text. The engine parses the text by the kind of the
field. A value that does not parse gives one error on that field.

- Given the raw value "abc" for a whole number field. When the engine parses the value,
  then the result is one error with the message "must be a whole number". (assumed)
- Given the raw value "2026-02-30" for the field `Due`. When the engine parses the value,
  then the result is one error with the message "must be a date in the form YYYY-MM-DD".
  (assumed)
- Given the raw value "2026-10-18" for the field `Due`, when the engine parses the value,
  then the result is `Some (DateOnly(2026, 10, 18))`.
- Given the raw value "" for the optional field `Due`, when the engine parses the value,
  then the result is `None`.
- Given the raw value "" for the required field `Title`. When the engine parses the
  value, then the result is one error with the message "is required".
- Given the raw values "on", "true", and "false" for `Done`. When the engine parses each
  value, then the results are `true`, `true`, and `false`. A missing value is `false`.
  (assumed)
- Given the raw value "yes" for the field `Done`. When the engine parses the value, then
  the result is one error with the message "must be true or false". (assumed)

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

### Diff against pdlc/CH-0002-access

````
diff --git a/src/Typed.Engine/Typed.Engine.fsproj b/src/Typed.Engine/Typed.Engine.fsproj
index a7db7c4..e1167bf 100644
--- a/src/Typed.Engine/Typed.Engine.fsproj
+++ b/src/Typed.Engine/Typed.Engine.fsproj
@@ -8,6 +8,7 @@
     <Compile Include="Model.fs" />
     <Compile Include="Overrides.fs" />
     <Compile Include="Access.fs" />
+    <Compile Include="Validate.fs" />
   </ItemGroup>
 
 </Project>
diff --git a/src/Typed.Engine/Validate.fs b/src/Typed.Engine/Validate.fs
new file mode 100644
index 0000000..df1a238
--- /dev/null
+++ b/src/Typed.Engine/Validate.fs
@@ -0,0 +1,107 @@
+module Typed.Engine.Validate
+
+open System
+open System.Globalization
+open Microsoft.FSharp.Reflection
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+
+type FieldError = { Field: string; Message: string }
+
+let private required = "is required"
+
+/// The value inside an `option`, or None for the empty option.
+let private unwrapOption (value: obj) : obj option =
+    if isNull value then None else Some(value.GetType().GetProperty("Value").GetValue value)
+
+/// The text inside a semantic type such as `Title of string`.
+let private textOf (value: obj) : string =
+    match value with
+    | :? string as s -> s
+    | _ -> string (FSharpValue.GetUnionFields(value, value.GetType(), true) |> snd |> Array.head)
+
+/// The id inside a `Ref`.
+let private idOf (value: obj) : Guid =
+    FSharpValue.GetUnionFields(value, value.GetType(), true) |> snd |> Array.head :?> Guid
+
+/// Wide text keeps its spaces. Other text is trimmed.
+let private textToCheck (field: Field) (raw: string) : string =
+    if isWide field.Kind then raw else raw.Trim()
+
+let private textRules (field: Field) (maxLength: int) (raw: string) : string list =
+    let text = textToCheck field raw
+    if text = "" then [ required ]
+    else
+        [ if text.Length > maxLength then sprintf "must be %d characters or fewer" maxLength
+          yield! field.Check text ]
+
+let private kindErrors (field: Field) (value: obj) : string list =
+    match innerKind field.Kind with
+    | Text n -> textRules field n (textOf value)
+    | Reference _ when idOf value = Guid.Empty -> [ required ]
+    | _ -> []
+
+let private fieldErrors (items: OverrideItem list) (record: obj) (field: Field) : FieldError list =
+    let raw = field.Prop.GetValue record
+    let core = if isOptional field.Kind then unwrapOption raw else Some raw
+    let messages =
+        match core with
+        | None -> []
+        | Some value ->
+            kindErrors field value
+            @ [ for item in items do
+                    match item with
+                    | Rule(f, _, predicate, message) when f = field.Name && not (predicate raw) -> message
+                    | _ -> () ]
+    messages |> List.map (fun m -> { Field = field.Name; Message = m })
+
+/// Check a typed record. An empty list is a pass.
+let validate (model: Model) (items: OverrideItem list) (record: obj) : FieldError list =
+    model.Fields |> List.collect (fieldErrors items record)
+
+let private parseCore (field: Field) (kind: Kind) (raw: string) : Result<obj, string> =
+    let invariant = CultureInfo.InvariantCulture
+    match kind with
+    | Text _ ->
+        let text = textToCheck field raw
+        if text = "" then Result.Error required
+        elif field.Core = typeof<string> then Ok(box text)
+        else Ok(FSharpValue.MakeUnion(FSharpType.GetUnionCases(field.Core, true)[0], [| box text |], true))
+    | WholeNumber ->
+        let isNarrow = field.Core <> typeof<int64>
+        match Int64.TryParse(raw.Trim(), NumberStyles.Integer, invariant) with
+        | true, n when not isNarrow -> Ok(box n)
+        | true, n when n >= int64 Int32.MinValue && n <= int64 Int32.MaxValue -> Ok(box (int n))
+        | _ -> Result.Error "must be a whole number"
+    | Date ->
+        match DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", invariant, DateTimeStyles.None) with
+        | true, d -> Ok(box d)
+        | _ -> Result.Error "must be a date in the form YYYY-MM-DD"
+    | Boolean ->
+        match raw.Trim() with
+        | "on" | "true" -> Ok(box true)
+        | "false" -> Ok(box false)
+        | _ -> Result.Error "must be true or false"
+    | Enum cases ->
+        match FSharpType.GetUnionCases(field.Core, true) |> Array.tryFind (fun c -> c.Name = raw.Trim()) with
+        | Some c -> Ok(FSharpValue.MakeUnion(c, [||], true))
+        | None -> Result.Error("must be one of " + String.Join(", ", cases))
+    | _ -> Result.Error "cannot be read from text"
+
+/// Parse one raw value by kind. An optional field gives a boxed `option`. A missing boolean is false.
+let parseField (field: Field) (raw: string option) : Result<obj, FieldError> =
+    let fail message = Result.Error { Field = field.Name; Message = message }
+    let kind = innerKind field.Kind
+    let text = raw |> Option.defaultValue ""
+    let isEmptyInput = textToCheck field text = ""
+    if kind = Boolean && raw.IsNone then
+        Ok(box false)
+    elif isOptional field.Kind && isEmptyInput then
+        Ok null
+    else
+        match parseCore field kind text with
+        | Result.Error message -> fail message
+        | Ok value when isOptional field.Kind ->
+            let some = FSharpType.GetUnionCases(field.Prop.PropertyType) |> Array.find (fun c -> c.Name = "Some")
+            Ok(FSharpValue.MakeUnion(some, [| value |]))
+        | Ok value -> Ok value
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
    <Compile Include="Access.fs" />
    <Compile Include="Validate.fs" />
  </ItemGroup>

</Project>
````

### src/Typed.Engine/Validate.fs

````
module Typed.Engine.Validate

open System
open System.Globalization
open Microsoft.FSharp.Reflection
open Typed.Engine.Model
open Typed.Engine.Overrides

type FieldError = { Field: string; Message: string }

let private required = "is required"

/// The value inside an `option`, or None for the empty option.
let private unwrapOption (value: obj) : obj option =
    if isNull value then None else Some(value.GetType().GetProperty("Value").GetValue value)

/// The text inside a semantic type such as `Title of string`.
let private textOf (value: obj) : string =
    match value with
    | :? string as s -> s
    | _ -> string (FSharpValue.GetUnionFields(value, value.GetType(), true) |> snd |> Array.head)

/// The id inside a `Ref`.
let private idOf (value: obj) : Guid =
    FSharpValue.GetUnionFields(value, value.GetType(), true) |> snd |> Array.head :?> Guid

/// Wide text keeps its spaces. Other text is trimmed.
let private textToCheck (field: Field) (raw: string) : string =
    if isWide field.Kind then raw else raw.Trim()

let private textRules (field: Field) (maxLength: int) (raw: string) : string list =
    let text = textToCheck field raw
    if text = "" then [ required ]
    else
        [ if text.Length > maxLength then sprintf "must be %d characters or fewer" maxLength
          yield! field.Check text ]

let private kindErrors (field: Field) (value: obj) : string list =
    match innerKind field.Kind with
    | Text n -> textRules field n (textOf value)
    | Reference _ when idOf value = Guid.Empty -> [ required ]
    | _ -> []

let private fieldErrors (items: OverrideItem list) (record: obj) (field: Field) : FieldError list =
    let raw = field.Prop.GetValue record
    let core = if isOptional field.Kind then unwrapOption raw else Some raw
    let messages =
        match core with
        | None -> []
        | Some value ->
            kindErrors field value
            @ [ for item in items do
                    match item with
                    | Rule(f, _, predicate, message) when f = field.Name && not (predicate raw) -> message
                    | _ -> () ]
    messages |> List.map (fun m -> { Field = field.Name; Message = m })

/// Check a typed record. An empty list is a pass.
let validate (model: Model) (items: OverrideItem list) (record: obj) : FieldError list =
    model.Fields |> List.collect (fieldErrors items record)

let private parseCore (field: Field) (kind: Kind) (raw: string) : Result<obj, string> =
    let invariant = CultureInfo.InvariantCulture
    match kind with
    | Text _ ->
        let text = textToCheck field raw
        if text = "" then Result.Error required
        elif field.Core = typeof<string> then Ok(box text)
        else Ok(FSharpValue.MakeUnion(FSharpType.GetUnionCases(field.Core, true)[0], [| box text |], true))
    | WholeNumber ->
        let isNarrow = field.Core <> typeof<int64>
        match Int64.TryParse(raw.Trim(), NumberStyles.Integer, invariant) with
        | true, n when not isNarrow -> Ok(box n)
        | true, n when n >= int64 Int32.MinValue && n <= int64 Int32.MaxValue -> Ok(box (int n))
        | _ -> Result.Error "must be a whole number"
    | Date ->
        match DateOnly.TryParseExact(raw.Trim(), "yyyy-MM-dd", invariant, DateTimeStyles.None) with
        | true, d -> Ok(box d)
        | _ -> Result.Error "must be a date in the form YYYY-MM-DD"
    | Boolean ->
        match raw.Trim() with
        | "on" | "true" -> Ok(box true)
        | "false" -> Ok(box false)
        | _ -> Result.Error "must be true or false"
    | Enum cases ->
        match FSharpType.GetUnionCases(field.Core, true) |> Array.tryFind (fun c -> c.Name = raw.Trim()) with
        | Some c -> Ok(FSharpValue.MakeUnion(c, [||], true))
        | None -> Result.Error("must be one of " + String.Join(", ", cases))
    | _ -> Result.Error "cannot be read from text"

/// Parse one raw value by kind. An optional field gives a boxed `option`. A missing boolean is false.
let parseField (field: Field) (raw: string option) : Result<obj, FieldError> =
    let fail message = Result.Error { Field = field.Name; Message = message }
    let kind = innerKind field.Kind
    let text = raw |> Option.defaultValue ""
    let isEmptyInput = textToCheck field text = ""
    if kind = Boolean && raw.IsNone then
        Ok(box false)
    elif isOptional field.Kind && isEmptyInput then
        Ok null
    else
        match parseCore field kind text with
        | Result.Error message -> fail message
        | Ok value when isOptional field.Kind ->
            let some = FSharpType.GetUnionCases(field.Prop.PropertyType) |> Array.find (fun c -> c.Name = "Some")
            Ok(FSharpValue.MakeUnion(some, [| value |]))
        | Ok value -> Ok value
````

