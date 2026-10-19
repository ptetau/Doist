# Handoff · CH-0003 · build → show

Time: 2026-10-19T06:16:19+00:00

## Done

The builder wrote `src/Typed.Engine/Validate.fs` and added it to the project. All 87
checks pass, 32 of them for CAP-validate.R1 (errors for each field) to R8 (raw input by
kind). The check files did not change after the lock `6f34681`. The build commit is
`9f44a59`. The eight requirements are `built`.

## Decided on defaults

- Text is trimmed before the rules unless the field is wide text (assumed by the builder)
- An empty string for a boolean gives "must be true or false". Only a missing value is
  `false` (assumed by the builder)
- A decimal, a timestamp, an id, or a reference that does not parse gives "cannot be read
  from text" (assumed by the builder, no line covers it)

## Open

- none

## Look here

- src/Typed.Engine/Validate.fs (what to show)
- tests/Typed.Engine.Tests/CAP-validate.Tests.fs (the 32 checks)

## Don't redo

- The checks and the build. Both are committed and pass.

## Next: do this first

Record the terminal with one `dotnet fsi` call for each acceptance line, as the earlier
changes did, then the CAP-validate checks.
