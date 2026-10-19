Result: pass

Remit: security

Problems: none

Follow-ups:
- Flag an access rule whose model is not in the model list. `run` checks rules only through each described model, so a stray rule passes unseen. A new intent could add a finding for it.

Notes:
- `Checks.fs` is pure. It runs no command, query, file access, or network call, and it builds no path or HTML.
- The messages hold only model names, field names, type names, and the `unsafe` reason, all written by the developer. No secrets are logged.
- A type that fails `describe` yields its `MISSING_DEFAULT` errors. The build still fails, so the checks fail closed.
- A missing access rule, a wrong `ownedVia` target, and a non-bool `readOnlyWhenParent` field are all errors.
- `Persist.viewToSql` runs only to test the view. The code drops its SQL output.
- The change adds no dependency.
