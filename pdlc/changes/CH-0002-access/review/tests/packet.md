# Review packet · CH-0002-access · tests

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: tests

Default: yes

You judge the checks, not the code. You do not see the code.

- Each acceptance line in the change spec has at least one check. The exception is a
  requirement marked `(retire)`: its acceptance lines get no check, and each check that
  only proved them is gone.
- The name of each check contains its requirement ID.
- Each check tests what the acceptance line says, through the "Interface" in the change
  spec. The check does not depend on the internal structure of the code.
- A check proves each line at the surface that the line names. A line where the person
  clicks needs a check that clicks and reads what shows. A check of a helper below that
  surface is not sufficient alone.
- If a line depends on the current date or time, its check sets the clock through a seam
  at that same surface. A check that runs a command sets the clock with the environment
  variable or flag of the command. Each of these is a problem:
  - Using the real clock, or setting the time only on a helper below the surface.
  - Leaving a line to manual checks when a stand-in seam could reach the surface.
- Each check can fail. It fails on a plausible wrong version of the code (see "Wrong
  versions"). A check that passes, whatever the code does, does not count.
- A correct app passes each check. A fake that stands in for something real must use the
  names and the behaviour of the real thing. Then the check does not prove only the fake.
- No check can hang. Anything that waits for the app has a time limit.
- Each error code that the Interface lists has a check that expects that exact code.
- The checks control time, randomness, and vendors only through the "Test seams" of the
  Interface.
- The checks are sufficiently clear that a failure tells you what broke.

## Wrong versions

For each check, name one plausible wrong version of the code. Decide whether the check
fails on that version. Write one line for each check under "Wrong versions" in your
result: the check, the wrong version, and `fails` or `passes`. A check that passes a
plausible wrong version is a problem. Name the value or line that catches the wrong
version.

Look for these traps:

- Examples that also fit a wrong rule. These are common cases:
  - A rule about order, tested with items that are already in that order or in
    alphabetical order.
  - Values that come out the same if the code rounds up, rounds down, or rounds to the
    nearest value.
  - Text that is already in the case that the rule asks for.
- A limit that the check does not test exactly at the edge and one past the edge. For "at
  most 20", the value 20 passes and the value 21 fails. Values near the edge prove nothing
  about the edge.
- A rule about every member of a class that the check tests without a non-member.
  "Any other value is an error" needs a value close to an allowed value, not only a few
  far-off values. A close value differs by its case, a space, or a prefix. "Only the
  owner may" needs a person who is not the owner.

## The change spec

````
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
````

## The spec it covers: title and summary

````
# CAP-access · Decides who can see and change each record

Status: active

## What it is for

This capability holds the access rule of each model and builds the scope that every query
takes. The four rules are `owned`, `ownedVia`, `self`, and `system`. A rule can carry a
read-only condition on the parent. Access is denied by default. A record outside the
scope of a caller is not found.

## Code map

- src/Typed.Engine/Access.fs
````

## The requirements, in full

### CAP-access.R1 · Each model declares one of four rules

Status: verified · Intents: IN-0001

- Given `rule<TodoList> owned`, when a caller reads the rule, then the model name is
  "TodoList" and the access is `Owned`.
- Given `rule<Todo> (ownedVia <@ fun t -> t.List @>)`, when a caller reads the rule, then
  the access is `OwnedVia` with the reference field "List" and the parent "TodoList".
- Given `rule<User> self`, when a caller reads the rule, then the access is `Self`.
- Given `rule<Audit> system`, when a caller reads the rule, then the access is
  `SystemOnly`.

### CAP-access.R2 · A read-only condition names a bool field of the parent

Status: verified · Intents: IN-0001

- Given `ownedVia <@ fun t -> t.List @> |> readOnlyWhenParent <@ fun l -> l.Archived @>`,
  when a caller reads the rule, then the read-only condition names the model "TodoList"
  and the field "Archived".
- Given `owned |> readOnlyWhenParent <@ fun l -> l.Archived @>`, when a caller reads the
  rule, then the call fails with the message "readOnlyWhenParent only applies to
  ownedVia".

### CAP-access.R3 · Only this capability can make a scope

Status: verified · Intents: IN-0001

A scope is an owner filter, a self scope, or the engine-only scope. The scope type has a private
constructor. Other code cannot make one.

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

Status: verified · Intents: IN-0001

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

Status: verified · Intents: IN-0001

- Given a user `u1` and a list owned by `u1`. When the engine decides the owner of a new
  todo in that list, then the owner is `u1`.
- Given a user `u1` and a list owned by `u2`. When the engine decides the owner of a new
  todo in that list, then the result is the error `NOT_FOUND`.

## Specs it relies on

### CAP-models

````
# CAP-models · Describes a model from its F# record type

Status: active

## What it is for

This capability reads an F# record type and gives each engine a plan of the model. The
plan holds each field, its kind, its semantic type, its label, and the system fields. Each
engine takes its behaviour from this plan. The plan also holds the typed overrides and the
override catalogue.

## Code map

- src/Typed.Engine/Model.fs
- src/Typed.Engine/Overrides.fs

## Requirements

### CAP-models.R1 · Each field gets a kind from its type

Status: verified · Intents: IN-0001

The kinds are text, whole number, decimal, boolean, enum, date, timestamp, id, reference,
and optional. A field gets its kind from its type. A semantic type gets its kind from a
default in a list of type defaults. A plain union with no fields is an enum.

- Given the type defaults of the todo app, when the engine describes `Todo`, then `Title`
  has the kind text with the length 200.
- Given the type defaults of the todo app, when the engine describes `Todo`, then `Notes`
  has the kind optional of text with the length 5000.
- Given the type defaults of the todo app. When the engine describes `Todo`, then
  `Priority` has the kind enum with the cases `Low`, `Normal`, and `High`.
- Given the type defaults of the todo app, when the engine describes `Todo`, then `Due`
  has the kind optional of date.
- Given the type defaults of the todo app, when the engine describes `Todo`, then `List`
  has the kind reference to `TodoList`.
- Given the type defaults of the todo app. When the engine describes `Todo`, then `Done`
  has the kind boolean, and `CompletedAt` has the kind optional of timestamp.
- Given the union type `Priority` as a model. When the engine describes it, then the result
  is the error `WRONG_CONFIG_TYPE` with the message "Priority is not a record type."
  (assumed)

### CAP-models.R2 · A type with no default is an error that names each engine

Status: verified · Intents: IN-0001

When a field has a type that no default covers, the description fails. The error names
the model, the field, the type, and each engine.

- Given a model `Tagged` with a field `Tags: Title list`. When the engine describes
  `Tagged`, then the result is the error `MISSING_DEFAULT`. The message is
  "Tagged.Tags has type 'Title list', which has no default in Persist, Validate,
  Display, API."
- Given a model with two fields that have no default. When the engine describes the
  model, then the result holds one `MISSING_DEFAULT` error for each field.

### CAP-models.R3 · The system fields are known and never written from a model

Status: verified · Intents: IN-0001

The system fields are `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`. A model
does not declare them. The plan knows their names.

- Given any model, when a caller asks for the system field names, then the names are
  `Id`, `Owner`, `Version`, `CreatedAt`, and `UpdatedAt`.
- Given the model `Todo`. When the engine describes `Todo`, then the fields of the plan
  are the seven fields of the record, and no system field.

### CAP-models.R4 · The label comes from the field name

Status: verified · Intents: IN-0001

The field name is split into words. The first word starts with a capital letter. The other
words are lower case.

- Given the field name `CompletedAt`, when the engine makes the label, then the label is
  "Completed at".
- Given the field name `Due`, when the engine makes the label, then the label is "Due".
- Given the field name `TimeZone`, when the engine makes the label, then the label is
  "Time zone".
- Given the field name `ExternalId`, when the engine makes the label, then the label is
  "External id".
- Given the field name `HasDue`, when the engine makes the label, then the label is "Has
  due".

### CAP-models.R5 · Wide text is text longer than 400 characters

Status: verified · Intents: IN-0001

- Given the field `Notes` with the kind optional of text with the length 5000. When a
  caller asks if the field is wide, then the answer is yes.
- Given the field `Title` with the kind text with the length 200. When a caller asks if
  the field is wide, then the answer is no.
- Given a text type with the length 400, when a caller asks if the field is wide, then the
  answer is no.
- Given a text type with the length 401, when a caller asks if the field is wide, then the
  answer is yes.

### CAP-models.R6 · The title field is marked or named

Status: verified · Intents: IN-0001

The title field names a record in pickers and links. The title field is the first field
whose type carries `[<TitleType>]`. A `titleField` override replaces that choice.

- Given the model `TodoList` and no overrides, when a caller asks for the title field,
  then the field is `Name`.
- Given the model `User` and no overrides, when a caller asks for the title field, then
  the field is `DisplayName`.
- Given a model `NoTitle` with one field `Count: int` and no overrides, when a caller
  asks for the title field, then there is none.
- Given the model `NoTitle` and the override `titleField <@ fun n -> n.Count @>`, when a
  caller asks for the title field, then the field is `Count`.

### CAP-models.R7 · Overrides take typed field selectors

Status: verified · Intents: IN-0001

Each override in the catalogue takes a field selector such as `<@ fun t -> t.Due @>`. The
catalogue is `label`, `hiddenInForm`, `readOnly`, `listColumns`, `listEdit`, `listSort`,
`listView`, `titleField`, `rule`, `sensitive`, `column`, and `unsafe`. An override set is
typed by its model, so a selector of another model does not compile.

- Given the selector `<@ fun (t: Todo) -> t.Due @>`, when the engine reads the field name,
  then the name is "Due".
- Given `overrides<Todo> [ listSort <@ fun t -> t.Due @> Ascending ]`, when the engine
  reads the override set, then its model name is "Todo" and it holds one `ListSort`
  item for the field "Due".
- Given `listColumns [ col <@ fun t -> t.Title @>; col <@ fun t -> t.Due @> ]`, when the
  engine reads the item, then the item holds the fields "Title" and "Due", in that
  order.
- Given `listView "Active" <@ fun l -> not l.Archived @>`, when the engine reads the
  fields that the view uses, then the fields are "Archived" only.
````

### DES-names

````
# DES-names · The names in code follow the spec

Status: agreed · Default: yes · Ask at init: no

## Rule

Code, folders, and check names use the words of the spec: Access, Persist, Validate,
Display, API, Generator, model, semantic type, kind, override, access rule, hook, and
system field.

The design (`docs/design/typed-layers-engine.md`) and the blueprint
(`docs/design/typed-layers-engine.blueprint.md`) give advice on where the walls go. Their
component names are not the names in code. This table maps them:

| Design component | Code module |
|---|---|
| `AuthorizingEngine` | `Access` |
| `RecordsAccess` | `Persist` |
| `ValidatingEngine` | `Validate` |
| `PresentingEngine`, `PageClient` | `Display` |
| `ApiClient` | `Api` |
| `BuildTool`, `BuildManager`, `CheckingEngine`, `ModelsAccess` | `Generator` |
| `RecordsManager`, `SignInManager`, `JobsManager` | `Runtime` |
| `SessionAccess`, `IdentityAccess`, `LeaseAccess` | `Runtime` |

## Why

The spec's acceptance test says that the todo app has no hand-written code outside
models, access rules, overrides, and hooks. That test reads the code by the spec's names.
Two sets of names would make the test hard to read and easy to fail.
````

### DES-errors

````
# DES-errors · The project's error codes

Status: agreed · Default: yes · Ask at init: no

## Rule

Each error that the system can give has a code here, in `UPPER_SNAKE_CASE`. Acceptance
lines, Interface entries, and checks all use these codes. Thus, a check can expect the
exact error.

| Code | Meaning | Whose fault | Retry helps? |
|---|---|---|---|
| `NOT_FOUND` | The record does not exist, or is outside the scope of the caller | caller | no |
| `STALE_VERSION` | The save used an old `Version` | caller | no |
| `READ_ONLY_RECORD` | The record is read-only. The reason is in the error | caller | no |
| `VALIDATION_FAILED` | A field broke a rule. The errors are listed for each field | caller | no |
| `MISSING_DEFAULT` | A type has no default in an engine | caller | no |
| `MISSING_ACCESS_RULE` | A model has no access rule, or more than one | caller | no |
| `MISSING_FIELD` | An override names a field or a model that does not exist | caller | no |
| `WRONG_CONFIG_TYPE` | An override or a rule has config that does not fit the field | caller | no |
| `SYSTEM_FIELD_TARGET` | An override targets a system field | caller | no |
| `DANGLING_REFERENCE` | A reference points at a model that does not exist | caller | no |
| `NO_TITLE_FIELD` | A referenced model has no title field | caller | no |
| `UNPLANNED_DROP` | A migration would drop a column with no `renamedFrom` | caller | no |
| `UNSAFE_ESCAPE_HATCH` | Warning: an `unsafe` override, listed with its reason | caller | no |
| `REDUNDANT_OVERRIDE` | Warning: an override that equals the default | caller | no |
| `UNSAFE_MIGRATION` | A new required column has no default, so existing rows would fail | caller | no |
| `SHAPE_UNREADABLE` | The stored shape file cannot be read | caller | no |

When a requirement needs a new error, add a row. When the meaning is the same, use the
existing code again.

Add rows at intake and ready. Do not add a row while a change is in tests, build, or
verify. When you add a row, leave `Default:` as it is. Write each row the same way:

- The code in backticks.
- "Whose fault": `caller`, `system`, or `outside service`.
- "Retry helps?": `yes` or `no`.

Checks and the code of the app match on the error code. Each requirement decides what a
person sees, for example a message or a status. The message or the status can omit the
error code. If the caller cannot see the error code, a check expects the exact message and
status instead.

## Why

A check that expects "an error" passes for any error, also for a typo. A named code makes
the check exact. A named code also gives callers a stable value to handle.
````

### DES-pdlc-check-tagging

````
# DES-pdlc-check-tagging · How checks name their requirement

Status: agreed · Default: yes · Ask at init: yes

## Rule

The requirement ID starts the name of the check, for example `CAP-email.R3 sends from a
template`. A check that proves two requirements names the two requirements.

If the check runner does not accept `.` or `-` in names (Python's `unittest`, for
example), write the ID with underscores instead: `test_CAP_email_R3_sends_from_a_template`.
The two forms count as the same ID. Use only one form in each project.

Lay checks out the same way everywhere:

- Put new check files in the check folder (`tests/` in a project that had none). Name
  each file after the spec it proves, in a form the runner finds: `test_CAP_email.py`,
  `CAP-email.test.js`.
- Inside a file, make one group per requirement (a test class or a `describe` block).
  Name the group with the requirement ID: `class CAP_email_R3(unittest.TestCase)`,
  `describe("CAP-email.R3", ...)`.
- Each check's name still starts with the ID, then says what it proves.

## Why

The trace from a requirement to its checks is only as good as this naming. Thus, the
naming must be simple and the same everywhere.
````

## The checks

### Diff against pdlc/CH-0001-models

````
diff --git a/tests/Typed.Engine.Tests/CAP-access.Tests.fs b/tests/Typed.Engine.Tests/CAP-access.Tests.fs
new file mode 100644
index 0000000..e75987e
--- /dev/null
+++ b/tests/Typed.Engine.Tests/CAP-access.Tests.fs
@@ -0,0 +1,231 @@
+namespace Typed.Engine.Tests
+
+open System
+open Xunit
+open Typed.Engine.Model
+open Typed.Engine.Access
+open Typed.Engine.Tests.ModelsFixtures
+
+/// Models and rules of the todo app. The checks use only these.
+module AccessFixtures =
+
+    type Audit = { Action: string }
+
+    /// The model has no rule in any list below.
+    type Unruled = { Name: string }
+
+    /// A second parent model, to pin the reason rule.
+    type Project = { Code: Title; Closed: bool }
+
+    type Task = { Project: Ref<Project>; Title: Title }
+
+    let u1 = Guid.Parse "11111111-1111-1111-1111-111111111111"
+    let u2 = Guid.Parse "22222222-2222-2222-2222-222222222222"
+
+    let todoOwnedVia = ownedVia <@ fun (t: Todo) -> t.List @>
+
+    let readOnlyTodoOwnedVia =
+        todoOwnedVia |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>
+
+    /// The rules of the todo app, with the read-only condition on Todo.
+    let rules: AccessRule list =
+        [ rule<TodoList> owned
+          rule<Todo> readOnlyTodoOwnedVia
+          rule<User> self
+          rule<Audit> system ]
+
+    let scopeKind (r: Result<Scope, Finding>) : ScopeKind =
+        match r with
+        | Ok s -> s.Kind
+        | Result.Error f -> failwithf "Expected a scope, got: %A" f
+
+    let scopeError (r: Result<Scope, Finding>) : Finding =
+        match r with
+        | Ok s -> failwithf "Expected an error, got a scope: %A" s.Kind
+        | Result.Error f -> f
+
+open AccessFixtures
+
+module ``CAP-access-R1`` =
+
+    [<Fact>]
+    let ``CAP-access.R1 owned gives model TodoList and access Owned`` () =
+        let r = rule<TodoList> owned
+        Assert.Equal("TodoList", r.ModelName)
+        Assert.Equal(Owned, r.Access)
+
+    [<Fact>]
+    let ``CAP-access.R1 ownedVia gives reference List and parent TodoList`` () =
+        let r = rule<Todo> todoOwnedVia
+        Assert.Equal("Todo", r.ModelName)
+        Assert.Equal(OwnedVia("List", "TodoList", None), r.Access)
+
+    [<Fact>]
+    let ``CAP-access.R1 self gives access Self`` () =
+        let r = rule<User> self
+        Assert.Equal("User", r.ModelName)
+        Assert.Equal(Self, r.Access)
+
+    [<Fact>]
+    let ``CAP-access.R1 system gives access SystemOnly`` () =
+        let r = rule<Audit> system
+        Assert.Equal("Audit", r.ModelName)
+        Assert.Equal(SystemOnly, r.Access)
+
+module ``CAP-access-R2`` =
+
+    [<Fact>]
+    let ``CAP-access.R2 readOnlyWhenParent names TodoList and Archived`` () =
+        let r = rule<Todo> readOnlyTodoOwnedVia
+        Assert.Equal(OwnedVia("List", "TodoList", Some("TodoList", "Archived")), r.Access)
+
+    [<Fact>]
+    let ``CAP-access.R2 readOnlyWhenParent on owned fails with its message`` () =
+        let ex =
+            Assert.Throws<ArgumentException>(fun () ->
+                owned |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
+        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)
+
+    [<Fact>]
+    let ``CAP-access.R2 readOnlyWhenParent on self fails with its message`` () =
+        let ex =
+            Assert.Throws<ArgumentException>(fun () ->
+                self |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
+        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)
+
+    [<Fact>]
+    let ``CAP-access.R2 readOnlyWhenParent on system fails with its message`` () =
+        let ex =
+            Assert.Throws<ArgumentException>(fun () ->
+                system |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
+        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)
+
+module ``CAP-access-R3`` =
+
+    [<Fact>]
+    let ``CAP-access.R3 TodoList scope is the owner filter for u1`` () =
+        Assert.Equal(OwnerFilter u1, scopeKind (scopeFor rules u1 "TodoList"))
+
+    [<Fact>]
+    let ``CAP-access.R3 the owner filter follows the user`` () =
+        Assert.Equal(OwnerFilter u2, scopeKind (scopeFor rules u2 "TodoList"))
+
+    [<Fact>]
+    let ``CAP-access.R3 User scope is the self scope for u1`` () =
+        Assert.Equal(SelfFilter u1, scopeKind (scopeFor rules u1 "User"))
+
+    [<Fact>]
+    let ``CAP-access.R3 Todo with ownedVia scope is the owner filter for u1`` () =
+        Assert.Equal(OwnerFilter u1, scopeKind (scopeFor rules u1 "Todo"))
+
+    [<Fact>]
+    let ``CAP-access.R3 Audit with system gives NOT_FOUND`` () =
+        let f = scopeError (scopeFor rules u1 "Audit")
+        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
+
+    [<Fact>]
+    let ``CAP-access.R3 a model with no rule gives NOT_FOUND`` () =
+        let f = scopeError (scopeFor rules u1 "Unruled")
+        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
+
+    [<Fact>]
+    let ``CAP-access.R3 the engine scope is the engine-only scope`` () =
+        Assert.Equal(Engine, engineScope.Kind)
+
+module RecordFixtures =
+
+    /// A TodoList record with the given owner and Archived value.
+    let listRecord (owner: Guid) (name: string) (archived: bool) : Record =
+        { Model = "TodoList"
+          Id = Guid.NewGuid()
+          Owner = Some owner
+          Version = 1L
+          Values = Map [ "Name", box name; "Archived", box archived ] }
+
+    let okUnit (r: Result<unit, Finding>) : unit =
+        match r with
+        | Ok () -> ()
+        | Result.Error f -> failwithf "Expected Ok, got: %A" f
+
+    let errorOf (r: Result<'a, Finding>) : Finding =
+        match r with
+        | Ok v -> failwithf "Expected an error, got Ok: %A" v
+        | Result.Error f -> f
+
+open RecordFixtures
+
+module ``CAP-access-R4`` =
+
+    [<Fact>]
+    let ``CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD`` () =
+        let parent = listRecord u1 "Old" true
+        let f = errorOf (checkChange rules Change "Todo" (Some parent))
+        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
+        Assert.Equal("This list is archived", f.Message)
+
+    [<Fact>]
+    let ``CAP-access.R4 change to a todo of a live list passes`` () =
+        let parent = listRecord u1 "Groceries" false
+        okUnit (checkChange rules Change "Todo" (Some parent))
+
+    [<Fact>]
+    let ``CAP-access.R4 delete of an archived list itself passes`` () =
+        okUnit (checkChange rules Delete "TodoList" None)
+
+    [<Fact>]
+    let ``CAP-access.R4 change to a todo whose list does not exist gives NOT_FOUND`` () =
+        let f = errorOf (checkChange rules Change "Todo" None)
+        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
+
+    [<Fact>]
+    let ``CAP-access.R4 creation of a todo in an archived list gives READ_ONLY_RECORD`` () =
+        let parent = listRecord u1 "Old" true
+        let f = errorOf (checkChange rules Create "Todo" (Some parent))
+        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
+        Assert.Equal("This list is archived", f.Message)
+
+    [<Fact>]
+    let ``CAP-access.R4 creation of a todo in a live list passes`` () =
+        let parent = listRecord u1 "Groceries" false
+        okUnit (checkChange rules Create "Todo" (Some parent))
+
+    [<Fact>]
+    let ``CAP-access.R4 the reason comes from the parent model and field`` () =
+        let projectRules =
+            [ rule<Task> (
+                  ownedVia <@ fun (t: Task) -> t.Project @>
+                  |> readOnlyWhenParent <@ fun (p: Project) -> p.Closed @>
+              ) ]
+
+        let parent: Record =
+            { Model = "Project"
+              Id = Guid.NewGuid()
+              Owner = Some u1
+              Version = 1L
+              Values = Map [ "Closed", box true ] }
+
+        let f = errorOf (checkChange projectRules Change "Task" (Some parent))
+        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
+        Assert.Equal("This project is closed", f.Message)
+
+module ``CAP-access-R5`` =
+
+    [<Fact>]
+    let ``CAP-access.R5 todo in a list owned by u1 gets owner u1`` () =
+        let parent = listRecord u1 "Groceries" false
+        match ownerFor rules u1 "Todo" (Some parent) with
+        | Ok owner -> Assert.Equal(u1, owner)
+        | Result.Error f -> failwithf "Expected an owner, got: %A" f
+
+    [<Fact>]
+    let ``CAP-access.R5 the owner follows the parent and user`` () =
+        let parent = listRecord u2 "Groceries" false
+        match ownerFor rules u2 "Todo" (Some parent) with
+        | Ok owner -> Assert.Equal(u2, owner)
+        | Result.Error f -> failwithf "Expected an owner, got: %A" f
+
+    [<Fact>]
+    let ``CAP-access.R5 todo in a list owned by u2 for user u1 gives NOT_FOUND`` () =
+        let parent = listRecord u2 "Groceries" false
+        let f = errorOf (ownerFor rules u1 "Todo" (Some parent))
+        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
diff --git a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
index f3ba2c6..0ff181d 100644
--- a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
+++ b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
@@ -7,6 +7,7 @@
 
   <ItemGroup>
     <Compile Include="CAP-models.Tests.fs" />
+    <Compile Include="CAP-access.Tests.fs" />
   </ItemGroup>
 
   <ItemGroup>
````

### tests/Typed.Engine.Tests/CAP-access.Tests.fs

````
namespace Typed.Engine.Tests

open System
open Xunit
open Typed.Engine.Model
open Typed.Engine.Access
open Typed.Engine.Tests.ModelsFixtures

/// Models and rules of the todo app. The checks use only these.
module AccessFixtures =

    type Audit = { Action: string }

    /// The model has no rule in any list below.
    type Unruled = { Name: string }

    /// A second parent model, to pin the reason rule.
    type Project = { Code: Title; Closed: bool }

    type Task = { Project: Ref<Project>; Title: Title }

    let u1 = Guid.Parse "11111111-1111-1111-1111-111111111111"
    let u2 = Guid.Parse "22222222-2222-2222-2222-222222222222"

    let todoOwnedVia = ownedVia <@ fun (t: Todo) -> t.List @>

    let readOnlyTodoOwnedVia =
        todoOwnedVia |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @>

    /// The rules of the todo app, with the read-only condition on Todo.
    let rules: AccessRule list =
        [ rule<TodoList> owned
          rule<Todo> readOnlyTodoOwnedVia
          rule<User> self
          rule<Audit> system ]

    let scopeKind (r: Result<Scope, Finding>) : ScopeKind =
        match r with
        | Ok s -> s.Kind
        | Result.Error f -> failwithf "Expected a scope, got: %A" f

    let scopeError (r: Result<Scope, Finding>) : Finding =
        match r with
        | Ok s -> failwithf "Expected an error, got a scope: %A" s.Kind
        | Result.Error f -> f

open AccessFixtures

module ``CAP-access-R1`` =

    [<Fact>]
    let ``CAP-access.R1 owned gives model TodoList and access Owned`` () =
        let r = rule<TodoList> owned
        Assert.Equal("TodoList", r.ModelName)
        Assert.Equal(Owned, r.Access)

    [<Fact>]
    let ``CAP-access.R1 ownedVia gives reference List and parent TodoList`` () =
        let r = rule<Todo> todoOwnedVia
        Assert.Equal("Todo", r.ModelName)
        Assert.Equal(OwnedVia("List", "TodoList", None), r.Access)

    [<Fact>]
    let ``CAP-access.R1 self gives access Self`` () =
        let r = rule<User> self
        Assert.Equal("User", r.ModelName)
        Assert.Equal(Self, r.Access)

    [<Fact>]
    let ``CAP-access.R1 system gives access SystemOnly`` () =
        let r = rule<Audit> system
        Assert.Equal("Audit", r.ModelName)
        Assert.Equal(SystemOnly, r.Access)

module ``CAP-access-R2`` =

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent names TodoList and Archived`` () =
        let r = rule<Todo> readOnlyTodoOwnedVia
        Assert.Equal(OwnedVia("List", "TodoList", Some("TodoList", "Archived")), r.Access)

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent on owned fails with its message`` () =
        let ex =
            Assert.Throws<ArgumentException>(fun () ->
                owned |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent on self fails with its message`` () =
        let ex =
            Assert.Throws<ArgumentException>(fun () ->
                self |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)

    [<Fact>]
    let ``CAP-access.R2 readOnlyWhenParent on system fails with its message`` () =
        let ex =
            Assert.Throws<ArgumentException>(fun () ->
                system |> readOnlyWhenParent <@ fun (l: TodoList) -> l.Archived @> |> ignore)
        Assert.Equal("readOnlyWhenParent only applies to ownedVia", ex.Message)

module ``CAP-access-R3`` =

    [<Fact>]
    let ``CAP-access.R3 TodoList scope is the owner filter for u1`` () =
        Assert.Equal(OwnerFilter u1, scopeKind (scopeFor rules u1 "TodoList"))

    [<Fact>]
    let ``CAP-access.R3 the owner filter follows the user`` () =
        Assert.Equal(OwnerFilter u2, scopeKind (scopeFor rules u2 "TodoList"))

    [<Fact>]
    let ``CAP-access.R3 User scope is the self scope for u1`` () =
        Assert.Equal(SelfFilter u1, scopeKind (scopeFor rules u1 "User"))

    [<Fact>]
    let ``CAP-access.R3 Todo with ownedVia scope is the owner filter for u1`` () =
        Assert.Equal(OwnerFilter u1, scopeKind (scopeFor rules u1 "Todo"))

    [<Fact>]
    let ``CAP-access.R3 Audit with system gives NOT_FOUND`` () =
        let f = scopeError (scopeFor rules u1 "Audit")
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)

    [<Fact>]
    let ``CAP-access.R3 a model with no rule gives NOT_FOUND`` () =
        let f = scopeError (scopeFor rules u1 "Unruled")
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)

    [<Fact>]
    let ``CAP-access.R3 the engine scope is the engine-only scope`` () =
        Assert.Equal(Engine, engineScope.Kind)

module RecordFixtures =

    /// A TodoList record with the given owner and Archived value.
    let listRecord (owner: Guid) (name: string) (archived: bool) : Record =
        { Model = "TodoList"
          Id = Guid.NewGuid()
          Owner = Some owner
          Version = 1L
          Values = Map [ "Name", box name; "Archived", box archived ] }

    let okUnit (r: Result<unit, Finding>) : unit =
        match r with
        | Ok () -> ()
        | Result.Error f -> failwithf "Expected Ok, got: %A" f

    let errorOf (r: Result<'a, Finding>) : Finding =
        match r with
        | Ok v -> failwithf "Expected an error, got Ok: %A" v
        | Result.Error f -> f

open RecordFixtures

module ``CAP-access-R4`` =

    [<Fact>]
    let ``CAP-access.R4 change to a todo of an archived list gives READ_ONLY_RECORD`` () =
        let parent = listRecord u1 "Old" true
        let f = errorOf (checkChange rules Change "Todo" (Some parent))
        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
        Assert.Equal("This list is archived", f.Message)

    [<Fact>]
    let ``CAP-access.R4 change to a todo of a live list passes`` () =
        let parent = listRecord u1 "Groceries" false
        okUnit (checkChange rules Change "Todo" (Some parent))

    [<Fact>]
    let ``CAP-access.R4 delete of an archived list itself passes`` () =
        okUnit (checkChange rules Delete "TodoList" None)

    [<Fact>]
    let ``CAP-access.R4 change to a todo whose list does not exist gives NOT_FOUND`` () =
        let f = errorOf (checkChange rules Change "Todo" None)
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)

    [<Fact>]
    let ``CAP-access.R4 creation of a todo in an archived list gives READ_ONLY_RECORD`` () =
        let parent = listRecord u1 "Old" true
        let f = errorOf (checkChange rules Create "Todo" (Some parent))
        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
        Assert.Equal("This list is archived", f.Message)

    [<Fact>]
    let ``CAP-access.R4 creation of a todo in a live list passes`` () =
        let parent = listRecord u1 "Groceries" false
        okUnit (checkChange rules Create "Todo" (Some parent))

    [<Fact>]
    let ``CAP-access.R4 the reason comes from the parent model and field`` () =
        let projectRules =
            [ rule<Task> (
                  ownedVia <@ fun (t: Task) -> t.Project @>
                  |> readOnlyWhenParent <@ fun (p: Project) -> p.Closed @>
              ) ]

        let parent: Record =
            { Model = "Project"
              Id = Guid.NewGuid()
              Owner = Some u1
              Version = 1L
              Values = Map [ "Closed", box true ] }

        let f = errorOf (checkChange projectRules Change "Task" (Some parent))
        Assert.Equal(ErrorCode.READ_ONLY_RECORD, f.Code)
        Assert.Equal("This project is closed", f.Message)

module ``CAP-access-R5`` =

    [<Fact>]
    let ``CAP-access.R5 todo in a list owned by u1 gets owner u1`` () =
        let parent = listRecord u1 "Groceries" false
        match ownerFor rules u1 "Todo" (Some parent) with
        | Ok owner -> Assert.Equal(u1, owner)
        | Result.Error f -> failwithf "Expected an owner, got: %A" f

    [<Fact>]
    let ``CAP-access.R5 the owner follows the parent and user`` () =
        let parent = listRecord u2 "Groceries" false
        match ownerFor rules u2 "Todo" (Some parent) with
        | Ok owner -> Assert.Equal(u2, owner)
        | Result.Error f -> failwithf "Expected an owner, got: %A" f

    [<Fact>]
    let ``CAP-access.R5 todo in a list owned by u2 for user u1 gives NOT_FOUND`` () =
        let parent = listRecord u2 "Groceries" false
        let f = errorOf (ownerFor rules u1 "Todo" (Some parent))
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
````

### tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="CAP-models.Tests.fs" />
    <Compile Include="CAP-access.Tests.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
  </ItemGroup>

</Project>
````

