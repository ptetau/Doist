# IN-0001 · Engine core: generator, checks, validate, persist

Status: ready · Ticket: none · Branch: none · Added: 2026-10-19

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
- CAP-checks.R11 (duplicate override): added at ready
- CAP-checks.R12 (finding order): added at ready
- CAP-checks.R13 (sort or view that cannot be served): added at ready
- CAP-validate.R1 (errors for each field): added
- CAP-validate.R2 (Title rules): added
- CAP-validate.R3 (Notes length): added
- CAP-validate.R4 (time zone): added
- CAP-validate.R5 (reference required): added
- CAP-validate.R6 (rule override): added
- CAP-validate.R7 (enum case): added
- CAP-validate.R8 (raw input by kind): added at ready
- CAP-access.R1 (four rules): added
- CAP-access.R2 (read-only condition): added
- CAP-access.R3 (sealed scope): added, changed at ready
- CAP-access.R4 (read-only refusal): added
- CAP-access.R5 (owner from parent): added
- CAP-persist.R1 (tables): added
- CAP-persist.R2 (foreign keys): added
- CAP-persist.R3 (indexes): added
- CAP-persist.R4 (list query): added
- CAP-persist.R5 (keyset paging): added, changed at ready
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
- DES-errors: 16 rows added

## Changes

The changes, in stack order. Each change covers one spec.

1. CH-0001 (CAP-models): The engine can describe an F# record type as a model plan.
2. CH-0002 (CAP-access): Each model declares one access rule.
3. CH-0003 (CAP-validate): One rule set checks a record and gives the errors for each field.
4. CH-0004 (CAP-persist): The engine writes the schema, the foreign keys, the indexes, and the list queries for each model.
5. CH-0005 (CAP-checks): Every build check runs over the models, the access rules, and the override sets.
6. CH-0006 (CAP-todo-app): The todo app exists as models, type defaults, access rules, overrides, and two hooks, and nothing else.
7. CH-0007 (JOB-generate): An app builder runs one command that prints every finding, sets the exit code, and writes the schema.

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
- CAP-checks.R9 (redundant override warning): the message of the redundant override warning → "Override Todo.Due label
  matches the default." (assumed)
- CAP-validate.R7 (enum case): the message for an unknown enum case → "must be one of Low, Normal,
  High" (assumed)
- CAP-access.R3 (sealed scope): was "the owner filter for `u1`, and `u1` is the record id".
  Now "the self scope for `u1`, which matches only the record whose `Id` is `u1`". The `User` table has
  no `Owner` column (CAP-persist.R1 (tables)), so the guess gave way.
- CAP-persist.R5 (keyset paging): was "a page size of 300 → the engine uses the size 200".
  Now "a page size outside 1 to 200 → `VALIDATION_FAILED` on `limit`". One rule for both edges.
- CAP-persist.R5 (keyset paging): no page size → 50 (assumed). A bad cursor → `VALIDATION_FAILED` on
  `cursor` (assumed)
- CAP-persist.R3 (indexes) and R4 (list query): the wide-text sort and the unsupported view moved to
  CAP-checks.R13 (sort or view that cannot be served). The checks produce those findings.
- CAP-persist.R7 (stale save): a new record is at version 1. A save with no version →
  `VALIDATION_FAILED` on `Version` (assumed)
- CAP-persist.R8 (migration drop): the shape lives in `build/shape.json`. No file → the full schema. A file
  that is not JSON → `SHAPE_UNREADABLE`. A new required column → `UNSAFE_MIGRATION`
  (assumed)
- CAP-models.R1 (kinds from types): an empty record → a plan with no fields. A union as a model →
  `WRONG_CONFIG_TYPE` (assumed)
- CAP-checks.R11 (duplicate override): two overrides of one kind on one field → `WRONG_CONFIG_TYPE` (assumed)
- CAP-checks.R12 (finding order): findings in model order, then field order (assumed)
- CAP-validate.R2 (Title rules): a tab is not a line break. `<script>` passes validation, because
  encoding is the job of the display (assumed)
- CAP-validate.R3 (Notes length): the length of notes counts as given, with no trim (assumed)
- CAP-validate.R4 (time zone): the time zone name is case-sensitive (assumed)
- CAP-validate.R8 (raw input by kind): the parse messages for a whole number, a date, and a boolean.
  The values "on", "true", "false" for a checkbox (assumed)
- CAP-access.R4 (read-only refusal): creating a todo in an archived list → `READ_ONLY_RECORD` (assumed)
- CAP-todo-app.R6 (no other code): how to prove "no other code" → the app project has only the four
  files (assumed)
- JOB-generate.R1 (findings and exit code): the command → `dotnet run --project src/Typed.Engine.Generator --
  generate`, output lines `<severity> <code> <message>` to standard output. An unknown
  command → usage and exit 2 (assumed)
- JOB-generate.R2 (schema file): where the schema goes → `build/schema.sql`. The command creates
  `build/` (assumed)
- CAP-todo-app is a capability, not a job, because the models and hooks are shared logic
  for the later page and API jobs (assumed)
- Change stage: the project files (`Typed.Engine.fsproj`, `Todo.App.fsproj`, the generator
  project, and the test project) are in no code map. Each change lists its own `Compile`
  entries as parts of the shared file (assumed).
- Change stage: shared contracts `Record`, `HookContext`, and the `ErrorCode` union live
  in `Model.fs`, under CAP-models (model plan) (assumed).
- Change stage: the checks command names the test project, `dotnet test
  tests/Typed.Engine.Tests`, because `dotnet test` needs a project or solution file.
- Change stage: a check proves JOB-generate.R1 (findings and exit code) and R2 (schema
  file) with a changed `Config` through `Generator.run`. No check edits the app code
  (assumed).

## Handoff

### Handoff · IN-0001 · change → tests

Time: 2026-10-19T05:38:18+00:00

### Done

Wrote seven change specs, one for each spec, in stack order. They are CH-0001 (models),
CH-0002 (access), CH-0003 (validate), CH-0004 (persist), CH-0005 (checks), CH-0006 (todo
app), and CH-0007 (generate). Each change copies its requirements from its spec and gives the F#
interface, the test seams, the files, and the check files.

### Decided on defaults

- The project files are shared parts, listed in each change (assumed)
- `Record`, `HookContext`, and `ErrorCode` live in `Model.fs` (assumed)
- CH-0004 (persist): `Dialect`, `queryCount`, `onBeforeChildDelete`, and `Rename` as seams and shapes (assumed)
- CH-0002 (access): the read-only reason is built from the parent model name and the field (assumed)
- CH-0007 (generate): `--out`, `Generator.run`, and `Config` as the seams (assumed)

### Open

- The branch question: pdlc expects `main` and one `pdlc/CH-*` branch for each change.
  This session is pinned to one branch. The user decides before the tests stage starts
  CH-0001 (models).

### Look here

- pdlc/changes/CH-0001-models/change.md to pdlc/changes/CH-0007-generate/change.md
- pdlc/config.md (the checks commands)
- docs/design/typed-layers-engine.blueprint.md (section 6, the Gherkin scenarios)

### Don't redo

- The requirement text and the order of the changes.

### Next: do this first

Start CH-0001 (models). Its test writer must make the test project
`tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` with xUnit and a project reference to
`src/Typed.Engine/Typed.Engine.fsproj`.
