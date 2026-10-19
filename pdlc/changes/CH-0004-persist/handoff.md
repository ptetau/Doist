# Handoff · CH-0004 · build → show

Time: 2026-10-19T06:46:50+00:00

## Done

The builder wrote `src/Typed.Engine/Persist.fs` and added it to the project. All 139
checks pass, 49 of them for CAP-persist.R1 (tables) to R8 (migration drop). The check
files did not change after the lock `bbd2344`. The build commit is `066a033`. The eight
requirements are `built`.

## Decided on defaults

The builder decided these where the Interface was silent. The verify reviewers must weigh
each one:

- `find`, `add`, `revise`, and `remove` use the field name as the column name. They do
  not follow the `column` override. `page` and the schema do (assumed by the builder)
- `page` with the engine scope gives `NOT_FOUND`, because its query always filters by
  owner (assumed by the builder)
- `listQuery` raises on an unknown or unsupported view name. `viewToSql` is the call that
  gives the reason as a value (assumed by the builder)
- `migration` does not drop a table that is missing from the next shape, and ignores a
  changed column type. No line covers either (assumed by the builder)
- `schema Sqlite` omits the foreign keys, because SQLite has no `ADD CONSTRAINT` (assumed
  by the builder)
- Only the SQLite path ran under the checks. On SQL Server the store leaves `Version` to
  the `rowversion` column (assumed by the builder)

## Open

- none

## Look here

- src/Typed.Engine/Persist.fs (what to show)
- tests/Typed.Engine.Tests/CAP-persist.Tests.fs (the 49 checks and the `PersistFixtures`)

## Don't redo

- The checks and the build. Both are committed and pass.

## Next: do this first

Record the terminal with one `dotnet fsi` call for each acceptance line, as the earlier
changes did, then the CAP-persist checks. The store lines run on SQLite in memory inside
the script.
