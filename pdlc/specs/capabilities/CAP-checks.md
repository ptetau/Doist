# CAP-checks · Fails the build when a model, rule, or override is wrong

Status: active

## What it is for

This capability runs every build check over the models, the access rules, and the
override sets. Each check gives findings. An error finding fails the build. A warning
finding does not. The generator job prints the findings.

## Code map

- src/Typed.Engine/Checks.fs

## Requirements

### CAP-checks.R1 · A model with no access rule fails the build

Status: proposed · Intents: IN-0001

- Given the model `TodoList` and no access rules. When the checks run, then the findings
  hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList has no access
  rule."
- Given the model `TodoList` and two access rules for `TodoList`. When the checks run,
  then the findings hold the error `MISSING_ACCESS_RULE` with the message "Model TodoList
  has more than one access rule."
- Given the three models of the todo app and the three access rules of the todo app.
  When the checks run, then there is no `MISSING_ACCESS_RULE` finding.

### CAP-checks.R2 · An override that names a missing field fails the build

Status: proposed · Intents: IN-0001

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

Status: proposed · Intents: IN-0001

- Given the override `rule <@ fun t -> t.Due @>` whose predicate takes a `string`, when
  the checks run, then the findings hold the error `WRONG_CONFIG_TYPE`. The message
  names `Todo.Due`, the expected type, and the type of the field.
- Given the override `rule <@ fun t -> t.Title @>` whose predicate takes a `Title`, when
  the checks run, then there is no `WRONG_CONFIG_TYPE` finding.

### CAP-checks.R4 · An override that targets a system field fails the build

Status: proposed · Intents: IN-0001

- Given a `Column` item for the field "Version" of `Todo`. When the checks run, then the
  findings hold the error `SYSTEM_FIELD_TARGET` with the message "Override targets system
  field Todo.Version."

### CAP-checks.R5 · A reference to a model that does not exist fails the build

Status: proposed · Intents: IN-0001

- Given the model `Todo` alone, with its field `List: Ref<TodoList>`. When the checks run,
  then the findings hold the error `DANGLING_REFERENCE` with the message "Field Todo.List
  refers to TodoList, which is not a model."

### CAP-checks.R6 · A referenced model with no title field fails the build

Status: proposed · Intents: IN-0001

- Given a model `NoTitle` with one field `Count: int`, and a model `Child` with the field
  `Parent: Ref<NoTitle>`, and no `titleField` override. When the checks run, then the
  findings hold the error `NO_TITLE_FIELD` with the message "NoTitle has no title field,
  but Child.Parent refers to it."
- Given the same two models and the override `titleField <@ fun n -> n.Count @>` for
  `NoTitle`, when the checks run, then there is no `NO_TITLE_FIELD` finding.

### CAP-checks.R7 · An `ownedVia` rule must name a reference to its parent

Status: proposed · Intents: IN-0001

- Given the access rule `ownedVia <@ fun t -> t.Title @>` for `Todo`. When the checks
  run, then the findings hold the error `WRONG_CONFIG_TYPE` with the message "ownedVia
  Todo.Title must be a reference to TodoList."
- Given the rule `readOnlyWhenParent <@ fun l -> l.Name @>` on the `ownedVia` rule of
  `Todo`. When the checks run, then the findings hold the error `WRONG_CONFIG_TYPE` with
  the message "readOnlyWhenParent names TodoList.Name, which is not a bool field."

### CAP-checks.R8 · Each `unsafe` escape hatch is a warning that gives its reason

Status: proposed · Intents: IN-0001

- Given the override `unsafe "rawName" "uses raw SQL"` for `TodoList`. When the checks
  run, then the findings hold no error, and one warning with the message "Unsafe escape
  hatch TodoList.rawName: uses raw SQL".

### CAP-checks.R9 · An override that matches the default is a warning

Status: proposed · Intents: IN-0001

- Given the override `label <@ fun t -> t.Due @> "Due"` for `Todo`, when the checks run,
  then the findings hold one warning with the message "Override Todo.Due label matches
  the default." (assumed)
- Given the override `label <@ fun t -> t.Due @> "Due date"` for `Todo`, when the checks
  run, then there is no such warning.

### CAP-checks.R10 · The todo app passes every check

Status: proposed · Intents: IN-0001

- Given the three models, the three access rules, and the two override sets of the todo
  app. When the checks run, then there are no error findings.
