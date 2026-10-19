# Handoff · CH-0003 · tests → build

Time: 2026-10-19T06:13:04+00:00

## Done

The test writer wrote 32 checks in `tests/Typed.Engine.Tests/CAP-validate.Tests.fs`, one
module for each of CAP-validate.R1 (errors for each field) to R8 (raw input by kind). The
checks are red: `Typed.Engine.Validate` does not exist yet. The lock is `7fab997`. The
tests review passed with 0 problems.

## Decided on defaults

- The rule of a semantic type comes from `TypeDefault.Check`, which takes the trimmed text
  (assumed)
- `parseField` gives a boxed `option` for an optional field, so an empty optional value is
  `null` when boxed (assumed)

## Open

- none

## Look here

- pdlc/changes/CH-0003-validate/change.md (Interface, Files)
- tests/Typed.Engine.Tests/CAP-validate.Tests.fs (what the code must satisfy)
- src/Typed.Engine/Model.fs (`Field.Check`, `Kind`, `Finding`)

## Don't redo

- The checks. They are locked. Only the test writer changes them.

## Next: do this first

Add `Validate.fs` after `Access.fs` in `src/Typed.Engine/Typed.Engine.fsproj`. Make the
32 checks pass with `dotnet test tests/Typed.Engine.Tests`. Text is trimmed before the
length and the type rule. Notes length counts as given, with no trim.
