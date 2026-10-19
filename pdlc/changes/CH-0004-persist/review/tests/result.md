Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/CAP-persist.Tests.fs:2032 (module CAP-persist-R7, helper `todoAt`) · `while t.Version < target do` loops forever if `revise` returns Ok without raising the version. Every R7 check that calls `todoAt` then hangs, and it hangs with no time limit · remit rule "No check can hang"
- tests/Typed.Engine.Tests/CAP-persist.Tests.fs:1909 and :1936 · The line "Given a page size of 200, ... then the page holds up to 200" is not proved. The size-200 checks hold 25 rows and 1 row, so a version that caps a page at 50 or 100 still passes. No check seeds more than 50 rows and asks for 200 · CAP-persist.R5 line 5

Wrong versions:
- R1 Todo gets a table with the five system columns · schema omits `[UpdatedAt]` or writes `[Version] int` · fails (`[UpdatedAt] datetimeoffset NOT NULL` and `[Version] rowversion NOT NULL` asserted)
- R1 User with the rule self has no Owner column · every table gets `[Owner]` · fails (`DoesNotContain "[Owner]"`)
- R1 Todo with an owned rule keeps the Owner column · `[Owner]` dropped for all tables · fails
- R1 Todo fields get their SQL types · `[Notes]` as `nvarchar(5000)` or `[Priority]` as `nvarchar(16)` · fails
- R1 listSort on Due adds the persisted HasDue column · HasDue column missing or not PERSISTED · fails
- R1 no listSort means no HasDue column · HasDue always added · fails
- R1 the column override renames Subject · override ignored · fails (`[ExternalSubject]` asserted and `[Subject]` absent)
- R1 the sensitive override leaves the schema as it was · sensitive drops the column or adds a comment · fails (compared with the plain schema)
- R2 Todo gets the foreign key FK_Todo_List · constraint name `FK_Todo_TodoList` or no trailing `;` · fails (full exact text)
- R2 foreignKeys lists the key of Todo once · key listed twice · fails
- R2 the schema of the three models has no CASCADE · `ON DELETE CASCADE` added · fails (case-insensitive)
- R2 exactly one Todo index starts with the column List · no FK index, or two · fails (count 1; the list index starts with Owner, so a separate index is needed)
- R3 TodoList with two views gets one index · one index for each view (two equal indexes) · fails (count 1)
- R3 Todo with listSort on Due gets one index · columns `Owner, List, Due, Id` without HasDue · fails
- R3 User gets a unique index on Subject · index not unique, or an extra list index · fails (length 1, `Unique`)
- R3 owned model with a title and no sort · no fallback index · fails
- R4 the Todo list query names its columns · `SELECT *` or all columns including Notes · fails (exact prefix)
- R4 the Todo list query filters by owner and parent · `[Owner]` filter missing · fails
- R4 the Todo list query orders by HasDue descending · `[HasDue] ASC` · fails (exact suffix)
- R4 the Archived view adds Archived = 1 · view ignored · fails
- R4 the Active view does not hold Archived = 1 · Active uses the Archived clause · fails
- R4 TodoList with no listColumns · adds system columns such as `[Owner]` · fails (exact prefix)
- R4 Todo with no listColumns does not select Notes · wide column selected · fails
- R5 paging 4 at a time · cursor on Due only, so ties are skipped · fails (about 4 todos share each date, with page size 4)
- R5 paging follows the sort · HasDue ascending, so empty dates come first · fails
- R5 a page of size 1 runs one query · an extra COUNT query · fails (query count delta 1)
- R5 a page of size 200 runs one query · an extra COUNT or per-row query · fails
- R5 a page size outside 1 to 200 · limit `<= 200` replaced by `< 200`, or `>= 0` · fails (0 and 201 present)
- R5 a page size of 200 and a page size of 1 are accepted · limit `< 200` · fails
- R5 no page size gives a page of 50 · default 100 or 20 · fails (55 rows, expect 50)
- R5 a bad cursor · cursor error without field `cursor` · fails
- R5 size 200 holds up to 200 · page capped at 50 or 100 · passes (no check seeds more than 50 rows and asks for 200)
- R6 removing a list removes the list and its 3 todos · children not deleted · fails
- R6 removing a list keeps the todos of another list · child delete not filtered by parent · fails
- R6 a failure after the second todo · no transaction · fails (todos 1 and 2 would be gone)
- R6 u1 removing a list of u2 · children deleted before the owner check, without rollback · fails (u2 todos checked)
- R7 save with version 3 on version 3 · Version not raised · fails (also hangs, see Problems)
- R7 save with version 3 on version 4 · stale check missing · fails (Title changed and compared)
- R7 a new todo is stored at version 1 · version 0 · fails
- R7 a save with no version · treated as no check · fails (field and message asserted)
- R8 no renamedFrom gives UNPLANNED_DROP · silent DROP COLUMN · fails
- R8 renamedFrom gives sp_rename · DROP and ADD in place of rename · fails (`DROP COLUMN` absent asserted)
- R8 one more optional column · ALTER writes `NOT NULL` · fails
- R8 one more required text column · all new columns refused, or none · fails (Tags test passes the first, this test the second)
- R8 no stored shape gives the full schema · empty migration · fails
- R8 a written shape reads back the same · `writeShape` stores a lossy form · fails
- R8 no shape file reads as None · returns an error · fails
- R8 a shape file that is not JSON · wrong code or message · fails

Follow-ups:
- none

Notes:
- No acceptance line depends on the date or the time, so no clock seam is needed. `fixedNow` is passed through `Store.create`.
- R8 line 5 says "the engine stores the new shape". The Interface gives `migration` as a pure function with no store step, so the checks prove only the full schema and the `writeShape`/`readShape` round trip. The spec should name the surface that stores the shape.
- R5 rows with `Version` and `viewToSql` have no acceptance line and no check. This is correct.
