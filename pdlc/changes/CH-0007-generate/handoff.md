# Handoff · CH-0007 (generate)

## Checks

`tests/Typed.Engine.Tests/JOB-generate.Tests.fs` holds the checks for JOB-generate.R1 to R3. The lock is in `change.md`.

## What to build

- The project `src/Typed.Engine.Generator/Typed.Engine.Generator.fsproj`, with `Generator.fs` and `Program.fs`.
- In `Generator.fs`: `Config`, `run`, `todoConfig`, and `execute`. The Interface in `change.md` gives their types.
- `Program.main` calls `execute argv (todoConfig out) Console.Out Console.Error`.
- A `README.md` at the root. It gives the `generate` command, the exit codes 0, 1, and 2 (one line for each), the `--out` flag, and the command `dotnet test tests/Typed.Engine.Tests`.

## Notes

- The generator writes `schema.sql` and `shape.json` only when no finding is an error.
- The checks run the real command through `dotnet run`, so the build must work from a clean folder.
