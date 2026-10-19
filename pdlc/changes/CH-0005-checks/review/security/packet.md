# Review packet · CH-0005-checks · security

Judge only what is in this packet. Write your result next to it, in result.md.

# Remit: security

Default: yes

You judge if the change is safe.

- The code validates input from users, files, or the network as the requirements say,
  before it uses the input.
- There is no injection: the code builds commands, queries, paths, and HTML safely.
- There are no secrets in code, logs, or errors.
- Permissions and access checks are correct, and they fail closed.
- Each new dependency is necessary and trustworthy.

## The change spec

````
# CH-0005 · Checks

Intent: IN-0001 · Scope: CAP-checks · Status: built · Ticket: none · Branch: pdlc/CH-0005-checks · Base: pdlc/CH-0004-persist
Tests-Locked: 09ce3bfba7806424b1e5c4c262c49dde5bc47552

## Delivers

Every build check runs over the models, the access rules, and the override sets. The findings name the error or the warning, in model order.

## Requirements

### CAP-checks.R1 · A model with no access rule fails the build

- Given the model `TodoList` and no access rules. When the checks run, then the findings
  hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList has no access
  rule."
- Given the model `TodoList` and two access rules for `TodoList`. When the checks run,
  then the findings hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList
  has more than one access rule."
- Given the three models of the todo app and the three access rules of the todo app.
  When the checks run, then there is no `MISSING_ACCESS_RULE` finding.

### CAP-checks.R2 · An override that names a missing field fails the build

- Given an override set for a model `Ghost` that is not in the model list. When the checks
  run, then the findings hold the error `MISSING_FIELD` with the message "Overrides name
  Ghost, which is not a model."
- Given a `Label` item for the field "Dew" of `Todo`. When the checks run, then the
  findings hold the error `MISSING_FIELD` with the message "Override names missing field
  Todo.Dew."

### CAP-checks.R3 · An override with config for the wrong type fails the build

- Given the override `rule <@ fun t -> t.Due @>` whose predicate takes a `string`, when
  the checks run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message
  names `Todo.Due`, the expected type, and the type of the field.
- Given the override `rule <@ fun t -> t.Title @>` whose predicate takes a `Title`, when
  the checks run, then there is no `WRONG_CONFIG_TYPE` finding.

### CAP-checks.R4 · An override that targets a system field fails the build

- Given a `Column` item for the field "Version" of `Todo`. When the checks run, then the
  findings hold the error `SYSTEM_FIELD_TARGET` with the message "Override targets system
  field Todo.Version."

### CAP-checks.R5 · A reference to a model that does not exist fails the build

- Given the model `Todo` alone, with its field `List: Ref<TodoList>`. When the checks run,
  then the findings hold the error `DANGLING_REFERENCE` with the message "Field Todo.List
  refers to TodoList, which is not a model."

### CAP-checks.R6 · A referenced model with no title field fails the build

- Given a model `NoTitle` with one field `Count: int`, and a model `Child` with the field
  `Parent: Ref<NoTitle>`, and no `titleField` override. When the checks run, then the
  findings hold the error `NO_TITLE_FIELD` with the message "NoTitle has no title field,
  but Child.Parent refers to it."
- Given the same two models and the override `titleField <@ fun n -> n.Count @>` for
  `NoTitle`, when the checks run, then there is no `NO_TITLE_FIELD` finding.

### CAP-checks.R7 · An `ownedVia` rule must name a reference to its parent

- Given the access rule `ownedVia <@ fun t -> t.Title @>` for `Todo`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE` with the message "ownedVia
  Todo.Title must be a reference to TodoList."
- Given the rule `readOnlyWhenParent <@ fun l -> l.Name @>` on the `ownedVia` rule of
  `Todo`. When the checks run, then the findings hold the error `WRONG_CONFIG_TYPE` with
  the message "readOnlyWhenParent names TodoList.Name, which is not a bool field."

### CAP-checks.R8 · Each `unsafe` escape hatch is a warning that gives its reason

- Given the override `unsafe "rawName" "uses raw SQL"` for `TodoList`. When the checks
  run, then the findings hold no error, and one warning `UNSAFE_ESCAPE_HATCH` with the message
  "Unsafe escape hatch TodoList.rawName: uses raw SQL".

### CAP-checks.R9 · An override that matches the default is a warning

- Given the override `label <@ fun t -> t.Due @> "Due"` for `Todo`. When the checks run,
  then the findings hold one warning `REDUNDANT_OVERRIDE` with the message "Override
  Todo.Due label matches the default." (assumed)
- Given the override `label <@ fun t -> t.Due @> "Due date"` for `Todo`, when the checks
  run, then there is no such warning.

### CAP-checks.R10 · The todo app passes every check

- Given the three models, the three access rules, and the two override sets of the todo
  app. When the checks run, then there are no error findings.

### CAP-checks.R11 · Two overrides of one kind on one field fail the build

- Given two `label` items for the field `Due` of `Todo`. When the checks run, then the
  findings hold the error `WRONG_CONFIG_TYPE` with the message "Todo.Due has two label
  overrides." (assumed)
- Given one `label` item and one `readOnly` item for `Due`, when the checks run, then there
  is no such finding.

### CAP-checks.R12 · Findings come in model order, then field order

- Given the models `TodoList` then `Todo`, each with one error. When the checks run, then
  the `TodoList` finding comes before the `Todo` finding. (assumed)
- Given no models and no rules, when the checks run, then there are no findings.

### CAP-checks.R13 · A sort or a view that the store cannot serve fails the build

- Given the override `listSort <@ fun t -> t.Notes @> Ascending`. When the checks run,
  then the findings hold the error `WRONG_CONFIG_TYPE`. The message is "Cannot sort Todo
  by Notes: wide text is never indexed."
- Given the view `listView "Long" <@ fun t -> t.Title.Length > 3 @>`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message starts with
  "Unsupported list view for Todo:". (assumed)
- Given the view `listView "High" <@ fun t -> t.Priority = High @>`, when the checks run,
  then there is no such finding.

## Relies on

- CAP-models (used, not changed)
- CAP-access (used, not changed)
- CAP-persist (used, not changed)
- DES-names
- DES-errors
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

In `src/Typed.Engine/Checks.fs`, module `Typed.Engine.Checks`:

- `run: TypeDefault list -> Type list -> AccessRule list -> ModelOverrides list -> Finding list`: every finding, errors and warnings, in model order then field order.
- `errors: Finding list -> Finding list` and `warnings: Finding list -> Finding list`: the two halves of a result.
- Each finding gives its code in `Code`, as `DES-errors` names it, and its text in `Message`. The messages are the quoted texts in the acceptance lines.
- Codes: `MISSING_ACCESS_RULE`, `MISSING_FIELD`, `WRONG_CONFIG_TYPE`, `SYSTEM_FIELD_TARGET`, `DANGLING_REFERENCE`, `NO_TITLE_FIELD`, `UNSAFE_ESCAPE_HATCH` (warning), `REDUNDANT_OVERRIDE` (warning)

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

none

## Files

- src/Typed.Engine/Checks.fs
- src/Typed.Engine/Typed.Engine.fsproj: the Compile entry of this spec

## Check files

- tests/Typed.Engine.Tests/CAP-checks.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

## Steps

### Step 1 · A model with no access rule fails the build · [ ]

- Requirement: CAP-checks.R1
- Check: the checks named `CAP-checks.R1 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R1 (a model with no access rule fails the build) passes

### Step 2 · An override that names a missing field fails the build · [ ]

- Requirement: CAP-checks.R2
- Check: the checks named `CAP-checks.R2 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R2 (an override that names a missing field fails the build) passes

### Step 3 · An override with config for the wrong type fails the build · [ ]

- Requirement: CAP-checks.R3
- Check: the checks named `CAP-checks.R3 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R3 (an override with config for the wrong type fails the build) passes

### Step 4 · An override that targets a system field fails the build · [ ]

- Requirement: CAP-checks.R4
- Check: the checks named `CAP-checks.R4 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R4 (an override that targets a system field fails the build) passes

### Step 5 · A reference to a model that does not exist fails the build · [ ]

- Requirement: CAP-checks.R5
- Check: the checks named `CAP-checks.R5 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R5 (a reference to a model that does not exist fails the build) passes

### Step 6 · A referenced model with no title field fails the build · [ ]

- Requirement: CAP-checks.R6
- Check: the checks named `CAP-checks.R6 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R6 (a referenced model with no title field fails the build) passes

### Step 7 · An `ownedVia` rule must name a reference to its parent · [ ]

- Requirement: CAP-checks.R7
- Check: the checks named `CAP-checks.R7 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R7 (an `ownedvia` rule must name a reference to its parent) passes

### Step 8 · Each `unsafe` escape hatch is a warning that gives its reason · [ ]

- Requirement: CAP-checks.R8
- Check: the checks named `CAP-checks.R8 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R8 (each `unsafe` escape hatch is a warning that gives its reason) passes

### Step 9 · An override that matches the default is a warning · [ ]

- Requirement: CAP-checks.R9
- Check: the checks named `CAP-checks.R9 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R9 (an override that matches the default is a warning) passes

### Step 10 · The todo app passes every check · [ ]

- Requirement: CAP-checks.R10
- Check: the checks named `CAP-checks.R10 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R10 (the todo app passes every check) passes

### Step 11 · Two overrides of one kind on one field fail the build · [ ]

- Requirement: CAP-checks.R11
- Check: the checks named `CAP-checks.R11 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R11 (two overrides of one kind on one field fail the build) passes

### Step 12 · Findings come in model order, then field order · [ ]

- Requirement: CAP-checks.R12
- Check: the checks named `CAP-checks.R12 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R12 (findings come in model order, then field order) passes

### Step 13 · A sort or a view that the store cannot serve fails the build · [ ]

- Requirement: CAP-checks.R13
- Check: the checks named `CAP-checks.R13 ...` in `tests/Typed.Engine.Tests/CAP-checks.Tests.fs`
- Done when: every check of CAP-checks.R13 (a sort or a view that the store cannot serve fails the build) passes

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `CAP-checks` as the ready stage left them.
- Built: 176 checks pass (36 new). The builder added Checks.fs and its Compile entry.
````

## The spec it covers: title and summary

````
# CAP-checks · Fails the build when a model, rule, or override is wrong

Status: active

## What it is for

This capability runs every build check over the models, the access rules, and the
override sets. Each check gives findings. An error finding fails the build. A warning
finding does not. The generator job prints the findings.

## Code map

- src/Typed.Engine/Checks.fs
````

## The requirements, in full

### CAP-checks.R1 · A model with no access rule fails the build

Status: built · Intents: IN-0001

- Given the model `TodoList` and no access rules. When the checks run, then the findings
  hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList has no access
  rule."
- Given the model `TodoList` and two access rules for `TodoList`. When the checks run,
  then the findings hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList
  has more than one access rule."
- Given the three models of the todo app and the three access rules of the todo app.
  When the checks run, then there is no `MISSING_ACCESS_RULE` finding.

### CAP-checks.R2 · An override that names a missing field fails the build

Status: built · Intents: IN-0001

The F# compiler refuses a selector of a field that does not exist. This check covers an
override set that names a model with no description. It also covers an override item that names a
field that the plan does not hold.

- Given an override set for a model `Ghost` that is not in the model list. When the checks
  run, then the findings hold the error `MISSING_FIELD` with the message "Overrides name
  Ghost, which is not a model."
- Given a `Label` item for the field "Dew" of `Todo`. When the checks run, then the
  findings hold the error `MISSING_FIELD` with the message "Override names missing field
  Todo.Dew."

### CAP-checks.R3 · An override with config for the wrong type fails the build

Status: built · Intents: IN-0001

- Given the override `rule <@ fun t -> t.Due @>` whose predicate takes a `string`, when
  the checks run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message
  names `Todo.Due`, the expected type, and the type of the field.
- Given the override `rule <@ fun t -> t.Title @>` whose predicate takes a `Title`, when
  the checks run, then there is no `WRONG_CONFIG_TYPE` finding.

### CAP-checks.R4 · An override that targets a system field fails the build

Status: built · Intents: IN-0001

- Given a `Column` item for the field "Version" of `Todo`. When the checks run, then the
  findings hold the error `SYSTEM_FIELD_TARGET` with the message "Override targets system
  field Todo.Version."

### CAP-checks.R5 · A reference to a model that does not exist fails the build

Status: built · Intents: IN-0001

- Given the model `Todo` alone, with its field `List: Ref<TodoList>`. When the checks run,
  then the findings hold the error `DANGLING_REFERENCE` with the message "Field Todo.List
  refers to TodoList, which is not a model."

### CAP-checks.R6 · A referenced model with no title field fails the build

Status: built · Intents: IN-0001

- Given a model `NoTitle` with one field `Count: int`, and a model `Child` with the field
  `Parent: Ref<NoTitle>`, and no `titleField` override. When the checks run, then the
  findings hold the error `NO_TITLE_FIELD` with the message "NoTitle has no title field,
  but Child.Parent refers to it."
- Given the same two models and the override `titleField <@ fun n -> n.Count @>` for
  `NoTitle`, when the checks run, then there is no `NO_TITLE_FIELD` finding.

### CAP-checks.R7 · An `ownedVia` rule must name a reference to its parent

Status: built · Intents: IN-0001

- Given the access rule `ownedVia <@ fun t -> t.Title @>` for `Todo`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE` with the message "ownedVia
  Todo.Title must be a reference to TodoList."
- Given the rule `readOnlyWhenParent <@ fun l -> l.Name @>` on the `ownedVia` rule of
  `Todo`. When the checks run, then the findings hold the error `WRONG_CONFIG_TYPE` with
  the message "readOnlyWhenParent names TodoList.Name, which is not a bool field."

### CAP-checks.R8 · Each `unsafe` escape hatch is a warning that gives its reason

Status: built · Intents: IN-0001

- Given the override `unsafe "rawName" "uses raw SQL"` for `TodoList`. When the checks
  run, then the findings hold no error, and one warning `UNSAFE_ESCAPE_HATCH` with the message
  "Unsafe escape hatch TodoList.rawName: uses raw SQL".

### CAP-checks.R9 · An override that matches the default is a warning

Status: built · Intents: IN-0001

- Given the override `label <@ fun t -> t.Due @> "Due"` for `Todo`. When the checks run,
  then the findings hold one warning `REDUNDANT_OVERRIDE` with the message "Override
  Todo.Due label matches the default." (assumed)
- Given the override `label <@ fun t -> t.Due @> "Due date"` for `Todo`, when the checks
  run, then there is no such warning.

### CAP-checks.R10 · The todo app passes every check

Status: built · Intents: IN-0001

- Given the three models, the three access rules, and the two override sets of the todo
  app. When the checks run, then there are no error findings.

### CAP-checks.R11 · Two overrides of one kind on one field fail the build

Status: built · Intents: IN-0001

- Given two `label` items for the field `Due` of `Todo`. When the checks run, then the
  findings hold the error `WRONG_CONFIG_TYPE` with the message "Todo.Due has two label
  overrides." (assumed)
- Given one `label` item and one `readOnly` item for `Due`, when the checks run, then there
  is no such finding.

### CAP-checks.R12 · Findings come in model order, then field order

Status: built · Intents: IN-0001

- Given the models `TodoList` then `Todo`, each with one error. When the checks run, then
  the `TodoList` finding comes before the `Todo` finding. (assumed)
- Given no models and no rules, when the checks run, then there are no findings.

### CAP-checks.R13 · A sort or a view that the store cannot serve fails the build

Status: built · Intents: IN-0001

Wide text is never indexed, so a list cannot sort by it. A list view must be a predicate
that the store can turn into a filter: a bool field, `not` of one, or an equality.

- Given the override `listSort <@ fun t -> t.Notes @> Ascending`. When the checks run,
  then the findings hold the error `WRONG_CONFIG_TYPE`. The message is "Cannot sort Todo
  by Notes: wide text is never indexed."
- Given the view `listView "Long" <@ fun t -> t.Title.Length > 3 @>`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message starts with
  "Unsupported list view for Todo:". (assumed)
- Given the view `listView "High" <@ fun t -> t.Priority = High @>`, when the checks run,
  then there is no such finding.

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

### CAP-access

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

## Requirements

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
````

### CAP-persist

````
# CAP-persist · Stores and loads records

Status: active

## What it is for

This capability writes the schema, the indexes, and the queries for each model. It stores
and loads records with an owner filter on every query. It pages lists by keyset. It
deletes children with their parent in one transaction. It refuses a stale save. It emits
a migration when the stored shape changes. The stored shape lives in `build/shape.json`
(assumed).

The checks of this capability run on SQLite in memory, as `DES-test-database` says. The
SQL that the capability writes targets SQL Server.

## Code map

- src/Typed.Engine/Persist.fs

## Requirements

### CAP-persist.R1 · Each model gets a table with its system columns

Status: verified · Intents: IN-0001

- Given the model `Todo` and its access rule. When the engine writes the schema, then the
  schema holds `CREATE TABLE [Todo]` with the columns `[Id] uniqueidentifier NOT NULL
  PRIMARY KEY`, `[Owner] uniqueidentifier NOT NULL`, `[Version] rowversion NOT NULL`,
  `[CreatedAt] datetimeoffset NOT NULL`, and `[UpdatedAt] datetimeoffset NOT NULL`.
- Given the model `User` with the rule `self`, when the engine writes the schema, then
  the table `[User]` has no `[Owner]` column.
- Given the model `Todo`. When the engine writes the schema, then `[Title]` is
  `nvarchar(200) NOT NULL`, `[Notes]` is `nvarchar(max) NULL`, `[Priority]` is
  `varchar(16) NOT NULL`, `[Due]` is `date NULL`, and `[CompletedAt]` is `datetimeoffset
  NULL`.
- Given the model `Todo` with `listSort` on `Due`, when the engine writes the schema, then
  the table has the column `[HasDue] AS (CASE WHEN [Due] IS NULL THEN 0 ELSE 1 END)
  PERSISTED`.
- Given the override `column <@ fun u -> u.Subject @> "ExternalSubject"`. When the engine
  writes the schema, then the column is `[ExternalSubject] nvarchar(255) NOT NULL`.
- Given the override `sensitive <@ fun u -> u.Subject @>`, when the engine writes the
  schema, then the schema does not change.

### CAP-persist.R2 · Foreign keys are indexed and never cascade

Status: verified · Intents: IN-0001

- Given the model `Todo`, when the engine writes the schema, then the schema holds
  `ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES
  [TodoList] ([Id]);`.
- Given the three models of the todo app, when the engine writes the schema, then the
  text "CASCADE" does not appear.
- Given the model `Todo`, when the engine derives the indexes, then one index starts with
  the column `List`.

### CAP-persist.R3 · Each list query gets one index

Status: verified · Intents: IN-0001

The index columns are the owner, then the filter fields of the view, then the sort fields,
then `Id`. An optional sort field is led by its `Has` column. Wide text is never indexed.
A unique semantic type gets a unique index.

- Given the model `TodoList` with its two list views, when the engine derives the indexes,
  then one index has the columns `Owner`, `Archived`, `Name`, `Id`.
- Given the model `Todo` with `listSort` on `Due`, when the engine derives the indexes,
  then one index has the columns `Owner`, `List`, `HasDue`, `Due`, `Id`.
- Given the model `User`, when the engine derives the indexes, then there is a unique
  index on `Subject`, and no list index.
- Given an owned model with a title field, no `listSort`, and no `listView`. When the
  engine derives the indexes, then one index has the columns `Owner`, the title field,
  `Id`. (assumed)

### CAP-persist.R4 · Every list query filters by owner and names its columns

Status: verified · Intents: IN-0001

- Given the model `Todo` and its overrides, when the engine writes the list query, then
  the query starts with `SELECT TOP (@limit) [Id], [Title], [Due], [Priority], [Done]
  FROM [Todo]`.
- Given the model `Todo`, when the engine writes the list query, then the `WHERE` clause
  holds `[Owner] = @owner` and `[List] = @parent`.
- Given the model `Todo`, when the engine writes the list query, then the query ends with
  `ORDER BY [HasDue] DESC, [Due] ASC, [Id] ASC`.
- Given the model `TodoList` and the view "Archived", when the engine writes the list
  query, then the `WHERE` clause holds `[Archived] = 1`.
- Given the model `TodoList` and no `listColumns`, when the engine writes the list query,
  then the columns are `[Id], [Name], [Archived]`, and no wide text column.

### CAP-persist.R5 · Keyset paging returns each record one time

Status: verified · Intents: IN-0001

- Given 25 todos of one user in one list, with 6 empty due dates and the rest spread over
  3 dates. When a caller pages through the list 4 at a time, then the caller gets each of
  the 25 todos one time. The caller gets no other todo.
- Given the same 25 todos. When the caller asks for a page of size 1 and a page of size
  200, then each page runs one query.
- Given a page size of 300. When a caller asks for a page, then the result is the error
  `VALIDATION_FAILED` on `limit` with the message "must be between 1 and 200". (assumed)
- Given a page size of 0, when a caller asks for a page, then the result is the same error.
- Given a page size of 200, when a caller asks for a page, then the page holds up to 200.
- Given no page size, when a caller asks for a page, then the engine uses the size 50.
  (assumed)
- Given the cursor "not-a-cursor". When a caller asks for the next page, then the result
  is the error `VALIDATION_FAILED` on `cursor` with the message "is not valid". (assumed)

### CAP-persist.R6 · Deleting a parent deletes its children in one transaction

Status: verified · Intents: IN-0001

- Given a list with 3 todos, when a caller removes the list, then the list and its 3
  todos are gone.
- Given a list with 3 todos, and a failure after the second todo is deleted. When a caller
  removes the list, then the list and all 3 todos still exist.
- Given a list owned by `u2`, when `u1` removes the list, then the result is the error
  `NOT_FOUND`, and the list still exists.

### CAP-persist.R7 · A stale save fails with a conflict

Status: verified · Intents: IN-0001

- Given a todo at version 3. When a caller saves the todo with version 3, then the save
  passes and the todo is at version 4.
- Given a todo at version 4. When a caller saves the todo with version 3, then the result
  is the error `STALE_VERSION`, and the stored todo does not change.
- Given a new todo, when a caller adds it, then the stored todo is at version 1. (assumed)
- Given a todo at version 3. When a caller saves the todo with no version, then the result
  is the error `VALIDATION_FAILED` on `Version` with the message "is required". (assumed)

### CAP-persist.R8 · A migration never drops a column without a note

Status: verified · Intents: IN-0001

- Given the last shape of `Todo` with the column `Due`, and a new shape with the column
  `DueDate` and no `renamedFrom`. When the engine writes the migration, then the result is
  the error `UNPLANNED_DROP` with the message "Todo.Due would be dropped. Add renamedFrom
  or an explicit drop."
- Given the same shapes and the note `renamedFrom "Due"` on `DueDate`, when the engine
  writes the migration, then the migration holds `EXEC sp_rename 'Todo.Due', 'DueDate',
  'COLUMN';`.
- Given the last shape of `Todo` and a new shape with one more optional column `Tags`.
  When the engine writes the migration, then the migration holds `ALTER TABLE [Todo] ADD
  [Tags] nvarchar(200) NULL`.
- Given a new shape with one more required text column `Code`. When the engine writes the
  migration, then the result is the error `UNSAFE_MIGRATION`. The message is "Todo.Code is
  required and has no default, so existing rows would fail." (assumed)
- Given no stored shape. When the engine writes the migration, then the migration is the
  full schema, and the engine stores the new shape. (assumed)
- Given a stored shape that is not valid JSON. When the engine reads the shape, then the
  result is the error `SHAPE_UNREADABLE`. The message is "build/shape.json cannot be read.
  Delete it to start from an empty shape." (assumed)
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

## The code

### Diff against pdlc/CH-0004-persist

````
diff --git a/src/Typed.Engine/Checks.fs b/src/Typed.Engine/Checks.fs
new file mode 100644
index 0000000..660c231
--- /dev/null
+++ b/src/Typed.Engine/Checks.fs
@@ -0,0 +1,212 @@
+module Typed.Engine.Checks
+
+open System
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+open Typed.Engine.Access
+
+let private make (severity: Severity) (code: ErrorCode) (model: string) (field: string option) (message: string) : Finding =
+    { Severity = severity
+      Code = code
+      Model = model
+      Field = field
+      Message = message }
+
+let private error code model field message = make Severity.Error code model field message
+let private warning code model field message = make Severity.Warning code model field message
+
+/// A finding with its place in the field order of its model.
+type private Placed = int * Finding
+
+/// The name of the kind of an item that may appear once for each field.
+let private singleKind (item: OverrideItem) : (string * string) option =
+    match item with
+    | Label(f, _) -> Some("label", f)
+    | HiddenInForm f -> Some("hiddenInForm", f)
+    | ReadOnly f -> Some("readOnly", f)
+    | ListEdit f -> Some("listEdit", f)
+    | Sensitive f -> Some("sensitive", f)
+    | Column(f, _) -> Some("column", f)
+    | _ -> None
+
+let private accessChecks (models: Model list) (rules: AccessRule list) (model: Model) : Placed list =
+    let own = rules |> List.filter (fun r -> r.ModelName = model.Name)
+    let count =
+        match own with
+        | [] -> [ -1, error MISSING_ACCESS_RULE model.Name None (sprintf "Model %s has no access rule." model.Name) ]
+        | [ _ ] -> []
+        | _ -> [ -1, error MISSING_ACCESS_RULE model.Name None (sprintf "Model %s has more than one access rule." model.Name) ]
+    let wrong (field: string option) message = -1, error WRONG_CONFIG_TYPE model.Name field message
+    let ofRule (r: AccessRule) : Placed list =
+        match r.Access with
+        | OwnedVia(reference, parent, readOnlyWhen) ->
+            let isRef =
+                model.Fields
+                |> List.exists (fun f -> f.Name = reference && refTarget f.Kind = Some parent)
+            let refFinding =
+                if isRef then
+                    []
+                else
+                    [ wrong (Some reference) (sprintf "ownedVia %s.%s must be a reference to %s." model.Name reference parent) ]
+            let roFinding =
+                match readOnlyWhen with
+                | Some(roModel, roField) ->
+                    let isBool =
+                        models
+                        |> List.tryFind (fun m -> m.Name = roModel)
+                        |> Option.bind (fun m -> m.Fields |> List.tryFind (fun f -> f.Name = roField))
+                        |> Option.exists (fun f -> innerKind f.Kind = Boolean)
+                    if isBool then
+                        []
+                    else
+                        [ wrong None (sprintf "readOnlyWhenParent names %s.%s, which is not a bool field." roModel roField) ]
+                | None -> []
+            refFinding @ roFinding
+        | _ -> []
+    count @ (own |> List.collect ofRule)
+
+let private referenceChecks
+    (names: string list)
+    (models: Model list)
+    (setsOf: string -> OverrideItem list)
+    (model: Model)
+    : Placed list =
+    model.Fields
+    |> List.mapi (fun i f -> i, f)
+    |> List.collect (fun (i, f) ->
+        match refTarget f.Kind with
+        | None -> []
+        | Some target when not (List.contains target names) ->
+            [ i,
+              error
+                  DANGLING_REFERENCE
+                  model.Name
+                  (Some f.Name)
+                  (sprintf "Field %s.%s refers to %s, which is not a model." model.Name f.Name target) ]
+        | Some target ->
+            match models |> List.tryFind (fun m -> m.Name = target) with
+            | Some t when (titleFieldName t.Fields (setsOf target)).IsNone ->
+                [ i,
+                  error
+                      NO_TITLE_FIELD
+                      model.Name
+                      (Some f.Name)
+                      (sprintf "%s has no title field, but %s.%s refers to it." target model.Name f.Name) ]
+            | _ -> [])
+
+let private itemChecks (model: Model) (items: OverrideItem list) : Placed list =
+    let indexOf (name: string) =
+        model.Fields |> List.tryFindIndex (fun f -> f.Name = name)
+    let find (name: string) = model.Fields |> List.tryFind (fun f -> f.Name = name)
+    let last = Int32.MaxValue
+    let fieldChecks (item: OverrideItem) : Placed list =
+        let placed (name: string) = defaultArg (indexOf name) last
+        let named =
+            match item with
+            | ListView _ -> []
+            | _ -> fieldsOf item
+        let system, other = named |> List.partition (fun f -> List.contains f systemFields)
+        let systemFindings =
+            system
+            |> List.map (fun f ->
+                last,
+                error SYSTEM_FIELD_TARGET model.Name (Some f) (sprintf "Override targets system field %s.%s." model.Name f))
+        let missing =
+            other
+            |> List.filter (fun f -> (find f).IsNone)
+            |> List.map (fun f ->
+                last, error MISSING_FIELD model.Name (Some f) (sprintf "Override names missing field %s.%s." model.Name f))
+        let typed =
+            match item with
+            | Rule(f, predicateType, _, _) ->
+                match find f with
+                | Some field when field.Core <> predicateType ->
+                    [ placed f,
+                      error
+                          WRONG_CONFIG_TYPE
+                          model.Name
+                          (Some f)
+                          (sprintf
+                              "Rule %s.%s expects a predicate of type %s, but the predicate takes %s."
+                              model.Name
+                              f
+                              (displayType field.Core)
+                              (displayType predicateType)) ]
+                | _ -> []
+            | ListSort(f, _) ->
+                match find f with
+                | Some field when isWide field.Kind ->
+                    [ placed f,
+                      error
+                          WRONG_CONFIG_TYPE
+                          model.Name
+                          (Some f)
+                          (sprintf "Cannot sort %s by %s: wide text is never indexed." model.Name f) ]
+                | _ -> []
+            | ListView(_, filter) ->
+                match Persist.viewToSql items filter with
+                | Result.Error reason ->
+                    [ last, error WRONG_CONFIG_TYPE model.Name None (sprintf "Unsupported list view for %s: %s" model.Name reason) ]
+                | Ok _ -> []
+            | Label(f, text) ->
+                match find f with
+                | Some field when field.Label = text ->
+                    [ placed f,
+                      warning
+                          REDUNDANT_OVERRIDE
+                          model.Name
+                          (Some f)
+                          (sprintf "Override %s.%s label matches the default." model.Name f) ]
+                | _ -> []
+            | Unsafe(name, reason) ->
+                [ last,
+                  warning
+                      UNSAFE_ESCAPE_HATCH
+                      model.Name
+                      (Some name)
+                      (sprintf "Unsafe escape hatch %s.%s: %s" model.Name name reason) ]
+            | _ -> []
+        systemFindings @ missing @ typed
+    let duplicates =
+        items
+        |> List.choose singleKind
+        |> List.countBy id
+        |> List.filter (fun (_, n) -> n > 1)
+        |> List.map (fun ((kind, f), _) ->
+            defaultArg (indexOf f) last,
+            error WRONG_CONFIG_TYPE model.Name (Some f) (sprintf "%s.%s has two %s overrides." model.Name f kind))
+    (items |> List.collect fieldChecks) @ duplicates
+
+/// Every finding, errors and warnings, in model order then field order.
+let run
+    (defaults: TypeDefault list)
+    (types: Type list)
+    (rules: AccessRule list)
+    (sets: ModelOverrides list)
+    : Finding list =
+    let described = types |> List.map (describe defaults)
+    let models = described |> List.choose (function Ok m -> Some m | Result.Error _ -> None)
+    let names = types |> List.map (fun t -> t.Name)
+    let setsOf (name: string) =
+        sets |> List.filter (fun s -> s.ModelName = name) |> List.collect (fun s -> s.Items)
+    let ofModel (model: Model) : Finding list =
+        accessChecks models rules model @ referenceChecks names models setsOf model @ itemChecks model (setsOf model.Name)
+        |> List.sortBy fst
+        |> List.map snd
+    let perType =
+        described
+        |> List.collect (function
+            | Ok m -> ofModel m
+            | Result.Error fs -> fs)
+    let ghosts =
+        sets
+        |> List.filter (fun s -> not (List.contains s.ModelName names))
+        |> List.map (fun s ->
+            error MISSING_FIELD s.ModelName None (sprintf "Overrides name %s, which is not a model." s.ModelName))
+    perType @ ghosts
+
+let errors (findings: Finding list) : Finding list =
+    findings |> List.filter (fun f -> f.Severity = Severity.Error)
+
+let warnings (findings: Finding list) : Finding list =
+    findings |> List.filter (fun f -> f.Severity = Severity.Warning)
diff --git a/src/Typed.Engine/Typed.Engine.fsproj b/src/Typed.Engine/Typed.Engine.fsproj
index 39cd82c..e6c4598 100644
--- a/src/Typed.Engine/Typed.Engine.fsproj
+++ b/src/Typed.Engine/Typed.Engine.fsproj
@@ -10,6 +10,7 @@
     <Compile Include="Access.fs" />
     <Compile Include="Validate.fs" />
     <Compile Include="Persist.fs" />
+    <Compile Include="Checks.fs" />
   </ItemGroup>
 
 </Project>
````

### src/Typed.Engine/Checks.fs

````
module Typed.Engine.Checks

open System
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access

let private make (severity: Severity) (code: ErrorCode) (model: string) (field: string option) (message: string) : Finding =
    { Severity = severity
      Code = code
      Model = model
      Field = field
      Message = message }

let private error code model field message = make Severity.Error code model field message
let private warning code model field message = make Severity.Warning code model field message

/// A finding with its place in the field order of its model.
type private Placed = int * Finding

/// The name of the kind of an item that may appear once for each field.
let private singleKind (item: OverrideItem) : (string * string) option =
    match item with
    | Label(f, _) -> Some("label", f)
    | HiddenInForm f -> Some("hiddenInForm", f)
    | ReadOnly f -> Some("readOnly", f)
    | ListEdit f -> Some("listEdit", f)
    | Sensitive f -> Some("sensitive", f)
    | Column(f, _) -> Some("column", f)
    | _ -> None

let private accessChecks (models: Model list) (rules: AccessRule list) (model: Model) : Placed list =
    let own = rules |> List.filter (fun r -> r.ModelName = model.Name)
    let count =
        match own with
        | [] -> [ -1, error MISSING_ACCESS_RULE model.Name None (sprintf "Model %s has no access rule." model.Name) ]
        | [ _ ] -> []
        | _ -> [ -1, error MISSING_ACCESS_RULE model.Name None (sprintf "Model %s has more than one access rule." model.Name) ]
    let wrong (field: string option) message = -1, error WRONG_CONFIG_TYPE model.Name field message
    let ofRule (r: AccessRule) : Placed list =
        match r.Access with
        | OwnedVia(reference, parent, readOnlyWhen) ->
            let isRef =
                model.Fields
                |> List.exists (fun f -> f.Name = reference && refTarget f.Kind = Some parent)
            let refFinding =
                if isRef then
                    []
                else
                    [ wrong (Some reference) (sprintf "ownedVia %s.%s must be a reference to %s." model.Name reference parent) ]
            let roFinding =
                match readOnlyWhen with
                | Some(roModel, roField) ->
                    let isBool =
                        models
                        |> List.tryFind (fun m -> m.Name = roModel)
                        |> Option.bind (fun m -> m.Fields |> List.tryFind (fun f -> f.Name = roField))
                        |> Option.exists (fun f -> innerKind f.Kind = Boolean)
                    if isBool then
                        []
                    else
                        [ wrong None (sprintf "readOnlyWhenParent names %s.%s, which is not a bool field." roModel roField) ]
                | None -> []
            refFinding @ roFinding
        | _ -> []
    count @ (own |> List.collect ofRule)

let private referenceChecks
    (names: string list)
    (models: Model list)
    (setsOf: string -> OverrideItem list)
    (model: Model)
    : Placed list =
    model.Fields
    |> List.mapi (fun i f -> i, f)
    |> List.collect (fun (i, f) ->
        match refTarget f.Kind with
        | None -> []
        | Some target when not (List.contains target names) ->
            [ i,
              error
                  DANGLING_REFERENCE
                  model.Name
                  (Some f.Name)
                  (sprintf "Field %s.%s refers to %s, which is not a model." model.Name f.Name target) ]
        | Some target ->
            match models |> List.tryFind (fun m -> m.Name = target) with
            | Some t when (titleFieldName t.Fields (setsOf target)).IsNone ->
                [ i,
                  error
                      NO_TITLE_FIELD
                      model.Name
                      (Some f.Name)
                      (sprintf "%s has no title field, but %s.%s refers to it." target model.Name f.Name) ]
            | _ -> [])

let private itemChecks (model: Model) (items: OverrideItem list) : Placed list =
    let indexOf (name: string) =
        model.Fields |> List.tryFindIndex (fun f -> f.Name = name)
    let find (name: string) = model.Fields |> List.tryFind (fun f -> f.Name = name)
    let last = Int32.MaxValue
    let fieldChecks (item: OverrideItem) : Placed list =
        let placed (name: string) = defaultArg (indexOf name) last
        let named =
            match item with
            | ListView _ -> []
            | _ -> fieldsOf item
        let system, other = named |> List.partition (fun f -> List.contains f systemFields)
        let systemFindings =
            system
            |> List.map (fun f ->
                last,
                error SYSTEM_FIELD_TARGET model.Name (Some f) (sprintf "Override targets system field %s.%s." model.Name f))
        let missing =
            other
            |> List.filter (fun f -> (find f).IsNone)
            |> List.map (fun f ->
                last, error MISSING_FIELD model.Name (Some f) (sprintf "Override names missing field %s.%s." model.Name f))
        let typed =
            match item with
            | Rule(f, predicateType, _, _) ->
                match find f with
                | Some field when field.Core <> predicateType ->
                    [ placed f,
                      error
                          WRONG_CONFIG_TYPE
                          model.Name
                          (Some f)
                          (sprintf
                              "Rule %s.%s expects a predicate of type %s, but the predicate takes %s."
                              model.Name
                              f
                              (displayType field.Core)
                              (displayType predicateType)) ]
                | _ -> []
            | ListSort(f, _) ->
                match find f with
                | Some field when isWide field.Kind ->
                    [ placed f,
                      error
                          WRONG_CONFIG_TYPE
                          model.Name
                          (Some f)
                          (sprintf "Cannot sort %s by %s: wide text is never indexed." model.Name f) ]
                | _ -> []
            | ListView(_, filter) ->
                match Persist.viewToSql items filter with
                | Result.Error reason ->
                    [ last, error WRONG_CONFIG_TYPE model.Name None (sprintf "Unsupported list view for %s: %s" model.Name reason) ]
                | Ok _ -> []
            | Label(f, text) ->
                match find f with
                | Some field when field.Label = text ->
                    [ placed f,
                      warning
                          REDUNDANT_OVERRIDE
                          model.Name
                          (Some f)
                          (sprintf "Override %s.%s label matches the default." model.Name f) ]
                | _ -> []
            | Unsafe(name, reason) ->
                [ last,
                  warning
                      UNSAFE_ESCAPE_HATCH
                      model.Name
                      (Some name)
                      (sprintf "Unsafe escape hatch %s.%s: %s" model.Name name reason) ]
            | _ -> []
        systemFindings @ missing @ typed
    let duplicates =
        items
        |> List.choose singleKind
        |> List.countBy id
        |> List.filter (fun (_, n) -> n > 1)
        |> List.map (fun ((kind, f), _) ->
            defaultArg (indexOf f) last,
            error WRONG_CONFIG_TYPE model.Name (Some f) (sprintf "%s.%s has two %s overrides." model.Name f kind))
    (items |> List.collect fieldChecks) @ duplicates

/// Every finding, errors and warnings, in model order then field order.
let run
    (defaults: TypeDefault list)
    (types: Type list)
    (rules: AccessRule list)
    (sets: ModelOverrides list)
    : Finding list =
    let described = types |> List.map (describe defaults)
    let models = described |> List.choose (function Ok m -> Some m | Result.Error _ -> None)
    let names = types |> List.map (fun t -> t.Name)
    let setsOf (name: string) =
        sets |> List.filter (fun s -> s.ModelName = name) |> List.collect (fun s -> s.Items)
    let ofModel (model: Model) : Finding list =
        accessChecks models rules model @ referenceChecks names models setsOf model @ itemChecks model (setsOf model.Name)
        |> List.sortBy fst
        |> List.map snd
    let perType =
        described
        |> List.collect (function
            | Ok m -> ofModel m
            | Result.Error fs -> fs)
    let ghosts =
        sets
        |> List.filter (fun s -> not (List.contains s.ModelName names))
        |> List.map (fun s ->
            error MISSING_FIELD s.ModelName None (sprintf "Overrides name %s, which is not a model." s.ModelName))
    perType @ ghosts

let errors (findings: Finding list) : Finding list =
    findings |> List.filter (fun f -> f.Severity = Severity.Error)

let warnings (findings: Finding list) : Finding list =
    findings |> List.filter (fun f -> f.Severity = Severity.Warning)
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
    <Compile Include="Validate.fs" />
    <Compile Include="Persist.fs" />
    <Compile Include="Checks.fs" />
  </ItemGroup>

</Project>
````

