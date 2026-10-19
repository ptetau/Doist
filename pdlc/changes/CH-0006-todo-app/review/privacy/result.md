Result: pass

Remit: privacy

Problems: none

Follow-ups: none

Notes:
- The change touches a little personal data: `User.Subject` (ExternalId), `User.DisplayName`, and `User.TimeZone`. The spec names these fields.
- ExternalId is hidden and unique (Defaults.fs:820-824). The spec asks for this.
- No code logs data or prints it in an error. The validation messages (Defaults.fs) do not repeat the input value.
- The code sends data nowhere. It stores nothing itself. The `self` rule limits a user to their own record.
- Retention and the person's right to see or remove data are not in this spec, so they are not judged here.
