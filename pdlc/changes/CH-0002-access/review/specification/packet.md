# Review packet · CH-0002-access · specification

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: specification

Default: yes

You judge if the code does what the spec says. You do not see the checks.

- The code meets the acceptance lines of each requirement. For a requirement marked
  `(retire)`, the change removes the code that served the requirement instead.
- The code does nothing that the spec did not ask for.
- Only the files in "Files" changed. In a file that "Files" lists with parts, only those
  parts changed. The change stays in its one scope. A job change must not edit the code of
  a capability.
- No old wording survives. The title and the summary of the spec, the cross-references in
  its requirements, and a README in "Files" show the behaviour as it is now. They include
  what the change adds. Nothing that the change removes or changes still shows the old
  way. The README says how to run the checks, and names any seam that a user can set.
- The spec text, the README text, and the code comments in the change follow the writing
  rules (`DES-writing-style`, in the packet). Each break of a rule is a problem. Give the
  line and the rule.
- Each finding under "Writing check" in the packet is a problem, unless you judge the
  finding to be a false alarm. For each false alarm, say why under "Notes".
- If the packet lists frames, read each frame. Each frame must show what its caption
  claims, after the step, not the screen before the step. A blank frame, or a frame that
  shows something else, is a problem.

## The change spec

````
# CH-0002 · Access

Intent: IN-0001 · Scope: CAP-access · Status: building · Ticket: none · Branch: pdlc/CH-0002-access · Base: pdlc/CH-0001-models
Tests-Locked: da2df15

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

Status: built · Intents: IN-0001

- Given `rule<TodoList> owned`, when a caller reads the rule, then the model name is
  "TodoList" and the access is `Owned`.
- Given `rule<Todo> (ownedVia <@ fun t -> t.List @>)`, when a caller reads the rule, then
  the access is `OwnedVia` with the reference field "List" and the parent "TodoList".
- Given `rule<User> self`, when a caller reads the rule, then the access is `Self`.
- Given `rule<Audit> system`, when a caller reads the rule, then the access is
  `SystemOnly`.

### CAP-access.R2 · A read-only condition names a bool field of the parent

Status: built · Intents: IN-0001

- Given `ownedVia <@ fun t -> t.List @> |> readOnlyWhenParent <@ fun l -> l.Archived @>`,
  when a caller reads the rule, then the read-only condition names the model "TodoList"
  and the field "Archived".
- Given `owned |> readOnlyWhenParent <@ fun l -> l.Archived @>`, when a caller reads the
  rule, then the call fails with the message "readOnlyWhenParent only applies to
  ownedVia".

### CAP-access.R3 · Only this capability can make a scope

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

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

### DES-writing-style

````
# DES-writing-style · How pdlc writes

Status: agreed · Default: yes · Ask at init: yes

## Rule

This rule is a copy of `WRITING-GUIDELINES.md` from the pskills repository.

These guidelines apply to all writing in this repository. They also apply to all text that
an agent writes when it uses the skills in this repository. They have four parts:

1. Part 1 gives the rules of ASD-STE100 Simplified Technical English (STE) in short form.
2. Part 2 gives two more rules: the local-references rule and the described-IDs rule.
3. Part 3 tells you how to apply the rules to each type of text.
4. Part 4 tells you how to examine your text against the rules.

### Where these guidelines apply

Use these guidelines for all of these types of text:

- The replies that an agent writes to the user in a conversation.
- Skills: the `SKILL.md` file, its reference files, and its templates.
- Agent definitions, system prompts, and the prompts that an agent writes for other agents.
- The documents that a skill or an agent writes: specs, designs, plans, reports, and web
  pages.
- Commit messages, pull request text, and code comments.

These items are not prose. Do not change them to obey the rules:

- Code, commands, file paths, and identifiers.
- Error codes, field names, and values.
- Quoted text. For example, the words of a user, or the trigger phrases in the description
  of a skill.
- The names of products, organisations, and published works.

### Part 1: STE rules

STE is a controlled language for technical text. The standard has 53 rules in nine
sections, and a dictionary of approved words. This part gives the rules in short form,
with the rule numbers of the standard. This repository does not contain the dictionary.
Part 4 tells you how to get the dictionary and how to use it.

#### Words (STE section 1)

- **1.1** Use approved words, technical nouns, and technical verbs.
- **1.2 to 1.4** Use an approved word only as its approved part of speech. Use the word
  only with its approved meaning and in its approved forms. For example, "check" is
  approved as a noun. As a verb, write "make sure that".
- **1.5, 1.6, 1.8 to 1.10** Technical nouns are the names of items in your subject.
  Examples in software are "commit", "branch", "file", "schema", and "pull request". Use
  the terms that your project uses. Select short terms. Do not use slang or jargon.
- **1.7** Do not use a technical noun as a verb. For example, do not write "action the
  ticket". Write "close the ticket".
- **1.11** Use one technical noun for one item. Do not use a different noun for the same
  item in a different part of the text.
- **1.12, 1.13** Technical verbs are the actions in your subject. In software, "commit",
  "merge", "deploy", "parse", and "click" are technical verbs. Do not use a technical verb
  as a noun. Write "merge the branch", not "do a merge of the branch".
- **1.14** Use New Zealand English spelling, for example "behaviour", "colour", and
  "organise". STE uses American English unless your own rules give a different standard.
  These guidelines give New Zealand English. Do not change the spelling of code, field
  names, or quoted text.

#### Multi-word nouns (STE section 2)

- **2.1** Do not write a multi-word noun of more than three words. For example, do not
  write "test result cache file path". Write "the path of the cache file for test
  results".
- **2.2** If a technical noun has more than three words, write it in full one time. Then
  use a shorter form, or join the words with hyphens.

#### Verbs (STE section 3)

- **3.1, 3.2** Use only these verb forms:
  - The infinitive: "to run".
  - The imperative: "Run the tests."
  - The simple present, the simple past, and the simple future: "runs", "ran", "will run".
  - The past participle as an adjective: "the merged branch".
- **3.3** Use the past participle as an adjective, for example "the merged branch".
- **3.4** Do not use auxiliary verbs to make complex verb constructions. For example, do
  not write "the file should have been updated". Write "update the file".
- **3.5** Use the "-ing" form of a verb only in a technical noun, for example "load
  balancing". Do not write "When running the tests, read the log". Write "When you run
  the tests, read the log".
- **3.6** Use the active voice. In descriptive text, you can use the passive voice only
  when you do not know who or what does the action.
- **Modal verbs (3.2, 3.4).** STE has "must" and "can". It does not have "should" or "may".
  Keep the strength of the original text:
  - A rule that always holds: write "must" or "must not".
  - Advice or a default: write "Try to …" or "Usually, …". Do not write "must".
  - A permission: write "is permitted to …". Write "can" for an ability.
  - The word "never": write "Do not …".
- **3.7** Use a verb to show an action, not a noun. Write "install the package", not "do
  the installation of the package".

#### Sentences (STE section 4)

- **4.1** Write short and clear sentences. Give one topic in each sentence. Give accurate
  facts, not abstract statements. For example, do not write "Large inputs change the run
  time". Write "When the input has more than 1,000 rows, the run time increases".
- **4.2** Do not omit words to make a sentence shorter. Do not omit articles, verbs, or
  subjects. Do not use contractions. Write "do not", not "don't".
- **4.3** Use a vertical list for complex text.
- **4.4** Use connecting words to connect related sentences, for example "and", "but",
  "then", "thus", and "as a result".
- **4.5** When applicable, put "the", "a", "an", "this", or "these" before a noun.

#### Instructions (STE section 5)

STE calls this type of text a procedure.

- **5.1** Write a maximum of 20 words in each instruction.
- **5.2** Write only one instruction in each sentence, unless the actions occur at the
  same time.
- **5.3** Write instructions in the imperative: "Run the tests."
- **5.4** If the reader must know a condition first, start with the condition. Put a comma
  after the condition: "If the tests fail, stop."
- **5.5** Use a note only to give information, not to give an instruction.

#### Descriptive text (STE section 6)

Descriptive text gives information, not instructions. Do not use the imperative in
descriptive text.

- **6.1** Give information gradually. Give the primary point first, then the details.
- **6.2** Use key words and headings to show the structure of the text.
- **6.3** Write a maximum of 25 words in each descriptive sentence.
- **6.4 to 6.6** Use paragraphs to show related information. Give each paragraph only one
  topic. Write a maximum of six sentences in each paragraph.

#### Safety instructions (STE section 7)

In software, a safety instruction tells the reader about an action that can cause loss of
data or other damage. Examples are a force push, a deleted table, and a message to other
persons.

- **7.1** Start with a word that shows the level of risk. Write "Warning" for a risk to
  persons. Write "Caution" for a risk to data or equipment.
- **7.2** Then give a clear command or condition.
- **7.3** Then give the risk or the possible result.

For example: "Caution: Do not force push to a shared branch. A force push can erase the
commits of other persons."

#### Punctuation and word count (STE section 8)

- **8.1** Do not use the semicolon. Write two sentences.
- **8.2** Use a hyphen to join words that are directly related, for example "read-only".
- **8.3** Use parentheses for references, identifiers, abbreviations, short explanations,
  and alternatives.
- **8.4** In a vertical list, a colon ends a sentence for the word count.
- **8.5 to 8.7** For the word count, count each of these items as one word:
  - Text in parentheses.
  - A number, or a number with its unit.
  - An abbreviation or an identifier.
  - Quoted text.
  - A title or a heading.
  - A proper noun.
  - A hyphenated word.

#### Writing practices (STE section 9)

- **9.1** If you cannot replace a word with an approved word, write the sentence again
  with a different structure.
- **9.2** Use each word correctly.
- **9.3** Do not use phrasal verbs. For example, write "prepare" or "configure", not "set
  up". Write "find", not "find out".
- **9.4** Use the same terms and the same style in all of the text.

#### General recommendations (STE)

- **GR-1** Write "that" after verbs such as "make sure", "show", and "recommend". For
  example: "Make sure that the tests pass."
- **GR-2** When you use "with", read the sentence again. Make sure that the sentence has
  only one meaning.
- **GR-3, GR-4** If a pronoun can refer to more than one noun, write the noun. The
  local-references rule in Part 2 makes this rule stricter.
- **GR-6** Do not use Latin abbreviations. Write "for example", not "e.g.". Write "that
  is", not "i.e.". Do not write "etc.".
- **GR-7** Use gender-neutral language. Use "they" for a person whose pronouns you do not
  know.
- **GR-8** Use the possessive form, for example "the user's file", only when you are sure
  that it is correct.

### Part 2: Two more rules

These two rules apply together with the STE rules.

#### The local-references rule

The reader must not look back far to find what a word refers to.

- A pronoun ("it", "they", "this", "that", "these", "those") must refer to a noun in the
  same paragraph. A short noun, for example "the file" or "the change", must also refer
  to a noun in the same paragraph.
- Only one noun in that paragraph can match the reference. If two nouns can match, write
  the full noun.
- At the start of a paragraph, a list item, a table cell, or a reply, write the full noun.
  Do not use "It", "This", or "They" to refer to an earlier paragraph.
- A heading does not give a noun to the text below the heading. Under the heading
  "OutboxAccess", do not start with "It stores". Write "OutboxAccess stores".
- In a conversation, each reply must make sense alone. Do not write "the PR" or "the
  second option" to refer to an earlier reply. Write the name or the full description.

For example, do not write: "The run found a problem in the template and in the checker.
It is now correct." Write: "The run found a problem in the template and in the checker. I
corrected the template."

#### The described-IDs rule

An ID is a short code that names an item, for example "B2", "R3", "V1", "UC2", "PR #6",
"task #4", or a commit hash. An ID without a description makes the reader look back.

- Each time that you write an ID, write a short description of the item in the same
  sentence. For example: "B2 (cache the results)".
- When the reader must select an alternative, give each alternative its ID and its
  description in the question. Do not ask "B1, B2, or B3?".
- In a table, a row that gives the ID and the description is sufficient for that row.
- Make each description short and simple: two to eight words.
- If the ID is not necessary for the reader, do not use the ID. Use the description only.

For example, do not write: "Do you want B1 or B3?" Write: "Do you want B1 (keep the
cache) or B3 (remove the cache)?"

### Part 3: How to apply the rules

#### Replies to the user

- Give the answer or the result first. Then give the details.
- Use plain words, and give the context that the reader must have.
- Use short sentences. Use a list for three or more items. Do not write a paragraph when
  sentences in a list are sufficient.
- Write only about the topic. Do not add text that does not relate to the topic.
- Make each reply clear alone. Obey the local-references rule.
- When you ask a question with alternatives, obey the described-IDs rule.

#### Skills, agent definitions, and prompts

- If a skill gives an exact output format, the format has priority over these
  guidelines. For example, a skill can tell an agent to write one sentence for each
  option. Then the agent writes one sentence, also when the sentence has two topics.
- Write each step in the imperative. Write one instruction in each sentence.
- Put the condition first: "If the suite exists, load the suite."
- Give numbers to the steps of a procedure.
- Write the purpose of a skill, and other descriptive text, with the descriptive rules.
- In the `description` of a skill, keep the name and the trigger phrases of the skill.
  Do not change these quoted phrases. Write the other text of the description with the
  rules.
- A prompt for a different agent must make sense alone, because that agent does not see
  your conversation.

#### Documents and artifacts

- Start each section with its primary point.
- Use headings as key words.
- Give each technical noun that a reader possibly does not know in a "Words used here"
  list. Use one line for each word.
- Use a table to compare items. Use a list for steps and alternatives.

#### Commit messages and pull requests

- Write the subject line of a commit in the imperative, for example "Add the linter".
- Write the body as descriptive text.

### Part 4: Examine your text

1. Run the linter: `python check_writing.py FILE`. The linter is in the `scripts` folder of
   the `improve-writing` skill. The linter finds the problems that a script can measure.
2. To examine words against the STE dictionary, get the standard. The standard is free
   from <https://www.asd-ste100.org>. Then give the linter your copy:
   `--dictionary PATH`. The path can be the PDF file or a text file of the PDF.
3. Read the text again for the rules that a script cannot measure. Examples are one topic
   in each sentence, the local-references rule, and accurate facts.

The `/improve-writing` skill does these three steps and writes the text again.

### Source

- ASD-STE100 Simplified Technical English, Issue 9, 2025-01-15, by ASD (the Aerospace,
  Security and Defence Industries Association of Europe). Part 1 gives the rules of the
  standard in short form, with the rule numbers of the standard.
- The standard is free from <https://www.asd-ste100.org>. ASD does not let other persons
  distribute the standard. Thus, this file does not contain the text of the standard or
  its dictionary.
- ASD-STE100 is a registered trademark of ASD.
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

## The code

### Diff against pdlc/CH-0001-models

````
diff --git a/src/Typed.Engine/Access.fs b/src/Typed.Engine/Access.fs
new file mode 100644
index 0000000..3537e58
--- /dev/null
+++ b/src/Typed.Engine/Access.fs
@@ -0,0 +1,117 @@
+module Typed.Engine.Access
+
+open System
+open Microsoft.FSharp.Quotations
+open Microsoft.FSharp.Quotations.Patterns
+open Typed.Engine.Model
+
+type Access =
+    | Owned
+    | OwnedVia of reference: string * parent: string * readOnlyWhen: (string * string) option
+    | Self
+    | SystemOnly
+
+type AccessRule = { ModelName: string; Access: Access }
+
+type ScopeKind =
+    | OwnerFilter of Guid
+    | SelfFilter of Guid
+    | Engine
+
+/// A scope. Only this module makes one.
+type Scope =
+    private
+        { scopeKind: ScopeKind }
+
+    member this.Kind = this.scopeKind
+
+type Action =
+    | Create
+    | Change
+    | Delete
+
+let owned: Access = Owned
+let self: Access = Self
+let system: Access = SystemOnly
+
+/// The property that a quotation `fun x -> x.Prop` reads.
+let private propertyOf (expr: Expr) : Reflection.PropertyInfo =
+    match expr with
+    | Lambda(_, PropertyGet(Some _, prop, [])) -> prop
+    | _ -> invalidArg "expr" "Expected a quotation of the form fun x -> x.Property"
+
+let ownedVia (expr: Expr<'T -> Ref<'P>>) : Access =
+    let prop = propertyOf expr
+    OwnedVia(prop.Name, typeof<'P>.Name, None)
+
+let readOnlyWhenParent (expr: Expr<'P -> bool>) (access: Access) : Access =
+    match access with
+    | OwnedVia(reference, parent, _) -> OwnedVia(reference, parent, Some(typeof<'P>.Name, (propertyOf expr).Name))
+    | _ -> raise (ArgumentException "readOnlyWhenParent only applies to ownedVia")
+
+let rule<'T> (access: Access) : AccessRule =
+    { ModelName = typeof<'T>.Name
+      Access = access }
+
+let engineScope: Scope = { scopeKind = Engine }
+
+let private notFound (model: string) (message: string) : Finding =
+    { Severity = Error
+      Code = NOT_FOUND
+      Model = model
+      Field = None
+      Message = message }
+
+let private accessOf (rules: AccessRule list) (model: string) : Access option =
+    rules |> List.tryFind (fun r -> r.ModelName = model) |> Option.map (fun r -> r.Access)
+
+let scopeFor (rules: AccessRule list) (user: Guid) (model: string) : Result<Scope, Finding> =
+    match accessOf rules model with
+    | Some Owned
+    | Some(OwnedVia _) -> Ok { scopeKind = OwnerFilter user }
+    | Some Self -> Ok { scopeKind = SelfFilter user }
+    | Some SystemOnly
+    | None -> Result.Error(notFound model (sprintf "%s was not found." model))
+
+/// The reason text, such as "This list is archived".
+let private reasonOf (parent: string) (field: string) : string =
+    let word = parent |> Seq.fold (fun (w: string) c -> if Char.IsUpper c then string c else w + string c) ""
+    sprintf "This %s is %s" (word.ToLowerInvariant()) (field.ToLowerInvariant())
+
+let checkChange
+    (rules: AccessRule list)
+    (_action: Action)
+    (model: string)
+    (parent: Record option)
+    : Result<unit, Finding> =
+    match accessOf rules model with
+    | Some(OwnedVia(_, parentModel, readOnlyWhen)) ->
+        match parent with
+        | None -> Result.Error(notFound model (sprintf "The %s of this %s was not found." parentModel model))
+        | Some p ->
+            match readOnlyWhen with
+            | Some(roModel, field) when p.Values.TryFind field = Some(box true) ->
+                Result.Error
+                    { Severity = Error
+                      Code = READ_ONLY_RECORD
+                      Model = model
+                      Field = None
+                      Message = reasonOf roModel field }
+            | _ -> Ok()
+    | _ -> Ok()
+
+let ownerFor
+    (rules: AccessRule list)
+    (user: Guid)
+    (model: string)
+    (parent: Record option)
+    : Result<Guid, Finding> =
+    match accessOf rules model with
+    | Some Owned
+    | Some Self -> Ok user
+    | Some(OwnedVia _) ->
+        match parent with
+        | Some p when p.Owner = Some user -> Ok user
+        | _ -> Result.Error(notFound model (sprintf "%s was not found." model))
+    | Some SystemOnly
+    | None -> Result.Error(notFound model (sprintf "%s was not found." model))
diff --git a/src/Typed.Engine/Typed.Engine.fsproj b/src/Typed.Engine/Typed.Engine.fsproj
index fe07324..a7db7c4 100644
--- a/src/Typed.Engine/Typed.Engine.fsproj
+++ b/src/Typed.Engine/Typed.Engine.fsproj
@@ -7,6 +7,7 @@
   <ItemGroup>
     <Compile Include="Model.fs" />
     <Compile Include="Overrides.fs" />
+    <Compile Include="Access.fs" />
   </ItemGroup>
 
 </Project>
````

### src/Typed.Engine/Access.fs

````
module Typed.Engine.Access

open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Typed.Engine.Model

type Access =
    | Owned
    | OwnedVia of reference: string * parent: string * readOnlyWhen: (string * string) option
    | Self
    | SystemOnly

type AccessRule = { ModelName: string; Access: Access }

type ScopeKind =
    | OwnerFilter of Guid
    | SelfFilter of Guid
    | Engine

/// A scope. Only this module makes one.
type Scope =
    private
        { scopeKind: ScopeKind }

    member this.Kind = this.scopeKind

type Action =
    | Create
    | Change
    | Delete

let owned: Access = Owned
let self: Access = Self
let system: Access = SystemOnly

/// The property that a quotation `fun x -> x.Prop` reads.
let private propertyOf (expr: Expr) : Reflection.PropertyInfo =
    match expr with
    | Lambda(_, PropertyGet(Some _, prop, [])) -> prop
    | _ -> invalidArg "expr" "Expected a quotation of the form fun x -> x.Property"

let ownedVia (expr: Expr<'T -> Ref<'P>>) : Access =
    let prop = propertyOf expr
    OwnedVia(prop.Name, typeof<'P>.Name, None)

let readOnlyWhenParent (expr: Expr<'P -> bool>) (access: Access) : Access =
    match access with
    | OwnedVia(reference, parent, _) -> OwnedVia(reference, parent, Some(typeof<'P>.Name, (propertyOf expr).Name))
    | _ -> raise (ArgumentException "readOnlyWhenParent only applies to ownedVia")

let rule<'T> (access: Access) : AccessRule =
    { ModelName = typeof<'T>.Name
      Access = access }

let engineScope: Scope = { scopeKind = Engine }

let private notFound (model: string) (message: string) : Finding =
    { Severity = Error
      Code = NOT_FOUND
      Model = model
      Field = None
      Message = message }

let private accessOf (rules: AccessRule list) (model: string) : Access option =
    rules |> List.tryFind (fun r -> r.ModelName = model) |> Option.map (fun r -> r.Access)

let scopeFor (rules: AccessRule list) (user: Guid) (model: string) : Result<Scope, Finding> =
    match accessOf rules model with
    | Some Owned
    | Some(OwnedVia _) -> Ok { scopeKind = OwnerFilter user }
    | Some Self -> Ok { scopeKind = SelfFilter user }
    | Some SystemOnly
    | None -> Result.Error(notFound model (sprintf "%s was not found." model))

/// The reason text, such as "This list is archived".
let private reasonOf (parent: string) (field: string) : string =
    let word = parent |> Seq.fold (fun (w: string) c -> if Char.IsUpper c then string c else w + string c) ""
    sprintf "This %s is %s" (word.ToLowerInvariant()) (field.ToLowerInvariant())

let checkChange
    (rules: AccessRule list)
    (_action: Action)
    (model: string)
    (parent: Record option)
    : Result<unit, Finding> =
    match accessOf rules model with
    | Some(OwnedVia(_, parentModel, readOnlyWhen)) ->
        match parent with
        | None -> Result.Error(notFound model (sprintf "The %s of this %s was not found." parentModel model))
        | Some p ->
            match readOnlyWhen with
            | Some(roModel, field) when p.Values.TryFind field = Some(box true) ->
                Result.Error
                    { Severity = Error
                      Code = READ_ONLY_RECORD
                      Model = model
                      Field = None
                      Message = reasonOf roModel field }
            | _ -> Ok()
    | _ -> Ok()

let ownerFor
    (rules: AccessRule list)
    (user: Guid)
    (model: string)
    (parent: Record option)
    : Result<Guid, Finding> =
    match accessOf rules model with
    | Some Owned
    | Some Self -> Ok user
    | Some(OwnedVia _) ->
        match parent with
        | Some p when p.Owner = Some user -> Ok user
        | _ -> Result.Error(notFound model (sprintf "%s was not found." model))
    | Some SystemOnly
    | None -> Result.Error(notFound model (sprintf "%s was not found." model))
````

### src/Typed.Engine/Typed.Engine.fsproj

````
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="Model.fs" />
    <Compile Include="Overrides.fs" />
    <Compile Include="Access.fs" />
  </ItemGroup>

</Project>
````

## Writing check

`pdlc/bin/check_writing.py --since pdlc/CH-0001-models` on pdlc/changes/CH-0002-access/change.md, pdlc/specs/capabilities/CAP-access.md:

````
No findings.
````

## Visual review frames

Read each frame to see the change working:

- pdlc/changes/CH-0002-access/frames/01.png
- pdlc/changes/CH-0002-access/frames/02.png
- pdlc/changes/CH-0002-access/frames/03.png
- pdlc/changes/CH-0002-access/frames/04.png
- pdlc/changes/CH-0002-access/frames/05.png
- pdlc/changes/CH-0002-access/frames/06.png
- pdlc/changes/CH-0002-access/frames/07.png
- pdlc/changes/CH-0002-access/frames/08.png
- pdlc/changes/CH-0002-access/frames/09.png
- pdlc/changes/CH-0002-access/frames/10.png
- pdlc/changes/CH-0002-access/frames/11.png
- pdlc/changes/CH-0002-access/frames/12.png
- pdlc/changes/CH-0002-access/frames/13.png
- pdlc/changes/CH-0002-access/frames/14.png
- pdlc/changes/CH-0002-access/frames/15.png
- pdlc/changes/CH-0002-access/frames/16.png
- pdlc/changes/CH-0002-access/frames/17.png
- pdlc/changes/CH-0002-access/frames/18.png
- pdlc/changes/CH-0002-access/frames/19.png
- pdlc/changes/CH-0002-access/frames/20.png
