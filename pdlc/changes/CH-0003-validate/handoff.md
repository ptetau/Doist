# Handoff · CH-0003 · show → verify

Time: 2026-10-19T06:19:44+00:00

## Done

Recorded `review.gif` with 32 frames in a terminal. Frame 1 builds the library. Frames 2
to 31 each run `show.fsx` for one acceptance line of CAP-validate.R1 (errors for each
field) to R8 (raw input by kind), and print the error list or the parsed value. Frame 32
runs the 32 checks of CAP-validate and shows "Passed! 32". The GIF shows 30 of 30
acceptance lines.

## Decided on defaults

- none in this stage

## Open

- none

## Look here

- pdlc/changes/CH-0003-validate/review.gif and frames/
- pdlc/changes/CH-0003-validate/visual.json and show.fsx

## Don't redo

- The recording. Each frame was read. No frame shows an error that the spec did not ask
  for.

## Next: do this first

Run the checks, then send one packet for each remit to a fresh reviewer.
