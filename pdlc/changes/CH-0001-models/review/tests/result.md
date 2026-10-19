Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-models.R1 Title has kind text 200 · the engine ignores the type defaults and gives Text 255 or a fixed length · fails
- CAP-models.R1 Notes has kind optional of text 5000 · the engine drops the Optional wrapper for `option` types · fails
- CAP-models.R1 Priority has kind enum with Low Normal High · the engine sorts the cases (High, Low, Normal) · fails (the expected order is the declared order, not alphabetical)
- CAP-models.R1 Due has kind optional of date · the engine maps DateOnly to Timestamp · fails
- CAP-models.R1 List has kind reference to TodoList · the engine gives Reference "Ref" or Id · fails
- CAP-models.R1 Done is boolean and CompletedAt is optional of timestamp · the engine maps DateTimeOffset to Date · fails
- CAP-models.R1 a union type as a model is WRONG_CONFIG_TYPE · the engine gives a plan with no fields, or the code MISSING_DEFAULT · fails (the code, the count, and the message are all asserted)
- CAP-models.R2 a list of Title has no default and names each engine · the message shows `FSharpList<Title>` or omits an engine · fails (exact message)
- CAP-models.R2 two fields with no default give two MISSING_DEFAULT errors · the engine stops at the first failing field · fails (count 2, and both field names)
- CAP-models.R3 the system field names are known · the list lacks UpdatedAt or has another order · fails
- CAP-models.R3 the plan of Todo holds its seven fields and no system field · the plan adds Id and Version · fails
- CAP-models.R4 CompletedAt gives Completed at · the label title-cases each word ("Completed At") · fails
- CAP-models.R4 Due gives Due · the label lower-cases the first word · fails
- CAP-models.R4 TimeZone gives Time zone · the label title-cases each word · fails
- CAP-models.R4 ExternalId gives External id · the label keeps "Id" as "ID" · fails
- CAP-models.R4 HasDue gives Has due · the label does not split the words · fails
- CAP-models.R4 the label of a described field comes from its name · describe sets Label to the raw name · fails
- CAP-models.R5 Notes is wide · isWide does not unwrap Optional · fails
- CAP-models.R5 Title is not wide · isWide is true for every Text · fails
- CAP-models.R5 text of length 400 is not wide · the limit is `>= 400` · fails
- CAP-models.R5 text of length 401 is wide · the limit is `> 401` · fails
- CAP-models.R5 a whole number is not wide · isWide is true for any non-Text kind · fails
- CAP-models.R6 TodoList has the title field Name · the code picks no title · fails
- CAP-models.R6 User has the title field DisplayName · the code picks the first text field (Subject) · fails (Subject comes before DisplayName)
- CAP-models.R6 NoTitle has no title field · the code falls back to the first field · fails
- CAP-models.R6 the titleField override names Count · the code ignores overrides · fails
- CAP-models.R7 the selector for Due reads the name Due · fieldName returns the type name or the whole expression text · fails
- CAP-models.R7 listSort gives one ListSort item for Due · the model name comes from the wrong type, or the item holds the wrong field · fails
- CAP-models.R7 listColumns holds Title then Due in order · the code sorts the fields · fails (Title, Due is the reverse of alphabetical order)
- CAP-models.R7 listView uses the field Archived only · referencedFields returns the lambda parameter, or lists a field twice · fails

Follow-ups:
- The listSort check asserts only `Ascending`. A wrong version that always gives `Ascending` passes. The acceptance line names only the field, so this is not a problem. A new intent could add a `Descending` check.
- The R6 line "a titleField override replaces the marked choice" has no check on a model that has both a marked type and an override. The acceptance lines do not ask for one. A new intent could add it.
- The R7 text "a selector of another model does not compile" has no check. It is a compile-time rule, not an acceptance line.

Notes:
- The type defaults in the check file give every entry the check `fun _ -> []`. The spec table lists "no line break" and "known IANA zone". No R1 to R7 line reads `Check`, so this does not affect a result.
- No line depends on the clock, randomness, or a vendor. The Test seams section says "none", and no check needs a seam.
- No check waits on anything, so none can hang.
- Both error codes in the Interface (`MISSING_DEFAULT`, `WRONG_CONFIG_TYPE`) have a check that expects the exact code.
- Every check name starts with its requirement ID. Each requirement has one module with a hyphen in the name.
