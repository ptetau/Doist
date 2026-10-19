# Handoff · CH-0004 · tests → build

Time: 2026-10-19T06:39:33+00:00

## Done

The test writer wrote 49 checks in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`, one
module for each of CAP-persist.R1 (tables) to R8 (migration drop). They run on SQLite in
memory. The checks are red: `Typed.Engine.Persist` does not exist yet. The lock is
`bbd2344`. The tests review passed on the third round.

## Decided on defaults

- `Store.create conn dialect now hook` is curried, with `hook: (int -> unit) option`
  (assumed)
- `Record.Values` holds each field in its stored form, as the Interface lists (assumed)
- `migration` is pure. The caller writes the shape with `writeShape` (assumed)
- `Shape` and `Rename` are F# records, so they compare by value (assumed)

## Open

- none

## Look here

- pdlc/changes/CH-0004-persist/change.md (Interface, Test seams, Files)
- tests/Typed.Engine.Tests/CAP-persist.Tests.fs (what the code must satisfy)
- src/Typed.Engine/Model.fs, Overrides.fs, Access.fs (`Record`, `Scope.Kind`, `Access`)

## Don't redo

- The checks. They are locked. Only the test writer changes them.

## Next: do this first

Add `Persist.fs` after `Validate.fs` in `src/Typed.Engine/Typed.Engine.fsproj`. The store
runs on any `DbConnection`. For `Dialect.Sqlite`, `TOP (@n)` becomes `LIMIT @n`,
`rowversion` becomes an integer `Version` column, and `[HasDue]` is a generated column.
The engine project needs no SQLite package. Read the checks' `PersistFixtures` for the
exact calls. Make the 49 checks pass with `dotnet test tests/Typed.Engine.Tests`.
