# IN-0013 · A cursor shows no field values

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

A keyset cursor holds the sort values of the last row, such as a title, in plain base64. Sign or encrypt the cursor, so a cursor never shows a field value. Also escape `]` in SQL identifiers, or check the names, in case a name ever comes from outside input.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0004 (persist) (privacy, security)

## Handoff

- none yet
