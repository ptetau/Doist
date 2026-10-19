Result: pass

Remit: privacy

Problems: none

Follow-ups:
- A keyset cursor holds the last row's sort values, for example a title, in plain base64. A new intent could sign or encrypt the cursor, so a cursor never shows field values. The spec does not ask for it, and the values belong to the caller who gets the page.

Notes:
- `Persist.fs` has no logging and no output call. Error messages hold model names, column names, and fixed text. None holds a record value.
- Queries bind every value as a parameter. Every read and write takes the scope owner filter.
- `build/shape.json` holds table and column names only, with no record data.
- The `sensitive` override changes nothing in the schema, as CAP-persist.R1 says. The change stores no extra personal data and sets no retention rule. The spec asks for neither.
- I read the diff and the first full copy of `Persist.fs` in the packet. The packet's later copy and the check files are not needed for this remit.
