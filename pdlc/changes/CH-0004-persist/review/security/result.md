Result: pass

Remit: security

Problems: none

Follow-ups:
- `page` and `listQuery` add no owner filter for the access rule `SystemOnly`. Only the scope from CAP-access guards that case, and it never makes a user scope for such a model. A guard in `page` could return `NOT_FOUND` when the access is `SystemOnly` and the scope is not the engine scope. This adds defence in depth and fails closed.
- `remove` returns `Ok` and commits when the final parent delete affects 0 rows. This can happen if another caller deletes the parent after the first `find`. A check of the row count, or a `find` inside the transaction, would close that window.
- Identifiers go into SQL as `[name]` with no escape of `]`. Names come from model types and `column` overrides, which the developer writes. Escaping `]` or checking names would help if names ever come from outside input.

Notes:
- All values are bound as parameters: the owner, parent, cursor, limit, view literals, and record values. Only developer-authored names (model, field, and column names) enter SQL text.
- The cursor is user input. A bad cursor gives `VALIDATION_FAILED`. A forged cursor cannot widen access, because the owner filter stays in every list query.
- `page` with the engine scope gives `NOT_FOUND`. `find`, `revise`, and `remove` apply the scope filter. A missing or foreign record gives `NOT_FOUND`.
- `remove` runs in one transaction and rolls back on a raised failure. The parent is checked through the scope before the children are deleted.
- The code holds no secrets. Errors and messages hold only the model, field, and fixed text.
- No new dependency is added. The fsproj only gains the `Compile` entry.
