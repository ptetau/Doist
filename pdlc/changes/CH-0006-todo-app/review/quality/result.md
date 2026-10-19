Result: pass

Remit: quality

Problems: none

Follow-ups:
- `Defaults.fs` adds a one-line check for `Title` and a known-zone check for `TimeZoneId`. No acceptance line asks for them. A new intent could state them as requirements with checks, such as "a Title with a line break fails validation" and "an unknown time zone fails validation".

Notes:
- Names follow the spec words (DES-names). The four files and the module names match the Interface section.
- No dead code and no repetition. `completionTime` and `isOverdue` are short and match the R4 and R5 rules.
- The app code raises no errors, so DES-errors does not apply here.
