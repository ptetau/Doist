# IN-0001 · Engine core: generator, checks, validate, persist

Status: new · Ticket: none · Branch: none · Added: 2026-10-19

## What and why

Build stages 1 to 3 of the build plan in the spec (`docs/spec/typed-layers-engine.md`):

1. The two-stage build and the generator. Load the compiled models, access rules, and
   overrides. Add the system fields. Run every build check. Each row of the spec's check
   table gives its error.
2. The Validate engine. Kind defaults, type defaults, and `rule` overrides. The same
   field errors for forms and for the API.
3. The Persist and Access engines. The schema, the derived indexes, the owner filter on
   every query, keyset paging, and the child deletes.

Also the two hooks of the todo app, because they are plain functions and test well.

These stages can be checked in this sandbox, with SQLite in memory as the test double
(`DES-test-database`). The web, sign-in, sessions, audit, and security stages come in a
later intent, when CI has SQL Server.

Names follow the spec (`DES-names`). The design and the blueprint in `docs/design/` give
the walls and the Gherkin scenarios. Use the scenarios as the start of the checks.

## Requirements

Intake writes this section. The section lists the requirements that this intent added or
changed.

- none yet

## Changes

The change stage writes this section. The section lists the changes that deliver this
intent, in order.

- none yet

## Notes

- Checks command: `dotnet test tests/`. Set at init.
- The engine core was built once before in this session, with 21 passing tests, then
  reverted. That build is proof that the stages fit in F# with xUnit.

## Handoff

Each intent stage (intake, ready, change) writes this section for the next stage. Use the
handoff template, with its sections as `###` headings. Replace this section each time.
