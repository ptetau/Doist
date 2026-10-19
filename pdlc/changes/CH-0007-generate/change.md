# CH-0007 · Generate

Intent: IN-0001 · Scope: JOB-generate · Status: planned · Ticket: none · Branch: none · Base: main
Tests-Locked: none

## Delivers

An app builder runs one command that prints every finding, sets the exit code, and writes the schema. The README tells how to run the generator and the checks.

## Requirements

### JOB-generate.R1 · The command prints each finding and sets the exit code

- Given the todo app, when the builder runs `generate`, then the output holds no `error`
  line and the exit code is 0.
- Given the todo app with the access rule of `Todo` removed. When the builder runs
  `generate`, then the output holds the line "error MISSING_ACCESS_RULE Model Todo has no
  access rule." and the exit code is 1.
- Given the todo app with the override `unsafe "rawName" "uses raw SQL"` on `TodoList`.
  When the builder runs `generate`, then the output holds the line "warning
  UNSAFE_ESCAPE_HATCH Unsafe escape hatch TodoList.rawName: uses raw SQL". The exit
  code is 0.
- Given the command `frobnicate`. When the builder runs it, then the output holds "usage:
  generate" and the exit code is 2. (assumed)
- Given the todo app, when the builder runs `generate`, then each finding line goes to
  standard output. (assumed)

### JOB-generate.R2 · The command writes the schema when there is no error

- Given the todo app. When the builder runs `generate`, then the file `build/schema.sql`
  holds the tables, the foreign keys, and the indexes of the three models. (assumed)
- Given the todo app with the access rule of `Todo` removed, when the builder runs
  `generate`, then `build/schema.sql` is not written.
- Given no `build/` folder, when the builder runs `generate`, then the command creates the
  folder. (assumed)

### JOB-generate.R3 · The README tells how to run the generator and the checks

- Given the README, when a reader looks for the generator, then the README gives the
  `generate` command and the meaning of its exit codes.
- Given the README, when a reader looks for the checks, then the README gives the command
  `dotnet test tests/`.

## Relies on

- CAP-models (used, not changed)
- CAP-checks (used, not changed)
- CAP-access (used, not changed)
- CAP-persist (used, not changed)
- CAP-todo-app (used, not changed)
- DES-names
- DES-errors
- DES-test-database
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

The command, in `src/Typed.Engine.Generator/Program.fs`:

- `dotnet run --project src/Typed.Engine.Generator -- generate [--out <folder>]`. The default folder is `build/`. (assumed)
- Output: one line for each finding on standard output, as `<severity> <code> <message>`, where severity is `error` or `warning`.
- Exit codes: 0 (no error finding), 1 (at least one error finding), 2 (unknown command, after the line "usage: generate [--out <folder>]").
- Files written on exit 0: `<folder>/schema.sql` (the SQL Server schema) and `<folder>/shape.json` (the stored shape).

The library entry that the command calls, in `src/Typed.Engine.Generator/Generator.fs`, module `Typed.Engine.Generator`:

- `type Config = { Defaults: TypeDefault list; Models: Type list; Rules: AccessRule list; Overrides: ModelOverrides list; OutDir: string }`
- `run: Config -> TextWriter -> int`: runs the checks, writes the lines, writes the files, and gives the exit code. The checks call `run` with a changed `Config` to prove the lines about a removed rule or an added `unsafe` override. (assumed)
- `todoConfig: outDir: string -> Config`: the config of the todo app, which `Program.main` uses.

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement, named with the requirement ID with a hyphen in place of the dot, in double backticks, such as ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

- output folder: the `--out` flag, or `Config.OutDir`. The checks use a temporary folder.
- the app under test: `Generator.run` takes a `Config`. A check can remove a rule or add an override without a change to the app code. The README names `--out`. (assumed)

## Files

- src/Typed.Engine.Generator/Program.fs
- src/Typed.Engine.Generator/Generator.fs (assumed)
- src/Typed.Engine.Generator/Typed.Engine.Generator.fsproj (assumed)
- README.md

## Check files

- tests/Typed.Engine.Tests/JOB-generate.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj: the Compile entry and the project reference to `Typed.Engine.Generator`

## Steps

### Step 1 · The command prints each finding and sets the exit code · [ ]

- Requirement: JOB-generate.R1
- Check: the checks named `JOB-generate.R1 ...` in `tests/Typed.Engine.Tests/JOB-generate.Tests.fs`
- Done when: every check of JOB-generate.R1 (the command prints each finding and sets the exit code) passes

### Step 2 · The command writes the schema when there is no error · [ ]

- Requirement: JOB-generate.R2
- Check: the checks named `JOB-generate.R2 ...` in `tests/Typed.Engine.Tests/JOB-generate.Tests.fs`
- Done when: every check of JOB-generate.R2 (the command writes the schema when there is no error) passes

### Step 3 · The README tells how to run the generator and the checks · [ ]

- Requirement: JOB-generate.R3
- Check: the checks named `JOB-generate.R3 ...` in `tests/Typed.Engine.Tests/JOB-generate.Tests.fs`
- Done when: every check of JOB-generate.R3 (the readme tells how to run the generator and the checks) passes

### Step 4 · README · [ ]

- Requirement: JOB-generate.R3
- Check: the checks named `JOB-generate.R3 ...`
- Done when: the README tells what this change adds, names the `--out` seam, and gives the commands `dotnet run --project src/Typed.Engine.Generator -- generate` and `dotnet test tests/Typed.Engine.Tests`

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `JOB-generate` as the ready stage left them.
