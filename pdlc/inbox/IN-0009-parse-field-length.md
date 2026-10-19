# IN-0009 · parseField gives the length error

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`parseField` does not check the text length. `validate` does. A caller that stores the parsed value without `validate` could keep over-long text. Make `parseField` give "must be N characters or fewer" too, or state that every write path calls `validate`.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0003 (validate) (security)

## Handoff

- none yet
