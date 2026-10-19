# Handoff · CH-0001 · tests → build

Time: 2026-10-19T05:44:48+00:00

## Done

The test writer wrote 30 checks in `tests/Typed.Engine.Tests/CAP-models.Tests.fs`, one
module for each of CAP-models.R1 (kinds from types) to R7 (typed overrides). The checks
are red: the project `src/Typed.Engine` does not exist yet. The lock is `5dba628`. The
tests review passed with 0 problems.

## Decided on defaults

- CAP-models.R1 (kinds from types): the empty-record line → removed, because F# cannot
  declare a record with no fields (assumed)
- The checks declare the todo app types themselves, as the Interface writes them
  (assumed)

## Open

- none

## Look here

- pdlc/changes/CH-0001-models/change.md (Interface, Files)
- tests/Typed.Engine.Tests/CAP-models.Tests.fs (what the code must satisfy)
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj (expects `../../src/Typed.Engine/Typed.Engine.fsproj`)

## Don't redo

- The checks. They are locked. Only the test writer changes them.

## Next: do this first

Make `src/Typed.Engine/Typed.Engine.fsproj` with `Model.fs` then `Overrides.fs`, and
`.gitignore` with `bin/`, `obj/`, and `build/`. Then make the 30 checks pass with
`dotnet test tests/Typed.Engine.Tests`. The checks define `Title`, `Priority`, and the
records themselves, so the engine must give an enum and a `Ref<_>` their kinds with no
entry in the defaults list.
