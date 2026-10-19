Result: pass

Remit: privacy

Problems: none

Follow-ups: none

Notes:
- Not applicable in substance. `Validate.fs` checks field values in memory. It stores nothing, logs nothing, and sends nothing out.
- Every error message is a fixed text, or holds only a length or the enum case names. No message echoes the input value. The `FieldError` holds the field name and the message only.
