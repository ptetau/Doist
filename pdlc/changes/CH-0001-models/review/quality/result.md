Result: pass

Remit: quality

Problems: none

Follow-ups:
- `displayType` reads only the first generic argument. A type such as `Map<string, int>` would show as `Map<String>`. A new intent could name every argument in the MISSING_DEFAULT message.
- `fieldName` fails with `failwithf` and a raw `%A` dump of the quotation when the selector is not a property read. A new intent could add a coded finding (for example `MISSING_FIELD`) with a short message.
- `labelOf` splits every capital letter, so an acronym such as `URL` becomes "U r l". A new intent could keep acronyms together.

Notes:
- The names follow DES-names and the Interface. The error text of `MISSING_DEFAULT` and `WRONG_CONFIG_TYPE` matches the acceptance lines and names the model, field, type, and engines.
- `describe` carries the default as a four-item tuple (`found`) and then rebuilds the field from it. It also filters `results` twice. A small record or a partition would read more simply. This is a style point, not a rule break.
- `Severity.Error` and `Result.Error` clash in `Model.fs`. The code uses `Result.Error` where needed. The spec fixes the `Error` name, so this is not a problem.
- `unsafe` takes no selector, while the Interface text says each override takes one. The `Unsafe` item has no field, so the code is consistent with the item shape.
