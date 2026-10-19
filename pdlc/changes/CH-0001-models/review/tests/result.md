Result: pass

Remit: tests

Problems:
none

Wrong versions:
- CAP-models.R1 Title has kind text 200 · the field gets Text 255 or the wrong default · fails
- CAP-models.R1 Notes has kind optional of text 5000 · Optional is dropped, or Notes gets Text 200 · fails
- CAP-models.R1 Priority has kind enum with Low Normal High · cases come out sorted (High, Low, Normal) · fails (the declared order is not alphabetical)
- CAP-models.R1 Due has kind optional of date · Due is Optional Timestamp, or plain Date · fails
- CAP-models.R1 List has kind reference to TodoList · target is "Ref<TodoList>" or the field is Id · fails
- CAP-models.R1 Done is boolean and CompletedAt is optional of timestamp · CompletedAt is Optional Date · fails
- CAP-models.R1 a union type as a model is WRONG_CONFIG_TYPE · the engine returns another code, or a plan with no fields · fails (exact code and message)
- CAP-models.R2 a list of Title has no default and names each engine · the engine uses the element default for `Title list` and gives a plan · fails
- CAP-models.R2 two fields with no default give two MISSING_DEFAULT errors · the engine stops at the first missing field · fails (count 2 and both field names)
- CAP-models.R3 the system field names are known · the list lacks UpdatedAt or has another order · fails
- CAP-models.R3 the plan of Todo holds its seven fields and no system field · the plan adds Id, Owner and the other system fields · fails
- CAP-models.R4 CompletedAt gives Completed at · title case, "Completed At" · fails
- CAP-models.R4 Due gives Due · the label is lower case "due" · fails
- CAP-models.R4 TimeZone gives Time zone · title case, "Time Zone" · fails
- CAP-models.R4 ExternalId gives External id · the trailing "Id" is kept as "ID" or "Id" · fails
- CAP-models.R4 HasDue gives Has due · title case, "Has Due" · fails
- CAP-models.R4 the label of a described field comes from its name · describe leaves Label as the raw name · fails
- CAP-models.R5 Notes is wide · isWide does not unwrap Optional · fails
- CAP-models.R5 Title is not wide · the limit is 100 or less · fails
- CAP-models.R5 text of length 400 is not wide · the test is `>= 400` · fails
- CAP-models.R5 text of length 401 is wide · the test is `> 401` · fails
- CAP-models.R5 a whole number is not wide · isWide answers yes for any non-text kind · fails
- CAP-models.R6 TodoList has the title field Name · no title field is found · fails
- CAP-models.R6 User has the title field DisplayName · the engine takes the first field (Subject), or the field named Name or Title · fails
- CAP-models.R6 NoTitle has no title field · the engine falls back to the first field · fails
- CAP-models.R6 the titleField override names Count · the override is ignored · fails
- CAP-models.R7 the selector for Due reads the name Due · fieldName returns the type name or the lambda variable "t" · fails
- CAP-models.R7 listSort gives one ListSort item for Due · the model name is the full type name, or the item has another field · fails
- CAP-models.R7 listColumns holds Title then Due in order · the fields come out reversed or sorted (Due, Title) · fails
- CAP-models.R7 listView uses the field Archived only · referencedFields also returns the lambda variable or the `not` call · fails

Follow-ups:
none

Notes:
- The sort-direction check uses only Ascending. A version that always stores Ascending passes it. The acceptance line names no direction, so this is not a problem.
- No check has a type that carries both a `[<TitleType>]` mark and a `titleField` override that names another field. The spec says the override replaces the mark. No acceptance line tests it. A later change could add a check.
- The type defaults use a no-op `Check` for all four entries. The spec table lists real checks. No R1 to R7 line depends on the checks, so this is not a problem.
- No line depends on time, randomness, or a vendor. "Test seams: none" holds. No check can hang.
- All 30 checks start with their requirement ID, and each requirement has its own module.
