Result: pass

Remit: tests

Problems: none

Wrong versions:
- JOB-generate.R1 the todo app prints no error line and exits 0 · the command prints an `error` line or exits 1 for the todo app · fails (exit code and `error ` prefix asserted)
- JOB-generate.R1 ...without the Todo access rule... · message text differs, or exit code stays 0 · fails (exact line and code 1)
- JOB-generate.R1 an unsafe override ... · the warning prints twice, prints as `error`, or exits 1 · fails (count equals 1, no `error ` line, code 0)
- JOB-generate.R1 the command frobnicate prints usage and exits 2 · unknown command exits 1 or prints no usage · fails (code 2 and the usage text)
- JOB-generate.R1 usage line to standard output · usage goes to stderr · fails (stdout must hold it, stderr must not)
- JOB-generate.R1 error finding line goes to standard output · findings written to stderr, or stderr not empty · fails (stdout holds line, stderr equals "")
- JOB-generate.R1 unsafe warning goes to standard output · warning written to stderr · fails (same asserts)
- JOB-generate.R2 writes schema.sql with tables, foreign keys, indexes · schema holds two tables, or no foreign key on Todo, or no index · fails (exactly 3 CREATE TABLE, FK in a Todo statement, index in a Todo statement). It passes a version that writes indexes for Todo only. See Notes.
- JOB-generate.R2 also writes shape.json · shape.json not written · fails
- JOB-generate.R2 an error finding leaves schema.sql unwritten · schema written despite an error · fails (the folder is new, so no old file hides the write)
- JOB-generate.R2 a missing build folder is created · code writes without creating the folder or its parent · fails
- JOB-generate.R3 the README gives the generate command · README lacks the command · fails
- JOB-generate.R3 exit codes 0, 1, 2 · README lists only code 0 and 1, or merges the codes on one line · fails (one line per code, each with its own meaning)
- JOB-generate.R3 the README gives the command dotnet test tests/ · README gives only `dotnet test` · fails
- JOB-generate.R3 the README names the --out seam · README omits `--out` · fails

Follow-ups: none

Notes:
- The index part of the R2 schema check is weak. It proves one index statement on Todo, not one on TodoList or User. The line is marked (assumed). A stricter check could name the three index sets from CAP-persist.R3.
- The command checks run `dotnet run` in several test classes. xUnit runs classes in parallel, so parallel builds of the same project may lock files and give a flaky failure. Each run has a 300000 ms time limit, so none can hang. No acceptance line fails because of this.
- The temporary folders are never deleted. This does not affect any line.
- The clock does not affect any line of this change.
- The Interface lists no error code of its own. `MISSING_ACCESS_RULE` is checked with its exact line, and the `UNSAFE_ESCAPE_HATCH` line is checked too.
