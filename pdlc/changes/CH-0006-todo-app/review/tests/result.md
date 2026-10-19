Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-todo-app.R1 the models are User then TodoList then Todo · Defaults.models in another order · fails (list order assert)
- CAP-todo-app.R1 User has the fields Subject, DisplayName, and TimeZone · a field missing or a wrong length · fails (field list and Text 255/200/64)
- CAP-todo-app.R1 TodoList has the fields Name and Archived · Name typed as plain string · fails (Text 200 assert)
- CAP-todo-app.R1 Todo has its seven fields with their kinds · Notes not optional, or Priority cases reordered · fails (exact kinds)
- CAP-todo-app.R1 Title is text 200 · Title default text 100 · fails
- CAP-todo-app.R1 Notes is text 5000 · Notes default text 500 · fails
- CAP-todo-app.R1 TimeZoneId is text 64 · text 32 · fails
- CAP-todo-app.R1 ExternalId is text 255, unique, and hidden · ExternalId not hidden, or not unique · fails (both flags asserted True)
- CAP-todo-app.R1 the type defaults hold exactly the four types · an extra default type · fails
- CAP-todo-app.R2 there are exactly three access rules · a fourth rule · fails
- CAP-todo-app.R2 User is self · User owned · fails
- CAP-todo-app.R2 TodoList is owned · TodoList self · fails
- CAP-todo-app.R2 Todo is ownedVia List and read-only when Archived · no readOnlyWhenParent · fails (Some(...) in the tuple)
- CAP-todo-app.R3 the overrides of Todo are the four of the spec · listEdit on another field, or sort Descending · fails (matched by case, count 4)
- CAP-todo-app.R3 listSort of Todo is Due ascending · Descending · fails
- CAP-todo-app.R3 listColumns of Todo are in that order · Done before Priority · fails
- CAP-todo-app.R3 the overrides of TodoList are the views Active then Archived · views in swapped order · fails (name list)
- CAP-todo-app.R3 the Active view is not Archived ... · Active view without `not` · fails (Contains "Not" on Active, DoesNotContain on Archived)
- CAP-todo-app.R4 ticking a todo sets CompletedAt to now · hook keeps the saved value · fails (stale 17 Oct value in the saved todo)
- CAP-todo-app.R4 unticking a todo empties CompletedAt · hook never clears · fails (saved todo carries the old time)
- CAP-todo-app.R4 saving a todo that stays done keeps the first CompletedAt · hook sets now whenever Done is true · fails (clock 12:00 against 09:00)
- CAP-todo-app.R5 an open todo due on the Auckland yesterday is overdue · today taken in UTC · fails (UTC today is 18 Oct, so not before)
- CAP-todo-app.R5 a done todo due on the Auckland yesterday is not overdue · Done ignored · fails
- CAP-todo-app.R5 an open todo due on the Auckland today is not overdue · comparison `<=` instead of `<` · fails (due 19 Oct equals Auckland today)
- CAP-todo-app.R5 an open todo with no due date is not overdue · a missing due date treated as overdue · fails
- CAP-todo-app.R6 the source files are Model, Defaults, Schema, and Hooks · an extra Helpers.fs · fails
- CAP-todo-app.R6 the engine checks find no error · an access rule missing · fails (MISSING_ACCESS_RULE is an error)

Follow-ups: none

Notes:
- The "fields of the spec" in R1 (User: Subject, DisplayName, TimeZone; TodoList: Name, Archived) come from the app spec. That spec is not in the packet. I could not confirm those names. The Todo fields match the seven fields in CAP-models.
- The R3 view check reads the `%A` text of a quotation and looks for "Not". This ties the check to how the quotation prints. It is acceptable here, because the other checks pin the structure.
- The R6 file filter skips any path that starts with "obj" or "bin", also a source file named like `obj_x.fs`. This is a small gap.
- No error code is listed in the Interface for this change, so no code check is needed. The clock is set only through HookContext, as the Interface allows. No check waits on anything.
