Result: pass

Remit: specification

Problems: none

Follow-ups:
- `checkChange` returns `Ok` for a model with no rule or a `system` model. The spec only covers `ownedVia`. A new intent could refuse these, because access is denied by default.
- `ownerFor` returns the user for `Owned` and `Self` models. The spec states only the `ownedVia` cases. A new intent could state these cases.
- `checkChange` refuses a `Delete` of a todo in an archived list, as it refuses `Create` and `Change`. The spec asks only about a delete of the list itself. The Progress log already names this as an extra check.

Notes:
- All five requirements (R1 to R5) match the code in `Access.fs`. The Interface entries match too. The `Scope` record is private and `scopeFor` and `engineScope` are the only makers.
- Only `Access.fs` and the Compile entry in `Typed.Engine.fsproj` changed. No README is in "Files".
- The Writing check shows no findings. The one code comment reads as plain English.
- All 20 frames show what their captions claim, after the step. Frame 20 shows 24 passing checks of CAP-access. The Progress log says 54 checks pass in total, which includes the CAP-models checks. This is consistent.
