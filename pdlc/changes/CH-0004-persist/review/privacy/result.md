Result: pass

Remit: privacy

Problems: none

Follow-ups:
- The cursor holds the sort-field values of the last row as plain base64 JSON. A caller can read them. The values come from rows the caller already sees under the owner filter, so no other person's data leaks. A new intent could sign or encrypt the cursor. IN-0013 (cursor privacy) already covers this.
- The engine stores every field of a model, including fields marked `sensitive`. The spec only requires that `sensitive` leaves the schema unchanged (R1). A new intent could add protection at rest for sensitive columns.

Notes:
- The change is mostly not applicable to privacy: `Persist.fs` is generic schema, query, and store code. It names no personal data itself. The todo app's `User` model (`Subject`, `DisplayName`) passes through it.
- No logging or printing of data. Findings hold the model name, the field name, and fixed messages. The `viewToSql` error prints the view expression, not row data.
- All queries bind values as parameters and filter by owner. A record outside the scope returns `NOT_FOUND`.
- No data goes to any outside place. `shape.json` holds the schema only, with no row data.
