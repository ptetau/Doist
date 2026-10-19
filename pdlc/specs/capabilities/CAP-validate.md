# CAP-validate · Checks a record against one rule set

Status: active

## What it is for

This capability checks the values of a record and returns the errors for each field. One
rule set serves the forms and the API. The rules come from the kind, from the semantic
type, and from `rule` overrides.

## Code map

- src/Typed.Engine/Validate.fs

## Requirements

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
