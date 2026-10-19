# Typed engine

This repository holds the typed engine, the todo app, and the generator.

## Generate the schema

```
dotnet run --project src/Typed.Engine.Generator -- generate [--out <folder>]
```

The command prints one line for each finding, as `<severity> <code> <message>`.
It writes `schema.sql` and `shape.json` to the folder when no finding is an error.
The flag `--out` names the folder. The default folder is `build/`.

Exit codes:

- 0: success, no error finding.
- 1: at least one error finding. The command writes no files.
- 2: unknown command. The command prints the usage line.

## Run the checks

```
dotnet test tests/Typed.Engine.Tests
```
