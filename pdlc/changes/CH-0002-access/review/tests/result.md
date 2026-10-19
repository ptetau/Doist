Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-access.R1 owned gives model TodoList and access Owned · rule returns a wrong model name or `Self` · fails (Assert on "TodoList" and `Owned`)
- CAP-access.R1 ownedVia gives reference List and parent TodoList · parent is taken from the model name, or the reference is empty · fails (exact `OwnedVia("List", "TodoList", None)`)
- CAP-access.R1 self gives access Self · self maps to `Owned` · fails
- CAP-access.R1 system gives access SystemOnly · system maps to `Self` · fails
- CAP-access.R2 readOnlyWhenParent names TodoList and Archived · the condition stores the child model or a fixed field · fails (exact `Some("TodoList","Archived")`)
- CAP-access.R2 on owned fails with its message · the call returns the access unchanged for `Owned` · fails (`Assert.Throws`)
- CAP-access.R2 on self fails with its message · the guard covers only `Owned` · fails
- CAP-access.R2 on system fails with its message · the guard covers only `Owned` and `Self` · fails
- CAP-access.R3 TodoList scope is the owner filter for u1 · `scopeFor` returns `Engine` · fails
- CAP-access.R3 the owner filter follows the user · `scopeFor` hard-codes the user id · fails (u2 case)
- CAP-access.R3 User scope is the self scope for u1 · `self` gives `OwnerFilter` · fails
- CAP-access.R3 Todo with ownedVia scope is the owner filter for u1 · `ownedVia` gives `SelfFilter` or an error · fails
- CAP-access.R3 Audit with system gives NOT_FOUND · `system` gives `Engine` scope or another code · fails (exact `NOT_FOUND`)
- CAP-access.R3 a model with no rule gives NOT_FOUND · an unruled model gets an owner filter (open by default) · fails
- CAP-access.R3 the engine scope is the engine-only scope · `engineScope` has kind `OwnerFilter Guid.Empty` · fails
- CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD · `checkChange` ignores the condition · fails (code and message)
- CAP-access.R4 change to a todo of a live list passes · `checkChange` refuses every change with a parent · fails
- CAP-access.R4 delete of an archived list itself passes · the check applies the condition to the `TodoList` rule · fails only if that rule were refused; `Owned` has no condition, so a plausible wrong version that refuses all `Delete` is caught · fails
- CAP-access.R4 change to a todo whose list does not exist gives NOT_FOUND · a missing parent returns `Ok` · fails
- CAP-access.R4 creation of a todo in an archived list gives READ_ONLY_RECORD · `Create` skips the condition · fails
- CAP-access.R4 creation of a todo in a live list passes · `Create` always refuses · fails
- CAP-access.R4 the reason comes from the parent model and field · the reason is hard-coded "This list is archived" or reads `Archived` · fails (Project/Closed gives "This project is closed")
- CAP-access.R5 todo in a list owned by u1 gets owner u1 · `ownerFor` returns a new Guid · fails
- CAP-access.R5 the owner follows the parent and user · `ownerFor` returns a fixed u1 · fails (u2 case)
- CAP-access.R5 todo in a list owned by u2 for user u1 gives NOT_FOUND · `ownerFor` returns the parent owner with no user check · fails

Follow-ups:
- Add a check that `checkChange` for `Delete` of a todo in an archived list is refused (or passes, as a new rule decides). The spec has no line for it. A new intent could set the rule.
- Add a check that `ownerFor` with no parent gives `NOT_FOUND`. The spec has no line for it.
- Add a compile-time proof that no other module can make a `Scope`. The R3 title asks for a sealed type, but no acceptance line states it.

Notes:
- Every acceptance line of R1 to R5 has at least one check. The names carry the requirement ID and each module is named with the ID.
- No line depends on time, randomness, or a vendor. The Interface lists no seams, and the checks use none.
- The two error codes in the Interface, `NOT_FOUND` and `READ_ONLY_RECORD`, each have a check with the exact code.
- The delete line (R4, third) uses a `TodoList` with no parent, so it proves that `Delete` of an `Owned` model passes. It does not prove the archived state of the record. The line is met as written.
