# IN-0002 · A selector that is not a field gives a coded finding

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`fieldName` raises an exception with a raw dump of the quotation when a selector is not a property read, such as `fun t -> t.A + 1`. Give a `Finding` with a code instead, such as `WRONG_CONFIG_TYPE`, and a short message. Then the generator fails closed with a code, not an exception.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0001 (models) (specification, security, quality)

## Handoff

- none yet
