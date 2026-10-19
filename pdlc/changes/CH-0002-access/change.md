# CH-0002 · Access

Intent: IN-0001 · Scope: CAP-access · Status: in review · Ticket: none · Branch: pdlc/CH-0002-access · Base: pdlc/CH-0001-models
Tests-Locked: a042dce

## Delivers

Each model declares one access rule. The engine builds the sealed scope for a user and decides the owner of a child. It refuses a change to a read-only record with its reason.

## Requirements

### CAP-access.R1 · Each model declares one of four rules

- Given `rule<TodoList> owned`, when a caller reads the rule, then the model name is
  "TodoList" and the access is `Owned`.
- Given `rule<Todo> (ownedVia <@ fun t -> t.List @>)`, when a caller reads the rule, then
  the access is `OwnedVia` with the reference field "List" and the parent "TodoList".
- Given `rule<User> self`, when a caller reads the rule, then the access is `Self`.
- Given `rule<Audit> system`, when a caller reads the rule, then the access is
  `SystemOnly`.

### CAP-access.R2 · A read-only condition names a bool field of the parent

- Given `ownedVia <@ fun t -> t.List @> |> readOnlyWhenParent <@ fun l -> l.Archived @>`,
  when a caller reads the rule, then the read-only condition names the model "TodoList"
  and the field "Archived".
- Given `owned |> readOnlyWhenParent <@ fun l -> l.Archived @>`, when a caller reads the
  rule, then the call fails with the message "readOnlyWhenParent only applies to
  ownedVia".

### CAP-access.R3 · Only this capability can make a scope

- Given a user with the id `u1`, when the engine makes the scope for `TodoList`, then the
  scope is the owner filter for `u1`.
- Given the model `User` with the rule `self`. When the engine makes the scope for a user
  `u1`, then the scope is the self scope for `u1`. It matches only the record whose `Id` is
  `u1`. (assumed)
- Given the model `Todo` with its `ownedVia` rule. When the engine makes the scope for `u1`,
  then the scope is the owner filter for `u1`, because the owner is copied to each todo.
- Given the model `Audit` with the rule `system`, when the engine makes a user scope, then
  the result is the error `NOT_FOUND`.
- Given the engine itself, when the engine makes the build scope, then the scope is the
  engine-only scope.

### CAP-access.R4 · A change to a read-only record is refused with its reason

- Given the list "Old" with `Archived = true`. When the engine checks a change to a todo of
  that list, then the result is the error `READ_ONLY_RECORD`. The reason is "This list is
  archived".
- Given the list "Groceries" with `Archived = false`, when the engine checks a change to a
  todo of that list, then the check passes.
- Given the list "Old" with `Archived = true`, when the engine checks a delete of the list
  itself, then the check passes.
- Given a todo whose list does not exist. When the engine checks a change to the todo,
  then the result is the error `NOT_FOUND`.
- Given the list "Old" with `Archived = true`. When the engine checks the creation of a
  todo in that list, then the result is the error `READ_ONLY_RECORD`. The reason is "This
  list is archived". (assumed)

### CAP-access.R5 · The owner of a child is copied from its parent

- Given a user `u1` and a list owned by `u1`. When the engine decides the owner of a new
  todo in that list, then the owner is `u1`.
- Given a user `u1` and a list owned by `u2`. When the engine decides the owner of a new
  todo in that list, then the result is the error `NOT_FOUND`.

## Relies on

- CAP-models (used, not changed)
- DES-names
- DES-errors
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

In `src/Typed.Engine/Access.fs`, module `Typed.Engine.Access`:

- `type Access = Owned | OwnedVia of reference: string * parent: string * readOnlyWhen: (string * string) option | Self | SystemOnly`
- `type AccessRule = { ModelName: string; Access: Access }`
- `owned: Access`, `self: Access`, `system: Access`
- `ownedVia: Expr<'T -> Ref<'P>> -> Access`
- `readOnlyWhenParent: Expr<'P -> bool> -> Access -> Access`: raises `ArgumentException` with the message "readOnlyWhenParent only applies to ownedVia" for any other access.
- `rule<'T>: Access -> AccessRule`
- `type ScopeKind = OwnerFilter of Guid | SelfFilter of Guid | Engine`
- `type Scope`: a sealed type with a private constructor and the member `Kind: ScopeKind`. Only this module makes one. (assumed)
- `engineScope: Scope`
- `scopeFor: AccessRule list -> user: Guid -> model: string -> Result<Scope, Finding>`
  - errors: `NOT_FOUND` (a `system` model, or a model with no rule)
- `type Action = Create | Change | Delete`
- `checkChange: AccessRule list -> Action -> model: string -> parent: Record option -> Result<unit, Finding>`
  - errors: `READ_ONLY_RECORD` with the reason in `Message`, `NOT_FOUND` (the parent is missing for an `ownedVia` model)
  - The reason is "This <parent word> is <field>". The parent word is the last word of the parent model name, in lower case, such as "list" from `TodoList`. (assumed)
- `ownerFor: AccessRule list -> user: Guid -> model: string -> parent: Record option -> Result<Guid, Finding>`
  - errors: `NOT_FOUND` (the parent belongs to another user)

`Record` comes from `Typed.Engine.Model` (CAP-models, the model plan), as do `Finding`,
`ErrorCode`, and `Ref`. A check builds one directly:

```fsharp
type Record = { Model: string; Id: Guid; Owner: Guid option; Version: int64; Values: Map<string, obj> }

let archivedList (owner: Guid) : Record =
    { Model = "TodoList"; Id = Guid.NewGuid(); Owner = Some owner; Version = 1L
      Values = Map [ "Name", box "Old"; "Archived", box true ] }
```

`checkChange` reads the read-only field of the parent from `Values`, by the field name that
the rule gives, and expects a boxed `bool`. `ownerFor` reads the parent's `Owner`.

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

none

## Files

- src/Typed.Engine/Access.fs
- src/Typed.Engine/Typed.Engine.fsproj: the Compile entry of this spec

## Check files

- tests/Typed.Engine.Tests/CAP-access.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

## Steps

### Step 1 · Each model declares one of four rules · [x]

- Requirement: CAP-access.R1
- Check: the checks named `CAP-access.R1 ...` in `tests/Typed.Engine.Tests/CAP-access.Tests.fs`
- Done when: every check of CAP-access.R1 (each model declares one of four rules) passes

### Step 2 · A read-only condition names a bool field of the parent · [x]

- Requirement: CAP-access.R2
- Check: the checks named `CAP-access.R2 ...` in `tests/Typed.Engine.Tests/CAP-access.Tests.fs`
- Done when: every check of CAP-access.R2 (a read-only condition names a bool field of the parent) passes

### Step 3 · Only this capability can make a scope · [x]

- Requirement: CAP-access.R3
- Check: the checks named `CAP-access.R3 ...` in `tests/Typed.Engine.Tests/CAP-access.Tests.fs`
- Done when: every check of CAP-access.R3 (only this capability can make a scope) passes

### Step 4 · A change to a read-only record is refused with its reason · [x]

- Requirement: CAP-access.R4
- Check: the checks named `CAP-access.R4 ...` in `tests/Typed.Engine.Tests/CAP-access.Tests.fs`
- Done when: every check of CAP-access.R4 (a change to a read-only record is refused with its reason) passes

### Step 5 · The owner of a child is copied from its parent · [x]

- Requirement: CAP-access.R5
- Check: the checks named `CAP-access.R5 ...` in `tests/Typed.Engine.Tests/CAP-access.Tests.fs`
- Done when: every check of CAP-access.R5 (the owner of a child is copied from its parent) passes

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `CAP-access` as the ready stage left them.
- 2026-10-19T05:58:38+00:00 · Tests: the test writer wrote 24 checks in `CAP-access.Tests.fs`. The Interface
  gained the `Record` entry that CAP-access.R4 (read-only refusal) and R5 (owner from
  parent) need. Red: the checks fail to compile, because
  `Typed.Engine.Access` does not exist yet. Locked at da2df15.
- 2026-10-19T06:01:24+00:00 · Tests review: the first review failed on the packet, not the checks. The packet
  script diffs exact paths, and the project file entry had a suffix. With a bare path the
  second review passed with 0 problems. The reviewer listed three extra checks for
  later. They are a second parent model in the reason text, a delete of a todo in an
  archived list, and `ownerFor` with no parent.
- 2026-10-19T06:03:30+00:00 · Build: the builder wrote `Access.fs`. A first version had a public `Scope.Make`,
  which broke CAP-access.R3 (sealed scope). The builder removed it. `Scope` is a record
  with private fields. All 54 checks pass. The check files did not change after the lock.
  Commit `25a51a9`.
- 2026-10-19T06:07:18+00:00 · Verify round 1: 54 of 54 checks pass, one group for each requirement. The
  check files did not change after the lock. Specification, security, quality, and
  privacy passed with 0 problems. Compliance does not apply. Filed IN-0007 (checkChange
  fails closed) from the security follow-ups.
- 2026-10-19T06:08:35+00:00 · Gate correction: the gate read a wrong version in the tests review that still
  passed, a hard-coded reason. The test writer added a check with a second parent model
  and field. It passes, because the code derives the reason. Locked again at a042dce.
