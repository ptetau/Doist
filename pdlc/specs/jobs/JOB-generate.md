# JOB-generate · Run the build checks and write the schema from the command line

Status: active

## Who and why

An app builder, or CI, runs the generator after a change to the models. The generator
loads the models, the access rules, and the overrides. Then it runs every build check,
prints each finding, and writes the schema. The build fails when any finding is an error.

## Relies on

- Capabilities: CAP-models, CAP-checks, CAP-access, CAP-persist, CAP-todo-app
- Design: DES-errors, DES-names, DES-test-database

## Code map

- src/Typed.Engine.Generator/Program.fs: the `generate` command
- README.md: how to run the generator and the checks

## Requirements

### JOB-generate.R1 · The command prints each finding and sets the exit code

Status: ready · Intents: IN-0001

The command is `dotnet run --project src/Typed.Engine.Generator -- generate` (assumed).
It prints one line for each finding, in the form `<severity> <code> <message>`
(assumed). The exit code is 1 when any finding is an error, and 0 otherwise.

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

Status: ready · Intents: IN-0001

- Given the todo app. When the builder runs `generate`, then the file `build/schema.sql`
  holds the tables, the foreign keys, and the indexes of the three models. (assumed)
- Given the todo app with the access rule of `Todo` removed, when the builder runs
  `generate`, then `build/schema.sql` is not written.
- Given no `build/` folder, when the builder runs `generate`, then the command creates the
  folder. (assumed)

### JOB-generate.R3 · The README tells how to run the generator and the checks

Status: ready · Intents: IN-0001

- Given the README, when a reader looks for the generator, then the README gives the
  `generate` command and the meaning of its exit codes.
- Given the README, when a reader looks for the checks, then the README gives the command
  `dotnet test tests/`.
