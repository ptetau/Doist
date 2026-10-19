# Handoff · CH-0002 · tests → build

Time: 2026-10-19T06:01:24+00:00

## Done

The test writer wrote 24 checks in `tests/Typed.Engine.Tests/CAP-access.Tests.fs`, one
module for each of CAP-access.R1 (four rules) to R5 (owner from parent). The checks are
red: `Typed.Engine.Access` does not exist yet. The lock is `da2df15`. The tests review
passed with 0 problems on the second packet.

## Decided on defaults

- The `Record` entry of the Interface, with a `Values` map that holds the parent's fields
  (assumed)
- The read-only reason is "This <parent word> is <field>", with the last word of the
  parent model name in lower case (assumed)

## Open

- none

## Look here

- pdlc/changes/CH-0002-access/change.md (Interface, Files)
- tests/Typed.Engine.Tests/CAP-access.Tests.fs (what the code must satisfy)
- src/Typed.Engine/Model.fs (`Record`, `Finding`, `ErrorCode`, `Ref`)

## Don't redo

- The checks. They are locked. Only the test writer changes them.

## Next: do this first

Add `Access.fs` after `Overrides.fs` in `src/Typed.Engine/Typed.Engine.fsproj`. Make
the 24 checks pass with `dotnet test tests/Typed.Engine.Tests`. The checks build a
`Scope` only through `scopeFor` and `engineScope`, and read its `Kind`.
