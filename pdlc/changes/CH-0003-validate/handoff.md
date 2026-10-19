# Handoff · CH-0003 · verify → build

Time: 2026-10-19T06:21:54+00:00

## Done

Verify round 1. All 87 checks pass. Security and privacy passed. Specification failed on
one long sentence in the change spec, which is now split. Quality failed on three points
in `src/Typed.Engine/Validate.fs`. The three points go to the build stage. No acceptance
line changes.

## Decided on defaults

- none

## Open

Fix these three points in `src/Typed.Engine/Validate.fs`, with no new behaviour and no
change to the checks:

1. The `WholeNumber` branch of `parseCore` holds two copies of the same parse and the
   same message, one for `int64` and one for `int`. Keep one parse, and pick the
   width in one place.
2. The rule "wide text keeps its spaces, other text is trimmed" appears in `textRules`
   and again in `parseCore`. Write it one time, in one private function, and call it
   from both.
3. The empty-optional test in `parseField`, `not (isWide field.Kind) && text.Trim() = ""
   || text = ""`, repeats the trim rule a third time and relies on operator precedence.
   Use the one function from point 2, and name the test.

## Look here

- pdlc/changes/CH-0003-validate/review/quality/result.md
- src/Typed.Engine/Validate.fs

## Don't redo

- The checks. They are locked at `6f34681`. All 87 pass and must still pass.
- The recording. The show stage records again after the build.

## Next: do this first

Make the three changes above, then run `dotnet test tests/Typed.Engine.Tests`.
