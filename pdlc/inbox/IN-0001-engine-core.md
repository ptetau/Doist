# IN-0001 · Engine core: generator, checks, validate, persist

Status: specifying · Ticket: none · Branch: none · Added: 2026-10-19

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

- CAP-models.R1 (kinds from types): added
- CAP-models.R2 (missing default names each engine): added
- CAP-models.R3 (system fields): added
- CAP-models.R4 (label from name): added
- CAP-models.R5 (wide text): added
- CAP-models.R6 (title field): added
- CAP-models.R7 (typed overrides): added
- CAP-checks.R1 (no access rule): added
- CAP-checks.R2 (missing field): added
- CAP-checks.R3 (wrong config type): added
- CAP-checks.R4 (system field target): added
- CAP-checks.R5 (dangling reference): added
- CAP-checks.R6 (no title field): added
- CAP-checks.R7 (ownedVia shape): added
- CAP-checks.R8 (unsafe warning): added
- CAP-checks.R9 (redundant override warning): added
- CAP-checks.R10 (todo app passes): added
- CAP-validate.R1 (errors for each field): added
- CAP-validate.R2 (Title rules): added
- CAP-validate.R3 (Notes length): added
- CAP-validate.R4 (time zone): added
- CAP-validate.R5 (reference required): added
- CAP-validate.R6 (rule override): added
- CAP-validate.R7 (enum case): added
- CAP-access.R1 (four rules): added
- CAP-access.R2 (read-only condition): added
- CAP-access.R3 (sealed scope): added
- CAP-access.R4 (read-only refusal): added
- CAP-access.R5 (owner from parent): added
- CAP-persist.R1 (tables): added
- CAP-persist.R2 (foreign keys): added
- CAP-persist.R3 (indexes): added
- CAP-persist.R4 (list query): added
- CAP-persist.R5 (keyset paging): added
- CAP-persist.R6 (child deletes): added
- CAP-persist.R7 (stale save): added
- CAP-persist.R8 (migration drop): added
- CAP-todo-app.R1 (models and types): added
- CAP-todo-app.R2 (access rules): added
- CAP-todo-app.R3 (overrides): added
- CAP-todo-app.R4 (completion hook): added
- CAP-todo-app.R5 (overdue hook): added
- CAP-todo-app.R6 (no other code): added
- JOB-generate.R1 (findings and exit code): added
- JOB-generate.R2 (schema file): added
- JOB-generate.R3 (README): added
- DES-errors: 14 rows added

## Changes

The change stage writes this section. The section lists the changes that deliver this
intent, in order.

- none yet

## Notes

- Checks command: `dotnet test tests/`. Set at init.
- The engine core was built once before in this session, with 21 passing tests, then
  reverted. That build is proof that the stages fit in F# with xUnit.
- Branch: this session is pinned to `ccr-1ef8bdfa-1cxxi7`, and the repository has no
  local `main`. The intent stages commit on that branch as the stand-in for main.
  `record_merges.py` did not run, because it runs only on main. No change has merged, so
  nothing was missed.
- Stage 3 here covers the schema, indexes, owner filter, paging, child deletes, stale
  saves, and migrations. The audit log, sessions, and the daily jobs wait for a later
  intent (assumed), because they need SQL Server to prove.
- CAP-checks.R9: the message of the redundant override warning → "Override Todo.Due label
  matches the default." (assumed)
- CAP-validate.R7: the message for an unknown enum case → "must be one of Low, Normal,
  High" (assumed)
- CAP-access.R3: the scope for a `self` model → the owner filter, where the record id is
  the user id (assumed)
- CAP-persist.R5: the page size limits → default 50, maximum 200, from the spec. A size
  over 200 is cut to 200 (assumed)
- CAP-todo-app.R6: how to prove "no other code" → the app project has only the four
  files (assumed)
- JOB-generate.R1: the command → `dotnet run --project src/Typed.Engine.Generator --
  generate`, output lines `<severity> <code> <message>` (assumed)
- JOB-generate.R2: where the schema goes → `build/schema.sql` (assumed)
- CAP-todo-app is a capability, not a job, because the models and hooks are shared logic
  for the later page and API jobs (assumed)

## Handoff

### Handoff · IN-0001 · intake → ready

Time: 2026-10-19T05:25:41+00:00

### Done

Wrote six capability specs and one job spec, with 46 requirements. Added 14 rows to
`DES-errors`. The specs follow the spec's names (`DES-names`): CAP-models (the model
plan), CAP-checks (build checks), CAP-validate, CAP-access, CAP-persist, CAP-todo-app
(the first app), and JOB-generate (the command).

### Decided on defaults

- CAP-checks.R9: the message of the redundant override warning → "Override Todo.Due label
  matches the default." (assumed)
- CAP-validate.R7: the message for an unknown enum case → "must be one of Low, Normal,
  High" (assumed)
- CAP-access.R3: the scope for a `self` model → the owner filter with the user id as the
  record id (assumed)
- CAP-persist.R5: page size over 200 → cut to 200, no size → 50 (assumed)
- CAP-todo-app.R6: proof of "no other code" → only four files in the app project
  (assumed)
- JOB-generate.R1: command and output line form (assumed)
- JOB-generate.R2: schema path `build/schema.sql` (assumed)
- Stage 3 scope: audit, sessions, and jobs wait for a later intent (assumed)
- CAP-todo-app is a capability (assumed)

### Open

- The branch question: pdlc expects `main` and one `pdlc/CH-*` branch for each change.
  This session is pinned to one branch. The user decides before the tests stage.
- CAP-persist.R6 (child deletes) needs a way to inject a failure mid-transaction. The
  change stage must give a seam.
- CAP-persist.R8 (migrations) needs a stored shape. The change stage decides where the
  shape lives.

### Look here

- pdlc/specs/capabilities/CAP-*.md
- pdlc/specs/jobs/JOB-generate.md
- pdlc/specs/design/DES-errors.md
- docs/spec/typed-layers-engine.md (the source)
- docs/design/typed-layers-engine.blueprint.md (section 6 scenarios)

### Don't redo

- The spec names and the mapping in `DES-names`.
- The SQLite test double rule in `DES-test-database`.

### Next: do this first

Run the ready checklist over each requirement. Look hardest at CAP-persist.R5 (paging),
R6 (child deletes), R7 (stale save), and R8 (migration), which have the most implied
behaviour.
