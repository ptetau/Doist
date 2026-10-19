Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-checks.R1 a model with no access rule is an error · the code reports the error only when rules are given for other models · fails (empty rule list expects the exact text)
- CAP-checks.R1 a model with two access rules is an error · the code treats "two or more" as the no-rule message · fails (exact message "more than one")
- CAP-checks.R1 a model with exactly one access rule has no such finding · the code requires at least two rules · fails
- CAP-checks.R1 the three models with the three rules have no such finding · the code ignores the User rule and flags it · fails
- CAP-checks.R2 an override set for a model that is not in the list · the code skips unknown models silently · fails
- CAP-checks.R2 a Label item for a field that does not exist · the code checks only the model name · fails
- CAP-checks.R2 a Label item for a field that exists · the code flags every Label item · fails
- CAP-checks.R3 a rule predicate of type string on Due · the code never compares types · fails
- CAP-checks.R3 the message names the expected type DateOnly before the predicate type string · the code swaps the two types in the message · fails (index order)
- CAP-checks.R3 a rule predicate of type Title on Title · the code flags every Rule item · fails
- CAP-checks.R4 a Column item for Version · the code allows Column on any field · fails
- CAP-checks.R4 a Column item for an ordinary field · the code flags every Column item · fails
- CAP-checks.R5 a reference to a model that is not in the list · the code never checks reference targets · fails
- CAP-checks.R5 a reference to a model in the list · the code flags every Ref field · fails
- CAP-checks.R6 a referenced model with no title field · the code skips models without an override · fails
- CAP-checks.R6 a titleField override removes the finding · the code ignores the titleField override · fails
- CAP-checks.R7 an ownedVia rule on a field that is not a reference · the code skips the reference check · fails (Title is text)
- CAP-checks.R7 a read-only condition on a field that is not bool · the code skips the bool check · fails (Name is text)
- CAP-checks.R7 a read-only condition on a bool field · the code flags every read-only condition · fails
- CAP-checks.R8 an unsafe override is one warning · the code gives an error severity, or leaves out the reason · fails
- CAP-checks.R8 the reason of each unsafe override is its own · the code reuses the first reason, or drops the second warning · fails
- CAP-checks.R9 a label equal to the default is a warning · the code never compares to the default · fails
- CAP-checks.R9 a camel-case default · the code compares to the raw field name "CompletedAt" · fails
- CAP-checks.R9 lower case due · the code compares ignoring case · fails
- CAP-checks.R9 upper case DUE · the code compares ignoring case · fails
- CAP-checks.R9 a label that differs · the code flags every label · fails
- CAP-checks.R10 the todo app has no error findings · the code flags a valid listEdit, listColumns, or listView · fails
- CAP-checks.R11 two label overrides on one field · the code never counts duplicates · fails
- CAP-checks.R11 one label and one readOnly · the code flags any two items on one field · fails
- CAP-checks.R11 one label on each of two fields · the code flags any two labels in one set · fails
- CAP-checks.R12 TodoList then Todo · the code sorts findings by name or by code · fails (the Todo-first test catches a sort by name; the TodoList-first test catches reverse order)
- CAP-checks.R12 Todo then TodoList · the code sorts findings by name · fails
- CAP-checks.R12 findings of one model follow field order · the code orders by override item order · fails (Done is first in the set, Title is first in the record)
- CAP-checks.R12 no models and no rules · the code adds a stray finding on empty input · fails
- CAP-checks.R13 a sort by wide text · the code allows sort on optional text · fails
- CAP-checks.R13 401 characters · the code uses a limit of 401 or more · fails
- CAP-checks.R13 400 characters · the code uses a limit of 400 or less · fails
- CAP-checks.R13 a sort by Title · the code refuses every text sort · fails
- CAP-checks.R13 a view on a computed length · the code accepts any predicate · fails
- CAP-checks.R13 a view on an equality · the code refuses equality, or refuses everything but bool fields · fails

Follow-ups:
- CAP-checks.R4 names only `Version`. A new intent could add checks for `Id`, `Owner`, `CreatedAt`, and `UpdatedAt`. The checks would catch a code version that guards only one system field.
- CAP-checks.R7 has no check for an `ownedVia` field that is a reference to a different model than the named parent. A new intent could add the line and the check.

Notes:
- R7 builds the bad rules from the record directly, because the typed `ownedVia` and `readOnlyWhenParent` calls refuse the bad selectors at compile time. This reaches the same `run` surface.
- R13 uses `match t.Title with Title s -> s.Length` in place of `t.Title.Length`, because `Title` has no `Length` member. The check keeps the intent of the line.
- The R3 order check pins "expected type before field type" in the message. The acceptance line lists the order "Todo.Due, the expected type, and the type of the field", so this fits.
- No check uses a clock, randomness, or a vendor. The Interface lists no seams. No check waits on anything.
