# IN-0010 · validate never raises

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`validate` raises on a null string in a required text field, because `textOf` reads a null as a union. A `rule` predicate that raises is not caught. The spec says no function raises. Return "is required" for a null value, and turn a raising predicate into a field error.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0003 (validate) (security)

## Handoff

- none yet
