# Review packet · CH-0003-validate · tests

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

### Step 1 · Validation returns one error list for each field · [ ]

- Requirement: CAP-validate.R1
- Check: the checks named `CAP-validate.R1 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R1 (validation returns one error list for each field) passes

### Step 2 · A `Title` is 1 to 200 characters on one line, trimmed · [ ]

- Requirement: CAP-validate.R2
- Check: the checks named `CAP-validate.R2 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R2 (a `title` is 1 to 200 characters on one line, trimmed) passes

### Step 3 · `Notes` is up to 5,000 characters and can be empty · [ ]

- Requirement: CAP-validate.R3
- Check: the checks named `CAP-validate.R3 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R3 (`notes` is up to 5,000 characters and can be empty) passes

### Step 4 · A `TimeZoneId` is a known IANA zone · [ ]

- Requirement: CAP-validate.R4
- Check: the checks named `CAP-validate.R4 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R4 (a `timezoneid` is a known iana zone) passes

### Step 5 · A reference must point at a record · [ ]

- Requirement: CAP-validate.R5
- Check: the checks named `CAP-validate.R5 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R5 (a reference must point at a record) passes

### Step 6 · A `rule` override adds its message when its predicate fails · [ ]

- Requirement: CAP-validate.R6
- Check: the checks named `CAP-validate.R6 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R6 (a `rule` override adds its message when its predicate fails) passes

### Step 7 · An enum value must be a known case · [ ]

- Requirement: CAP-validate.R7
- Check: the checks named `CAP-validate.R7 ...` in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`
- Done when: every check of CAP-validate.R7 (an enum value must be a known case) passes

### Step 8 · Raw input parses by kind, or gives a field error · [ ]

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
  "pacific/auckland" on Linux, so one R4 check failed against correct code. The rule now
  matches the zone id exactly. The test writer mirrored it. All 87 checks pass with the
  builder's code. Locked again at 6f34681.
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

Status: ready · Intents: IN-0001

The result of a check is a list of field errors. Each field error holds the field name
and one message. An empty list means that the record passes.

- Given the list `{ Name = Title "Groceries"; Archived = false }`, when validation runs,
  then the result is an empty list.
- Given a record with two broken fields. When validation runs, then the result holds one
  error for each broken field, each with its own field name.

### CAP-validate.R2 · A `Title` is 1 to 200 characters on one line, trimmed

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

- Given a `User` with the time zone "Pacific/Auckland", when validation runs, then there
  is no error on `TimeZone`.
- Given a `User` with the time zone "Mars/Base". When validation runs, then the result is
  one error on `TimeZone` with the message "must be a known time zone, such as
  Pacific/Auckland".
- Given a `User` with the time zone "pacific/auckland". When validation runs, then the
  result is one error on `TimeZone`, because the name is case-sensitive. (assumed)

### CAP-validate.R5 · A reference must point at a record

Status: ready · Intents: IN-0001

- Given a `Todo` with `List = Ref Guid.Empty`, when validation runs, then the result is one
  error on `List` with the message "is required".
- Given a `Todo` with `List = Ref` of a non-empty id, when validation runs, then there is no
  error on `List`.

### CAP-validate.R6 · A `rule` override adds its message when its predicate fails

Status: ready · Intents: IN-0001

- Given the override `rule <@ fun t -> t.Title @> (fun (Title s) -> s <> "forbidden")
  "Title is reserved"`, and a `Todo` with the title "forbidden". When validation runs,
  then the result is one error on `Title` with the message "Title is reserved".
- Given the same override and a `Todo` with the title "Pay rent", when validation runs,
  then there is no error on `Title`.

### CAP-validate.R7 · An enum value must be a known case

Status: ready · Intents: IN-0001

The F# type makes an unknown case impossible in code. This rule applies to raw input, such
as a form value or a JSON string.

- Given the raw value "Urgent" for the field `Priority` of `Todo`. When the engine parses
  the value, then the result is one error on `Priority` with the message "must be one of
  Low, Normal, High". (assumed)
- Given the raw value "High" for the field `Priority`, when the engine parses the value,
  then the result is the case `High`.

### CAP-validate.R8 · Raw input parses by kind, or gives a field error

Status: ready · Intents: IN-0001

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

## The checks

### Diff against pdlc/CH-0002-access

````
diff --git a/tests/Typed.Engine.Tests/CAP-validate.Tests.fs b/tests/Typed.Engine.Tests/CAP-validate.Tests.fs
new file mode 100644
index 0000000..7483ac1
--- /dev/null
+++ b/tests/Typed.Engine.Tests/CAP-validate.Tests.fs
@@ -0,0 +1,273 @@
+namespace Typed.Engine.Tests
+
+open System
+open Xunit
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+open Typed.Engine.Validate
+open Typed.Engine.Tests.ModelsFixtures
+
+/// Records and helpers of the validation checks. The checks use only these.
+module ValidateFixtures =
+
+    let oneLine (s: string) = if s.IndexOfAny([| '\r'; '\n' |]) >= 0 then [ "must be on one line" ] else []
+
+    let knownZone (s: string) =
+        if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then []
+        else [ "must be a known time zone, such as Pacific/Auckland" ]
+
+    /// The defaults of the todo app, with the rule of each type, as the spec writes them.
+    let defaults : TypeDefault list =
+        [ { Clr = typeof<Title>;      Kind = Text 200;  Check = oneLine;        Hidden = false; Unique = false }
+          { Clr = typeof<Notes>;      Kind = Text 5000; Check = (fun _ -> []); Hidden = false; Unique = false }
+          { Clr = typeof<TimeZoneId>; Kind = Text 64;   Check = knownZone;      Hidden = false; Unique = false }
+          { Clr = typeof<ExternalId>; Kind = Text 255;  Check = (fun _ -> []); Hidden = true;  Unique = true } ]
+
+    /// Shadows `ModelsFixtures.describeOk`. It uses the defaults above.
+    let describeOk (t: Type) : Model =
+        match describe defaults t with
+        | Ok m -> m
+        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs
+
+    let spaces (n: int) = String(' ', n)
+    let chars (n: int) = String('a', n)
+
+    let validTodo: Todo =
+        { List = Ref(Guid.Parse "33333333-3333-3333-3333-333333333333")
+          Title = Title "Pay rent"
+          Notes = None
+          Priority = Normal
+          Due = None
+          Done = false
+          CompletedAt = None }
+
+    let validUser: User =
+        { Subject = ExternalId "sub-1"
+          DisplayName = Title "Ana"
+          TimeZone = TimeZoneId "Pacific/Auckland" }
+
+    let run (t: Type) (items: OverrideItem list) (record: obj) : FieldError list =
+        validate (describeOk t) items record
+
+    let todoErrors (t: Todo) = run typeof<Todo> [] (box t)
+    let listErrors (l: TodoList) = run typeof<TodoList> [] (box l)
+    let userErrors (u: User) = run typeof<User> [] (box u)
+
+    let onField (name: string) (errs: FieldError list) =
+        errs |> List.filter (fun e -> e.Field = name)
+
+    let listNamed (name: string) : TodoList = { Name = Title name; Archived = false }
+
+    let parseOk (f: Field) (raw: string option) : obj =
+        match parseField f raw with
+        | Ok v -> v
+        | Result.Error e -> failwithf "Expected a value, got: %A" e
+
+    let parseErr (f: Field) (raw: string option) : FieldError =
+        match parseField f raw with
+        | Ok v -> failwithf "Expected an error, got: %A" v
+        | Result.Error e -> e
+
+    let todoField (name: string) = field (describeOk typeof<Todo>) name
+
+open ValidateFixtures
+
+module ``CAP-validate-R1`` =
+
+    [<Fact>]
+    let ``CAP-validate.R1 a valid list gives an empty list`` () =
+        Assert.Empty(listErrors { Name = Title "Groceries"; Archived = false })
+
+    [<Fact>]
+    let ``CAP-validate.R1 two broken fields give one error for each with its own field name`` () =
+        let errs =
+            todoErrors
+                { validTodo with
+                    Title = Title "   "
+                    Notes = Some(Notes(chars 5001)) }
+        Assert.Equal(2, errs.Length)
+        let sorted = errs |> List.sortBy (fun e -> e.Field)
+        Assert.Equal<FieldError list>(
+            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" }
+              { Field = "Title"; Message = "is required" } ],
+            sorted
+        )
+
+module ``CAP-validate-R2`` =
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name with spaces at both ends is valid`` () =
+        Assert.Empty(listErrors (listNamed "  Groceries  "))
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name of spaces only is required`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "Name"; Message = "is required" } ],
+            listErrors (listNamed "   ")
+        )
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name of 200 characters is valid`` () =
+        Assert.Empty(listErrors (listNamed (chars 200)))
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name of 201 characters is too long`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "Name"; Message = "must be 200 characters or fewer" } ],
+            listErrors (listNamed (chars 201))
+        )
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name with a line break must be on one line`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "Name"; Message = "must be on one line" } ],
+            listErrors (listNamed "Shop\nlater")
+        )
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name with a tab is valid`` () =
+        Assert.Empty(listErrors (listNamed "Shop\tlater"))
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name of 200 characters and three spaces at each end is valid`` () =
+        Assert.Empty(listErrors (listNamed (spaces 3 + chars 200 + spaces 3)))
+
+    [<Fact>]
+    let ``CAP-validate.R2 a name with script tags is valid`` () =
+        Assert.Empty(listErrors (listNamed "<script>alert(1)</script>"))
+
+module ``CAP-validate-R3`` =
+
+    let private withNotes n = todoErrors { validTodo with Notes = n }
+
+    [<Fact>]
+    let ``CAP-validate.R3 no notes gives no error on Notes`` () =
+        Assert.Empty(onField "Notes" (withNotes None))
+
+    [<Fact>]
+    let ``CAP-validate.R3 notes of 5000 characters give no error on Notes`` () =
+        Assert.Empty(onField "Notes" (withNotes (Some(Notes(chars 5000)))))
+
+    [<Fact>]
+    let ``CAP-validate.R3 notes of 5001 characters give one error on Notes`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" } ],
+            withNotes (Some(Notes(chars 5001)))
+        )
+
+    [<Fact>]
+    let ``CAP-validate.R3 notes of 5000 characters and one space give one error with no trim`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" } ],
+            withNotes (Some(Notes(chars 5000 + " ")))
+        )
+
+module ``CAP-validate-R4`` =
+
+    let private withZone z = userErrors { validUser with TimeZone = TimeZoneId z }
+
+    [<Fact>]
+    let ``CAP-validate.R4 Pacific/Auckland gives no error on TimeZone`` () =
+        Assert.Empty(onField "TimeZone" (withZone "Pacific/Auckland"))
+
+    [<Fact>]
+    let ``CAP-validate.R4 Mars/Base gives one error on TimeZone`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "TimeZone"
+                Message = "must be a known time zone, such as Pacific/Auckland" } ],
+            withZone "Mars/Base"
+        )
+
+    [<Fact>]
+    let ``CAP-validate.R4 a zone name with the wrong case gives one error on TimeZone`` () =
+        Assert.Equal<FieldError list>(
+            [ { Field = "TimeZone"
+                Message = "must be a known time zone, such as Pacific/Auckland" } ],
+            withZone "pacific/auckland"
+        )
+
+module ``CAP-validate-R5`` =
+
+    [<Fact>]
+    let ``CAP-validate.R5 an empty reference gives one error on List`` () =
+        let errs = todoErrors { validTodo with List = Ref Guid.Empty }
+        Assert.Equal<FieldError list>([ { Field = "List"; Message = "is required" } ], errs)
+
+    [<Fact>]
+    let ``CAP-validate.R5 a reference with an id gives no error on List`` () =
+        let errs = todoErrors { validTodo with List = Ref(Guid.NewGuid()) }
+        Assert.Empty(onField "List" errs)
+
+module ``CAP-validate-R6`` =
+
+    let private reserved =
+        (overrides<Todo>
+            [ rule <@ fun (t: Todo) -> t.Title @> (fun (Title s) -> s <> "forbidden") "Title is reserved" ])
+            .Items
+
+    [<Fact>]
+    let ``CAP-validate.R6 a failing predicate adds its message on Title`` () =
+        let errs = run typeof<Todo> reserved (box { validTodo with Title = Title "forbidden" })
+        Assert.Equal<FieldError list>([ { Field = "Title"; Message = "Title is reserved" } ], errs)
+
+    [<Fact>]
+    let ``CAP-validate.R6 a passing predicate adds no error on Title`` () =
+        let errs = run typeof<Todo> reserved (box validTodo)
+        Assert.Empty(onField "Title" errs)
+
+module ``CAP-validate-R7`` =
+
+    [<Fact>]
+    let ``CAP-validate.R7 Urgent for Priority gives one error on Priority`` () =
+        let e = parseErr (todoField "Priority") (Some "Urgent")
+        Assert.Equal({ Field = "Priority"; Message = "must be one of Low, Normal, High" }, e)
+
+    [<Fact>]
+    let ``CAP-validate.R7 High for Priority gives the case High`` () =
+        let v = parseOk (todoField "Priority") (Some "High")
+        Assert.Equal(box High, v)
+
+module ``CAP-validate-R8`` =
+
+    [<Fact>]
+    let ``CAP-validate.R8 abc for a whole number gives an error`` () =
+        let f = field (describeOk typeof<NoTitle>) "Count"
+        Assert.Equal("must be a whole number", (parseErr f (Some "abc")).Message)
+
+    [<Fact>]
+    let ``CAP-validate.R8 2026-02-30 for Due gives an error`` () =
+        let e = parseErr (todoField "Due") (Some "2026-02-30")
+        Assert.Equal("must be a date in the form YYYY-MM-DD", e.Message)
+
+    [<Fact>]
+    let ``CAP-validate.R8 2026-10-18 for Due gives Some date`` () =
+        let v = parseOk (todoField "Due") (Some "2026-10-18")
+        Assert.Equal(box (Some(DateOnly(2026, 10, 18))), v)
+
+    [<Fact>]
+    let ``CAP-validate.R8 an empty value for the optional Due gives None`` () =
+        // A boxed None is null.
+        Assert.Null(parseOk (todoField "Due") (Some ""))
+
+    [<Fact>]
+    let ``CAP-validate.R8 an empty value for the required Title gives an error`` () =
+        let e = parseErr (todoField "Title") (Some "")
+        Assert.Equal("is required", e.Message)
+
+    [<Fact>]
+    let ``CAP-validate.R8 on and true for Done give true`` () =
+        Assert.Equal(box true, parseOk (todoField "Done") (Some "on"))
+        Assert.Equal(box true, parseOk (todoField "Done") (Some "true"))
+
+    [<Fact>]
+    let ``CAP-validate.R8 false for Done gives false`` () =
+        Assert.Equal(box false, parseOk (todoField "Done") (Some "false"))
+
+    [<Fact>]
+    let ``CAP-validate.R8 a missing value for Done gives false`` () =
+        Assert.Equal(box false, parseOk (todoField "Done") None)
+
+    [<Fact>]
+    let ``CAP-validate.R8 yes for Done gives an error`` () =
+        let e = parseErr (todoField "Done") (Some "yes")
+        Assert.Equal("must be true or false", e.Message)
diff --git a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
index 0ff181d..a5e5625 100644
--- a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
+++ b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
@@ -8,6 +8,7 @@
   <ItemGroup>
     <Compile Include="CAP-models.Tests.fs" />
     <Compile Include="CAP-access.Tests.fs" />
+    <Compile Include="CAP-validate.Tests.fs" />
   </ItemGroup>
 
   <ItemGroup>
````

### tests/Typed.Engine.Tests/CAP-validate.Tests.fs

````
namespace Typed.Engine.Tests

open System
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Validate
open Typed.Engine.Tests.ModelsFixtures

/// Records and helpers of the validation checks. The checks use only these.
module ValidateFixtures =

    let oneLine (s: string) = if s.IndexOfAny([| '\r'; '\n' |]) >= 0 then [ "must be on one line" ] else []

    let knownZone (s: string) =
        if TimeZoneInfo.GetSystemTimeZones() |> Seq.exists (fun z -> z.Id = s) then []
        else [ "must be a known time zone, such as Pacific/Auckland" ]

    /// The defaults of the todo app, with the rule of each type, as the spec writes them.
    let defaults : TypeDefault list =
        [ { Clr = typeof<Title>;      Kind = Text 200;  Check = oneLine;        Hidden = false; Unique = false }
          { Clr = typeof<Notes>;      Kind = Text 5000; Check = (fun _ -> []); Hidden = false; Unique = false }
          { Clr = typeof<TimeZoneId>; Kind = Text 64;   Check = knownZone;      Hidden = false; Unique = false }
          { Clr = typeof<ExternalId>; Kind = Text 255;  Check = (fun _ -> []); Hidden = true;  Unique = true } ]

    /// Shadows `ModelsFixtures.describeOk`. It uses the defaults above.
    let describeOk (t: Type) : Model =
        match describe defaults t with
        | Ok m -> m
        | Result.Error fs -> failwithf "Expected a plan, got: %A" fs

    let spaces (n: int) = String(' ', n)
    let chars (n: int) = String('a', n)

    let validTodo: Todo =
        { List = Ref(Guid.Parse "33333333-3333-3333-3333-333333333333")
          Title = Title "Pay rent"
          Notes = None
          Priority = Normal
          Due = None
          Done = false
          CompletedAt = None }

    let validUser: User =
        { Subject = ExternalId "sub-1"
          DisplayName = Title "Ana"
          TimeZone = TimeZoneId "Pacific/Auckland" }

    let run (t: Type) (items: OverrideItem list) (record: obj) : FieldError list =
        validate (describeOk t) items record

    let todoErrors (t: Todo) = run typeof<Todo> [] (box t)
    let listErrors (l: TodoList) = run typeof<TodoList> [] (box l)
    let userErrors (u: User) = run typeof<User> [] (box u)

    let onField (name: string) (errs: FieldError list) =
        errs |> List.filter (fun e -> e.Field = name)

    let listNamed (name: string) : TodoList = { Name = Title name; Archived = false }

    let parseOk (f: Field) (raw: string option) : obj =
        match parseField f raw with
        | Ok v -> v
        | Result.Error e -> failwithf "Expected a value, got: %A" e

    let parseErr (f: Field) (raw: string option) : FieldError =
        match parseField f raw with
        | Ok v -> failwithf "Expected an error, got: %A" v
        | Result.Error e -> e

    let todoField (name: string) = field (describeOk typeof<Todo>) name

open ValidateFixtures

module ``CAP-validate-R1`` =

    [<Fact>]
    let ``CAP-validate.R1 a valid list gives an empty list`` () =
        Assert.Empty(listErrors { Name = Title "Groceries"; Archived = false })

    [<Fact>]
    let ``CAP-validate.R1 two broken fields give one error for each with its own field name`` () =
        let errs =
            todoErrors
                { validTodo with
                    Title = Title "   "
                    Notes = Some(Notes(chars 5001)) }
        Assert.Equal(2, errs.Length)
        let sorted = errs |> List.sortBy (fun e -> e.Field)
        Assert.Equal<FieldError list>(
            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" }
              { Field = "Title"; Message = "is required" } ],
            sorted
        )

module ``CAP-validate-R2`` =

    [<Fact>]
    let ``CAP-validate.R2 a name with spaces at both ends is valid`` () =
        Assert.Empty(listErrors (listNamed "  Groceries  "))

    [<Fact>]
    let ``CAP-validate.R2 a name of spaces only is required`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Name"; Message = "is required" } ],
            listErrors (listNamed "   ")
        )

    [<Fact>]
    let ``CAP-validate.R2 a name of 200 characters is valid`` () =
        Assert.Empty(listErrors (listNamed (chars 200)))

    [<Fact>]
    let ``CAP-validate.R2 a name of 201 characters is too long`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Name"; Message = "must be 200 characters or fewer" } ],
            listErrors (listNamed (chars 201))
        )

    [<Fact>]
    let ``CAP-validate.R2 a name with a line break must be on one line`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Name"; Message = "must be on one line" } ],
            listErrors (listNamed "Shop\nlater")
        )

    [<Fact>]
    let ``CAP-validate.R2 a name with a tab is valid`` () =
        Assert.Empty(listErrors (listNamed "Shop\tlater"))

    [<Fact>]
    let ``CAP-validate.R2 a name of 200 characters and three spaces at each end is valid`` () =
        Assert.Empty(listErrors (listNamed (spaces 3 + chars 200 + spaces 3)))

    [<Fact>]
    let ``CAP-validate.R2 a name with script tags is valid`` () =
        Assert.Empty(listErrors (listNamed "<script>alert(1)</script>"))

module ``CAP-validate-R3`` =

    let private withNotes n = todoErrors { validTodo with Notes = n }

    [<Fact>]
    let ``CAP-validate.R3 no notes gives no error on Notes`` () =
        Assert.Empty(onField "Notes" (withNotes None))

    [<Fact>]
    let ``CAP-validate.R3 notes of 5000 characters give no error on Notes`` () =
        Assert.Empty(onField "Notes" (withNotes (Some(Notes(chars 5000)))))

    [<Fact>]
    let ``CAP-validate.R3 notes of 5001 characters give one error on Notes`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" } ],
            withNotes (Some(Notes(chars 5001)))
        )

    [<Fact>]
    let ``CAP-validate.R3 notes of 5000 characters and one space give one error with no trim`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "Notes"; Message = "must be 5000 characters or fewer" } ],
            withNotes (Some(Notes(chars 5000 + " ")))
        )

module ``CAP-validate-R4`` =

    let private withZone z = userErrors { validUser with TimeZone = TimeZoneId z }

    [<Fact>]
    let ``CAP-validate.R4 Pacific/Auckland gives no error on TimeZone`` () =
        Assert.Empty(onField "TimeZone" (withZone "Pacific/Auckland"))

    [<Fact>]
    let ``CAP-validate.R4 Mars/Base gives one error on TimeZone`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "TimeZone"
                Message = "must be a known time zone, such as Pacific/Auckland" } ],
            withZone "Mars/Base"
        )

    [<Fact>]
    let ``CAP-validate.R4 a zone name with the wrong case gives one error on TimeZone`` () =
        Assert.Equal<FieldError list>(
            [ { Field = "TimeZone"
                Message = "must be a known time zone, such as Pacific/Auckland" } ],
            withZone "pacific/auckland"
        )

module ``CAP-validate-R5`` =

    [<Fact>]
    let ``CAP-validate.R5 an empty reference gives one error on List`` () =
        let errs = todoErrors { validTodo with List = Ref Guid.Empty }
        Assert.Equal<FieldError list>([ { Field = "List"; Message = "is required" } ], errs)

    [<Fact>]
    let ``CAP-validate.R5 a reference with an id gives no error on List`` () =
        let errs = todoErrors { validTodo with List = Ref(Guid.NewGuid()) }
        Assert.Empty(onField "List" errs)

module ``CAP-validate-R6`` =

    let private reserved =
        (overrides<Todo>
            [ rule <@ fun (t: Todo) -> t.Title @> (fun (Title s) -> s <> "forbidden") "Title is reserved" ])
            .Items

    [<Fact>]
    let ``CAP-validate.R6 a failing predicate adds its message on Title`` () =
        let errs = run typeof<Todo> reserved (box { validTodo with Title = Title "forbidden" })
        Assert.Equal<FieldError list>([ { Field = "Title"; Message = "Title is reserved" } ], errs)

    [<Fact>]
    let ``CAP-validate.R6 a passing predicate adds no error on Title`` () =
        let errs = run typeof<Todo> reserved (box validTodo)
        Assert.Empty(onField "Title" errs)

module ``CAP-validate-R7`` =

    [<Fact>]
    let ``CAP-validate.R7 Urgent for Priority gives one error on Priority`` () =
        let e = parseErr (todoField "Priority") (Some "Urgent")
        Assert.Equal({ Field = "Priority"; Message = "must be one of Low, Normal, High" }, e)

    [<Fact>]
    let ``CAP-validate.R7 High for Priority gives the case High`` () =
        let v = parseOk (todoField "Priority") (Some "High")
        Assert.Equal(box High, v)

module ``CAP-validate-R8`` =

    [<Fact>]
    let ``CAP-validate.R8 abc for a whole number gives an error`` () =
        let f = field (describeOk typeof<NoTitle>) "Count"
        Assert.Equal("must be a whole number", (parseErr f (Some "abc")).Message)

    [<Fact>]
    let ``CAP-validate.R8 2026-02-30 for Due gives an error`` () =
        let e = parseErr (todoField "Due") (Some "2026-02-30")
        Assert.Equal("must be a date in the form YYYY-MM-DD", e.Message)

    [<Fact>]
    let ``CAP-validate.R8 2026-10-18 for Due gives Some date`` () =
        let v = parseOk (todoField "Due") (Some "2026-10-18")
        Assert.Equal(box (Some(DateOnly(2026, 10, 18))), v)

    [<Fact>]
    let ``CAP-validate.R8 an empty value for the optional Due gives None`` () =
        // A boxed None is null.
        Assert.Null(parseOk (todoField "Due") (Some ""))

    [<Fact>]
    let ``CAP-validate.R8 an empty value for the required Title gives an error`` () =
        let e = parseErr (todoField "Title") (Some "")
        Assert.Equal("is required", e.Message)

    [<Fact>]
    let ``CAP-validate.R8 on and true for Done give true`` () =
        Assert.Equal(box true, parseOk (todoField "Done") (Some "on"))
        Assert.Equal(box true, parseOk (todoField "Done") (Some "true"))

    [<Fact>]
    let ``CAP-validate.R8 false for Done gives false`` () =
        Assert.Equal(box false, parseOk (todoField "Done") (Some "false"))

    [<Fact>]
    let ``CAP-validate.R8 a missing value for Done gives false`` () =
        Assert.Equal(box false, parseOk (todoField "Done") None)

    [<Fact>]
    let ``CAP-validate.R8 yes for Done gives an error`` () =
        let e = parseErr (todoField "Done") (Some "yes")
        Assert.Equal("must be true or false", e.Message)
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

