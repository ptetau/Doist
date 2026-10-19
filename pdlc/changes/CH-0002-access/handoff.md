# Handoff · CH-0002 · build → show

Time: 2026-10-19T06:03:30+00:00

## Done

The builder wrote `src/Typed.Engine/Access.fs` and added it to the project. All 54 checks
pass, 24 of them for CAP-access.R1 (four rules) to R5 (owner from parent). The check
files did not change after the lock `da2df15`. The build commit is `25a51a9`. The five
requirements are `built`.

## Decided on defaults

- `Scope` is a record with private fields, so only the `Access` module builds one
  (assumed by the builder)
- `checkChange` applies the read-only rule to every action on a child, so a delete of a
  todo in an archived list is refused too (assumed by the builder)

## Open

- none

## Look here

- src/Typed.Engine/Access.fs (what to show)
- tests/Typed.Engine.Tests/CAP-access.Tests.fs (the 24 checks)

## Don't redo

- The checks and the build. Both are committed and pass.

## Next: do this first

This change is a library with no screen. Record the terminal with one `dotnet fsi` call
for each acceptance line, as CH-0001 (models) did, then the CAP-access checks.
