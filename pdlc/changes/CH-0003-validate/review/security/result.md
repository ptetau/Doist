Result: pass

Remit: security

Problems: none

Follow-ups:
- `parseField` does not check the length of a text value. A caller who parses raw input and skips `validate` can get a text value longer than the kind allows. A new intent could add the length check to `parseField`. IN-0009 (parseField length) already covers this.
- `validate` and `parseField` can raise on malformed input. Examples are a null record, a throwing `rule` predicate, or a `Text` field with a type that is not a union. A new intent could return these cases as errors. IN-0010 (validate never raises) already covers this.

Notes:
- The code builds no command, query, path, or HTML. Reflection works only on types from the model, never on names from input.
- Error messages are fixed text. The enum message lists the model's cases and does not echo the raw input.
- No secrets, no new dependency.
- Parsing uses the invariant culture and exact formats, so the result does not depend on the host locale.
- The time zone rule uses an exact id match. It does not call `FindSystemTimeZoneById`.
