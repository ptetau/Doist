Result: pass

Remit: security

Problems: none

Follow-ups:
- `fieldName` throws with `failwithf` on a selector that is not a property read, for example `fun t -> t.A + 1`. A later intent could report this as a `Finding` such as `WRONG_CONFIG_TYPE`, so the generator fails closed with a code and not an exception.
- `describe` and `Rule` do not cap the number of fields or the size of input. Nothing in this change takes network or file input, so a later intent can add limits where the input first arrives.

Notes:
- The code is pure. It does no file, network, shell, or query work. It builds no command, path, query, or HTML from input.
- Error messages hold only type, model, and field names. They hold no secret.
- No dependency was added. The project file has only the SDK reference.
- `Rule` uses `unbox<'F>`. A wrong object type throws and does not pass. This is fail-closed.
