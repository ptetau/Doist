Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-validate.R1 a valid list gives an empty list · validate always adds an error · fails
- CAP-validate.R1 two broken fields · validate stops at the first error · fails (2 errors expected, Notes and Title each named)
- CAP-validate.R2 spaces at both ends · length or required rule runs on the untrimmed text · passes only if trimmed, so fails on the wrong version (the 200 chars plus 3 spaces each end check also catches it)
- CAP-validate.R2 spaces only · trim skipped, so "   " passes · fails
- CAP-validate.R2 200 characters · limit is "< 200" · fails
- CAP-validate.R2 201 characters · limit is "<= 201" · fails
- CAP-validate.R2 line break · one-line rule skipped · fails
- CAP-validate.R2 tab · tab treated as a line break · fails
- CAP-validate.R2 200 chars plus 3 spaces each end · length counted before trim · fails
- CAP-validate.R2 script tags · code rejects "<" or ">" · fails
- CAP-validate.R3 no notes · None treated as a required empty value · fails
- CAP-validate.R3 5000 characters · limit is "< 5000" · fails
- CAP-validate.R3 5001 characters · limit is "<= 5001" · fails
- CAP-validate.R3 5000 characters and one space · code trims Notes · fails (a trimmed 5000 gives no error)
- CAP-validate.R4 Pacific/Auckland · every zone rejected · fails
- CAP-validate.R4 Mars/Base · zone check skipped · fails
- CAP-validate.R4 wrong case · code uses FindSystemTimeZoneById (case-insensitive on Linux) · fails
- CAP-validate.R5 empty reference · Guid.Empty accepted · fails
- CAP-validate.R5 reference with an id · every reference rejected · fails
- CAP-validate.R6 failing predicate · rule overrides ignored · fails
- CAP-validate.R6 passing predicate · message added whatever the predicate gives · fails
- CAP-validate.R7 Urgent · unknown case defaults to the first case · fails; case order is caught by "Low, Normal, High" (declared order, not alphabetical)
- CAP-validate.R7 High · parser returns the first case · fails (High is the last case)
- CAP-validate.R8 abc for whole number · parser returns 0 · fails
- CAP-validate.R8 2026-02-30 · date parser clamps to the month end · fails
- CAP-validate.R8 2026-10-18 · parser returns the date without Some · fails
- CAP-validate.R8 empty for optional Due · empty gives an error · fails
- CAP-validate.R8 empty for required Title · empty gives an empty string · fails
- CAP-validate.R8 on and true · "on" not accepted · fails
- CAP-validate.R8 false · any non-empty value gives true · fails
- CAP-validate.R8 missing Done · missing gives an error · fails
- CAP-validate.R8 yes for Done · "yes" accepted as true · fails

Follow-ups:
- Parsing of enum and boolean values is case-sensitive in no spec line. A new intent could say whether "high" or "True" parses, and add a close-value check for it.
- R8 has no check that a missing value for a required non-boolean field gives "is required". A new intent could add that line.

Notes:
- Each of the 8 requirements has a module named with its ID and checks that start with the ID.
- No line depends on the clock, and the Interface lists no seam and no error code, so none is needed.
- No check waits on anything, so none can hang.
- The R4 checks use the real zone list of the machine, as the Interface says. The result depends on the machine having tzdata.
