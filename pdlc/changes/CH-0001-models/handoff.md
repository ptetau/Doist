# Handoff · CH-0001 · show → verify

Time: 2026-10-19T05:53:38+00:00

## Done

Recorded `review.gif` with 30 frames in a terminal. Frame 1 builds the engine library.
Frames 2 to 29 each run `show.fsx` for one acceptance line of CAP-models.R1 (kinds from
types) to R7 (typed overrides), and print the value, the error code, or the message that
the line names. Frame 30 runs the 30 checks of CAP-models and shows "Passed! 30". The GIF
shows 28 of 28 acceptance lines.

## Decided on defaults

- The show runs `dotnet fsi` on a helper script, one call for each line, because the
  change is a library with no screen (assumed)

## Open

- none

## Look here

- pdlc/changes/CH-0001-models/review.gif and frames/
- pdlc/changes/CH-0001-models/visual.json and show.fsx

## Don't redo

- The recording. Each frame was read. No frame shows an error that the spec did not ask
  for.

## Next: do this first

Run the checks, then send one packet for each remit to a fresh reviewer.
