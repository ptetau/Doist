# DES-test-database · Which database the checks use

Status: agreed · Default: yes · Ask at init: no

## Rule

The checks of the engine library use SQLite in memory as a test double. SQL Server is
the target database, and the checks that need SQL Server run only in CI, in a SQL Server
container.

- A check of the generator, the build checks, validation, or the SQL that the engine
  writes runs in SQLite in memory. The check swaps `TOP (@n)` for `LIMIT @n` and uses a
  generated column for each `Has` column.
- A check of the web app, sign-in, sessions, the audit log, or a security rule needs SQL
  Server and a sign-in provider. Mark the check `[<RequiresSqlServer>]`. The checks
  command skips it when no `SQLSERVER_CONNECTION` setting is set.
- A change that only adds such checks is not verified until CI runs them.

## Why

This sandbox has no SQL Server and no sign-in provider. Without a test double, no check of
the engine library can run here. Without the SQL Server marker, a skipped check would look
like a pass.
