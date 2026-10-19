Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/JOB-generate.Tests.fs:1364 · The check "the command prints each finding line to standard output" calls `run` with a `StringWriter`. It never reads the standard output of the command. A `Program.main` that passes `Console.Error` to `run` passes this check. The todo app prints no finding, so the real-command check cannot see a finding line either. · JOB-generate.R1, line "each finding line goes to standard output", and the remit rule that a check proves a line at the surface that the line names.

Wrong versions:
- JOB-generate.R1 the todo app prints no error line and exits 0 · main returns 0 always, or prints an error line to stdout on the todo app · fails (the exit code and stdout lines are read from the real command)
- JOB-generate.R1 without the Todo access rule · the message says "has more than one access rule" or the code is wrong · fails (exact line match)
- JOB-generate.R1 an unsafe override on TodoList · the warning is printed twice, or the message drops the reason · passes for a double print (the title says "one warning" but the check does not count); fails for a dropped reason
- JOB-generate.R1 frobnicate prints usage and exits 2 · main prints usage to stderr · passes (the check reads stdout + stderr together); a wrong exit code fails
- JOB-generate.R1 each finding line to standard output · main passes Console.Error to run · passes (the check uses a StringWriter, not the command)
- JOB-generate.R2 writes schema.sql · the schema holds two tables, or no foreign key, or no index · fails (3 CREATE TABLE, FOREIGN KEY, and INDEX are asserted)
- JOB-generate.R2 also writes shape.json · shape.json is not written · fails
- JOB-generate.R2 an error finding leaves schema.sql unwritten · the schema is written before the checks run · fails (the folder holds no schema.sql and exit is 1)
- JOB-generate.R2 a missing build folder is created · run writes into a folder without creating it · fails (the parent folder does not exist either)
- JOB-generate.R3 the README gives the generate command · the README gives another command · fails
- JOB-generate.R3 exit codes 0, 1, and 2 · the README gives only code 1 · fails; a README with "net8.0" on a line holding "error" after the word "exit" could satisfy code 0 · passes (weak match)
- JOB-generate.R3 the README gives `dotnet test tests/Typed.Engine.Tests` · the README has no test command · fails
- JOB-generate.R3 the README names --out · the README omits --out · fails

Follow-ups:
- none

Notes:
- The exit code 1 and the exit code of the unsafe case are proved through `run`, as the Interface seam allows. No check proves that `Program.main` returns the exit code of `run`. A `main` that ignores it passes the exit-1 checks. The one real-command check with exit 0 cannot catch it. Consider one real-command check with a failing config if the Interface adds a seam for it.
- The exit code matching in the README check scans every line after the first "exit" line and takes the first line that holds the digit. It can match an unrelated line.
- The default folder `build/` is not tested. No acceptance line asks for it beyond the assumed "no build/ folder" line, which the check proves with `--out` style config.
