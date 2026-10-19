Result: pass

Remit: security

Problems: none

Follow-ups:
- Escape or validate identifiers and literals that the engine puts into SQL text. `q` wraps a name in brackets without doubling `]`. `sp_rename` takes unquoted names. `migration` copies the `Computed` text and the type from the stored shape into SQL. The names come from developer models and `build/shape.json`, not from users, so no acceptance line is broken. A new intent could add the check.
- Make `remove` check the row count of the parent delete. A parent that vanishes after `find` would still commit the child deletes. A new intent could roll back when the count is 0.
- Make `page` refuse a `SystemOnly` model for a user scope. Today the query has no owner filter for that rule. CAP-access.R3 stops a user scope from existing for such a model, so this is defence in depth.
- Cap the cursor length before decode. A new intent could add the cap.

Notes:
- Every value from a caller (owner, parent, cursor parts, view literals, ids, versions) goes in as a bound parameter. The `limit` is checked as 1 to 200 before use.
- An engine scope in `page` returns NOT_FOUND, so it fails closed. `find`, `revise`, and `remove` apply the scope filter to the read and to the write.
- The child delete in `remove` skips the owner filter. It runs only after `find` proved the parent is in scope, and a child copies its owner from the parent.
- Errors hold fixed text. They hold no secret, connection string, or file path.
- A bad view name in `listQuery` raises an exception instead of a finding. This is already filed as IN-0012.
- The cursor is not signed. IN-0013 (cursor privacy) already covers it.
- The change adds no package reference. The `fsproj` change is the Compile entry only.
