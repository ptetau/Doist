# Handoff · CH-0005 · show → verify

Time: 2026-10-19T10:14:32+00:00

## Done

Recorded `pdlc/changes/CH-0005-checks/review.gif` with 26 frames. The GIF shows 24 of 24 acceptance lines of CAP-checks (R1 (no access rule) to R13 (unsupported view)). Each frame shows the call to `Checks.run` and its findings.

- Frame 1: the build of `Typed.Engine` succeeds.
- Frames 2 to 4 (R1): no rule gives MISSING_ACCESS_RULE. Two rules give "more than one access rule". The todo app gives none.
- Frames 5 and 6 (R2): an override for Ghost and a Label for Todo.Dew give MISSING_FIELD.
- Frames 7 and 8 (R3): a string predicate on Todo.Due gives WRONG_CONFIG_TYPE. A Title predicate on Todo.Title gives none.
- Frame 9 (R4): a Column item for Todo.Version gives SYSTEM_FIELD_TARGET.
- Frame 10 (R5): Todo alone gives DANGLING_REFERENCE. It also gives a WRONG_CONFIG_TYPE from the rule, which names TodoList.
- Frames 11 and 12 (R6): NoTitle gives NO_TITLE_FIELD. A titleField override removes it.
- Frames 13 and 14 (R7): ownedVia on Todo.Title and readOnlyWhenParent on TodoList.Name give WRONG_CONFIG_TYPE.
- Frame 15 (R8): unsafe "rawName" gives one UNSAFE_ESCAPE_HATCH warning and no error.
- Frames 16 and 17 (R9): label "Due" gives REDUNDANT_OVERRIDE. Label "Due date" gives none.
- Frame 18 (R10): the todo app gives no findings.
- Frames 19 and 20 (R11): two labels on Todo.Due give WRONG_CONFIG_TYPE. A label and a readOnly give none.
- Frames 21 and 22 (R12): TodoList comes before Todo. No models give no findings.
- Frames 23 to 25 (R13): listSort on Notes and listView on a computed length give WRONG_CONFIG_TYPE. listView on an equality gives none.
- Frame 26: `dotnet test` shows 176 passed, 0 failed.

## Decided on defaults

- Frame 10 (R5) shows the rule `readOnlyTodoOwnedVia`, as the check does. Thus a second finding appears.
- The R13 (unsupported view) message shows the quotation tree. The spec only fixes its start.
- I looked at 10 frames in full: 2, 3, 4, 6, 7, 10, 14, 22, 25, and 26. I read the text output of all 24 lines.

## Open

- none

## Look here

- `pdlc/changes/CH-0005-checks/review.gif`, `pdlc/changes/CH-0005-checks/frames/`, and `pdlc/changes/CH-0005-checks/show.fsx`.

## Don't redo

- The build, the 176 checks, and the recording.

## Next: do this first

Run `/pdlc:verify` for CH-0005 (checks).
