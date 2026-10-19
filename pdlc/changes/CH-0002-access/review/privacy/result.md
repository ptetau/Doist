Result: pass

Remit: privacy

Problems: none

Follow-ups: none

Notes:
- Mostly not applicable. The change handles user ids (Guid) and the `Owner` of a record. It stores nothing, logs nothing, and sends nothing out.
- Error messages name only models and fields, such as "Todo was not found." They hold no user id and no record value.
- `scopeFor` and `ownerFor` return `NOT_FOUND` for a record of another user. The result does not reveal that the record exists.
