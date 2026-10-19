Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-validate.R1 a valid list gives an empty list · validate always adds an error for Archived = false · fails (Assert.Empty)
- CAP-validate.R1 two broken fields ... · validate stops at the first error, or reports both under one field name · fails (Length 2 and the sorted Notes/Title pair)
- CAP-validate.R2 spaces at both ends · no trim, so the padded name is judged as given · passes only if the name is under 200, so the 3-space 200-char check catches it · fails there
- CAP-validate.R2 spaces only is required · trim skipped, "   " counts as 3 characters · fails (expects "is required")
- CAP-validate.R2 200 characters is valid · limit coded as < 200 · fails
- CAP-validate.R2 201 characters is too long · limit coded as <= 201 · fails
- CAP-validate.R2 line break · the one-line rule skipped, or it checks only '\r' · fails ("\n" in the value)
- CAP-validate.R2 tab is valid · the rule rejects any control character · fails
- CAP-validate.R2 200 characters and three spaces at each end · length counted before trim · fails
- CAP-validate.R2 script tags · validate rejects angle brackets · fails
- CAP-validate.R3 no notes · None treated as an empty required value · fails
- CAP-validate.R3 5000 characters · limit coded as < 5000 · fails
- CAP-validate.R3 5001 characters · limit coded as <= 5001 · fails
- CAP-validate.R3 5000 characters and one space · Notes trimmed before the length check · fails
- CAP-validate.R4 Pacific/Auckland · every zone rejected · fails
- CAP-validate.R4 Mars/Base · Check of the type not run on the field · fails
- CAP-validate.R4 wrong case · the code lowercases the name before the lookup · fails on Linux (see Notes)
- CAP-validate.R5 empty reference · Ref Guid.Empty accepted · fails
- CAP-validate.R5 reference with an id · every reference rejected · fails
- CAP-validate.R6 failing predicate · overrides ignored · fails
- CAP-validate.R6 passing predicate · the message is added whatever the predicate gives · fails
- CAP-validate.R7 Urgent · an unknown value is mapped to the first case · fails
- CAP-validate.R7 High · the parse returns the string "High", not the case · fails (box High)
- CAP-validate.R8 abc for a whole number · abc parsed as 0 · fails
- CAP-validate.R8 2026-02-30 · date parse clamps to the month end · fails
- CAP-validate.R8 2026-10-18 · the result is the bare date, with no Some · fails
- CAP-validate.R8 empty value for optional Due · an error for an empty optional value · fails
- CAP-validate.R8 empty value for required Title · Ok with an empty string · fails
- CAP-validate.R8 on and true for Done · "on" not accepted · fails
- CAP-validate.R8 false for Done · any non-empty value gives true · fails
- CAP-validate.R8 missing value for Done · None gives an error · fails
- CAP-validate.R8 yes for Done · "yes" accepted as true · fails

Follow-ups:
- Add a check that "high" (lower case) for Priority gives the R7 error, and that "TRUE" for Done has a set result. The spec does not say whether case counts for enums and booleans. A new intent could state the rule.
- Add a check that a missing value for a required field gives "is required", and that a missing value for an optional field gives None. R8 names only the empty string.

Notes:
- All 32 checks name their requirement ID, and each module uses the ID with a hyphen. Each acceptance line has at least one check. The Interface lists no error codes and no test seams. No check uses time, randomness, or a vendor, and none can hang.
- The R4 checks use the system time zone database through the `knownZone` rule that the spec gives. The case-sensitive check needs a case-sensitive lookup, as on Linux with tzdata installed. The spec fixes this rule, so it is not a problem.
- The R6 check reads `.Items` from the override set, and the R8 whole-number check uses `NoTitle` from `ModelsFixtures`. Both come from CAP-models, which is outside this packet. I took them as present.
