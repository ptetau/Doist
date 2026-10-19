# Handoff · CH-0003 · verify → user

Time: 2026-10-19T06:29:20+00:00

## Done

Proposed CH-0003 (validate) as draft pull request https://github.com/ptetau/Doist/pull/4,
against `pdlc/CH-0002-access`. All 88 checks of the branch pass. The four remits that
apply passed with 0 problems in round 2. The eight requirements of CAP-validate
(validation) are `verified`. The change is `in review`.

## Decided on defaults

- none in this stage

## Open

- The merge gate waits for CH-0004 (persist) to CH-0007 (generate).
- Three follow-up intents await approval: IN-0008 (parse messages for all kinds), IN-0009
  (parseField length), and IN-0010 (validate never raises).

## Look here

- https://github.com/ptetau/Doist/pull/4

## Don't redo

- The checks, the build, the recording, and the reviews of CH-0003 (validate).

## Next: do this first

Start the tests stage of CH-0004 (persist), from the branch `pdlc/CH-0003-validate`.
