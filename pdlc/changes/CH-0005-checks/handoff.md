# Handoff · CH-0005 checks

## Checks

`tests/Typed.Engine.Tests/CAP-checks.Tests.fs` holds 36 checks, one module for each of CAP-checks.R1 to R13. The tests lock is in the header of `change.md`.

## What to build

- `src/Typed.Engine/Checks.fs` with module `Typed.Engine.Checks` and a `run` function. Add it to the fsproj after `Persist.fs`.
- R13 uses `Persist.viewToSql`.
- Findings come in model order, then field order.

## Notes

- R3 checks that the message names `Todo.Due`, "string", and "DateOnly".
- R9 for `CompletedAt` expects "Override Todo.CompletedAt label matches the default."
- R7 builds `OwnedVia` and the read-only condition directly, because the selectors cannot compile with a non-reference field.
- The spec line in R13 writes `t.Title.Length`. `Title` has no `Length`, so the check reads the inner string.
- The other system fields (`Id`, `Owner`, `CreatedAt`, `UpdatedAt`) have no check as `SYSTEM_FIELD_TARGET` targets.
