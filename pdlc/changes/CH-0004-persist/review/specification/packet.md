# Review packet · CH-0004-persist · specification

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
# CH-0004 · Persist

Intent: IN-0001 · Scope: CAP-persist · Status: building · Ticket: none · Branch: pdlc/CH-0004-persist · Base: pdlc/CH-0003-validate
Tests-Locked: bbd2344

## Delivers

The engine writes the schema, the foreign keys, the indexes, and the list queries for each model. A store adds, revises, removes, finds, and pages records with an owner filter, keyset paging, child deletes, and stale-save refusal. A migration follows the stored shape.

## Requirements

### CAP-persist.R1 · Each model gets a table with its system columns

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

- Given the model `Todo`, when the engine writes the schema, then the schema holds
  `ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES
  [TodoList] ([Id]);`.
- Given the three models of the todo app, when the engine writes the schema, then the
  text "CASCADE" does not appear.
- Given the model `Todo`, when the engine derives the indexes, then one index starts with
  the column `List`.

### CAP-persist.R3 · Each list query gets one index

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

- Given a list with 3 todos, when a caller removes the list, then the list and its 3
  todos are gone.
- Given a list with 3 todos, and a failure after the second todo is deleted. When a caller
  removes the list, then the list and all 3 todos still exist.
- Given a list owned by `u2`, when `u1` removes the list, then the result is the error
  `NOT_FOUND`, and the list still exists.

### CAP-persist.R7 · A stale save fails with a conflict

- Given a todo at version 3. When a caller saves the todo with version 3, then the save
  passes and the todo is at version 4.
- Given a todo at version 4. When a caller saves the todo with version 3, then the result
  is the error `STALE_VERSION`, and the stored todo does not change.
- Given a new todo, when a caller adds it, then the stored todo is at version 1. (assumed)
- Given a todo at version 3. When a caller saves the todo with no version, then the result
  is the error `VALIDATION_FAILED` on `Version` with the message "is required". (assumed)

### CAP-persist.R8 · A migration never drops a column without a note

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

## Relies on

- CAP-models (used, not changed)
- CAP-access (used, not changed)
- DES-test-database (SQLite double)
- DES-names
- DES-errors
- DES-writing-style
- DES-pdlc-check-tagging

## Interface

In `src/Typed.Engine/Persist.fs`, module `Typed.Engine.Persist`:

Schema and queries (pure):

- `type Dialect = SqlServer | Sqlite`. The checks pass `Sqlite`. Every acceptance line that quotes SQL uses `SqlServer`. (assumed)
- `type IndexDef = { Name: string; Table: string; Columns: string list; Unique: bool }`
- `createTable: Dialect -> Model -> OverrideItem list -> Access -> string`
- `foreignKeys: Model -> OverrideItem list -> Access -> string list`
- `indexes: Model -> OverrideItem list -> Access -> IndexDef list`
- `schema: Dialect -> (Model * OverrideItem list * Access) list -> string`
- `sortKeys: Model -> OverrideItem list -> (string * Direction) list`
- `viewToSql: OverrideItem list -> Expr -> Result<string * (string * obj) list, string>`: a list view as a `WHERE` clause with its literals, or the reason that the view is not supported. CAP-checks (build checks) calls this. (assumed)
- `type ListQuery = { Sql: string; Keys: (string * Direction) list; Literals: (string * obj) list }`
- `listQuery: Dialect -> Model -> OverrideItem list -> Access -> view: string option -> afterCursor: bool -> ListQuery`

The store (runs SQL):

- `type Store`: made by `Store.create (conn: System.Data.Common.DbConnection) (dialect: Dialect) (now: unit -> DateTimeOffset) (onBeforeChildDelete: (int -> unit) option)`. The four arguments are curried. Pass `None` for no hook.
- `Record.Values` holds each field in its stored form (assumed). An empty optional field has no key. The forms are these:
  - text, and a semantic text type: `string`
  - an enum: its case name, as `string`
  - a reference: `Guid`
  - a boolean: `bool`
  - a date: `DateOnly`
  - a timestamp: `DateTimeOffset`
  - a whole number: `int64`
  - a decimal: `decimal`
- `Shape` and `Rename` are F# records, so two equal shapes compare equal. `migration None next renames` gives exactly `schema SqlServer` for the models of `next`. The migration always writes SQL Server. (assumed)
- `type Page = { Rows: Record list; Next: string option }`
- `find: Store -> Scope -> Model -> Guid -> Result<Record, Finding>` · errors: `NOT_FOUND`
- `page: Store -> Scope -> Model -> OverrideItem list -> Access -> view: string option -> parent: Guid option -> cursor: string option -> size: int option -> Result<Page, Finding>` · errors: `VALIDATION_FAILED` with `Field = Some "limit"` or `Some "cursor"`
- `add: Store -> Scope -> Model -> Record -> Result<Record, Finding>`: sets `Id`, `Owner`, `Version = 1`, `CreatedAt`, `UpdatedAt`.
- `revise: Store -> Scope -> Model -> id: Guid -> version: int64 option -> Record -> Result<Record, Finding>` · errors: `STALE_VERSION`, `VALIDATION_FAILED` with `Field = Some "Version"`, `NOT_FOUND`
- `remove: Store -> Scope -> Model -> children: (Model * string) list -> id: Guid -> Result<unit, Finding>`: `children` gives each child model with its reference field. One transaction. · errors: `NOT_FOUND`
- `queryCount: Store -> int`: how many SQL commands the store has run. The paging check reads it. (assumed)

Migrations:

- `type Shape`: the stored shape of every table, as JSON. `shapeOf: (Model * OverrideItem list * Access) list -> Shape`
- `readShape: path: string -> Result<Shape option, Finding>` · errors: `SHAPE_UNREADABLE`. No file gives `None`.
- `writeShape: path: string -> Shape -> unit`
- `type Rename = { Model: string; From: string; To: string }`: the `renamedFrom` note. (assumed)
- `migration: previous: Shape option -> next: Shape -> Rename list -> Result<string, Finding>` · errors: `UNPLANNED_DROP`, `UNSAFE_MIGRATION`
- Where the shape is stored: `migration` is pure. The caller that gets `Ok` from it writes `next` with `writeShape path next`, and the next run reads it back with `readShape path`. That pair is the store step of CAP-persist.R8 (migration drop). The generator job is that caller. (assumed)

The checks are xUnit `[<Fact>]` functions in F#. Put one `module` for each requirement. Name it with the requirement ID, with a hyphen in place of the dot, in double backticks: ``module ``CAP-models-R1`` ``. F# does not allow a dot in a module name. Start each check name with the ID, as ``let ``CAP-models.R1 Title has kind text 200`` () = ...``. The checks project is `tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj` (xUnit, .NET 8). The first change makes that project. Later changes add their check file to its `Compile` list.

### Test seams

- clock: `Store.create ... now` takes the time function.
- store: `Store.create` takes any `DbConnection`. The checks pass an open `Microsoft.Data.Sqlite` connection to `Data Source=:memory:` with `Dialect.Sqlite`. The schema for SQLite comes from `schema Sqlite ...`.
- failure inside the delete transaction: `Store.create` takes an optional `onBeforeChildDelete: (int -> unit)`. The store calls it before it deletes each child, with the count of children deleted so far. A check raises from it to prove CAP-persist.R6 (child deletes). (assumed)
- paths: `readShape` and `writeShape` take the path. The checks use a temporary folder.

## Files

- src/Typed.Engine/Persist.fs
- src/Typed.Engine/Typed.Engine.fsproj: the Compile entry and the package reference of this spec

## Check files

- tests/Typed.Engine.Tests/CAP-persist.Tests.fs
- tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj

## Steps

### Step 1 · Each model gets a table with its system columns · [x]

- Requirement: CAP-persist.R1
- Check: the checks named `CAP-persist.R1 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R1 (each model gets a table with its system columns) passes

### Step 2 · Foreign keys are indexed and never cascade · [x]

- Requirement: CAP-persist.R2
- Check: the checks named `CAP-persist.R2 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R2 (foreign keys are indexed and never cascade) passes

### Step 3 · Each list query gets one index · [x]

- Requirement: CAP-persist.R3
- Check: the checks named `CAP-persist.R3 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R3 (each list query gets one index) passes

### Step 4 · Every list query filters by owner and names its columns · [x]

- Requirement: CAP-persist.R4
- Check: the checks named `CAP-persist.R4 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R4 (every list query filters by owner and names its columns) passes

### Step 5 · Keyset paging returns each record one time · [x]

- Requirement: CAP-persist.R5
- Check: the checks named `CAP-persist.R5 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R5 (keyset paging returns each record one time) passes

### Step 6 · Deleting a parent deletes its children in one transaction · [x]

- Requirement: CAP-persist.R6
- Check: the checks named `CAP-persist.R6 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R6 (deleting a parent deletes its children in one transaction) passes

### Step 7 · A stale save fails with a conflict · [x]

- Requirement: CAP-persist.R7
- Check: the checks named `CAP-persist.R7 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R7 (a stale save fails with a conflict) passes

### Step 8 · A migration never drops a column without a note · [x]

- Requirement: CAP-persist.R8
- Check: the checks named `CAP-persist.R8 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R8 (a migration never drops a column without a note) passes

## Manual checks

none

## Progress log

- Planned by the change stage of IN-0001 (engine core). The requirements are copied from `CAP-persist` as the ready stage left them.
- 2026-10-19T06:33:28+00:00 · Tests: the test writer wrote 47 checks in `CAP-persist.Tests.fs`, on SQLite in
  memory. The Interface settled the curried `Store.create`. It also settled the stored
  forms in `Record.Values`, and the record shapes. Red: the checks fail to compile, because
  `Typed.Engine.Persist` does not exist yet. Locked at 1e2b637.
- 2026-10-19T06:35:42+00:00 · Tests round 1: the review failed on a helper that could loop forever, and on a
  page-of-200 line with no proof. The test writer bounded the loop and added a 210-row
  check. Still red: `Typed.Engine.Persist` does not exist. Locked again at 81284e2.
- 2026-10-19T06:38:04+00:00 · Tests round 2: the review failed on a system-column check that read the whole
  schema. It also failed on the store step of CAP-persist.R8 (migration drop), which had no
  proof. The
  Interface now names `writeShape` as that step. The test writer fixed both. Locked again
  at bbd2344.
- 2026-10-19T06:46:50+00:00 · Build: the builder wrote `Persist.fs`. All 139 checks pass. The check files did
  not change after the lock. Commit `066a033`. The builder named six points where the
  Interface was silent. They are in the handoff for the verify reviewers.
````

## The spec it covers: title and summary

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
````

## The requirements, in full

### CAP-persist.R1 · Each model gets a table with its system columns

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

- Given the model `Todo`, when the engine writes the schema, then the schema holds
  `ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES
  [TodoList] ([Id]);`.
- Given the three models of the todo app, when the engine writes the schema, then the
  text "CASCADE" does not appear.
- Given the model `Todo`, when the engine derives the indexes, then one index starts with
  the column `List`.

### CAP-persist.R3 · Each list query gets one index

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

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

Status: built · Intents: IN-0001

- Given a list with 3 todos, when a caller removes the list, then the list and its 3
  todos are gone.
- Given a list with 3 todos, and a failure after the second todo is deleted. When a caller
  removes the list, then the list and all 3 todos still exist.
- Given a list owned by `u2`, when `u1` removes the list, then the result is the error
  `NOT_FOUND`, and the list still exists.

### CAP-persist.R7 · A stale save fails with a conflict

Status: built · Intents: IN-0001

- Given a todo at version 3. When a caller saves the todo with version 3, then the save
  passes and the todo is at version 4.
- Given a todo at version 4. When a caller saves the todo with version 3, then the result
  is the error `STALE_VERSION`, and the stored todo does not change.
- Given a new todo, when a caller adds it, then the stored todo is at version 1. (assumed)
- Given a todo at version 3. When a caller saves the todo with no version, then the result
  is the error `VALIDATION_FAILED` on `Version` with the message "is required". (assumed)

### CAP-persist.R8 · A migration never drops a column without a note

Status: built · Intents: IN-0001

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

### DES-test-database

````
# DES-test-database · Which database the checks use

Status: agreed · Default: yes · Ask at init: no

## Rule

The checks of the engine library use SQLite in memory as a test double. SQL Server is
the target database, and the checks that need SQL Server run only in CI, in a SQL Server
container.

- A check of the generator, the build checks, validation, or the SQL that the engine
  writes runs in SQLite in memory. The check swaps `TOP (@n)` for `LIMIT @n` and uses a
  generated column for each `Has` column.
- A check of the web app, sign-in, sessions, the audit log, or a security rule needs SQL
  Server and a sign-in provider. Mark the check `[<RequiresSqlServer>]`. The checks
  command skips it when no `SQLSERVER_CONNECTION` setting is set.
- A change that only adds such checks is not verified until CI runs them.

## Why

This sandbox has no SQL Server and no sign-in provider. Without a test double, no check of
the engine library can run here. Without the SQL Server marker, a skipped check would look
like a pass.
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

### Diff against pdlc/CH-0003-validate

````
diff --git a/src/Typed.Engine/Persist.fs b/src/Typed.Engine/Persist.fs
new file mode 100644
index 0000000..748e56f
--- /dev/null
+++ b/src/Typed.Engine/Persist.fs
@@ -0,0 +1,967 @@
+module Typed.Engine.Persist
+
+open System
+open System.Data
+open System.Data.Common
+open System.Globalization
+open System.IO
+open System.Text
+open System.Text.Json
+open System.Text.Json.Nodes
+open Microsoft.FSharp.Quotations
+open Microsoft.FSharp.Quotations.Patterns
+open Microsoft.FSharp.Quotations.DerivedPatterns
+open Microsoft.FSharp.Reflection
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+open Typed.Engine.Access
+
+type Dialect =
+    | SqlServer
+    | Sqlite
+
+type IndexDef =
+    { Name: string
+      Table: string
+      Columns: string list
+      Unique: bool }
+
+// ---- Shape: the stored form of every table ----
+
+type ColumnShape =
+    { ColName: string
+      SqlType: string
+      Nullable: bool
+      /// The expression of a computed column, or "" for a plain column.
+      Computed: string }
+
+type ForeignKeyShape =
+    { FkName: string
+      FkColumn: string
+      FkTarget: string }
+
+type TableShape =
+    { TableName: string
+      Cols: ColumnShape list
+      ForeignKeys: ForeignKeyShape list
+      Indexes: IndexDef list }
+
+type Shape = { Tables: TableShape list }
+
+/// The `renamedFrom` note: the column To of the model replaces the column From.
+type Rename = { Model: string; From: string; To: string }
+
+// ---- Names and helpers ----
+
+let private q (name: string) : string = "[" + name + "]"
+
+let private finding (code: ErrorCode) (model: string) (field: string option) (message: string) : Finding =
+    { Severity = Severity.Error
+      Code = code
+      Model = model
+      Field = field
+      Message = message }
+
+let private notFound (model: string) : Finding =
+    finding NOT_FOUND model None (sprintf "%s was not found." model)
+
+let private colName (items: OverrideItem list) (field: string) : string =
+    items
+    |> List.tryPick (function
+        | Column(f, n) when f = field -> Some n
+        | _ -> None)
+    |> Option.defaultValue field
+
+let private hasOwner (access: Access) : bool =
+    match access with
+    | Owned
+    | OwnedVia _ -> true
+    | Self
+    | SystemOnly -> false
+
+let private sqlType (kind: Kind) : string =
+    match innerKind kind with
+    | Text n when n > 4000 -> "nvarchar(max)"
+    | Text n -> sprintf "nvarchar(%d)" n
+    | WholeNumber -> "bigint"
+    | Decimal -> "decimal(18,4)"
+    | Boolean -> "bit"
+    | Enum _ -> "varchar(16)"
+    | Date -> "date"
+    | Timestamp -> "datetimeoffset"
+    | Id
+    | Reference _ -> "uniqueidentifier"
+    | Optional _ -> "nvarchar(max)"
+
+let private sqliteType (t: string) : string =
+    if t.StartsWith "nvarchar" || t.StartsWith "varchar" || t = "date" || t = "datetimeoffset" || t = "uniqueidentifier" then
+        "text"
+    elif t = "bit" || t = "bigint" || t = "rowversion" || t = "int" then
+        "integer"
+    elif t.StartsWith "decimal" then
+        "numeric"
+    else
+        t
+
+// ---- Sort keys ----
+
+type private KeyInfo =
+    { KeyName: string
+      KeyCol: string
+      KeyDir: Direction
+      KeyKind: Kind
+      KeyNullable: bool
+      /// The field that supplies the value of the key.
+      KeyField: string
+      /// True for a `HasX` key, which is 1 when the field X has a value.
+      KeyIsFlag: bool }
+
+let private sortTarget (model: Model) (items: OverrideItem list) : (Field * Direction) option =
+    let chosen =
+        match items |> List.tryPick (function ListSort(f, d) -> Some(f, d) | _ -> None) with
+        | Some chosen -> Some chosen
+        | None -> titleFieldName model.Fields items |> Option.map (fun f -> f, Ascending)
+    chosen
+    |> Option.bind (fun (name, d) ->
+        model.Fields |> List.tryFind (fun f -> f.Name = name) |> Option.map (fun f -> f, d))
+
+let private keyInfos (model: Model) (items: OverrideItem list) : KeyInfo list =
+    let idKey =
+        { KeyName = "Id"
+          KeyCol = "Id"
+          KeyDir = Ascending
+          KeyKind = Id
+          KeyNullable = false
+          KeyField = "Id"
+          KeyIsFlag = false }
+    match sortTarget model items with
+    | None -> [ idKey ]
+    | Some(f, d) ->
+        let flag =
+            if isOptional f.Kind then
+                [ { KeyName = "Has" + f.Name
+                    KeyCol = "Has" + f.Name
+                    KeyDir = Descending
+                    KeyKind = WholeNumber
+                    KeyNullable = false
+                    KeyField = f.Name
+                    KeyIsFlag = true } ]
+            else
+                []
+        let key =
+            { KeyName = f.Name
+              KeyCol = colName items f.Name
+              KeyDir = d
+              KeyKind = innerKind f.Kind
+              KeyNullable = isOptional f.Kind
+              KeyField = f.Name
+              KeyIsFlag = false }
+        flag @ [ key; idKey ]
+
+let sortKeys (model: Model) (items: OverrideItem list) : (string * Direction) list =
+    keyInfos model items |> List.map (fun k -> k.KeyName, k.KeyDir)
+
+// ---- Indexes and foreign keys ----
+
+let private parentColumn (items: OverrideItem list) (access: Access) : string list =
+    match access with
+    | OwnedVia(reference, _, _) -> [ colName items reference ]
+    | _ -> []
+
+let indexes (model: Model) (items: OverrideItem list) (access: Access) : IndexDef list =
+    let indexName prefix (cols: string list) =
+        sprintf "%s_%s_%s" prefix model.Name (String.concat "_" cols)
+    let listIndex =
+        if hasOwner access then
+            let viewCols =
+                items
+                |> List.collect (fun i ->
+                    match i with
+                    | ListView(_, filter) -> referencedFields filter |> List.map (colName items)
+                    | _ -> [])
+            let cols =
+                ("Owner" :: parentColumn items access)
+                @ viewCols
+                @ (keyInfos model items |> List.map (fun k -> k.KeyCol))
+                |> List.distinct
+            [ { Name = indexName "IX" cols; Table = model.Name; Columns = cols; Unique = false } ]
+        else
+            []
+    let uniqueIndexes =
+        model.Fields
+        |> List.filter (fun f -> f.Unique)
+        |> List.map (fun f ->
+            let cols = [ colName items f.Name ]
+            { Name = indexName "UX" cols; Table = model.Name; Columns = cols; Unique = true })
+    let startsWith (col: string) =
+        (listIndex @ uniqueIndexes) |> List.exists (fun i -> i.Columns.Head = col)
+    let foreignKeyIndexes =
+        model.Fields
+        |> List.filter (fun f -> (refTarget f.Kind).IsSome)
+        |> List.map (fun f -> colName items f.Name)
+        |> List.filter (fun col -> not (startsWith col))
+        |> List.map (fun col ->
+            { Name = indexName "IX" [ col ]; Table = model.Name; Columns = [ col ]; Unique = false })
+    listIndex @ foreignKeyIndexes @ uniqueIndexes
+
+// ---- Schema ----
+
+let private tableShape (model: Model, items: OverrideItem list, access: Access) : TableShape =
+    let column name sqlTypeName nullable : ColumnShape =
+        { ColName = name; SqlType = sqlTypeName; Nullable = nullable; Computed = "" }
+    let system =
+        [ column "Id" "uniqueidentifier" false ]
+        @ (if hasOwner access then [ column "Owner" "uniqueidentifier" false ] else [])
+        @ [ column "Version" "rowversion" false
+            column "CreatedAt" "datetimeoffset" false
+            column "UpdatedAt" "datetimeoffset" false ]
+    let fields =
+        model.Fields
+        |> List.map (fun f -> column (colName items f.Name) (sqlType f.Kind) (isOptional f.Kind))
+    let computed =
+        keyInfos model items
+        |> List.filter (fun k -> k.KeyIsFlag)
+        |> List.map (fun k ->
+            { ColName = k.KeyCol
+              SqlType = "int"
+              Nullable = false
+              Computed = sprintf "CASE WHEN %s IS NULL THEN 0 ELSE 1 END" (q (colName items k.KeyField)) })
+    let foreignKeys =
+        model.Fields
+        |> List.choose (fun f ->
+            refTarget f.Kind
+            |> Option.map (fun target ->
+                { FkName = sprintf "FK_%s_%s" model.Name f.Name
+                  FkColumn = colName items f.Name
+                  FkTarget = target }))
+    { TableName = model.Name
+      Cols = system @ fields @ computed
+      ForeignKeys = foreignKeys
+      Indexes = indexes model items access }
+
+let shapeOf (models: (Model * OverrideItem list * Access) list) : Shape =
+    { Tables = models |> List.map tableShape }
+
+let private renderColumn (dialect: Dialect) (c: ColumnShape) : string =
+    let typ =
+        match dialect with
+        | SqlServer -> c.SqlType
+        | Sqlite -> sqliteType c.SqlType
+    if c.Computed <> "" then
+        match dialect with
+        | SqlServer -> sprintf "%s AS (%s) PERSISTED" (q c.ColName) c.Computed
+        | Sqlite -> sprintf "%s %s AS (%s) STORED" (q c.ColName) typ c.Computed
+    else
+        sprintf
+            "%s %s %s%s"
+            (q c.ColName)
+            typ
+            (if c.Nullable then "NULL" else "NOT NULL")
+            (if c.ColName = "Id" then " PRIMARY KEY" else "")
+
+let private renderTable (dialect: Dialect) (t: TableShape) : string =
+    let columns = t.Cols |> List.map (fun c -> "    " + renderColumn dialect c) |> String.concat ",\n"
+    sprintf "CREATE TABLE %s (\n%s\n);" (q t.TableName) columns
+
+let private renderForeignKey (table: string) (fk: ForeignKeyShape) : string =
+    sprintf
+        "ALTER TABLE %s ADD CONSTRAINT %s FOREIGN KEY (%s) REFERENCES %s (%s);"
+        (q table)
+        (q fk.FkName)
+        (q fk.FkColumn)
+        (q fk.FkTarget)
+        (q "Id")
+
+let private renderIndex (i: IndexDef) : string =
+    sprintf
+        "CREATE %sINDEX %s ON %s (%s);"
+        (if i.Unique then "UNIQUE " else "")
+        (q i.Name)
+        (q i.Table)
+        (i.Columns |> List.map q |> String.concat ", ")
+
+/// SQLite has no ALTER TABLE ADD CONSTRAINT, so its schema holds no foreign keys.
+let private renderForeignKeys (dialect: Dialect) (t: TableShape) : string list =
+    match dialect with
+    | SqlServer -> t.ForeignKeys |> List.map (renderForeignKey t.TableName)
+    | Sqlite -> []
+
+let private renderShape (dialect: Dialect) (shape: Shape) : string =
+    (shape.Tables |> List.map (renderTable dialect))
+    @ (shape.Tables |> List.collect (renderForeignKeys dialect))
+    @ (shape.Tables |> List.collect (fun t -> t.Indexes |> List.map renderIndex))
+    |> String.concat "\n"
+
+let createTable (dialect: Dialect) (model: Model) (items: OverrideItem list) (access: Access) : string =
+    renderTable dialect (tableShape (model, items, access))
+
+let foreignKeys (model: Model) (items: OverrideItem list) (access: Access) : string list =
+    renderForeignKeys SqlServer (tableShape (model, items, access))
+
+let schema (dialect: Dialect) (models: (Model * OverrideItem list * Access) list) : string =
+    renderShape dialect (shapeOf models)
+
+// ---- List views and list queries ----
+
+/// A list view as a WHERE clause with its literals, or the reason that the view is not supported.
+let viewToSql (items: OverrideItem list) (expr: Expr) : Result<string * (string * obj) list, string> =
+    let literals = ResizeArray<string * obj>()
+    let unsupported (e: Expr) : Result<string, string> =
+        Result.Error(sprintf "The view %A is not supported." e)
+    let isBoolProperty (p: Reflection.PropertyInfo) = p.PropertyType = typeof<bool>
+    let column (p: Reflection.PropertyInfo) = q (colName items p.Name)
+    let literal (v: obj) : string =
+        match v with
+        | :? bool as b -> if b then "1" else "0"
+        | _ ->
+            let name = sprintf "v%d" literals.Count
+            literals.Add((name, v))
+            "@" + name
+    let operand (e: Expr) : Result<string, string> =
+        match e with
+        | PropertyGet(Some(Var _), p, []) -> Ok(column p)
+        | Value(v, _) when not (isNull v) -> Ok(literal v)
+        | NewUnionCase(case, []) -> Ok(literal (box case.Name))
+        | _ -> unsupported e
+    let comparison (op: string) (a: Expr) (b: Expr) : Result<string, string> =
+        match operand a, operand b with
+        | Ok l, Ok r -> Ok(sprintf "%s %s %s" l op r)
+        | Result.Error m, _
+        | _, Result.Error m -> Result.Error m
+    let rec cond (e: Expr) : Result<string, string> =
+        match e with
+        | Lambda(_, body) -> cond body
+        | PropertyGet(Some(Var _), p, []) when isBoolProperty p -> Ok(sprintf "%s = 1" (column p))
+        | Call(None, mi, [ PropertyGet(Some(Var _), p, []) ]) when mi.Name = "Not" && isBoolProperty p ->
+            Ok(sprintf "%s = 0" (column p))
+        | Call(None, mi, [ a ]) when mi.Name = "Not" -> cond a |> Result.map (sprintf "NOT (%s)")
+        | IfThenElse(a, b, Value((:? bool as f), _)) when not f -> combine "AND" a b
+        | IfThenElse(a, Value((:? bool as t), _), b) when t -> combine "OR" a b
+        | Call(None, mi, [ a; b ]) when mi.Name = "op_Equality" -> comparison "=" a b
+        | Call(None, mi, [ a; b ]) when mi.Name = "op_Inequality" -> comparison "<>" a b
+        | Call(None, mi, [ a; b ]) when mi.Name = "op_LessThan" -> comparison "<" a b
+        | Call(None, mi, [ a; b ]) when mi.Name = "op_GreaterThan" -> comparison ">" a b
+        | Call(None, mi, [ a; b ]) when mi.Name = "op_LessThanOrEqual" -> comparison "<=" a b
+        | Call(None, mi, [ a; b ]) when mi.Name = "op_GreaterThanOrEqual" -> comparison ">=" a b
+        | _ -> unsupported e
+    and combine (word: string) (a: Expr) (b: Expr) : Result<string, string> =
+        match cond a, cond b with
+        | Ok l, Ok r -> Ok(sprintf "(%s) %s (%s)" l word r)
+        | Result.Error m, _
+        | _, Result.Error m -> Result.Error m
+    cond expr |> Result.map (fun sql -> sql, List.ofSeq literals)
+
+type ListQuery =
+    { Sql: string
+      Keys: (string * Direction) list
+      Literals: (string * obj) list }
+
+/// The fields that the list shows: the `listColumns` override, else every field that is not wide.
+let private listedFields (model: Model) (items: OverrideItem list) : string list =
+    match items |> List.tryPick (function ListColumns cs -> Some cs | _ -> None) with
+    | Some cs -> cs
+    | None -> model.Fields |> List.filter (fun f -> not (isWide f.Kind)) |> List.map (fun f -> f.Name)
+
+/// The fields that a list query reads: the listed ones, then the key fields that the list does not show.
+let private selectedFields (model: Model) (items: OverrideItem list) : string list =
+    let listed = listedFields model items
+    let extra =
+        keyInfos model items
+        |> List.map (fun k -> k.KeyField)
+        |> List.filter (fun f -> f <> "Id" && not (List.contains f listed))
+        |> List.distinct
+    listed @ extra
+
+let private keysetCondition (keys: KeyInfo list) : string =
+    let equal (i: int) (k: KeyInfo) =
+        if k.KeyNullable then
+            sprintf "(%s = @c%d OR (%s IS NULL AND @c%d IS NULL))" (q k.KeyCol) i (q k.KeyCol) i
+        else
+            sprintf "%s = @c%d" (q k.KeyCol) i
+    let after (i: int) (k: KeyInfo) =
+        let op = if k.KeyDir = Ascending then ">" else "<"
+        sprintf "%s %s @c%d" (q k.KeyCol) op i
+    keys
+    |> List.mapi (fun i k ->
+        let before = keys |> List.take i |> List.mapi equal
+        "(" + String.concat " AND " (before @ [ after i k ]) + ")")
+    |> String.concat " OR "
+    |> sprintf "(%s)"
+
+let listQuery
+    (dialect: Dialect)
+    (model: Model)
+    (items: OverrideItem list)
+    (access: Access)
+    (view: string option)
+    (afterCursor: bool)
+    : ListQuery =
+    let keys = keyInfos model items
+    let columns =
+        "Id" :: (selectedFields model items |> List.map (colName items)) |> List.map q |> String.concat ", "
+    let owner =
+        match access with
+        | Owned -> [ "[Owner] = @owner" ]
+        | OwnedVia(reference, _, _) -> [ "[Owner] = @owner"; sprintf "%s = @parent" (q (colName items reference)) ]
+        | Self -> [ "[Id] = @owner" ]
+        | SystemOnly -> []
+    let viewSql, literals =
+        match view with
+        | None -> [], []
+        | Some name ->
+            match items |> List.tryPick (function ListView(n, f) when n = name -> Some f | _ -> None) with
+            | None -> failwithf "%s has no list view '%s'." model.Name name
+            | Some filter ->
+                match viewToSql items filter with
+                | Ok(sql, lits) -> [ "(" + sql + ")" ], lits
+                | Result.Error reason -> failwith reason
+    let cursor = if afterCursor then [ keysetCondition keys ] else []
+    let filters = owner @ viewSql @ cursor
+    let where = if filters.IsEmpty then "" else " WHERE " + String.concat " AND " filters
+    let order =
+        keys
+        |> List.map (fun k -> sprintf "%s %s" (q k.KeyCol) (if k.KeyDir = Ascending then "ASC" else "DESC"))
+        |> String.concat ", "
+    let head, tail =
+        match dialect with
+        | SqlServer -> "SELECT TOP (@limit) ", ""
+        | Sqlite -> "SELECT ", " LIMIT @limit"
+    { Sql = sprintf "%s%s FROM %s%s ORDER BY %s%s" head columns (q model.Name) where order tail
+      Keys = keys |> List.map (fun k -> k.KeyName, k.KeyDir)
+      Literals = literals }
+
+// ---- Values in the database ----
+
+let private invariant = CultureInfo.InvariantCulture
+
+let private toDb (dialect: Dialect) (v: obj) : obj =
+    match v with
+    | null -> box DBNull.Value
+    | :? Guid as g when dialect = Sqlite -> box (g.ToString())
+    | :? DateOnly as d when dialect = Sqlite -> box (d.ToString("yyyy-MM-dd", invariant))
+    | :? DateTimeOffset as t when dialect = Sqlite -> box (t.ToString("o", invariant))
+    | _ -> v
+
+let rec private fromDb (kind: Kind) (v: obj) : obj =
+    match kind with
+    | Text _
+    | Enum _ -> box (string v)
+    | Boolean ->
+        match v with
+        | :? bool -> v
+        | _ -> box (Convert.ToInt64(v, invariant) <> 0L)
+    | WholeNumber ->
+        match v with
+        | :? (byte[]) as bytes -> box (BitConverter.ToInt64(Array.rev bytes, 0))
+        | _ -> box (Convert.ToInt64(v, invariant))
+    | Decimal -> box (Convert.ToDecimal(v, invariant))
+    | Date ->
+        match v with
+        | :? DateOnly -> v
+        | :? DateTime as d -> box (DateOnly.FromDateTime d)
+        | _ -> box (DateOnly.Parse(string v, invariant))
+    | Timestamp ->
+        match v with
+        | :? DateTimeOffset -> v
+        | _ -> box (DateTimeOffset.Parse(string v, invariant))
+    | Id
+    | Reference _ ->
+        match v with
+        | :? Guid -> v
+        | _ -> box (Guid.Parse(string v))
+    | Optional k -> fromDb k v
+
+/// A value as text, for the cursor.
+let private render (v: obj) : string =
+    match v with
+    | :? Guid as g -> g.ToString("D")
+    | :? DateOnly as d -> d.ToString("yyyy-MM-dd", invariant)
+    | :? DateTimeOffset as t -> t.ToString("o", invariant)
+    | :? bool as b -> if b then "1" else "0"
+    | :? decimal as d -> d.ToString(invariant)
+    | _ -> Convert.ToString(v, invariant)
+
+let private parse (kind: Kind) (text: string) : obj =
+    match innerKind kind with
+    | Boolean -> box (text = "1")
+    | WholeNumber -> box (Int64.Parse(text, invariant))
+    | Decimal -> box (System.Decimal.Parse(text, invariant))
+    | Date -> box (DateOnly.ParseExact(text, "yyyy-MM-dd", invariant))
+    | Timestamp -> box (DateTimeOffset.Parse(text, invariant))
+    | Id
+    | Reference _ -> box (Guid.Parse text)
+    | _ -> box text
+
+// ---- Cursor ----
+
+let private encodeCursor (values: string option list) : string =
+    let array = JsonArray()
+    for v in values do
+        match v with
+        | Some s -> array.Add(JsonValue.Create(s))
+        | None -> array.Add(null)
+    Convert.ToBase64String(Encoding.UTF8.GetBytes(array.ToJsonString()))
+        .TrimEnd('=')
+        .Replace('+', '-')
+        .Replace('/', '_')
+
+/// The key values of a cursor, or None when the cursor is not valid.
+let private decodeCursor (keys: KeyInfo list) (cursor: string) : obj option list option =
+    try
+        let base64 = cursor.Replace('-', '+').Replace('_', '/')
+        let padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')
+        let text = Encoding.UTF8.GetString(Convert.FromBase64String padded)
+        let items = JsonNode.Parse(text).AsArray() |> Seq.toList
+        if items.Length <> keys.Length then
+            None
+        else
+            List.map2
+                (fun (k: KeyInfo) (n: JsonNode) ->
+                    if isNull n then None else Some(parse k.KeyKind (n.GetValue<string>())))
+                keys
+                items
+            |> Some
+    with _ ->
+        None
+
+// ---- Store ----
+
+type Store internal (connection: DbConnection, dialect: Dialect, now: unit -> DateTimeOffset, hook: (int -> unit) option) =
+    let mutable commands = 0
+    member _.Connection = connection
+    member _.Dialect = dialect
+    member _.Now() = now ()
+    member _.Hook = hook
+    member _.Commands = commands
+    member _.Counted() = commands <- commands + 1
+
+module Store =
+    let create
+        (conn: DbConnection)
+        (dialect: Dialect)
+        (now: unit -> DateTimeOffset)
+        (onBeforeChildDelete: (int -> unit) option)
+        : Store =
+        Store(conn, dialect, now, onBeforeChildDelete)
+
+let queryCount (store: Store) : int = store.Commands
+
+type Page = { Rows: Record list; Next: string option }
+
+let private bind (store: Store) (tx: DbTransaction option) (sql: string) (args: (string * obj) list) : DbCommand =
+    let cmd = store.Connection.CreateCommand()
+    cmd.CommandText <- sql
+    tx |> Option.iter (fun t -> cmd.Transaction <- t)
+    for (name, value) in args do
+        let p = cmd.CreateParameter()
+        p.ParameterName <- "@" + name
+        p.Value <- toDb store.Dialect value
+        cmd.Parameters.Add p |> ignore
+    store.Counted()
+    cmd
+
+let private run (store: Store) (tx: DbTransaction option) (sql: string) (args: (string * obj) list) : int =
+    use cmd = bind store tx sql args
+    cmd.ExecuteNonQuery()
+
+/// The rows of a query. A column with no value has no key.
+let private fetch
+    (store: Store)
+    (tx: DbTransaction option)
+    (sql: string)
+    (args: (string * obj) list)
+    (cols: (string * Kind) list)
+    : Map<string, obj> list =
+    use cmd = bind store tx sql args
+    use reader = cmd.ExecuteReader()
+    [ while reader.Read() do
+          yield
+              cols
+              |> List.mapi (fun i (name, kind) ->
+                  if reader.IsDBNull i then None else Some(name, fromDb kind (reader.GetValue i)))
+              |> List.choose id
+              |> Map.ofList ]
+
+let private fieldCols (model: Model) : (string * Kind) list =
+    model.Fields |> List.map (fun f -> f.Name, innerKind f.Kind)
+
+let private scopeFilter (scope: Scope) : string * (string * obj) list =
+    match scope.Kind with
+    | OwnerFilter u -> " AND [Owner] = @owner", [ "owner", box u ]
+    | SelfFilter u -> " AND [Id] = @owner", [ "owner", box u ]
+    | Engine -> "", []
+
+let private toRecord (model: Model) (row: Map<string, obj>) : Record =
+    let names = model.Fields |> List.map (fun f -> f.Name)
+    ({ Model = model.Name
+       Id = unbox<Guid> row["Id"]
+       Owner = row.TryFind "Owner" |> Option.map unbox<Guid>
+       Version = row.TryFind "Version" |> Option.map unbox<int64> |> Option.defaultValue 0L
+       Values = row |> Map.filter (fun k _ -> List.contains k names) }
+    : Record)
+
+let find (store: Store) (scope: Scope) (model: Model) (id: Guid) : Result<Record, Finding> =
+    let owned =
+        match scope.Kind with
+        | OwnerFilter _ -> true
+        | _ -> false
+    let cols =
+        [ "Id", Id ]
+        @ (if owned then [ "Owner", Id ] else [])
+        @ [ "Version", WholeNumber ]
+        @ fieldCols model
+    let filter, args = scopeFilter scope
+    let sql =
+        sprintf
+            "SELECT %s FROM %s WHERE [Id] = @id%s"
+            (cols |> List.map (fst >> q) |> String.concat ", ")
+            (q model.Name)
+            filter
+    match fetch store None sql (("id", box id) :: args) cols with
+    | [] -> Result.Error(notFound model.Name)
+    | row :: _ -> Ok(toRecord model row)
+
+let private validation (model: Model) (field: string) (message: string) : Finding =
+    finding VALIDATION_FAILED model.Name (Some field) message
+
+let page
+    (store: Store)
+    (scope: Scope)
+    (model: Model)
+    (items: OverrideItem list)
+    (access: Access)
+    (view: string option)
+    (parent: Guid option)
+    (cursor: string option)
+    (size: int option)
+    : Result<Page, Finding> =
+    let limit = defaultArg size 50
+    let keys = keyInfos model items
+    let decoded =
+        match cursor with
+        | None -> Ok None
+        | Some c ->
+            match decodeCursor keys c with
+            | Some values -> Ok(Some values)
+            | None -> Result.Error(validation model "cursor" "is not valid")
+    let owner =
+        match scope.Kind with
+        | OwnerFilter u
+        | SelfFilter u -> Some u
+        | Engine -> None
+    if limit < 1 || limit > 200 then
+        Result.Error(validation model "limit" "must be between 1 and 200")
+    else
+        match decoded, owner with
+        | Result.Error f, _ -> Result.Error f
+        | Ok _, None -> Result.Error(notFound model.Name)
+        | Ok values, Some ownerId ->
+            let query = listQuery store.Dialect model items access view values.IsSome
+            let parentArgs =
+                match access with
+                | OwnedVia _ -> [ "parent", (match parent with Some g -> box g | None -> null) ]
+                | _ -> []
+            let cursorArgs =
+                match values with
+                | Some vs -> vs |> List.mapi (fun i v -> sprintf "c%d" i, Option.toObj v)
+                | None -> []
+            let args = [ "limit", box (limit + 1); "owner", box ownerId ] @ parentArgs @ query.Literals @ cursorArgs
+            let fieldKinds = fieldCols model |> Map.ofList
+            let cols =
+                ("Id", Id)
+                :: (selectedFields model items |> List.map (fun f -> f, fieldKinds[f]))
+            let rows = fetch store None query.Sql args cols
+            let shown = rows |> List.truncate limit
+            let listed = listedFields model items
+            let records =
+                shown
+                |> List.map (fun row ->
+                    ({ Model = model.Name
+                       Id = unbox<Guid> row["Id"]
+                       Owner = None
+                       Version = 0L
+                       Values = row |> Map.filter (fun k _ -> List.contains k listed) }
+                    : Record))
+            let next =
+                if rows.Length > limit then
+                    let last = List.last shown
+                    let keyValue (k: KeyInfo) : string option =
+                        if k.KeyIsFlag then
+                            Some(if last.ContainsKey k.KeyField then "1" else "0")
+                        else
+                            last.TryFind k.KeyField |> Option.map render
+                    Some(encodeCursor (keys |> List.map keyValue))
+                else
+                    None
+            Ok { Rows = records; Next = next }
+
+/// The version as the database compares it.
+let private versionArg (store: Store) (version: int64) : obj =
+    match store.Dialect with
+    | Sqlite -> box version
+    | SqlServer -> box (Array.rev (BitConverter.GetBytes version))
+
+let private presentValues (model: Model) (record: Record) : (string * obj) list =
+    model.Fields
+    |> List.choose (fun f ->
+        match record.Values.TryFind f.Name with
+        | Some v when not (isNull v) -> Some(f.Name, v)
+        | _ -> None)
+
+let add (store: Store) (scope: Scope) (model: Model) (record: Record) : Result<Record, Finding> =
+    let now = store.Now()
+    let id =
+        match scope.Kind with
+        | SelfFilter u -> u
+        | Engine when record.Id <> Guid.Empty -> record.Id
+        | _ -> Guid.NewGuid()
+    let owner =
+        match scope.Kind with
+        | OwnerFilter u -> Some u
+        | SelfFilter _ -> None
+        | Engine -> record.Owner
+    let system =
+        [ "Id", box id ]
+        @ (match owner with Some o -> [ "Owner", box o ] | None -> [])
+        @ (if store.Dialect = Sqlite then [ "Version", box 1L ] else [])
+        @ [ "CreatedAt", box now; "UpdatedAt", box now ]
+    let columns = system @ presentValues model record
+    let sql =
+        sprintf
+            "INSERT INTO %s (%s) VALUES (%s)"
+            (q model.Name)
+            (columns |> List.map (fst >> q) |> String.concat ", ")
+            (columns |> List.map (fun (n, _) -> "@p_" + n) |> String.concat ", ")
+    run store None sql (columns |> List.map (fun (n, v) -> "p_" + n, v)) |> ignore
+    find store scope model id
+
+let revise
+    (store: Store)
+    (scope: Scope)
+    (model: Model)
+    (id: Guid)
+    (version: int64 option)
+    (record: Record)
+    : Result<Record, Finding> =
+    match version with
+    | None -> Result.Error(validation model "Version" "is required")
+    | Some v ->
+        match find store scope model id with
+        | Result.Error f -> Result.Error f
+        | Ok _ ->
+            let present = presentValues model record
+            let sets =
+                (model.Fields
+                 |> List.map (fun f ->
+                     if present |> List.exists (fun (n, _) -> n = f.Name) then
+                         sprintf "%s = @p_%s" (q f.Name) f.Name
+                     else
+                         sprintf "%s = NULL" (q f.Name)))
+                @ [ "[UpdatedAt] = @now" ]
+                @ (if store.Dialect = Sqlite then [ "[Version] = [Version] + 1" ] else [])
+            let filter, scopeArgs = scopeFilter scope
+            let sql =
+                sprintf
+                    "UPDATE %s SET %s WHERE [Id] = @id AND [Version] = @version%s"
+                    (q model.Name)
+                    (String.concat ", " sets)
+                    filter
+            let args =
+                (present |> List.map (fun (n, value) -> "p_" + n, value))
+                @ [ "now", box (store.Now()); "id", box id; "version", versionArg store v ]
+                @ scopeArgs
+            if run store None sql args = 0 then
+                Result.Error(finding STALE_VERSION model.Name None (sprintf "%s was changed by someone else." model.Name))
+            else
+                find store scope model id
+
+let remove
+    (store: Store)
+    (scope: Scope)
+    (model: Model)
+    (children: (Model * string) list)
+    (id: Guid)
+    : Result<unit, Finding> =
+    match find store scope model id with
+    | Result.Error f -> Result.Error f
+    | Ok _ ->
+        use tx = store.Connection.BeginTransaction()
+        let inTx = Some tx
+        let mutable deleted = 0
+        for (child, reference) in children do
+            let ids =
+                fetch
+                    store
+                    inTx
+                    (sprintf "SELECT [Id] FROM %s WHERE %s = @id" (q child.Name) (q reference))
+                    [ "id", box id ]
+                    [ "Id", Id ]
+            for row in ids do
+                match store.Hook with
+                | Some hook -> hook deleted
+                | None -> ()
+                run store inTx (sprintf "DELETE FROM %s WHERE [Id] = @id" (q child.Name)) [ "id", row["Id"] ]
+                |> ignore
+                deleted <- deleted + 1
+        let filter, scopeArgs = scopeFilter scope
+        run store inTx (sprintf "DELETE FROM %s WHERE [Id] = @id%s" (q model.Name) filter) (("id", box id) :: scopeArgs)
+        |> ignore
+        tx.Commit()
+        Ok()
+
+// ---- Shape file ----
+
+let private jsonString (s: string) : JsonNode = JsonValue.Create(s) :> JsonNode
+let private jsonBool (b: bool) : JsonNode = JsonValue.Create(b) :> JsonNode
+
+let private jsonStrings (xs: string list) : JsonNode =
+    JsonArray(xs |> List.map jsonString |> Array.ofList) :> JsonNode
+
+let private jsonObject (pairs: (string * JsonNode) list) : JsonNode =
+    let o = JsonObject()
+    for (k, v) in pairs do
+        o[k] <- v
+    o :> JsonNode
+
+let private jsonArray (xs: JsonNode list) : JsonNode = JsonArray(Array.ofList xs) :> JsonNode
+
+let private shapeToJson (shape: Shape) : string =
+    let column (c: ColumnShape) =
+        jsonObject
+            [ "name", jsonString c.ColName
+              "type", jsonString c.SqlType
+              "nullable", jsonBool c.Nullable
+              "computed", jsonString c.Computed ]
+    let foreignKey (fk: ForeignKeyShape) =
+        jsonObject [ "name", jsonString fk.FkName; "column", jsonString fk.FkColumn; "target", jsonString fk.FkTarget ]
+    let index (i: IndexDef) =
+        jsonObject
+            [ "name", jsonString i.Name
+              "table", jsonString i.Table
+              "columns", jsonStrings i.Columns
+              "unique", jsonBool i.Unique ]
+    let table (t: TableShape) =
+        jsonObject
+            [ "name", jsonString t.TableName
+              "columns", jsonArray (t.Cols |> List.map column)
+              "foreignKeys", jsonArray (t.ForeignKeys |> List.map foreignKey)
+              "indexes", jsonArray (t.Indexes |> List.map index) ]
+    (jsonObject [ "tables", jsonArray (shape.Tables |> List.map table) ])
+        .ToJsonString(JsonSerializerOptions(WriteIndented = true))
+
+let private shapeOfJson (text: string) : Shape =
+    let root = JsonNode.Parse text
+    let str (n: JsonNode) (key: string) = n[key].GetValue<string>()
+    let flag (n: JsonNode) (key: string) = n[key].GetValue<bool>()
+    let list (n: JsonNode) (key: string) = n[key].AsArray() |> Seq.toList
+    let column (n: JsonNode) : ColumnShape =
+        { ColName = str n "name"
+          SqlType = str n "type"
+          Nullable = flag n "nullable"
+          Computed = str n "computed" }
+    let foreignKey (n: JsonNode) : ForeignKeyShape =
+        { FkName = str n "name"
+          FkColumn = str n "column"
+          FkTarget = str n "target" }
+    let index (n: JsonNode) : IndexDef =
+        { Name = str n "name"
+          Table = str n "table"
+          Columns = list n "columns" |> List.map (fun c -> c.GetValue<string>())
+          Unique = flag n "unique" }
+    let table (n: JsonNode) : TableShape =
+        { TableName = str n "name"
+          Cols = list n "columns" |> List.map column
+          ForeignKeys = list n "foreignKeys" |> List.map foreignKey
+          Indexes = list n "indexes" |> List.map index }
+    { Tables = list root "tables" |> List.map table }
+
+let readShape (path: string) : Result<Shape option, Finding> =
+    if not (File.Exists path) then
+        Ok None
+    else
+        try
+            Ok(Some(shapeOfJson (File.ReadAllText path)))
+        with _ ->
+            Result.Error(
+                finding
+                    SHAPE_UNREADABLE
+                    ""
+                    None
+                    "build/shape.json cannot be read. Delete it to start from an empty shape."
+            )
+
+let writeShape (path: string) (shape: Shape) : unit =
+    let dir = Path.GetDirectoryName path
+    if not (String.IsNullOrEmpty dir) then
+        Directory.CreateDirectory dir |> ignore
+    File.WriteAllText(path, shapeToJson shape)
+
+// ---- Migration ----
+
+let private tableStatements (t: TableShape) : string list =
+    [ renderTable SqlServer t ]
+    @ (t.ForeignKeys |> List.map (renderForeignKey t.TableName))
+    @ (t.Indexes |> List.map renderIndex)
+
+let private tableChanges (renames: Rename list) (previous: TableShape) (next: TableShape) : Result<string list, Finding> =
+    let table = next.TableName
+    let here = renames |> List.filter (fun r -> r.Model = table)
+    let nextNames = next.Cols |> List.map (fun c -> c.ColName)
+    let previousNames = previous.Cols |> List.map (fun c -> c.ColName)
+    let dropped = previous.Cols |> List.filter (fun c -> not (List.contains c.ColName nextNames))
+    let renameOf (c: ColumnShape) =
+        here |> List.tryFind (fun r -> r.From = c.ColName && List.contains r.To nextNames)
+    match dropped |> List.tryFind (fun c -> (renameOf c).IsNone) with
+    | Some c ->
+        Result.Error(
+            finding
+                UNPLANNED_DROP
+                table
+                (Some c.ColName)
+                (sprintf "%s.%s would be dropped. Add renamedFrom or an explicit drop." table c.ColName)
+        )
+    | None ->
+        let renamed = dropped |> List.choose renameOf
+        let renameSql =
+            renamed |> List.map (fun r -> sprintf "EXEC sp_rename '%s.%s', '%s', 'COLUMN';" table r.From r.To)
+        let added =
+            next.Cols
+            |> List.filter (fun c ->
+                not (List.contains c.ColName previousNames)
+                && not (renamed |> List.exists (fun r -> r.To = c.ColName)))
+        match added |> List.tryFind (fun c -> not c.Nullable && c.Computed = "") with
+        | Some c ->
+            Result.Error(
+                finding
+                    UNSAFE_MIGRATION
+                    table
+                    (Some c.ColName)
+                    (sprintf "%s.%s is required and has no default, so existing rows would fail." table c.ColName)
+            )
+        | None ->
+            let addSql =
+                added |> List.map (fun c -> sprintf "ALTER TABLE %s ADD %s;" (q table) (renderColumn SqlServer c))
+            let newForeignKeys =
+                next.ForeignKeys
+                |> List.filter (fun fk -> not (previous.ForeignKeys |> List.exists (fun p -> p.FkName = fk.FkName)))
+                |> List.map (renderForeignKey table)
+            let newIndexes =
+                next.Indexes
+                |> List.filter (fun i -> not (previous.Indexes |> List.exists (fun p -> p.Name = i.Name)))
+                |> List.map renderIndex
+            Ok(renameSql @ addSql @ newForeignKeys @ newIndexes)
+
+/// The SQL Server migration from the previous shape to the next one. With no previous shape it is the full schema.
+let migration (previous: Shape option) (next: Shape) (renames: Rename list) : Result<string, Finding> =
+    match previous with
+    | None -> Ok(renderShape SqlServer next)
+    | Some prev ->
+        let changes =
+            next.Tables
+            |> List.map (fun t ->
+                match prev.Tables |> List.tryFind (fun p -> p.TableName = t.TableName) with
+                | None -> Ok(tableStatements t)
+                | Some p -> tableChanges renames p t)
+        match changes |> List.tryPick (function Result.Error f -> Some f | Ok _ -> None) with
+        | Some f -> Result.Error f
+        | None -> Ok(changes |> List.collect (function Ok s -> s | Result.Error _ -> []) |> String.concat "\n")
diff --git a/src/Typed.Engine/Typed.Engine.fsproj b/src/Typed.Engine/Typed.Engine.fsproj
index e1167bf..39cd82c 100644
--- a/src/Typed.Engine/Typed.Engine.fsproj
+++ b/src/Typed.Engine/Typed.Engine.fsproj
@@ -9,6 +9,7 @@
     <Compile Include="Overrides.fs" />
     <Compile Include="Access.fs" />
     <Compile Include="Validate.fs" />
+    <Compile Include="Persist.fs" />
   </ItemGroup>
 
 </Project>
````

### src/Typed.Engine/Persist.fs

````
module Typed.Engine.Persist

open System
open System.Data
open System.Data.Common
open System.Globalization
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Microsoft.FSharp.Quotations.DerivedPatterns
open Microsoft.FSharp.Reflection
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access

type Dialect =
    | SqlServer
    | Sqlite

type IndexDef =
    { Name: string
      Table: string
      Columns: string list
      Unique: bool }

// ---- Shape: the stored form of every table ----

type ColumnShape =
    { ColName: string
      SqlType: string
      Nullable: bool
      /// The expression of a computed column, or "" for a plain column.
      Computed: string }

type ForeignKeyShape =
    { FkName: string
      FkColumn: string
      FkTarget: string }

type TableShape =
    { TableName: string
      Cols: ColumnShape list
      ForeignKeys: ForeignKeyShape list
      Indexes: IndexDef list }

type Shape = { Tables: TableShape list }

/// The `renamedFrom` note: the column To of the model replaces the column From.
type Rename = { Model: string; From: string; To: string }

// ---- Names and helpers ----

let private q (name: string) : string = "[" + name + "]"

let private finding (code: ErrorCode) (model: string) (field: string option) (message: string) : Finding =
    { Severity = Severity.Error
      Code = code
      Model = model
      Field = field
      Message = message }

let private notFound (model: string) : Finding =
    finding NOT_FOUND model None (sprintf "%s was not found." model)

let private colName (items: OverrideItem list) (field: string) : string =
    items
    |> List.tryPick (function
        | Column(f, n) when f = field -> Some n
        | _ -> None)
    |> Option.defaultValue field

let private hasOwner (access: Access) : bool =
    match access with
    | Owned
    | OwnedVia _ -> true
    | Self
    | SystemOnly -> false

let private sqlType (kind: Kind) : string =
    match innerKind kind with
    | Text n when n > 4000 -> "nvarchar(max)"
    | Text n -> sprintf "nvarchar(%d)" n
    | WholeNumber -> "bigint"
    | Decimal -> "decimal(18,4)"
    | Boolean -> "bit"
    | Enum _ -> "varchar(16)"
    | Date -> "date"
    | Timestamp -> "datetimeoffset"
    | Id
    | Reference _ -> "uniqueidentifier"
    | Optional _ -> "nvarchar(max)"

let private sqliteType (t: string) : string =
    if t.StartsWith "nvarchar" || t.StartsWith "varchar" || t = "date" || t = "datetimeoffset" || t = "uniqueidentifier" then
        "text"
    elif t = "bit" || t = "bigint" || t = "rowversion" || t = "int" then
        "integer"
    elif t.StartsWith "decimal" then
        "numeric"
    else
        t

// ---- Sort keys ----

type private KeyInfo =
    { KeyName: string
      KeyCol: string
      KeyDir: Direction
      KeyKind: Kind
      KeyNullable: bool
      /// The field that supplies the value of the key.
      KeyField: string
      /// True for a `HasX` key, which is 1 when the field X has a value.
      KeyIsFlag: bool }

let private sortTarget (model: Model) (items: OverrideItem list) : (Field * Direction) option =
    let chosen =
        match items |> List.tryPick (function ListSort(f, d) -> Some(f, d) | _ -> None) with
        | Some chosen -> Some chosen
        | None -> titleFieldName model.Fields items |> Option.map (fun f -> f, Ascending)
    chosen
    |> Option.bind (fun (name, d) ->
        model.Fields |> List.tryFind (fun f -> f.Name = name) |> Option.map (fun f -> f, d))

let private keyInfos (model: Model) (items: OverrideItem list) : KeyInfo list =
    let idKey =
        { KeyName = "Id"
          KeyCol = "Id"
          KeyDir = Ascending
          KeyKind = Id
          KeyNullable = false
          KeyField = "Id"
          KeyIsFlag = false }
    match sortTarget model items with
    | None -> [ idKey ]
    | Some(f, d) ->
        let flag =
            if isOptional f.Kind then
                [ { KeyName = "Has" + f.Name
                    KeyCol = "Has" + f.Name
                    KeyDir = Descending
                    KeyKind = WholeNumber
                    KeyNullable = false
                    KeyField = f.Name
                    KeyIsFlag = true } ]
            else
                []
        let key =
            { KeyName = f.Name
              KeyCol = colName items f.Name
              KeyDir = d
              KeyKind = innerKind f.Kind
              KeyNullable = isOptional f.Kind
              KeyField = f.Name
              KeyIsFlag = false }
        flag @ [ key; idKey ]

let sortKeys (model: Model) (items: OverrideItem list) : (string * Direction) list =
    keyInfos model items |> List.map (fun k -> k.KeyName, k.KeyDir)

// ---- Indexes and foreign keys ----

let private parentColumn (items: OverrideItem list) (access: Access) : string list =
    match access with
    | OwnedVia(reference, _, _) -> [ colName items reference ]
    | _ -> []

let indexes (model: Model) (items: OverrideItem list) (access: Access) : IndexDef list =
    let indexName prefix (cols: string list) =
        sprintf "%s_%s_%s" prefix model.Name (String.concat "_" cols)
    let listIndex =
        if hasOwner access then
            let viewCols =
                items
                |> List.collect (fun i ->
                    match i with
                    | ListView(_, filter) -> referencedFields filter |> List.map (colName items)
                    | _ -> [])
            let cols =
                ("Owner" :: parentColumn items access)
                @ viewCols
                @ (keyInfos model items |> List.map (fun k -> k.KeyCol))
                |> List.distinct
            [ { Name = indexName "IX" cols; Table = model.Name; Columns = cols; Unique = false } ]
        else
            []
    let uniqueIndexes =
        model.Fields
        |> List.filter (fun f -> f.Unique)
        |> List.map (fun f ->
            let cols = [ colName items f.Name ]
            { Name = indexName "UX" cols; Table = model.Name; Columns = cols; Unique = true })
    let startsWith (col: string) =
        (listIndex @ uniqueIndexes) |> List.exists (fun i -> i.Columns.Head = col)
    let foreignKeyIndexes =
        model.Fields
        |> List.filter (fun f -> (refTarget f.Kind).IsSome)
        |> List.map (fun f -> colName items f.Name)
        |> List.filter (fun col -> not (startsWith col))
        |> List.map (fun col ->
            { Name = indexName "IX" [ col ]; Table = model.Name; Columns = [ col ]; Unique = false })
    listIndex @ foreignKeyIndexes @ uniqueIndexes

// ---- Schema ----

let private tableShape (model: Model, items: OverrideItem list, access: Access) : TableShape =
    let column name sqlTypeName nullable : ColumnShape =
        { ColName = name; SqlType = sqlTypeName; Nullable = nullable; Computed = "" }
    let system =
        [ column "Id" "uniqueidentifier" false ]
        @ (if hasOwner access then [ column "Owner" "uniqueidentifier" false ] else [])
        @ [ column "Version" "rowversion" false
            column "CreatedAt" "datetimeoffset" false
            column "UpdatedAt" "datetimeoffset" false ]
    let fields =
        model.Fields
        |> List.map (fun f -> column (colName items f.Name) (sqlType f.Kind) (isOptional f.Kind))
    let computed =
        keyInfos model items
        |> List.filter (fun k -> k.KeyIsFlag)
        |> List.map (fun k ->
            { ColName = k.KeyCol
              SqlType = "int"
              Nullable = false
              Computed = sprintf "CASE WHEN %s IS NULL THEN 0 ELSE 1 END" (q (colName items k.KeyField)) })
    let foreignKeys =
        model.Fields
        |> List.choose (fun f ->
            refTarget f.Kind
            |> Option.map (fun target ->
                { FkName = sprintf "FK_%s_%s" model.Name f.Name
                  FkColumn = colName items f.Name
                  FkTarget = target }))
    { TableName = model.Name
      Cols = system @ fields @ computed
      ForeignKeys = foreignKeys
      Indexes = indexes model items access }

let shapeOf (models: (Model * OverrideItem list * Access) list) : Shape =
    { Tables = models |> List.map tableShape }

let private renderColumn (dialect: Dialect) (c: ColumnShape) : string =
    let typ =
        match dialect with
        | SqlServer -> c.SqlType
        | Sqlite -> sqliteType c.SqlType
    if c.Computed <> "" then
        match dialect with
        | SqlServer -> sprintf "%s AS (%s) PERSISTED" (q c.ColName) c.Computed
        | Sqlite -> sprintf "%s %s AS (%s) STORED" (q c.ColName) typ c.Computed
    else
        sprintf
            "%s %s %s%s"
            (q c.ColName)
            typ
            (if c.Nullable then "NULL" else "NOT NULL")
            (if c.ColName = "Id" then " PRIMARY KEY" else "")

let private renderTable (dialect: Dialect) (t: TableShape) : string =
    let columns = t.Cols |> List.map (fun c -> "    " + renderColumn dialect c) |> String.concat ",\n"
    sprintf "CREATE TABLE %s (\n%s\n);" (q t.TableName) columns

let private renderForeignKey (table: string) (fk: ForeignKeyShape) : string =
    sprintf
        "ALTER TABLE %s ADD CONSTRAINT %s FOREIGN KEY (%s) REFERENCES %s (%s);"
        (q table)
        (q fk.FkName)
        (q fk.FkColumn)
        (q fk.FkTarget)
        (q "Id")

let private renderIndex (i: IndexDef) : string =
    sprintf
        "CREATE %sINDEX %s ON %s (%s);"
        (if i.Unique then "UNIQUE " else "")
        (q i.Name)
        (q i.Table)
        (i.Columns |> List.map q |> String.concat ", ")

/// SQLite has no ALTER TABLE ADD CONSTRAINT, so its schema holds no foreign keys.
let private renderForeignKeys (dialect: Dialect) (t: TableShape) : string list =
    match dialect with
    | SqlServer -> t.ForeignKeys |> List.map (renderForeignKey t.TableName)
    | Sqlite -> []

let private renderShape (dialect: Dialect) (shape: Shape) : string =
    (shape.Tables |> List.map (renderTable dialect))
    @ (shape.Tables |> List.collect (renderForeignKeys dialect))
    @ (shape.Tables |> List.collect (fun t -> t.Indexes |> List.map renderIndex))
    |> String.concat "\n"

let createTable (dialect: Dialect) (model: Model) (items: OverrideItem list) (access: Access) : string =
    renderTable dialect (tableShape (model, items, access))

let foreignKeys (model: Model) (items: OverrideItem list) (access: Access) : string list =
    renderForeignKeys SqlServer (tableShape (model, items, access))

let schema (dialect: Dialect) (models: (Model * OverrideItem list * Access) list) : string =
    renderShape dialect (shapeOf models)

// ---- List views and list queries ----

/// A list view as a WHERE clause with its literals, or the reason that the view is not supported.
let viewToSql (items: OverrideItem list) (expr: Expr) : Result<string * (string * obj) list, string> =
    let literals = ResizeArray<string * obj>()
    let unsupported (e: Expr) : Result<string, string> =
        Result.Error(sprintf "The view %A is not supported." e)
    let isBoolProperty (p: Reflection.PropertyInfo) = p.PropertyType = typeof<bool>
    let column (p: Reflection.PropertyInfo) = q (colName items p.Name)
    let literal (v: obj) : string =
        match v with
        | :? bool as b -> if b then "1" else "0"
        | _ ->
            let name = sprintf "v%d" literals.Count
            literals.Add((name, v))
            "@" + name
    let operand (e: Expr) : Result<string, string> =
        match e with
        | PropertyGet(Some(Var _), p, []) -> Ok(column p)
        | Value(v, _) when not (isNull v) -> Ok(literal v)
        | NewUnionCase(case, []) -> Ok(literal (box case.Name))
        | _ -> unsupported e
    let comparison (op: string) (a: Expr) (b: Expr) : Result<string, string> =
        match operand a, operand b with
        | Ok l, Ok r -> Ok(sprintf "%s %s %s" l op r)
        | Result.Error m, _
        | _, Result.Error m -> Result.Error m
    let rec cond (e: Expr) : Result<string, string> =
        match e with
        | Lambda(_, body) -> cond body
        | PropertyGet(Some(Var _), p, []) when isBoolProperty p -> Ok(sprintf "%s = 1" (column p))
        | Call(None, mi, [ PropertyGet(Some(Var _), p, []) ]) when mi.Name = "Not" && isBoolProperty p ->
            Ok(sprintf "%s = 0" (column p))
        | Call(None, mi, [ a ]) when mi.Name = "Not" -> cond a |> Result.map (sprintf "NOT (%s)")
        | IfThenElse(a, b, Value((:? bool as f), _)) when not f -> combine "AND" a b
        | IfThenElse(a, Value((:? bool as t), _), b) when t -> combine "OR" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_Equality" -> comparison "=" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_Inequality" -> comparison "<>" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_LessThan" -> comparison "<" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_GreaterThan" -> comparison ">" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_LessThanOrEqual" -> comparison "<=" a b
        | Call(None, mi, [ a; b ]) when mi.Name = "op_GreaterThanOrEqual" -> comparison ">=" a b
        | _ -> unsupported e
    and combine (word: string) (a: Expr) (b: Expr) : Result<string, string> =
        match cond a, cond b with
        | Ok l, Ok r -> Ok(sprintf "(%s) %s (%s)" l word r)
        | Result.Error m, _
        | _, Result.Error m -> Result.Error m
    cond expr |> Result.map (fun sql -> sql, List.ofSeq literals)

type ListQuery =
    { Sql: string
      Keys: (string * Direction) list
      Literals: (string * obj) list }

/// The fields that the list shows: the `listColumns` override, else every field that is not wide.
let private listedFields (model: Model) (items: OverrideItem list) : string list =
    match items |> List.tryPick (function ListColumns cs -> Some cs | _ -> None) with
    | Some cs -> cs
    | None -> model.Fields |> List.filter (fun f -> not (isWide f.Kind)) |> List.map (fun f -> f.Name)

/// The fields that a list query reads: the listed ones, then the key fields that the list does not show.
let private selectedFields (model: Model) (items: OverrideItem list) : string list =
    let listed = listedFields model items
    let extra =
        keyInfos model items
        |> List.map (fun k -> k.KeyField)
        |> List.filter (fun f -> f <> "Id" && not (List.contains f listed))
        |> List.distinct
    listed @ extra

let private keysetCondition (keys: KeyInfo list) : string =
    let equal (i: int) (k: KeyInfo) =
        if k.KeyNullable then
            sprintf "(%s = @c%d OR (%s IS NULL AND @c%d IS NULL))" (q k.KeyCol) i (q k.KeyCol) i
        else
            sprintf "%s = @c%d" (q k.KeyCol) i
    let after (i: int) (k: KeyInfo) =
        let op = if k.KeyDir = Ascending then ">" else "<"
        sprintf "%s %s @c%d" (q k.KeyCol) op i
    keys
    |> List.mapi (fun i k ->
        let before = keys |> List.take i |> List.mapi equal
        "(" + String.concat " AND " (before @ [ after i k ]) + ")")
    |> String.concat " OR "
    |> sprintf "(%s)"

let listQuery
    (dialect: Dialect)
    (model: Model)
    (items: OverrideItem list)
    (access: Access)
    (view: string option)
    (afterCursor: bool)
    : ListQuery =
    let keys = keyInfos model items
    let columns =
        "Id" :: (selectedFields model items |> List.map (colName items)) |> List.map q |> String.concat ", "
    let owner =
        match access with
        | Owned -> [ "[Owner] = @owner" ]
        | OwnedVia(reference, _, _) -> [ "[Owner] = @owner"; sprintf "%s = @parent" (q (colName items reference)) ]
        | Self -> [ "[Id] = @owner" ]
        | SystemOnly -> []
    let viewSql, literals =
        match view with
        | None -> [], []
        | Some name ->
            match items |> List.tryPick (function ListView(n, f) when n = name -> Some f | _ -> None) with
            | None -> failwithf "%s has no list view '%s'." model.Name name
            | Some filter ->
                match viewToSql items filter with
                | Ok(sql, lits) -> [ "(" + sql + ")" ], lits
                | Result.Error reason -> failwith reason
    let cursor = if afterCursor then [ keysetCondition keys ] else []
    let filters = owner @ viewSql @ cursor
    let where = if filters.IsEmpty then "" else " WHERE " + String.concat " AND " filters
    let order =
        keys
        |> List.map (fun k -> sprintf "%s %s" (q k.KeyCol) (if k.KeyDir = Ascending then "ASC" else "DESC"))
        |> String.concat ", "
    let head, tail =
        match dialect with
        | SqlServer -> "SELECT TOP (@limit) ", ""
        | Sqlite -> "SELECT ", " LIMIT @limit"
    { Sql = sprintf "%s%s FROM %s%s ORDER BY %s%s" head columns (q model.Name) where order tail
      Keys = keys |> List.map (fun k -> k.KeyName, k.KeyDir)
      Literals = literals }

// ---- Values in the database ----

let private invariant = CultureInfo.InvariantCulture

let private toDb (dialect: Dialect) (v: obj) : obj =
    match v with
    | null -> box DBNull.Value
    | :? Guid as g when dialect = Sqlite -> box (g.ToString())
    | :? DateOnly as d when dialect = Sqlite -> box (d.ToString("yyyy-MM-dd", invariant))
    | :? DateTimeOffset as t when dialect = Sqlite -> box (t.ToString("o", invariant))
    | _ -> v

let rec private fromDb (kind: Kind) (v: obj) : obj =
    match kind with
    | Text _
    | Enum _ -> box (string v)
    | Boolean ->
        match v with
        | :? bool -> v
        | _ -> box (Convert.ToInt64(v, invariant) <> 0L)
    | WholeNumber ->
        match v with
        | :? (byte[]) as bytes -> box (BitConverter.ToInt64(Array.rev bytes, 0))
        | _ -> box (Convert.ToInt64(v, invariant))
    | Decimal -> box (Convert.ToDecimal(v, invariant))
    | Date ->
        match v with
        | :? DateOnly -> v
        | :? DateTime as d -> box (DateOnly.FromDateTime d)
        | _ -> box (DateOnly.Parse(string v, invariant))
    | Timestamp ->
        match v with
        | :? DateTimeOffset -> v
        | _ -> box (DateTimeOffset.Parse(string v, invariant))
    | Id
    | Reference _ ->
        match v with
        | :? Guid -> v
        | _ -> box (Guid.Parse(string v))
    | Optional k -> fromDb k v

/// A value as text, for the cursor.
let private render (v: obj) : string =
    match v with
    | :? Guid as g -> g.ToString("D")
    | :? DateOnly as d -> d.ToString("yyyy-MM-dd", invariant)
    | :? DateTimeOffset as t -> t.ToString("o", invariant)
    | :? bool as b -> if b then "1" else "0"
    | :? decimal as d -> d.ToString(invariant)
    | _ -> Convert.ToString(v, invariant)

let private parse (kind: Kind) (text: string) : obj =
    match innerKind kind with
    | Boolean -> box (text = "1")
    | WholeNumber -> box (Int64.Parse(text, invariant))
    | Decimal -> box (System.Decimal.Parse(text, invariant))
    | Date -> box (DateOnly.ParseExact(text, "yyyy-MM-dd", invariant))
    | Timestamp -> box (DateTimeOffset.Parse(text, invariant))
    | Id
    | Reference _ -> box (Guid.Parse text)
    | _ -> box text

// ---- Cursor ----

let private encodeCursor (values: string option list) : string =
    let array = JsonArray()
    for v in values do
        match v with
        | Some s -> array.Add(JsonValue.Create(s))
        | None -> array.Add(null)
    Convert.ToBase64String(Encoding.UTF8.GetBytes(array.ToJsonString()))
        .TrimEnd('=')
        .Replace('+', '-')
        .Replace('/', '_')

/// The key values of a cursor, or None when the cursor is not valid.
let private decodeCursor (keys: KeyInfo list) (cursor: string) : obj option list option =
    try
        let base64 = cursor.Replace('-', '+').Replace('_', '/')
        let padded = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=')
        let text = Encoding.UTF8.GetString(Convert.FromBase64String padded)
        let items = JsonNode.Parse(text).AsArray() |> Seq.toList
        if items.Length <> keys.Length then
            None
        else
            List.map2
                (fun (k: KeyInfo) (n: JsonNode) ->
                    if isNull n then None else Some(parse k.KeyKind (n.GetValue<string>())))
                keys
                items
            |> Some
    with _ ->
        None

// ---- Store ----

type Store internal (connection: DbConnection, dialect: Dialect, now: unit -> DateTimeOffset, hook: (int -> unit) option) =
    let mutable commands = 0
    member _.Connection = connection
    member _.Dialect = dialect
    member _.Now() = now ()
    member _.Hook = hook
    member _.Commands = commands
    member _.Counted() = commands <- commands + 1

module Store =
    let create
        (conn: DbConnection)
        (dialect: Dialect)
        (now: unit -> DateTimeOffset)
        (onBeforeChildDelete: (int -> unit) option)
        : Store =
        Store(conn, dialect, now, onBeforeChildDelete)

let queryCount (store: Store) : int = store.Commands

type Page = { Rows: Record list; Next: string option }

let private bind (store: Store) (tx: DbTransaction option) (sql: string) (args: (string * obj) list) : DbCommand =
    let cmd = store.Connection.CreateCommand()
    cmd.CommandText <- sql
    tx |> Option.iter (fun t -> cmd.Transaction <- t)
    for (name, value) in args do
        let p = cmd.CreateParameter()
        p.ParameterName <- "@" + name
        p.Value <- toDb store.Dialect value
        cmd.Parameters.Add p |> ignore
    store.Counted()
    cmd

let private run (store: Store) (tx: DbTransaction option) (sql: string) (args: (string * obj) list) : int =
    use cmd = bind store tx sql args
    cmd.ExecuteNonQuery()

/// The rows of a query. A column with no value has no key.
let private fetch
    (store: Store)
    (tx: DbTransaction option)
    (sql: string)
    (args: (string * obj) list)
    (cols: (string * Kind) list)
    : Map<string, obj> list =
    use cmd = bind store tx sql args
    use reader = cmd.ExecuteReader()
    [ while reader.Read() do
          yield
              cols
              |> List.mapi (fun i (name, kind) ->
                  if reader.IsDBNull i then None else Some(name, fromDb kind (reader.GetValue i)))
              |> List.choose id
              |> Map.ofList ]

let private fieldCols (model: Model) : (string * Kind) list =
    model.Fields |> List.map (fun f -> f.Name, innerKind f.Kind)

let private scopeFilter (scope: Scope) : string * (string * obj) list =
    match scope.Kind with
    | OwnerFilter u -> " AND [Owner] = @owner", [ "owner", box u ]
    | SelfFilter u -> " AND [Id] = @owner", [ "owner", box u ]
    | Engine -> "", []

let private toRecord (model: Model) (row: Map<string, obj>) : Record =
    let names = model.Fields |> List.map (fun f -> f.Name)
    ({ Model = model.Name
       Id = unbox<Guid> row["Id"]
       Owner = row.TryFind "Owner" |> Option.map unbox<Guid>
       Version = row.TryFind "Version" |> Option.map unbox<int64> |> Option.defaultValue 0L
       Values = row |> Map.filter (fun k _ -> List.contains k names) }
    : Record)

let find (store: Store) (scope: Scope) (model: Model) (id: Guid) : Result<Record, Finding> =
    let owned =
        match scope.Kind with
        | OwnerFilter _ -> true
        | _ -> false
    let cols =
        [ "Id", Id ]
        @ (if owned then [ "Owner", Id ] else [])
        @ [ "Version", WholeNumber ]
        @ fieldCols model
    let filter, args = scopeFilter scope
    let sql =
        sprintf
            "SELECT %s FROM %s WHERE [Id] = @id%s"
            (cols |> List.map (fst >> q) |> String.concat ", ")
            (q model.Name)
            filter
    match fetch store None sql (("id", box id) :: args) cols with
    | [] -> Result.Error(notFound model.Name)
    | row :: _ -> Ok(toRecord model row)

let private validation (model: Model) (field: string) (message: string) : Finding =
    finding VALIDATION_FAILED model.Name (Some field) message

let page
    (store: Store)
    (scope: Scope)
    (model: Model)
    (items: OverrideItem list)
    (access: Access)
    (view: string option)
    (parent: Guid option)
    (cursor: string option)
    (size: int option)
    : Result<Page, Finding> =
    let limit = defaultArg size 50
    let keys = keyInfos model items
    let decoded =
        match cursor with
        | None -> Ok None
        | Some c ->
            match decodeCursor keys c with
            | Some values -> Ok(Some values)
            | None -> Result.Error(validation model "cursor" "is not valid")
    let owner =
        match scope.Kind with
        | OwnerFilter u
        | SelfFilter u -> Some u
        | Engine -> None
    if limit < 1 || limit > 200 then
        Result.Error(validation model "limit" "must be between 1 and 200")
    else
        match decoded, owner with
        | Result.Error f, _ -> Result.Error f
        | Ok _, None -> Result.Error(notFound model.Name)
        | Ok values, Some ownerId ->
            let query = listQuery store.Dialect model items access view values.IsSome
            let parentArgs =
                match access with
                | OwnedVia _ -> [ "parent", (match parent with Some g -> box g | None -> null) ]
                | _ -> []
            let cursorArgs =
                match values with
                | Some vs -> vs |> List.mapi (fun i v -> sprintf "c%d" i, Option.toObj v)
                | None -> []
            let args = [ "limit", box (limit + 1); "owner", box ownerId ] @ parentArgs @ query.Literals @ cursorArgs
            let fieldKinds = fieldCols model |> Map.ofList
            let cols =
                ("Id", Id)
                :: (selectedFields model items |> List.map (fun f -> f, fieldKinds[f]))
            let rows = fetch store None query.Sql args cols
            let shown = rows |> List.truncate limit
            let listed = listedFields model items
            let records =
                shown
                |> List.map (fun row ->
                    ({ Model = model.Name
                       Id = unbox<Guid> row["Id"]
                       Owner = None
                       Version = 0L
                       Values = row |> Map.filter (fun k _ -> List.contains k listed) }
                    : Record))
            let next =
                if rows.Length > limit then
                    let last = List.last shown
                    let keyValue (k: KeyInfo) : string option =
                        if k.KeyIsFlag then
                            Some(if last.ContainsKey k.KeyField then "1" else "0")
                        else
                            last.TryFind k.KeyField |> Option.map render
                    Some(encodeCursor (keys |> List.map keyValue))
                else
                    None
            Ok { Rows = records; Next = next }

/// The version as the database compares it.
let private versionArg (store: Store) (version: int64) : obj =
    match store.Dialect with
    | Sqlite -> box version
    | SqlServer -> box (Array.rev (BitConverter.GetBytes version))

let private presentValues (model: Model) (record: Record) : (string * obj) list =
    model.Fields
    |> List.choose (fun f ->
        match record.Values.TryFind f.Name with
        | Some v when not (isNull v) -> Some(f.Name, v)
        | _ -> None)

let add (store: Store) (scope: Scope) (model: Model) (record: Record) : Result<Record, Finding> =
    let now = store.Now()
    let id =
        match scope.Kind with
        | SelfFilter u -> u
        | Engine when record.Id <> Guid.Empty -> record.Id
        | _ -> Guid.NewGuid()
    let owner =
        match scope.Kind with
        | OwnerFilter u -> Some u
        | SelfFilter _ -> None
        | Engine -> record.Owner
    let system =
        [ "Id", box id ]
        @ (match owner with Some o -> [ "Owner", box o ] | None -> [])
        @ (if store.Dialect = Sqlite then [ "Version", box 1L ] else [])
        @ [ "CreatedAt", box now; "UpdatedAt", box now ]
    let columns = system @ presentValues model record
    let sql =
        sprintf
            "INSERT INTO %s (%s) VALUES (%s)"
            (q model.Name)
            (columns |> List.map (fst >> q) |> String.concat ", ")
            (columns |> List.map (fun (n, _) -> "@p_" + n) |> String.concat ", ")
    run store None sql (columns |> List.map (fun (n, v) -> "p_" + n, v)) |> ignore
    find store scope model id

let revise
    (store: Store)
    (scope: Scope)
    (model: Model)
    (id: Guid)
    (version: int64 option)
    (record: Record)
    : Result<Record, Finding> =
    match version with
    | None -> Result.Error(validation model "Version" "is required")
    | Some v ->
        match find store scope model id with
        | Result.Error f -> Result.Error f
        | Ok _ ->
            let present = presentValues model record
            let sets =
                (model.Fields
                 |> List.map (fun f ->
                     if present |> List.exists (fun (n, _) -> n = f.Name) then
                         sprintf "%s = @p_%s" (q f.Name) f.Name
                     else
                         sprintf "%s = NULL" (q f.Name)))
                @ [ "[UpdatedAt] = @now" ]
                @ (if store.Dialect = Sqlite then [ "[Version] = [Version] + 1" ] else [])
            let filter, scopeArgs = scopeFilter scope
            let sql =
                sprintf
                    "UPDATE %s SET %s WHERE [Id] = @id AND [Version] = @version%s"
                    (q model.Name)
                    (String.concat ", " sets)
                    filter
            let args =
                (present |> List.map (fun (n, value) -> "p_" + n, value))
                @ [ "now", box (store.Now()); "id", box id; "version", versionArg store v ]
                @ scopeArgs
            if run store None sql args = 0 then
                Result.Error(finding STALE_VERSION model.Name None (sprintf "%s was changed by someone else." model.Name))
            else
                find store scope model id

let remove
    (store: Store)
    (scope: Scope)
    (model: Model)
    (children: (Model * string) list)
    (id: Guid)
    : Result<unit, Finding> =
    match find store scope model id with
    | Result.Error f -> Result.Error f
    | Ok _ ->
        use tx = store.Connection.BeginTransaction()
        let inTx = Some tx
        let mutable deleted = 0
        for (child, reference) in children do
            let ids =
                fetch
                    store
                    inTx
                    (sprintf "SELECT [Id] FROM %s WHERE %s = @id" (q child.Name) (q reference))
                    [ "id", box id ]
                    [ "Id", Id ]
            for row in ids do
                match store.Hook with
                | Some hook -> hook deleted
                | None -> ()
                run store inTx (sprintf "DELETE FROM %s WHERE [Id] = @id" (q child.Name)) [ "id", row["Id"] ]
                |> ignore
                deleted <- deleted + 1
        let filter, scopeArgs = scopeFilter scope
        run store inTx (sprintf "DELETE FROM %s WHERE [Id] = @id%s" (q model.Name) filter) (("id", box id) :: scopeArgs)
        |> ignore
        tx.Commit()
        Ok()

// ---- Shape file ----

let private jsonString (s: string) : JsonNode = JsonValue.Create(s) :> JsonNode
let private jsonBool (b: bool) : JsonNode = JsonValue.Create(b) :> JsonNode

let private jsonStrings (xs: string list) : JsonNode =
    JsonArray(xs |> List.map jsonString |> Array.ofList) :> JsonNode

let private jsonObject (pairs: (string * JsonNode) list) : JsonNode =
    let o = JsonObject()
    for (k, v) in pairs do
        o[k] <- v
    o :> JsonNode

let private jsonArray (xs: JsonNode list) : JsonNode = JsonArray(Array.ofList xs) :> JsonNode

let private shapeToJson (shape: Shape) : string =
    let column (c: ColumnShape) =
        jsonObject
            [ "name", jsonString c.ColName
              "type", jsonString c.SqlType
              "nullable", jsonBool c.Nullable
              "computed", jsonString c.Computed ]
    let foreignKey (fk: ForeignKeyShape) =
        jsonObject [ "name", jsonString fk.FkName; "column", jsonString fk.FkColumn; "target", jsonString fk.FkTarget ]
    let index (i: IndexDef) =
        jsonObject
            [ "name", jsonString i.Name
              "table", jsonString i.Table
              "columns", jsonStrings i.Columns
              "unique", jsonBool i.Unique ]
    let table (t: TableShape) =
        jsonObject
            [ "name", jsonString t.TableName
              "columns", jsonArray (t.Cols |> List.map column)
              "foreignKeys", jsonArray (t.ForeignKeys |> List.map foreignKey)
              "indexes", jsonArray (t.Indexes |> List.map index) ]
    (jsonObject [ "tables", jsonArray (shape.Tables |> List.map table) ])
        .ToJsonString(JsonSerializerOptions(WriteIndented = true))

let private shapeOfJson (text: string) : Shape =
    let root = JsonNode.Parse text
    let str (n: JsonNode) (key: string) = n[key].GetValue<string>()
    let flag (n: JsonNode) (key: string) = n[key].GetValue<bool>()
    let list (n: JsonNode) (key: string) = n[key].AsArray() |> Seq.toList
    let column (n: JsonNode) : ColumnShape =
        { ColName = str n "name"
          SqlType = str n "type"
          Nullable = flag n "nullable"
          Computed = str n "computed" }
    let foreignKey (n: JsonNode) : ForeignKeyShape =
        { FkName = str n "name"
          FkColumn = str n "column"
          FkTarget = str n "target" }
    let index (n: JsonNode) : IndexDef =
        { Name = str n "name"
          Table = str n "table"
          Columns = list n "columns" |> List.map (fun c -> c.GetValue<string>())
          Unique = flag n "unique" }
    let table (n: JsonNode) : TableShape =
        { TableName = str n "name"
          Cols = list n "columns" |> List.map column
          ForeignKeys = list n "foreignKeys" |> List.map foreignKey
          Indexes = list n "indexes" |> List.map index }
    { Tables = list root "tables" |> List.map table }

let readShape (path: string) : Result<Shape option, Finding> =
    if not (File.Exists path) then
        Ok None
    else
        try
            Ok(Some(shapeOfJson (File.ReadAllText path)))
        with _ ->
            Result.Error(
                finding
                    SHAPE_UNREADABLE
                    ""
                    None
                    "build/shape.json cannot be read. Delete it to start from an empty shape."
            )

let writeShape (path: string) (shape: Shape) : unit =
    let dir = Path.GetDirectoryName path
    if not (String.IsNullOrEmpty dir) then
        Directory.CreateDirectory dir |> ignore
    File.WriteAllText(path, shapeToJson shape)

// ---- Migration ----

let private tableStatements (t: TableShape) : string list =
    [ renderTable SqlServer t ]
    @ (t.ForeignKeys |> List.map (renderForeignKey t.TableName))
    @ (t.Indexes |> List.map renderIndex)

let private tableChanges (renames: Rename list) (previous: TableShape) (next: TableShape) : Result<string list, Finding> =
    let table = next.TableName
    let here = renames |> List.filter (fun r -> r.Model = table)
    let nextNames = next.Cols |> List.map (fun c -> c.ColName)
    let previousNames = previous.Cols |> List.map (fun c -> c.ColName)
    let dropped = previous.Cols |> List.filter (fun c -> not (List.contains c.ColName nextNames))
    let renameOf (c: ColumnShape) =
        here |> List.tryFind (fun r -> r.From = c.ColName && List.contains r.To nextNames)
    match dropped |> List.tryFind (fun c -> (renameOf c).IsNone) with
    | Some c ->
        Result.Error(
            finding
                UNPLANNED_DROP
                table
                (Some c.ColName)
                (sprintf "%s.%s would be dropped. Add renamedFrom or an explicit drop." table c.ColName)
        )
    | None ->
        let renamed = dropped |> List.choose renameOf
        let renameSql =
            renamed |> List.map (fun r -> sprintf "EXEC sp_rename '%s.%s', '%s', 'COLUMN';" table r.From r.To)
        let added =
            next.Cols
            |> List.filter (fun c ->
                not (List.contains c.ColName previousNames)
                && not (renamed |> List.exists (fun r -> r.To = c.ColName)))
        match added |> List.tryFind (fun c -> not c.Nullable && c.Computed = "") with
        | Some c ->
            Result.Error(
                finding
                    UNSAFE_MIGRATION
                    table
                    (Some c.ColName)
                    (sprintf "%s.%s is required and has no default, so existing rows would fail." table c.ColName)
            )
        | None ->
            let addSql =
                added |> List.map (fun c -> sprintf "ALTER TABLE %s ADD %s;" (q table) (renderColumn SqlServer c))
            let newForeignKeys =
                next.ForeignKeys
                |> List.filter (fun fk -> not (previous.ForeignKeys |> List.exists (fun p -> p.FkName = fk.FkName)))
                |> List.map (renderForeignKey table)
            let newIndexes =
                next.Indexes
                |> List.filter (fun i -> not (previous.Indexes |> List.exists (fun p -> p.Name = i.Name)))
                |> List.map renderIndex
            Ok(renameSql @ addSql @ newForeignKeys @ newIndexes)

/// The SQL Server migration from the previous shape to the next one. With no previous shape it is the full schema.
let migration (previous: Shape option) (next: Shape) (renames: Rename list) : Result<string, Finding> =
    match previous with
    | None -> Ok(renderShape SqlServer next)
    | Some prev ->
        let changes =
            next.Tables
            |> List.map (fun t ->
                match prev.Tables |> List.tryFind (fun p -> p.TableName = t.TableName) with
                | None -> Ok(tableStatements t)
                | Some p -> tableChanges renames p t)
        match changes |> List.tryPick (function Result.Error f -> Some f | Ok _ -> None) with
        | Some f -> Result.Error f
        | None -> Ok(changes |> List.collect (function Ok s -> s | Result.Error _ -> []) |> String.concat "\n")
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
  </ItemGroup>

</Project>
````

## Writing check

`pdlc/bin/check_writing.py --since pdlc/CH-0003-validate` on pdlc/changes/CH-0004-persist/change.md, pdlc/specs/capabilities/CAP-persist.md:

````
No findings.
````

## Visual review frames

Read each frame to see the change working:

- pdlc/changes/CH-0004-persist/frames/01.png
- pdlc/changes/CH-0004-persist/frames/02.png
- pdlc/changes/CH-0004-persist/frames/03.png
- pdlc/changes/CH-0004-persist/frames/04.png
- pdlc/changes/CH-0004-persist/frames/05.png
- pdlc/changes/CH-0004-persist/frames/06.png
- pdlc/changes/CH-0004-persist/frames/07.png
- pdlc/changes/CH-0004-persist/frames/08.png
- pdlc/changes/CH-0004-persist/frames/09.png
- pdlc/changes/CH-0004-persist/frames/10.png
- pdlc/changes/CH-0004-persist/frames/11.png
- pdlc/changes/CH-0004-persist/frames/12.png
- pdlc/changes/CH-0004-persist/frames/13.png
- pdlc/changes/CH-0004-persist/frames/14.png
- pdlc/changes/CH-0004-persist/frames/15.png
- pdlc/changes/CH-0004-persist/frames/16.png
- pdlc/changes/CH-0004-persist/frames/17.png
- pdlc/changes/CH-0004-persist/frames/18.png
- pdlc/changes/CH-0004-persist/frames/19.png
- pdlc/changes/CH-0004-persist/frames/20.png
- pdlc/changes/CH-0004-persist/frames/21.png
- pdlc/changes/CH-0004-persist/frames/22.png
- pdlc/changes/CH-0004-persist/frames/23.png
- pdlc/changes/CH-0004-persist/frames/24.png
- pdlc/changes/CH-0004-persist/frames/25.png
- pdlc/changes/CH-0004-persist/frames/26.png
- pdlc/changes/CH-0004-persist/frames/27.png
- pdlc/changes/CH-0004-persist/frames/28.png
- pdlc/changes/CH-0004-persist/frames/29.png
- pdlc/changes/CH-0004-persist/frames/30.png
- pdlc/changes/CH-0004-persist/frames/31.png
- pdlc/changes/CH-0004-persist/frames/32.png
- pdlc/changes/CH-0004-persist/frames/33.png
- pdlc/changes/CH-0004-persist/frames/34.png
- pdlc/changes/CH-0004-persist/frames/35.png
- pdlc/changes/CH-0004-persist/frames/36.png
- pdlc/changes/CH-0004-persist/frames/37.png
- pdlc/changes/CH-0004-persist/frames/38.png
- pdlc/changes/CH-0004-persist/frames/39.png
- pdlc/changes/CH-0004-persist/frames/40.png
