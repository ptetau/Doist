# Handoff · CH-0003 · show → verify

Time: 2026-10-19T06:25:06+00:00

## Done

Recorded `review.gif` again after build round 2, with 32 frames in a terminal. Frame 1
builds the library. Frames 2 to 31 each run `show.fsx` for one acceptance line of
CAP-validate.R1 (errors for each field) to R8 (raw input by kind). Frame 32 runs the 32
checks of CAP-validate and shows "Passed! 32". The GIF shows 30 of 30 acceptance lines.

## Decided on defaults

- none in this stage

## Open

- none

## Look here

- pdlc/changes/CH-0003-validate/review.gif and frames/

## Don't redo

- The recording. The last frame was read after the second build.

## Next: do this first

Verify, round 2. Quality must look again at `Validate.fs`.
