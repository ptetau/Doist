Result: pass

Remit: tests

Problems: none

Wrong versions:
- CAP-access.R1 owned gives model TodoList and access Owned · rule returns the wrong model name or Self for owned · fails (the ModelName and Access asserts)
- CAP-access.R1 ownedVia gives reference List and parent TodoList · parent or reference left empty or swapped · fails (the exact `OwnedVia("List", "TodoList", None)` value)
- CAP-access.R1 self gives access Self · self returns Owned · fails
- CAP-access.R1 system gives access SystemOnly · system returns Self · fails
- CAP-access.R2 readOnlyWhenParent names TodoList and Archived · the condition keeps the child model name or drops the field · fails (the `Some("TodoList", "Archived")` value)
- CAP-access.R2 readOnlyWhenParent on owned fails with its message · only throws for some accesses, or a different message · fails (exact message)
- CAP-access.R2 readOnlyWhenParent on self fails with its message · self passes through unchanged · fails
- CAP-access.R2 readOnlyWhenParent on system fails with its message · system passes through unchanged · fails
- CAP-access.R3 TodoList scope is the owner filter for u1 · scope is SelfFilter or Engine · fails
- CAP-access.R3 the owner filter follows the user · the user id is hard-coded or taken from the first call · fails (u2 case)
- CAP-access.R3 User scope is the self scope for u1 · self gives OwnerFilter · fails
- CAP-access.R3 Todo with ownedVia scope is the owner filter for u1 · ownedVia gives SelfFilter or an error · fails
- CAP-access.R3 Audit with system gives NOT_FOUND · system gives Engine scope or another code · fails (exact code)
- CAP-access.R3 a model with no rule gives NOT_FOUND · a missing rule gives the owner filter (allow by default) · fails (Unruled)
- CAP-access.R3 the engine scope is the engine-only scope · engineScope has kind OwnerFilter · fails
- CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD · the read-only check is skipped for Change · fails (code and message)
- CAP-access.R4 change to a todo of a live list passes · the check refuses every change · fails
- CAP-access.R4 delete of an archived list itself passes · Delete of a model with no parent returns an error · fails
- CAP-access.R4 change to a todo whose list does not exist gives NOT_FOUND · a missing parent passes · fails
- CAP-access.R4 creation of a todo in an archived list gives READ_ONLY_RECORD · the check skips Create · fails
- CAP-access.R4 creation of a todo in a live list passes · Create always refused · fails
- CAP-access.R5 todo in a list owned by u1 gets owner u1 · the owner is a new Guid or None · fails
- CAP-access.R5 the owner follows the parent and user · the owner is hard-coded to u1 · fails (u2 case)
- CAP-access.R5 todo in a list owned by u2 for user u1 gives NOT_FOUND · the parent owner is not compared to the user · fails
- CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD · the code hard-codes "This list is archived" instead of deriving it from the parent model and field · passes (only one parent model and field exists; the derivation rule is marked assumed, so this is not a problem)

Follow-ups:
- Add a check that gives the reason for a second parent model and field, such as a "Project" with "Closed". It would prove that the reason comes from the parent word and the field name, not from a fixed string.
- Add a check for a Delete of a todo in an archived list. The spec does not say whether it passes.
- Add a check for `ownerFor` with no parent (NOT_FOUND). The spec lists no acceptance line for it.

Notes:
- The change has no clock, randomness, or vendor, so "Test seams: none" holds. No check can hang, since each is a pure call.
- The private constructor of `Scope` is a compile-time rule. A runtime check cannot prove it, and no acceptance line asks for one.
- Each check name starts with its requirement ID, and each module carries the ID with a hyphen. Both follow DES-pdlc-check-tagging.
- The fixtures `Todo`, `TodoList`, and `User` come from `ModelsFixtures` of CH-0001. They are not in the packet, so I could not see that `TodoList` has `Archived`.
