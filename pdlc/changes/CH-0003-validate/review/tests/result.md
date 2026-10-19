Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-validate.R1 a valid list gives an empty list · validate always returns one error · fails (Assert.Empty)
- CAP-validate.R1 two broken fields · validate stops at the first broken field · fails (expects 2 errors, Notes and Title)
- CAP-validate.R2 spaces at both ends is valid · validate rejects padded text · fails
- CAP-validate.R2 parseField trims the title · parseField returns the untrimmed text · fails (expects Title "Groceries")
- CAP-validate.R2 spaces only is required · blank text passes · fails
- CAP-validate.R2 200 characters is valid · limit is 199 · fails
- CAP-validate.R2 201 characters is too long · limit is 201 · fails
- CAP-validate.R2 line break · the line-break rule is skipped · fails
- CAP-validate.R2 tab is valid · tab counts as a line break · fails
- CAP-validate.R2 200 characters and three spaces each end · length counted before trim · fails (206 > 200)
- CAP-validate.R2 script tags · code rejects angle brackets · fails
- CAP-validate.R3 no notes · None gives an error · fails
- CAP-validate.R3 5000 characters · limit is 4999 · fails
- CAP-validate.R3 5001 characters · limit is 5001 · fails
- CAP-validate.R3 5000 characters and one space · notes are trimmed before counting · fails (the 5000-plus-space value)
- CAP-validate.R4 Pacific/Auckland · every zone is rejected · fails
- CAP-validate.R4 Mars/Base · zones are not checked · fails
- CAP-validate.R4 wrong case · lookup ignores case · fails (the "pacific/auckland" value)
- CAP-validate.R5 empty reference · Guid.Empty accepted · fails
- CAP-validate.R5 reference with an id · every reference rejected · fails
- CAP-validate.R6 failing predicate · the rule is ignored, or the error lands on another field · fails
- CAP-validate.R6 passing predicate · the message is added whatever the predicate gives · fails
- CAP-validate.R7 Urgent · unknown case maps to the first case · fails
- CAP-validate.R7 High · parse returns the string "High", not the case · fails
- CAP-validate.R8 abc for a whole number · a different message text · fails
- CAP-validate.R8 2026-02-30 · lenient date parse that rolls over · fails
- CAP-validate.R8 2026-10-18 · result is not wrapped in Some · fails
- CAP-validate.R8 empty value for optional Due · gives an error or a default date · fails
- CAP-validate.R8 empty value for required Title · empty text accepted · fails
- CAP-validate.R8 on and true for Done · only "true" accepted · fails (two asserts)
- CAP-validate.R8 false for Done · any non-empty text gives true · fails
- CAP-validate.R8 missing value for Done · missing gives an error · fails
- CAP-validate.R8 yes for Done · "yes" accepted as true · fails

Follow-ups: none

Notes:
- No line depends on time, and the Interface lists no seams and no error codes. Nothing is missing on those points.
- All 32 checks carry the requirement ID in the name, and each requirement has its own module.
- The checks use `describe`, `field`, `overrides`, `rule` and `.Items` from CAP-models. They do not look inside `Validate.fs`.
- `NoTitle` and `describeOk` come from `ModelsFixtures`, which this packet does not show. The `Count: int` field comes from the CAP-models R6 text.
