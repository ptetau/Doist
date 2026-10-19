# IN-0012 · listQuery gives a finding, not an exception

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`listQuery` raises with `failwithf` for an unknown view name or an unsupported view. Return a `Finding` with `WRONG_CONFIG_TYPE` instead, as `viewToSql` does.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0004 (persist) (quality)

## Handoff

- none yet
