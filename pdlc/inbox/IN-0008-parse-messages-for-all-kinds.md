# IN-0008 · A message for each kind that raw text can give

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`parseField` gives the message "cannot be read from text" for a decimal, a timestamp, an id, or a reference. No acceptance line asks for it, and the message does not tell the person what to do. Add an acceptance line and a clear message for each of these kinds, such as "must be a number with up to 2 decimal places" and "must be a time in the form YYYY-MM-DDTHH:MM:SSZ".

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0003 (validate) (specification, quality)

## Handoff

- none yet
