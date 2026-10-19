Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/JOB-generate.Tests.fs:1377-1393 · No check shows that the command sends finding lines to standard output. The todo app has no finding, so the command check reads only the usage line. The finding-line check drives `run` with a StringWriter. A `Program.main` that passes `Console.Error` to `run` passes every check. · JOB-generate.R1 line "each finding line goes to standard output" (a line about the command surface, with no check at that surface)

Wrong versions:
- JOB-generate.R1 the todo app prints no error line and exits 0 · the command exits 1 on a clean app, or prints an error line · fails (exit code assert, stdout assert)
- JOB-generate.R1 without the Todo access rule · the message text or the code differs, or the exit code is 0 · fails (exact line, exit 1)
- JOB-generate.R1 unsafe override · the warning counts as an error and gives exit 1 · fails (exit 0 assert, exact line count of 1)
- JOB-generate.R1 frobnicate prints usage and exits 2 · exit 1 for an unknown command · fails (exit 2 assert)
- JOB-generate.R1 usage to standard output not standard error · usage goes to stdout, but `Program.main` passes `Console.Error` to `run` for findings · passes (the problem above)
- JOB-generate.R1 library entry writes each finding line · findings written as `error: CODE msg` · fails (regex needs `error CODE msg`)
- JOB-generate.R2 todo app writes schema.sql · schema has all three tables and one foreign key, but only one model has an index · passes (the check needs only one FOREIGN KEY and one INDEX anywhere; minor)
- JOB-generate.R2 also writes shape.json · the command skips shape.json · fails
- JOB-generate.R2 error finding leaves schema.sql unwritten · the schema is written before the checks run · fails
- JOB-generate.R2 missing build folder is created · the code writes without creating the folder · fails (the folder and its parent do not exist)
- JOB-generate.R3 README gives the generate command · README omits the command · fails
- JOB-generate.R3 README gives the meaning of exit codes · README line "0: an error was found" · passes (the word "error" appears on the line for 0; the meaning is not read, minor)
- JOB-generate.R3 README gives the command dotnet test tests/ · README omits the command · fails
- JOB-generate.R3 README names the --out seam · README omits `--out` · fails

Follow-ups:
- none

Notes:
- The command checks use a 300000 ms limit, so none can hang.
- No line depends on the clock.
- R1 lines 2 and 3 and R2 line 2 use `Generator.run` with a changed `Config`. The Interface names this seam for those lines. The exit code mapping in `Program.main` for exit 1 is not checked through the command.
- The only error code the change spec names is `MISSING_ACCESS_RULE`, and one check expects that exact code.
