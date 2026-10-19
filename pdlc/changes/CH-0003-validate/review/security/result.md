Result: pass

Remit: security

Problems: none

Follow-ups:
- `parseField` does not check the text length. `validate` does. A caller that stores the parsed value without calling `validate` could keep over-long text. A new intent could make `parseField` give the length error, or could state that every write path calls `validate`.
- `validate` raises on a null string in a non-optional text field. `textOf` reads a null as a union and throws. The spec says no function raises. A new intent could return a "is required" error for a null value.
- A `rule` predicate that raises is not caught. A new intent could turn that into a field error.

Notes:
- No command, query, path, or HTML is built from input. The messages never include the raw value, so nothing leaks through errors.
- Parsing uses the invariant culture and exact formats. A kind with no text form gives "cannot be read from text", so it fails closed.
- Enum parsing matches a known case name exactly, so an unknown value cannot create a union value.
- The change adds no dependency.
