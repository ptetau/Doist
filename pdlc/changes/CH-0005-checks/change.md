# CH-0005 · Checks

Intent: IN-0001 · Scope: CAP-checks · Status: in review · Ticket: none · Branch: pdlc/CH-0005-checks · Base: pdlc/CH-0004-persist
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
- Verified: all remits pass. Filed IN-0015 (stray access rule) as a follow-up.
