# IN-0007 · checkChange fails closed

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

`checkChange` passes in three cases where it must refuse. A model with no access rule, or
a `system` model, passes. A read-only field that is missing from the parent, or that is not
a bool, passes. The parent is not checked against the owner of the caller. Make each case
a refusal with a code: `NOT_FOUND` for the first and third, and `WRONG_CONFIG_TYPE` for the
second.

## Requirements

- none yet

## Changes

- none yet

## Notes

- Awaits approval: found reviewing CH-0002 (access) (security)

## Handoff

- none yet
