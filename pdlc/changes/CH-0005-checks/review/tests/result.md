Result: pass

Remit: tests

Problems:
none

Wrong versions:
- CAP-checks.R1 no access rule · the code checks only for zero rules, so two rules give no finding · fails (the two-rule check expects the "more than one" message)
- CAP-checks.R1 two access rules · the code gives the "no access rule" text for both cases · fails (exact message)
- CAP-checks.R1 one rule / todo app · the code flags every model · fails (Assert.Empty)
- CAP-checks.R2 Ghost set · the code ignores sets for unknown models · fails
- CAP-checks.R2 Dew field · the code ignores unknown fields · fails
- CAP-checks.R2 existing field · the code flags every Label item · fails
- CAP-checks.R3 string on Due · the code skips the type compare · fails; swapping expected type and field type · passes (both "string" and "DateOnly" appear in the message, so the order is not checked)
- CAP-checks.R3 Title on Title · the code flags every Rule · fails
- CAP-checks.R4 Version · the code does not look at system fields · fails
- CAP-checks.R4 Title column · the code flags every Column item · fails
- CAP-checks.R5 dangling · the code never looks at Ref fields · fails
- CAP-checks.R5 todo app · the code flags every Ref field · fails
- CAP-checks.R6 no title · the code skips the title test · fails
- CAP-checks.R6 titleField override · the code ignores the override · fails
- CAP-checks.R7 Title as ownedVia · the code does not check the field kind · fails
- CAP-checks.R7 Name as read-only condition · the code does not check for bool · fails
- CAP-checks.R7 bool field · the code flags every read-only condition · fails
- CAP-checks.R8 one unsafe · the code gives an error, or drops the reason · fails (errors empty, exact message, severity)
- CAP-checks.R8 two unsafe · the code reuses the first reason · fails
- CAP-checks.R9 "Due" · the code never compares with the default · fails
- CAP-checks.R9 CompletedAt · the code compares with the raw field name, not the made label · fails
- CAP-checks.R9 "Due date" · the code warns on every label · fails; the code compares case-insensitively · passes (no near-case value such as "due" is tested)
- CAP-checks.R10 todo app · the code flags a valid list view, sort, or column list · fails
- CAP-checks.R11 two labels · the code allows duplicates · fails
- CAP-checks.R11 label and readOnly · the code flags any two items on one field · fails
- CAP-checks.R11 two fields · the code flags any two labels in a set · fails
- CAP-checks.R12 TodoList then Todo / Todo then TodoList · the code sorts the models by name (either direction) · fails (the two orders catch both)
- CAP-checks.R12 field order · the code keeps the order of the override items, or sorts by field name · fails (Done is first in the set, and first by name; Title is first in the record)
- CAP-checks.R12 empty · the code gives a fixed finding · fails
- CAP-checks.R13 Notes · the code allows wide-text sorts · fails
- CAP-checks.R13 401 / 400 · the code uses >= 400 or > 401 as the limit · fails (one test at each edge)
- CAP-checks.R13 Title sort · the code rejects every sort on text · fails
- CAP-checks.R13 computed view · the code accepts any predicate · fails
- CAP-checks.R13 equality view · the code rejects all views · fails

Follow-ups:
none

Notes:
- All eight codes of the Interface have a check that expects that exact code.
- No check uses a clock, so no seam is needed. The Interface lists none.
- R7 builds the access rule record by hand, because the typed `ownedVia` selector does not compile for a non-reference field. This is sound.
- R13 view check uses `match t.Title with Title s -> s.Length` in place of `t.Title.Length`. The comment gives the reason. The check still proves an unsupported view.
- R3 could assert the order of expected type and field type. It is a weakness, not a break of an acceptance line.
- R4 tests only `Version`, not the other system fields. The acceptance line names only `Version`.
