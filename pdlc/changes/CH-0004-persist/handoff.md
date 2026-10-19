# Handoff · CH-0004 · show → verify

Time: 2026-10-19T06:53:44+00:00

## Done

Recorded `review.gif` with 40 frames in a terminal. Frame 1 builds the library. Frames 2
to 39 each run `show.fsx` for one acceptance line of CAP-persist.R1 (tables) to R8
(migration drop), and print the SQL, the index columns, the row and query counts, the
error code, or the migration text. The store lines run on SQLite in memory. Frame 40 runs
the CAP-persist checks and shows "Passed! 51". The 51 counts the four rows of one Theory.
The GIF shows 38 of 38 acceptance lines.

## Decided on defaults

- none in this stage. The six builder decisions are in the "Decided on defaults" of the
  build handoff, now in git history, and in the progress log.

## Open

- none

## Look here

- pdlc/changes/CH-0004-persist/review.gif and frames/
- pdlc/changes/CH-0004-persist/visual.json and show.fsx

## Don't redo

- The recording. Frames 22, 35, and 40 were read. No frame shows an error that the spec
  did not ask for.

## Next: do this first

Run the checks, then send one packet for each remit to a fresh reviewer.
