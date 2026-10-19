# Handoff · CH-0003 · build → show

Time: 2026-10-19T06:23:16+00:00

## Done

The builder fixed the three quality points in `src/Typed.Engine/Validate.fs`: one
whole-number parse, one trim rule in `textToCheck`, and a named empty-input test. All 87
checks pass. The check files did not change after the lock `6f34681`. The build commit is
`40d743a`.

## Decided on defaults

- A whole number that does not fit an `int` field gives "must be a whole number"
  (assumed by the builder)

## Open

- none

## Look here

- src/Typed.Engine/Validate.fs
- pdlc/changes/CH-0003-validate/visual.json and show.fsx (unchanged, record again)

## Don't redo

- The checks and the build.

## Next: do this first

Record `visual.json` again, because the code changed. Then verify, round 2.
