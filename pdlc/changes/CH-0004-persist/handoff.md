# Handoff · CH-0004 · verify → user

Time: 2026-10-19T07:01:43+00:00

## Done

Proposed CH-0004 (persist) as draft pull request https://github.com/ptetau/Doist/pull/5,
against `pdlc/CH-0003-validate`. All 139 checks of the branch pass. The four remits that
apply passed with 0 problems in round 2. The eight requirements of CAP-persist (storage)
are `verified`. The change is `in review`.

## Decided on defaults

- none in this stage

## Open

- The merge gate waits for CH-0005 (checks) to CH-0007 (generate).
- Four follow-up intents await approval: IN-0011 (store fails closed), IN-0012 (listQuery
  finding), IN-0013 (cursor privacy), and IN-0014 (persist tidy).

## Look here

- https://github.com/ptetau/Doist/pull/5

## Don't redo

- The checks, the build, the recording, and the reviews of CH-0004 (persist).

## Next: do this first

Start the tests stage of CH-0005 (checks), from the branch `pdlc/CH-0004-persist`.
