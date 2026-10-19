# Handoff · CH-0001 · build → show

Time: 2026-10-19T05:48:21+00:00

## Done

The builder wrote `src/Typed.Engine/Model.fs`, `src/Typed.Engine/Overrides.fs`, the
project file, and `.gitignore`. All 30 checks of CAP-models.R1 (kinds from types) to R7
(typed overrides) pass. The check files did not change after the lock `c61b18d`. The build
commit is `367dc43`. The seven requirements are `built`.

## Decided on defaults

- `unsafe` takes two strings, `name` and `reason`, and no selector, because it names no
  field (assumed by the builder)
- A union whose cases all have no fields is an `Enum` kind with no entry in the defaults
  (the spec says so, the builder confirmed it)

## Open

- none

## Look here

- src/Typed.Engine/Model.fs and Overrides.fs (what to show)
- tests/Typed.Engine.Tests/CAP-models.Tests.fs (the 30 checks)

## Don't redo

- The checks and the build. Both are committed and pass.

## Next: do this first

This change is a library with no screen. Record the terminal: `dotnet test
tests/Typed.Engine.Tests --filter "DisplayName~CAP-models"` and its summary line.
