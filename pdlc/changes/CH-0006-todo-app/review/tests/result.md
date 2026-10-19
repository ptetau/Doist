Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/CAP-todo-app.Tests.fs:1081 · "CAP-todo-app.R4 ticking a todo sets CompletedAt to now" saves a todo with `CompletedAt = None`, so a hook that ignores `previous` passes all three R4 checks. The spec rule is "if `Done` changed to true, `CompletedAt` is now". A saved todo that carries a stale `CompletedAt` while `previous.Done = false` is never tested. Give the saved todo a stale `CompletedAt` (for example 2026-10-17T09:00:00Z) and expect now · CAP-todo-app.R4 line 1 and the "Wrong versions" rule.

Wrong versions:
- CAP-todo-app.R1 the models are User then TodoList then Todo · models listed as Todo, TodoList, User · fails (the list order)
- CAP-todo-app.R1 User has the fields Subject, DisplayName, and TimeZone · Subject typed as Title (text 200) · fails (Text 255 assertion)
- CAP-todo-app.R1 TodoList has the fields Name and Archived · Archived typed as optional bool · fails (Boolean assertion)
- CAP-todo-app.R1 Todo has its seven fields with their kinds · Notes not optional · fails (Optional(Text 5000))
- CAP-todo-app.R1 Title is text 200, not hidden, not unique · Title text 201 · fails (Text 200)
- CAP-todo-app.R1 Notes is text 5000, not hidden, not unique · Notes text 4000 · fails (Text 5000)
- CAP-todo-app.R1 TimeZoneId is text 64, not hidden, not unique · TimeZoneId text 100 · fails (Text 64)
- CAP-todo-app.R1 ExternalId is text 255, unique, and hidden · ExternalId not hidden · fails (Assert.True Hidden)
- CAP-todo-app.R1 the type defaults hold exactly the four types with a default · an extra default for Priority · fails (the sorted name list)
- CAP-todo-app.R2 there are exactly three access rules · a duplicate rule for Todo · fails (the sorted model names)
- CAP-todo-app.R2 User is self · User is owned · fails
- CAP-todo-app.R2 TodoList is owned · TodoList is system · fails
- CAP-todo-app.R2 Todo is ownedVia List and read-only when the list is Archived · no read-only condition · fails (Some("TodoList","Archived") value)
- CAP-todo-app.R3 the overrides of Todo are the four of the spec · listEdit on CompletedAt and readOnly on Done swapped · fails (the ReadOnly and ListEdit field lists)
- CAP-todo-app.R3 listSort of Todo is Due ascending · Due descending · fails (Ascending assertion)
- CAP-todo-app.R3 listColumns of Todo are Title, Due, Priority, Done in that order · Done before Priority · fails (list order)
- CAP-todo-app.R3 the overrides of TodoList are the views Active then Archived · views in the order Archived, Active · fails (name list)
- CAP-todo-app.R3 the Active view is not Archived and the Archived view is Archived · Active and Archived predicates swapped · fails ("Not" in the Archived text)
- CAP-todo-app.R4 ticking a todo sets CompletedAt to now · hook ignores `previous` and keeps a stale saved `CompletedAt` · passes (the saved todo has `CompletedAt = None`)
- CAP-todo-app.R4 unticking a todo empties CompletedAt · hook leaves CompletedAt on untick · fails (saved todo carries the old time)
- CAP-todo-app.R4 saving a todo that stays done keeps the first CompletedAt · hook sets now whenever Done is true · fails (at 12 versus at 9)
- CAP-todo-app.R5 an open todo due on the Auckland yesterday is overdue · hook compares to the UTC date · fails (UTC today is 10-18, so due 10-18 is not overdue)
- CAP-todo-app.R5 a done todo due on the Auckland yesterday is not overdue · hook ignores Done · fails
- CAP-todo-app.R5 an open todo due on the Auckland today is not overdue · hook uses `<=` · fails (due 10-19 equals today)
- CAP-todo-app.R5 an open todo with no due date is not overdue · hook treats no due date as overdue · fails
- CAP-todo-app.R6 the source files are Model, Defaults, Schema, and Hooks · an extra `Helpers.fs` · fails (the file list)
- CAP-todo-app.R6 the engine checks find no error in the models, rules, and overrides · a model with no access rule · fails (non-empty errors)

Follow-ups: none

Notes:
- The R3 view check reads `sprintf "%A"` of the quotation and looks for "Not". It depends on how the quotation prints. A correct `l.Archived = false` view would fail it. The line says "(not `Archived`)", so this is acceptable.
- The check "the type defaults hold exactly the four types" goes past the acceptance line. A correct app that adds a fifth default would fail it.
- The clock is set through `HookContext.Now` and `HookContext.TimeZone` in every R4 and R5 check. No check uses the real clock. No check waits on anything.
- The engine items used (`Hidden`, `Unique`, `errors`, `fieldsOf`) are not in the packet. I could not verify that they exist.
