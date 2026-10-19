# IN-0011 · The store fails closed in three cases

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`page` and `listQuery` add no owner filter for a `system` model. `remove` returns `Ok` when the final parent delete affects 0 rows, which can happen if another caller deleted the parent first. `page` on an `ownedVia` model with no parent returns an empty page and no error. Make each case a refusal with a code: `NOT_FOUND` for the first two, and `VALIDATION_FAILED` on `parent` for the third.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0004 (persist) (security, quality)

## Handoff

- none yet
