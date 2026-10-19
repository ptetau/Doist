Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-persist.R1 system columns · Todo table lacks `[Version] rowversion` or has `[Version] int` · fails (exact string asserts on the Todo table alone)
- CAP-persist.R1 User has no Owner · code adds Owner to every table · fails
- CAP-persist.R1 Owned keeps Owner · code drops Owner everywhere · fails
- CAP-persist.R1 SQL types · Notes written as `nvarchar(200)`, or Priority as `nvarchar(16)` · fails
- CAP-persist.R1 HasDue · column missing or not PERSISTED · fails
- CAP-persist.R1 no listSort, no HasDue · code always adds HasDue · fails
- CAP-persist.R1 column override · override ignored, `[Subject]` kept · fails
- CAP-persist.R1 sensitive · sensitive drops the column from the schema · fails (schema equality)
- CAP-persist.R2 FK · FK text differs, or no FK written · fails
- CAP-persist.R2 foreignKeys once · two keys for one reference · fails
- CAP-persist.R2 no CASCADE · FK written with ON DELETE CASCADE · fails
- CAP-persist.R2 index on List · no index starts with List, or two do · fails
- CAP-persist.R3 TodoList · one index per view, or `Owner, Name, Id` · fails (exact column list, count of 1)
- CAP-persist.R3 Todo · `HasDue` left out of the index, or a duplicate index · fails
- CAP-persist.R3 User · non-unique index, or an added list index · fails
- CAP-persist.R3 Note · no title-field index · fails
- CAP-persist.R4 columns · `SELECT *`, or the Notes column added · fails
- CAP-persist.R4 owner and parent · parent filter missing · fails
- CAP-persist.R4 order · `ORDER BY [Due] ASC, [Id] ASC` with no HasDue · fails (EndsWith)
- CAP-persist.R4 Archived view · view filter ignored · fails; Active view with `= 1` · fails
- CAP-persist.R4 TodoList columns · extra column in the list · fails
- CAP-persist.R5 paging 4 at a time · keyset on Due only, with no Id tie-break · fails (3 dates and 6 rows per date cross page edges)
- CAP-persist.R5 order · empty dates sort first (SQLite NULLs first) · fails (the order assert and the 6 empty at the end)
- CAP-persist.R5 size 1 and size 200 one query · an extra COUNT query for Next · fails
- CAP-persist.R5 200 of 210 · cap of 100 · fails; the cap off by one, 199 · fails
- CAP-persist.R5 size outside 1 to 200 · bound of `<= 200` changed to `< 200`, or `>= 0` · fails (accepts 1 and 200; rejects 0 and 201)
- CAP-persist.R5 default 50 · default 100 · fails (55 rows)
- CAP-persist.R5 bad cursor · cursor ignored, or a crash · fails
- CAP-persist.R6 removal · children left behind · fails
- CAP-persist.R6 keeps other list · delete of all todos · fails
- CAP-persist.R6 failure · no transaction, so todos 1 and 2 are gone · fails
- CAP-persist.R6 u2 list · no owner check, or children deleted before the check · fails
- CAP-persist.R7 save passes · version not bumped, or set to a constant · fails (the step loop asserts +1)
- CAP-persist.R7 stale · version not compared, or the write happens before the compare · fails
- CAP-persist.R7 new at version 1 · add sets version 0 · fails
- CAP-persist.R7 no version · treated as a save with no check · fails
- CAP-persist.R8 UNPLANNED_DROP · the column dropped silently · fails
- CAP-persist.R8 sp_rename · ADD plus DROP COLUMN instead · fails
- CAP-persist.R8 optional column · wrong type, or NOT NULL · fails
- CAP-persist.R8 required column · ADD with a default, no error · fails
- CAP-persist.R8 no stored shape · migration is empty, or the shape does not round-trip · fails
- CAP-persist.R8 round trip · lossy JSON · fails
- CAP-persist.R8 no file · returns an error for a missing file · fails
- CAP-persist.R8 not JSON · returns None or throws · fails

Follow-ups: none

Notes:
- The R5 paging decoys sit in another list, so removing the owner filter alone would pass R5. The R4 text check on `[Owner] = @owner` catches the missing filter in the query text. A decoy in the same list owned by u2 would close this gap.
- No line depends on the current date or time, so no clock check is needed.
- The R5 limit check is a `[<Theory>]`, not a `[<Fact>]`. It still runs and has the ID in its name.
- Every Interface error code has an exact-code check. The NOT_FOUND code is proved by `remove` and by `isGone` on `find`.
- The loop in `collectAll` stops after 30 pages, so no check can hang.
