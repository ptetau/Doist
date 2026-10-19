Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs:962-990 · The check "CAP-todo-app.R3 the overrides of Todo are the four of the spec" does not tell `readOnly CompletedAt` from `listEdit Done`. It only checks that the two items have fields [CompletedAt; Done] in that order and that their kinds differ. A wrong version with `listEdit CompletedAt` and `readOnly Done` passes. No other R3 check covers these two items. · CAP-todo-app.R3 line 1 (the overrides of `Todo` are `readOnly CompletedAt`, `listEdit Done`, ...), and the remit rule that each check fails on a plausible wrong version.

Wrong versions:
- R1 models are User, TodoList, Todo · models list in order TodoList, User, Todo · fails (list equality)
- R1 User fields · Subject typed Text 200 instead of 255 · fails (Text 255 assert)
- R1 TodoList fields · Archived missing · fails (field list)
- R1 Todo fields · Notes not optional · fails (Optional(Text 5000))
- R1 Title default · Title is text 100 · fails (Text 200)
- R1 Notes default · Notes is text 500 · fails (Text 5000)
- R1 TimeZoneId default · text 32 · fails (Text 64)
- R1 ExternalId default · hidden but not unique · fails (Assert.True(Unique))
- R1 exactly four defaults · an extra default for Priority · fails (sorted clr names)
- R2 exactly three rules · a fourth rule added · fails
- R2 User self · User is owned · fails
- R2 TodoList owned · TodoList is ownedVia · fails
- R2 Todo rule · no read-only condition on Archived · fails (OwnedVia tuple with Some)
- R3 Todo overrides four items · readOnly and listEdit swapped between CompletedAt and Done · passes (only the field order and "kinds differ" are checked)
- R3 listSort · sort Due Descending · fails (Ascending)
- R3 listColumns · Priority and Due swapped · fails (list order)
- R3 TodoList views · Active and Archived in the wrong order · fails (name order)
- R3 view predicates · both views are `not Archived` · fails (DoesNotContain "Not", NotEqual text)
- R4 tick · hook uses the real clock instead of ctx.Now · fails (09:00 asserted against 2026)
- R4 untick · CompletedAt kept when Done is false · fails (Assert.Equal None)
- R4 stays done · hook sets CompletedAt to now on every save with Done = true · fails (context is at 12:00, expected 09:00)
- R5 overdue · today taken in UTC · fails (due 10-18 on UTC today would not be overdue)
- R5 done · Done ignored · fails
- R5 due on the Auckland today · comparison is `<=` · fails (due 10-19 expected not overdue)
- R5 no due date · no due date counts as overdue · fails
- R6 source files · an extra `Helpers.fs` in src/Todo.App · fails (file list)
- R6 engine checks · an access rule missing · fails (errors not empty)

Follow-ups: none

Notes:
- All checks set the clock and time zone through `HookContext`, which is the named seam. No check uses the real clock.
- The Interface lists no error codes for this change, so no code check is needed.
- The R3 view checks read `sprintf "%A"` of a quotation. That output depends on how F# prints the quotation. It works for `not l.Archived` but is brittle.
- I could not see the app code, so I could not check the exact field lists of the models against the spec.
