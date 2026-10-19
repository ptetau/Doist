Result: pass

Remit: security

Problems: none

Follow-ups:
- `checkChange` returns `Ok` for a model with no rule, and for a `system` model. `scopeFor` and `ownerFor` return `NOT_FOUND` for these. A new intent could make `checkChange` fail closed with `NOT_FOUND` for them.
- `checkChange` treats a missing or non-bool read-only field in the parent `Values` as "not read-only". The change passes the check in that case. A new intent could make it fail closed with an error.
- `checkChange` does not check that the parent belongs to the caller. It has no user argument. `ownerFor` and `scopeFor` do the owner checks. A new intent could add an owner check to the change path.

Notes:
- `Scope` is a record with private fields. Code outside the module cannot build or copy one. This meets the sealed-scope line of CAP-access.R3.
- `scopeFor` and `ownerFor` deny by default: a `system` model, a model with no rule, and a parent of another user all give `NOT_FOUND`.
- Error messages hold only model names. They hold no secrets and no user input.
- No new dependency. No path, command, query, or HTML is built from input.
