# Handoff · CH-0002 · show → verify

Time: 2026-10-19T06:05:54+00:00

## Done

Recorded `review.gif` with 20 frames in a terminal. Frame 1 builds the library. Frames 2
to 19 each run `show.fsx` for one acceptance line of CAP-access.R1 (four rules) to R5
(owner from parent), and print the access, the scope kind, the error code, or the reason.
Frame 20 runs the 24 checks of CAP-access and shows "Passed! 24". The GIF shows 18 of 18
acceptance lines.

## Decided on defaults

- none in this stage

## Open

- none

## Look here

- pdlc/changes/CH-0002-access/review.gif and frames/
- pdlc/changes/CH-0002-access/visual.json and show.fsx

## Don't redo

- The recording. Each frame was read. No frame shows an error that the spec did not ask
  for.

## Next: do this first

Run the checks, then send one packet for each remit to a fresh reviewer.
