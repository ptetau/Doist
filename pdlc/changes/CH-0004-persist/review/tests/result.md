Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/CAP-persist.Tests.fs:1716-1722 · The check "Todo gets a table with the five system columns" looks for `[Id] ... PRIMARY KEY`, `[Version]`, `[CreatedAt]` and `[UpdatedAt]` in the whole three-table schema (`sqlServerSchema`), not in the `[Todo]` table. A Todo table that lacks one of these columns still passes, because `[TodoList]` and `[User]` have the same columns. Only `[Owner]` is checked on the Todo table alone, by a separate check. · CAP-persist.R1 line 1 (the `Todo` table holds these columns); remit: the check can fail on a plausible wrong version.
- tests/Typed.Engine.Tests/CAP-persist.Tests.fs:2138-2142 · The check "no stored shape gives the full schema" proves the first half of the line only (the migration equals the full schema). Nothing proves "the engine stores the new shape". The only related check is the `writeShape`/`readShape` round trip, which does not involve `migration`. · CAP-persist.R8 line 5 (assumed). The Interface names no surface for this store step, so the spec also needs to say where it happens.

Wrong versions:
- R1 five system columns · Todo table drops `[UpdatedAt]` (other tables keep it) · passes
- R1 User has no Owner · User table keeps `[Owner]` · fails (`DoesNotContain "[Owner]"` on the User table)
- R1 Todo keeps Owner · Owner dropped from owned tables · fails
- R1 SQL types · `[Notes]` as `nvarchar(200)` · fails
- R1 HasDue persisted · the column is not added · fails
- R1 no listSort, no HasDue · HasDue always added · fails
- R1 column override · override ignored, `[Subject]` kept · fails
- R1 sensitive override · sensitive drops the column · fails (schemas differ)
- R2 FK_Todo_List · the FK is missing or named differently · fails
- R2 foreignKeys once · the key is listed twice · fails
- R2 no CASCADE · the FK adds `ON DELETE CASCADE` · fails
- R2 one index starts with List · no FK index, or two · fails
- R3 TodoList index · the two views each get an index, or Name is left out · fails
- R3 Todo index · `HasDue` left out of the index · fails
- R3 User unique index · the index is not unique · fails
- R3 Note index · no title index · fails
- R4 columns · `SELECT *` · fails
- R4 owner and parent filter · the `[Owner]` filter is left out · fails
- R4 ORDER BY · `[HasDue] ASC` · fails
- R4 Archived view · the view filter is not applied · fails
- R4 Active view · the filter is inverted, or `= 1` is always added · fails
- R4 TodoList columns and Todo without listColumns · the wide `[Notes]` is selected · fails
- R5 paging 25 · the cursor has no `Id` tie-break, so same-date rows repeat or are skipped · fails (19 rows over 3 dates, pages of 4)
- R5 paging 25 · the parent filter is dropped · fails (decoy in another list of u1)
- R5 paging 25 · the owner filter is dropped · passes (the u2 decoy is in a different list, so the parent filter hides it)
- R5 sort order · empty dates sort first · fails
- R5 size 1 and size 200 one query · an extra count query · fails
- R5 210 rows · the cap is 100 · fails
- R5 theory 0, 201, 300, -1 · the limit is `<= 250` · fails (201). Cases 1 and 200 are accepted, so both edges are covered.
- R5 default 50 · the default is 100 or 20 · fails (55 rows)
- R5 bad cursor · the cursor is accepted · fails
- R6 removes list and 3 todos · the children are left · fails
- R6 other list kept · all todos are deleted · fails
- R6 failure after the second · no transaction, or no rollback · fails
- R6 u1 removes u2's list · the children are deleted before the owner check · fails (u2 todos are checked)
- R7 version 3 to 4 · the version does not rise · fails
- R7 stale · no version check · fails
- R7 new todo at 1 · new todo starts at 0 · fails
- R7 no version · a missing version is accepted · fails
- R8 UNPLANNED_DROP · a silent drop · fails
- R8 sp_rename · the rename note is ignored · fails
- R8 optional column · wrong ADD text · fails
- R8 UNSAFE_MIGRATION · a required column without a default is allowed · fails
- R8 no stored shape · a partial schema is returned · fails
- R8 round trip · the shape loses data · fails
- R8 no file · missing file gives an error · fails
- R8 SHAPE_UNREADABLE · invalid JSON throws or gives `None` · fails

Follow-ups:
- R5: the owner filter on a page is proved only at the string level (R4). Add a store-level line where u1 asks for a list that u2 owns, and expect no rows or `NOT_FOUND`.
- R7: no line covers a save with a version newer than the stored one. Add that line to pin the rule "any version other than the stored one is stale".

Notes:
- Names follow the tagging rule. Each module is named with its requirement ID (for example ``CAP-persist-R1``). Each check name starts with `CAP-persist.Rn`.
- Every Interface error code has a check with the exact code: `NOT_FOUND`, `STALE_VERSION`, `VALIDATION_FAILED` (limit, cursor, Version), `UNPLANNED_DROP`, `UNSAFE_MIGRATION`, `SHAPE_UNREADABLE`.
- The clock is set through `Store.create ... now`. No acceptance line depends on the date.
- The paging helper stops after 30 pages, so no check can hang.
- Checks use the `Sqlite` dialect for store runs and `SqlServer` for quoted SQL. This matches the Interface.
