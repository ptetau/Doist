Result: pass

Remit: quality

Problems: none

Follow-ups:
- Add a check for `ownerFor` and `checkChange` on a `Delete` of a todo in an archived list. The spec does not say what happens. The code refuses it, because `_action` is ignored. A new intent could state the rule.

Notes:
- Access.fs:772 · `reasonOf` finds the last word of the parent name with a `Seq.fold`. It works, but it is hard to read. A named helper such as `lastWord` would help.
- Access.fs:634, 675, 677 · The text `sprintf "%s was not found." model` appears three times. A `modelNotFound` helper would remove the repetition.
- Access.fs:777 · `_action` is unused. The Interface requires the parameter, so this is correct.
- Names follow DES-names and DES-errors. Error codes match the spec.
