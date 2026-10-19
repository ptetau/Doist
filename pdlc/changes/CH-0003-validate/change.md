# CH-0003 · Validate

Intent: IN-0001 · Scope: CAP-validate · Status: testing · Ticket: none · Branch: pdlc/CH-0003-validate · Base: pdlc/CH-0002-access
Tests-Locked: 7fab997

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
let knownZone (s: string) =
    try TimeZoneInfo.FindSystemTimeZoneById s |> ignore; []
    with _ -> [ "must be a known time zone, such as Pacific/Auckland" ]

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
