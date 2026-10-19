# Handoff · CH-0004 · verify → show

Time: 2026-10-19T06:56:21+00:00

## Done

Verify round 1. All 139 checks pass. Security, quality, and privacy passed. Specification
failed on one point: the caption of frame 40 said "49 checks" and the frame showed 51
runs, because one check is a Theory with four rows. The caption now says "49 checks, 51
runs". Filed IN-0011 (store fails closed), IN-0012 (listQuery finding), IN-0013 (cursor
privacy), and IN-0014 (persist tidy) from the follow-ups.

## Decided on defaults

- none

## Open

- Record `visual.json` again with the corrected caption. Nothing else changed.

## Look here

- pdlc/changes/CH-0004-persist/visual.json

## Don't redo

- The checks, the build, and the reviews that passed.

## Next: do this first

Record again, then verify, round 2.
