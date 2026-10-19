# Handoff · CH-0006 (todo app) · show → verify

Time: 2026-10-19T10:31:59+00:00

## Done

The GIF shows 14 acceptance lines out of 14. Frame 1 builds the app. Frames 2 to 15 each play one line. Frame 16 runs all checks: 206 passed, 0 failed.

- Frame 2, R1 (models): User has 3 fields, TodoList has 2, and Todo has 7.
- Frame 3, R1 (type defaults): Title is text 200, Notes is text 5000, TimeZoneId is text 64. ExternalId is text 255, hidden, and unique.
- Frame 4, R2 (access rules): User is Self, TodoList is Owned, and Todo is OwnedVia List, read-only when Archived.
- Frame 5, R3 (Todo overrides): ReadOnly CompletedAt, ListEdit Done, ListSort Due Ascending, and four list columns.
- Frame 6, R3 (TodoList views): the view "Active" comes first (not Archived), then "Archived".
- Frame 7, R4 (ticking): Done turns true at 09:00 and CompletedAt becomes 2026-10-18T09:00:00Z.
- Frame 8, R4 (unticking): Done turns false and CompletedAt is empty.
- Frame 9, R4 (saving again): Done stays true at 12:00 and CompletedAt stays 09:00.
- Frame 10, R5 (overdue by calendar): in Auckland the local date is 2026-10-19. A todo due 10-18, not done, is overdue.
- Frame 11, R5 (done todo): a done todo due 10-18 is not overdue.
- Frame 12, R5 (due tomorrow): a todo due 10-19, not done, is not overdue.
- Frame 13, R5 (no due date): a todo with no due date is not overdue.
- Frame 14, R6 (no other code): the source files are Defaults.fs, Hooks.fs, Model.fs, and Schema.fs.
- Frame 15, R6 (engine checks): Checks.run over the app gives no findings, with 0 errors and 0 warnings.

## Decided on defaults

none

## Open

none

## Look here

- pdlc/changes/CH-0006-todo-app/review.gif
- pdlc/changes/CH-0006-todo-app/frames/
- pdlc/changes/CH-0006-todo-app/show.fsx (plays one acceptance line for each key)
- pdlc/changes/CH-0006-todo-app/visual.json

## Don't redo

The build and the 206 checks passed. Each frame was read and shows what its caption claims. The app code and the checks are not changed.

## Next: do this first

Run /pdlc:verify for CH-0006 (todo app). Send one packet to each review remit.
