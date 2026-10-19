# Handoff · CH-0005 checks (show stage)

## Built

`src/Typed.Engine/Checks.fs` has `Checks.run`, `errors`, and `warnings`. It covers CAP-checks.R1 to R13.

## How to run

`dotnet test tests/Typed.Engine.Tests --filter "DisplayName~CAP-checks"` runs the 36 checks. The whole run has 176 checks.

## Choices the spec did not fix

- Findings come in model order, then field order. A finding with no field comes first. A finding with no real field comes last.
- A system field name gives only `SYSTEM_FIELD_TARGET`, not `MISSING_FIELD` too.
- A type with no default adds its `MISSING_DEFAULT` finding to the result.
- R11 also covers `hiddenInForm`, `readOnly`, `listEdit`, `sensitive`, and `column`.
