Result: fail

Remit: tests

Problems:
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj:n/a · The diff against the base shows only `CAP-access.Tests.fs`. The `Compile` entry for the new check file is not in the packet, so the checks may never build or run · "Check files" entry of the change spec (the fsproj Compile entry of this spec)

Wrong versions:
- CAP-access.R1 owned gives model TodoList and access Owned · `owned` is mapped to `Self` · fails (Assert.Equal(Owned, r.Access))
- CAP-access.R1 ownedVia gives reference List and parent TodoList · the parent is read from the wrong type argument, or the reference is "Id" · fails (OwnedVia("List","TodoList",None))
- CAP-access.R1 self gives access Self · `self` returns `Owned` · fails
- CAP-access.R1 system gives access SystemOnly · `system` returns `Self` · fails
- CAP-access.R2 readOnlyWhenParent names TodoList and Archived · the tuple is stored as (field, model) · fails (Some("TodoList","Archived"))
- CAP-access.R2 readOnlyWhenParent on owned fails with its message · `owned` is returned unchanged with no throw · fails (Assert.Throws)
- CAP-access.R2 readOnlyWhenParent on self fails with its message · the guard covers only `Owned` · fails
- CAP-access.R2 readOnlyWhenParent on system fails with its message · the guard covers only `Owned` and `Self` · fails
- CAP-access.R3 TodoList scope is the owner filter for u1 · `scopeFor` returns `Engine` · fails
- CAP-access.R3 the owner filter follows the user · `scopeFor` hard-codes one user id · fails (u2 case)
- CAP-access.R3 User scope is the self scope for u1 · `self` gives `OwnerFilter` · fails
- CAP-access.R3 Todo with ownedVia scope is the owner filter for u1 · `ownedVia` gives `SelfFilter` or NOT_FOUND · fails
- CAP-access.R3 Audit with system gives NOT_FOUND · a `system` model gets `Engine` scope · fails
- CAP-access.R3 a model with no rule gives NOT_FOUND · a missing rule gives a default owner filter · fails
- CAP-access.R3 the engine scope is the engine-only scope · `engineScope` is an `OwnerFilter` · fails
- CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD · the check ignores the parent value · fails
- CAP-access.R4 change to a todo of a live list passes · the check always refuses a Todo change · fails
- CAP-access.R4 delete of an archived list itself passes · `checkChange` demands a parent for every model · fails (the None parent gives NOT_FOUND). A version that reads the list's own `Archived` cannot be caught here, because the check passes no archived list. The Interface has no way to pass one.
- CAP-access.R4 change to a todo whose list does not exist gives NOT_FOUND · a missing parent is treated as Ok · fails
- CAP-access.R4 creation of a todo in an archived list gives READ_ONLY_RECORD · `Create` skips the read-only check · fails
- CAP-access.R4 creation of a todo in a live list passes · `Create` always refuses · fails
- CAP-access.R5 todo in a list owned by u1 gets owner u1 · `ownerFor` returns a new Guid · fails
- CAP-access.R5 the owner follows the parent and user · `ownerFor` hard-codes u1 · fails (u2 case)
- CAP-access.R5 todo in a list owned by u2 for user u1 gives NOT_FOUND · `ownerFor` returns the parent's owner with no user comparison · fails

Follow-ups:
- Add a self-scope check with a second user (`u2`). It would catch a version that hard-codes the id in `SelfFilter`.
- Add a check of the reason text for a parent model other than `TodoList`. The "parent word" rule is then proved, not just "list is archived".
- The private constructor of `Scope` (R3 text) has no check. A compile-fail check could be added in a new intent.

Notes:
- Each Interface error code (`NOT_FOUND`, `READ_ONLY_RECORD`) has a check that expects that exact code.
- No test seam is needed or used. No line depends on time, randomness, or a vendor.
- Every check name starts with its requirement ID, and each requirement has its own module.
- The checks rely on `Typed.Engine.Tests.ModelsFixtures` (`Todo`, `TodoList`, `User`) from CH-0001. That file is not in the packet.
