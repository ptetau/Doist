Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/JOB-generate.Tests.fs:1456-1470 · The check for "schema.sql holds the tables, the foreign keys, and the indexes of the three models" proves the tables of all three models. It proves only one foreign key (on Todo) and one index statement (any index naming Todo). A generator that writes indexes for Todo only passes. The `TodoList` index (Owner, Archived, Name, Id) and the `User` unique index on Subject are never checked. · JOB-generate.R2 line 1 (assumed): "the tables, the foreign keys, and the indexes of the three models"

Wrong versions:
- JOB-generate.R1 the todo app prints no error line and exits 0 · the command exits 0 but prints an `error` line, or exits 1 for the clean app · fails (exit code and line filter)
- JOB-generate.R1 the todo app without the Todo access rule prints MISSING_ACCESS_RULE and exits 1 · the message says "has more than one access rule", or the exit code is 0 · fails (exact line and exit code 1)
- JOB-generate.R1 an unsafe override on TodoList prints one warning and exits 0 · the warning is printed twice, or the exit code is 1 on a warning · fails (count of 1, exit code 0)
- JOB-generate.R1 the command frobnicate prints usage and exits 2 · exit code 1 for an unknown command · fails (exit code 2)
- JOB-generate.R1 the command prints the usage line to standard output and not to standard error · the usage line goes to standard error · fails (stdout line and stderr check)
- JOB-generate.R1 the error finding line goes to standard output and standard error stays empty · findings go to standard error · fails
- JOB-generate.R1 the unsafe override warning goes to standard output and standard error stays empty · warnings go to standard error · fails
- JOB-generate.R2 the todo app writes schema.sql with the tables, foreign keys, and indexes · indexes written for Todo only, or for fewer than three models · passes (the problem above); a missing table or missing Todo foreign key fails
- JOB-generate.R2 the todo app also writes shape.json · shape.json is not written · fails
- JOB-generate.R2 an error finding leaves schema.sql unwritten · schema.sql is written despite the error · fails
- JOB-generate.R2 a missing build folder is created · the code does not create the folder, or only one level of it · fails (the parent is also missing)
- JOB-generate.R3 the README gives the generate command · the README gives `dotnet run ... -- build` · fails
- JOB-generate.R3 the README gives the meaning of exit codes 0, 1, and 2 · the README lists no code 2 meaning, or swaps the meanings of 0 and 1 · fails (a separate line per code, with a meaning regex)
- JOB-generate.R3 the README gives the command dotnet test tests/ · the README gives only `dotnet test` · fails
- JOB-generate.R3 the README names the --out seam · the README omits `--out` · fails

Follow-ups: none

Notes:
- No line depends on the clock. The command runs have a 120 second limit. Both error codes that the Interface lists for this change (MISSING_ACCESS_RULE, UNSAFE_ESCAPE_HATCH) are matched exactly.
- The R3 exit-code check needs each code on its own line with no other code token. A README that gives the three codes in one sentence fails it. This is stricter than the acceptance line, but the failure message is clear.
- Check names and module names follow the ID rule.
