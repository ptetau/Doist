Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-todo-app.R1 models order · models list in the order Todo, TodoList, User · fails (exact list order)
- CAP-todo-app.R1 User fields · Subject typed as Text 200 · fails (Text 255 asserted)
- CAP-todo-app.R1 TodoList fields · Archived left out · fails (field list)
- CAP-todo-app.R1 Todo fields · Notes not optional · fails (Optional(Text 5000))
- CAP-todo-app.R1 Title default · Title marked unique · fails (Unique false)
- CAP-todo-app.R1 Notes default · length 500 · fails (Text 5000)
- CAP-todo-app.R1 TimeZoneId default · length 32 · fails (Text 64)
- CAP-todo-app.R1 ExternalId default · hidden but not unique · fails (both asserted)
- CAP-todo-app.R1 exactly four defaults · a fifth default added · fails (exact list)
- CAP-todo-app.R2 exactly three rules · a rule for a fourth model · fails
- CAP-todo-app.R2 User self · User owned · fails
- CAP-todo-app.R2 TodoList owned · TodoList self · fails
- CAP-todo-app.R2 Todo ownedVia · read-only condition missing · fails (Some("TodoList","Archived"))
- CAP-todo-app.R3 four Todo overrides · listEdit dropped · fails (count 4 and Contains)
- CAP-todo-app.R3 listSort · Descending · fails
- CAP-todo-app.R3 listColumns · Due and Title swapped · fails (order, because the spec order is not alphabetical)
- CAP-todo-app.R3 TodoList views order · Archived first · fails
- CAP-todo-app.R3 Active/Archived predicates · predicates swapped · fails (needs structural equality of the quoted predicate; see Notes)
- CAP-todo-app.R4 tick · CompletedAt set to a fixed or old time · fails (at 9 is asserted)
- CAP-todo-app.R4 untick · CompletedAt kept · fails (None asserted)
- CAP-todo-app.R4 stays done · CompletedAt reset to now whenever Done is true · fails (now is 12, expected 9)
- CAP-todo-app.R5 overdue · "today" taken in UTC · fails (UTC date is 18, so due 18 would not be overdue)
- CAP-todo-app.R5 done · Done ignored · fails
- CAP-todo-app.R5 due today · test uses `<=` · fails (due 19 is the Auckland today)
- CAP-todo-app.R5 no due date · a missing due date counts as overdue · fails
- CAP-todo-app.R6 files · an extra Util.fs added · fails
- CAP-todo-app.R6 engine checks · an override naming a missing field · fails (errors must be empty)

Follow-ups: none

Notes:
- Every acceptance line has a check at the surface the Interface names. The clock and zone are set through HookContext.
- The Interface lists no error code for this change, so no code check is due.
- The R3 view-predicate check depends on structural equality of OverrideItem. If the item holds a closure and not a quotation, the equality would fail for a correct app. The packet does not show OverrideItem, so I could not confirm this.
- No check waits on the app, so none can hang.
