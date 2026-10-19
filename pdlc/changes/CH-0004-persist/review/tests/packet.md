# Review packet · CH-0004-persist · tests

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
# CH-0004 · Persist

Intent: IN-0001 · Scope: CAP-persist · Status: testing · Ticket: none · Branch: pdlc/CH-0004-persist · Base: pdlc/CH-0003-validate
Tests-Locked: 81284e2

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
- `Record.Values` holds each field in its stored form: text and a semantic text type as `string`, an enum as its case name `string`, a reference as `Guid`, a boolean as `bool`, a date as `DateOnly`, a timestamp as `DateTimeOffset`, a whole number as `int64`, and a decimal as `decimal`. An empty optional field has no key. (assumed)
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

### Step 1 · Each model gets a table with its system columns · [ ]

- Requirement: CAP-persist.R1
- Check: the checks named `CAP-persist.R1 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R1 (each model gets a table with its system columns) passes

### Step 2 · Foreign keys are indexed and never cascade · [ ]

- Requirement: CAP-persist.R2
- Check: the checks named `CAP-persist.R2 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R2 (foreign keys are indexed and never cascade) passes

### Step 3 · Each list query gets one index · [ ]

- Requirement: CAP-persist.R3
- Check: the checks named `CAP-persist.R3 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R3 (each list query gets one index) passes

### Step 4 · Every list query filters by owner and names its columns · [ ]

- Requirement: CAP-persist.R4
- Check: the checks named `CAP-persist.R4 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R4 (every list query filters by owner and names its columns) passes

### Step 5 · Keyset paging returns each record one time · [ ]

- Requirement: CAP-persist.R5
- Check: the checks named `CAP-persist.R5 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R5 (keyset paging returns each record one time) passes

### Step 6 · Deleting a parent deletes its children in one transaction · [ ]

- Requirement: CAP-persist.R6
- Check: the checks named `CAP-persist.R6 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R6 (deleting a parent deletes its children in one transaction) passes

### Step 7 · A stale save fails with a conflict · [ ]

- Requirement: CAP-persist.R7
- Check: the checks named `CAP-persist.R7 ...` in `tests/Typed.Engine.Tests/CAP-persist.Tests.fs`
- Done when: every check of CAP-persist.R7 (a stale save fails with a conflict) passes

### Step 8 · A migration never drops a column without a note · [ ]

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

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

- Given the model `Todo`, when the engine writes the schema, then the schema holds
  `ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES
  [TodoList] ([Id]);`.
- Given the three models of the todo app, when the engine writes the schema, then the
  text "CASCADE" does not appear.
- Given the model `Todo`, when the engine derives the indexes, then one index starts with
  the column `List`.

### CAP-persist.R3 · Each list query gets one index

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

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

Status: ready · Intents: IN-0001

- Given a list with 3 todos, when a caller removes the list, then the list and its 3
  todos are gone.
- Given a list with 3 todos, and a failure after the second todo is deleted. When a caller
  removes the list, then the list and all 3 todos still exist.
- Given a list owned by `u2`, when `u1` removes the list, then the result is the error
  `NOT_FOUND`, and the list still exists.

### CAP-persist.R7 · A stale save fails with a conflict

Status: ready · Intents: IN-0001

- Given a todo at version 3. When a caller saves the todo with version 3, then the save
  passes and the todo is at version 4.
- Given a todo at version 4. When a caller saves the todo with version 3, then the result
  is the error `STALE_VERSION`, and the stored todo does not change.
- Given a new todo, when a caller adds it, then the stored todo is at version 1. (assumed)
- Given a todo at version 3. When a caller saves the todo with no version, then the result
  is the error `VALIDATION_FAILED` on `Version` with the message "is required". (assumed)

### CAP-persist.R8 · A migration never drops a column without a note

Status: ready · Intents: IN-0001

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

### Diff against pdlc/CH-0003-validate

````
diff --git a/tests/Typed.Engine.Tests/CAP-persist.Tests.fs b/tests/Typed.Engine.Tests/CAP-persist.Tests.fs
new file mode 100644
index 0000000..b5eb7c8
--- /dev/null
+++ b/tests/Typed.Engine.Tests/CAP-persist.Tests.fs
@@ -0,0 +1,645 @@
+namespace Typed.Engine.Tests
+
+open System
+open System.IO
+open Microsoft.Data.Sqlite
+open Xunit
+open Typed.Engine.Model
+open Typed.Engine.Overrides
+open Typed.Engine.Access
+open Typed.Engine.Persist
+open Typed.Engine.Tests.ModelsFixtures
+open Typed.Engine.Tests.AccessFixtures
+
+/// Models, overrides, and store helpers of the persistence checks. The checks use only these.
+module PersistFixtures =
+
+    /// An owned model with a title field, no listSort, and no listView.
+    type Note = { Heading: Title; Body: Notes }
+
+    /// Shapes of Todo for the migration checks. The model name is "Todo" in each module.
+    module DueRenamed =
+        type Todo =
+            { List: Ref<TodoList>
+              Title: Title
+              Notes: Notes option
+              Priority: Priority
+              DueDate: DateOnly option
+              Done: bool
+              CompletedAt: DateTimeOffset option }
+
+    module WithTags =
+        type Todo =
+            { List: Ref<TodoList>
+              Title: Title
+              Notes: Notes option
+              Priority: Priority
+              Due: DateOnly option
+              Done: bool
+              CompletedAt: DateTimeOffset option
+              Tags: Title option }
+
+    module WithCode =
+        type Todo =
+            { List: Ref<TodoList>
+              Title: Title
+              Notes: Notes option
+              Priority: Priority
+              Due: DateOnly option
+              Done: bool
+              CompletedAt: DateTimeOffset option
+              Code: Title }
+
+    let accessOf (modelName: string) : Access =
+        (rules |> List.find (fun r -> r.ModelName = modelName)).Access
+
+    let todoAccess = accessOf "Todo"
+    let listAccess = accessOf "TodoList"
+    let userAccess = accessOf "User"
+    let noteAccess = (rule<Note> owned).Access
+
+    let todoModel = describeOk typeof<Todo>
+    let listModel = describeOk typeof<TodoList>
+    let userModel = describeOk typeof<User>
+    let noteModel = describeOk typeof<Note>
+
+    let todoItems =
+        (overrides<Todo>
+            [ listColumns
+                  [ col <@ fun (t: Todo) -> t.Title @>
+                    col <@ fun (t: Todo) -> t.Due @>
+                    col <@ fun (t: Todo) -> t.Priority @>
+                    col <@ fun (t: Todo) -> t.Done @> ]
+              listSort <@ fun (t: Todo) -> t.Due @> Ascending ])
+            .Items
+
+    let listItems =
+        (overrides<TodoList>
+            [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
+              listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ])
+            .Items
+
+    let userColumnItems =
+        (overrides<User> [ column <@ fun (u: User) -> u.Subject @> "ExternalSubject" ]).Items
+
+    let userSensitiveItems =
+        (overrides<User> [ sensitive <@ fun (u: User) -> u.Subject @> ]).Items
+
+    /// The three models of the todo app with their overrides and access.
+    let appModels =
+        [ (listModel, listItems, listAccess)
+          (todoModel, todoItems, todoAccess)
+          (userModel, [], userAccess) ]
+
+    let sqlServerSchema = schema SqlServer appModels
+
+    // ---- store helpers ----
+
+    let fixedNow () = DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)
+
+    let okOf (r: Result<'a, Finding>) : 'a =
+        match r with
+        | Ok v -> v
+        | Result.Error f -> failwithf "Expected Ok, got: %A" f
+
+    let errOf (r: Result<'a, Finding>) : Finding =
+        match r with
+        | Ok v -> failwithf "Expected an error, got Ok: %A" v
+        | Result.Error f -> f
+
+    let scopeOf (user: Guid) (modelName: string) : Scope =
+        match scopeFor rules user modelName with
+        | Ok s -> s
+        | Result.Error f -> failwithf "Expected a scope, got: %A" f
+
+    /// An open in-memory SQLite connection with the schema of the todo app.
+    let openDb () : SqliteConnection =
+        let conn = new SqliteConnection("Data Source=:memory:")
+        conn.Open()
+        use cmd = conn.CreateCommand()
+        cmd.CommandText <- schema Sqlite appModels
+        cmd.ExecuteNonQuery() |> ignore
+        conn
+
+    let newStore (conn: SqliteConnection) (hook: (int -> unit) option) : Store =
+        Store.create conn Sqlite fixedNow hook
+
+    let listRec (name: string) : Record =
+        { Model = "TodoList"
+          Id = Guid.Empty
+          Owner = None
+          Version = 0L
+          Values = Map [ "Name", box name; "Archived", box false ] }
+
+    let todoRec (listId: Guid) (title: string) (due: DateOnly option) : Record =
+        let baseValues =
+            Map [ "List", box listId
+                  "Title", box title
+                  "Priority", box "Normal"
+                  "Done", box false ]
+        let values =
+            match due with
+            | Some d -> baseValues.Add("Due", box d)
+            | None -> baseValues
+        { Model = "Todo"
+          Id = Guid.Empty
+          Owner = None
+          Version = 0L
+          Values = values }
+
+    let addList store (user: Guid) (name: string) : Record =
+        okOf (add store (scopeOf user "TodoList") listModel (listRec name))
+
+    let addTodo store (user: Guid) (listId: Guid) (title: string) (due: DateOnly option) : Record =
+        okOf (add store (scopeOf user "Todo") todoModel (todoRec listId title due))
+
+    let findTodo store (user: Guid) (id: Guid) =
+        find store (scopeOf user "Todo") todoModel id
+
+    let findList store (user: Guid) (id: Guid) =
+        find store (scopeOf user "TodoList") listModel id
+
+    let isGone (r: Result<Record, Finding>) : bool =
+        match r with
+        | Result.Error f -> f.Code = ErrorCode.NOT_FOUND
+        | Ok _ -> false
+
+    let pageTodos store (user: Guid) (listId: Guid) (cursor: string option) (size: int option) =
+        page store (scopeOf user "Todo") todoModel todoItems todoAccess None (Some listId) cursor size
+
+    // ---- shape helpers ----
+
+    let shapeFor (t: Type) : Shape =
+        shapeOf [ (describeOk t, [], todoAccess) ]
+
+    let tempDir () : string =
+        let d = Path.Combine(Path.GetTempPath(), "persist-" + Guid.NewGuid().ToString("N"))
+        Directory.CreateDirectory d |> ignore
+        d
+
+    let hasIndex (cols: string list) (ixs: IndexDef list) : bool =
+        ixs |> List.exists (fun i -> i.Columns = cols)
+
+open PersistFixtures
+
+module ``CAP-persist-R1`` =
+
+    [<Fact>]
+    let ``CAP-persist.R1 Todo gets a table with the five system columns`` () =
+        Assert.Contains("CREATE TABLE [Todo]", sqlServerSchema)
+        Assert.Contains("[Id] uniqueidentifier NOT NULL PRIMARY KEY", sqlServerSchema)
+        Assert.Contains("[Owner] uniqueidentifier NOT NULL", sqlServerSchema)
+        Assert.Contains("[Version] rowversion NOT NULL", sqlServerSchema)
+        Assert.Contains("[CreatedAt] datetimeoffset NOT NULL", sqlServerSchema)
+        Assert.Contains("[UpdatedAt] datetimeoffset NOT NULL", sqlServerSchema)
+
+    [<Fact>]
+    let ``CAP-persist.R1 User with the rule self has no Owner column`` () =
+        let sql = createTable SqlServer userModel [] userAccess
+        Assert.Contains("CREATE TABLE [User]", sql)
+        Assert.DoesNotContain("[Owner]", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R1 Todo with an owned rule keeps the Owner column`` () =
+        let sql = createTable SqlServer todoModel todoItems todoAccess
+        Assert.Contains("[Owner] uniqueidentifier NOT NULL", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R1 Todo fields get their SQL types`` () =
+        let sql = createTable SqlServer todoModel todoItems todoAccess
+        Assert.Contains("[Title] nvarchar(200) NOT NULL", sql)
+        Assert.Contains("[Notes] nvarchar(max) NULL", sql)
+        Assert.Contains("[Priority] varchar(16) NOT NULL", sql)
+        Assert.Contains("[Due] date NULL", sql)
+        Assert.Contains("[CompletedAt] datetimeoffset NULL", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R1 listSort on Due adds the persisted HasDue column`` () =
+        let sql = createTable SqlServer todoModel todoItems todoAccess
+        Assert.Contains("[HasDue] AS (CASE WHEN [Due] IS NULL THEN 0 ELSE 1 END) PERSISTED", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R1 no listSort means no HasDue column`` () =
+        let sql = createTable SqlServer todoModel [] todoAccess
+        Assert.DoesNotContain("HasDue", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R1 the column override renames Subject to ExternalSubject`` () =
+        let sql = createTable SqlServer userModel userColumnItems userAccess
+        Assert.Contains("[ExternalSubject] nvarchar(255) NOT NULL", sql)
+        Assert.DoesNotContain("[Subject]", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R1 the sensitive override leaves the schema as it was`` () =
+        let plain = schema SqlServer [ (userModel, [], userAccess) ]
+        let withSensitive = schema SqlServer [ (userModel, userSensitiveItems, userAccess) ]
+        Assert.Equal(plain, withSensitive)
+
+module ``CAP-persist-R2`` =
+
+    [<Fact>]
+    let ``CAP-persist.R2 Todo gets the foreign key FK_Todo_List`` () =
+        Assert.Contains(
+            "ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES [TodoList] ([Id]);",
+            sqlServerSchema
+        )
+
+    [<Fact>]
+    let ``CAP-persist.R2 foreignKeys lists the key of Todo once`` () =
+        let fks = foreignKeys todoModel todoItems todoAccess
+        Assert.Equal(1, fks.Length)
+        Assert.Contains("[FK_Todo_List]", fks.Head)
+
+    [<Fact>]
+    let ``CAP-persist.R2 the schema of the three models has no CASCADE`` () =
+        Assert.DoesNotContain("CASCADE", sqlServerSchema, StringComparison.OrdinalIgnoreCase)
+
+    [<Fact>]
+    let ``CAP-persist.R2 exactly one Todo index starts with the column List`` () =
+        let starts = indexes todoModel todoItems todoAccess |> List.filter (fun i -> i.Columns.Head = "List")
+        Assert.Equal(1, starts.Length)
+
+module ``CAP-persist-R3`` =
+
+    [<Fact>]
+    let ``CAP-persist.R3 TodoList with two views gets one index on Owner Archived Name Id`` () =
+        let ixs = indexes listModel listItems listAccess
+        let matching = ixs |> List.filter (fun i -> i.Columns = [ "Owner"; "Archived"; "Name"; "Id" ])
+        Assert.Equal(1, matching.Length)
+
+    [<Fact>]
+    let ``CAP-persist.R3 Todo with listSort on Due gets one index on Owner List HasDue Due Id`` () =
+        let ixs = indexes todoModel todoItems todoAccess
+        let matching = ixs |> List.filter (fun i -> i.Columns = [ "Owner"; "List"; "HasDue"; "Due"; "Id" ])
+        Assert.Equal(1, matching.Length)
+
+    [<Fact>]
+    let ``CAP-persist.R3 User gets a unique index on Subject and no list index`` () =
+        let ixs = indexes userModel [] userAccess
+        Assert.Equal(1, ixs.Length)
+        Assert.Equal<string list>([ "Subject" ], ixs.Head.Columns)
+        Assert.True ixs.Head.Unique
+
+    [<Fact>]
+    let ``CAP-persist.R3 an owned model with a title and no sort gets an index on Owner Heading Id`` () =
+        let ixs = indexes noteModel [] noteAccess
+        Assert.True(hasIndex [ "Owner"; "Heading"; "Id" ] ixs)
+
+module ``CAP-persist-R4`` =
+
+    let private todoQuery () =
+        (listQuery SqlServer todoModel todoItems todoAccess None false).Sql
+
+    [<Fact>]
+    let ``CAP-persist.R4 the Todo list query names its columns`` () =
+        Assert.StartsWith("SELECT TOP (@limit) [Id], [Title], [Due], [Priority], [Done] FROM [Todo]", todoQuery ())
+
+    [<Fact>]
+    let ``CAP-persist.R4 the Todo list query filters by owner and parent`` () =
+        let sql = todoQuery ()
+        Assert.Contains("WHERE", sql)
+        Assert.Contains("[Owner] = @owner", sql)
+        Assert.Contains("[List] = @parent", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R4 the Todo list query orders by HasDue descending then Due then Id`` () =
+        Assert.EndsWith("ORDER BY [HasDue] DESC, [Due] ASC, [Id] ASC", todoQuery ())
+
+    [<Fact>]
+    let ``CAP-persist.R4 the Archived view adds Archived = 1`` () =
+        let sql = (listQuery SqlServer listModel listItems listAccess (Some "Archived") false).Sql
+        Assert.Contains("[Archived] = 1", sql)
+        Assert.Contains("[Owner] = @owner", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R4 the Active view does not hold Archived = 1`` () =
+        let sql = (listQuery SqlServer listModel listItems listAccess (Some "Active") false).Sql
+        Assert.DoesNotContain("[Archived] = 1", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R4 TodoList with no listColumns selects Id Name Archived`` () =
+        let sql = (listQuery SqlServer listModel listItems listAccess None false).Sql
+        Assert.StartsWith("SELECT TOP (@limit) [Id], [Name], [Archived] FROM [TodoList]", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R4 Todo with no listColumns does not select the wide Notes column`` () =
+        let sql = (listQuery SqlServer todoModel [] todoAccess None false).Sql
+        Assert.DoesNotContain("[Notes]", sql)
+
+module ``CAP-persist-R5`` =
+
+    let private d1 = DateOnly(2026, 11, 3)
+    let private d2 = DateOnly(2026, 11, 1)
+    let private d3 = DateOnly(2026, 11, 2)
+
+    /// 25 todos in one list of u1: 6 with no due date, 19 spread over 3 dates, not in order.
+    /// Decoys: another list of u1 and a list of u2.
+    let private seed (store: Store) =
+        let list = addList store u1 "Groceries"
+        let other = addList store u1 "Other"
+        let foreign = addList store u2 "Foreign"
+        let dates = [| d1; d2; d3 |]
+        let dueOf i = if i % 4 = 0 then None else Some dates.[i % 3]
+        let todos = [ for i in 1..25 -> addTodo store u1 list.Id (sprintf "Todo %d" i) (dueOf i) ]
+        addTodo store u1 other.Id "Decoy one" (Some d1) |> ignore
+        addTodo store u2 foreign.Id "Decoy two" None |> ignore
+        list, todos
+
+    let private collectAll store (listId: Guid) (size: int) : Guid list =
+        let rec loop cursor acc n =
+            if n > 30 then failwith "Paging did not end after 30 pages"
+            let p = okOf (pageTodos store u1 listId cursor (Some size))
+            let acc = acc @ (p.Rows |> List.map (fun r -> r.Id))
+            match p.Next with
+            | Some c when not p.Rows.IsEmpty -> loop (Some c) acc (n + 1)
+            | _ -> acc
+        loop None [] 0
+
+    [<Fact>]
+    let ``CAP-persist.R5 paging 4 at a time returns each of the 25 todos one time`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, todos = seed store
+        let got = collectAll store list.Id 4
+        Assert.Equal(25, got.Length)
+        Assert.Equal(25, (got |> List.distinct).Length)
+        Assert.Equal<Guid list>((todos |> List.map (fun t -> t.Id) |> List.sort), List.sort got)
+
+    [<Fact>]
+    let ``CAP-persist.R5 paging follows the sort: dated todos by date, then the empty ones`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, todos = seed store
+        let dueById =
+            todos
+            |> List.map (fun t ->
+                t.Id,
+                (match t.Values.TryFind "Due" with
+                 | Some v when not (isNull v) -> (0, v :?> DateOnly)
+                 | _ -> (1, DateOnly.MinValue)))
+            |> Map.ofList
+        let keys = collectAll store list.Id 4 |> List.map (fun id -> dueById.[id])
+        Assert.Equal<(int * DateOnly) list>(List.sort keys, keys)
+        Assert.Equal(6, keys |> List.filter (fun (g, _) -> g = 1) |> List.length)
+        Assert.Equal(3, keys |> List.map snd |> List.filter (fun d -> d <> DateOnly.MinValue) |> List.distinct |> List.length)
+
+    [<Fact>]
+    let ``CAP-persist.R5 a page of size 1 runs one query`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, _ = seed store
+        let before = queryCount store
+        let p = okOf (pageTodos store u1 list.Id None (Some 1))
+        Assert.Equal(1, p.Rows.Length)
+        Assert.Equal(1, queryCount store - before)
+
+    [<Fact>]
+    let ``CAP-persist.R5 a page of size 200 runs one query`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, _ = seed store
+        let before = queryCount store
+        let p = okOf (pageTodos store u1 list.Id None (Some 200))
+        Assert.Equal(25, p.Rows.Length)
+        Assert.Equal(1, queryCount store - before)
+
+    [<Fact>]
+    let ``CAP-persist.R5 a page of 200 holds 200 of 210 todos`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list = addList store u1 "Groceries"
+        for i in 1..210 do
+            addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
+        let p = okOf (pageTodos store u1 list.Id None (Some 200))
+        Assert.Equal(200, p.Rows.Length)
+        Assert.True p.Next.IsSome
+
+    let private limitError (size: int) =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list = addList store u1 "Groceries"
+        errOf (pageTodos store u1 list.Id None (Some size))
+
+    [<Theory>]
+    [<InlineData(0)>]
+    [<InlineData(201)>]
+    [<InlineData(300)>]
+    [<InlineData(-1)>]
+    let ``CAP-persist.R5 a page size outside 1 to 200 gives VALIDATION_FAILED on limit`` (size: int) =
+        let f = limitError size
+        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
+        Assert.Equal(Some "limit", f.Field)
+        Assert.Equal("must be between 1 and 200", f.Message)
+
+    [<Fact>]
+    let ``CAP-persist.R5 a page size of 200 and a page size of 1 are accepted`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list = addList store u1 "Groceries"
+        addTodo store u1 list.Id "One" None |> ignore
+        Assert.Equal(1, (okOf (pageTodos store u1 list.Id None (Some 200))).Rows.Length)
+        Assert.Equal(1, (okOf (pageTodos store u1 list.Id None (Some 1))).Rows.Length)
+
+    [<Fact>]
+    let ``CAP-persist.R5 no page size gives a page of 50`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list = addList store u1 "Groceries"
+        for i in 1..55 do
+            addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
+        let p = okOf (pageTodos store u1 list.Id None None)
+        Assert.Equal(50, p.Rows.Length)
+        Assert.True p.Next.IsSome
+
+    [<Fact>]
+    let ``CAP-persist.R5 a bad cursor gives VALIDATION_FAILED on cursor`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list = addList store u1 "Groceries"
+        let f = errOf (pageTodos store u1 list.Id (Some "not-a-cursor") (Some 4))
+        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
+        Assert.Equal(Some "cursor", f.Field)
+        Assert.Equal("is not valid", f.Message)
+
+module ``CAP-persist-R6`` =
+
+    let private children = [ (todoModel, "List") ]
+
+    let private seedList (store: Store) (user: Guid) =
+        let list = addList store user "Old"
+        let todos = [ for i in 1..3 -> addTodo store user list.Id (sprintf "Todo %d" i) None ]
+        list, todos
+
+    [<Fact>]
+    let ``CAP-persist.R6 removing a list removes the list and its 3 todos`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, todos = seedList store u1
+        okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
+        Assert.True(isGone (findList store u1 list.Id))
+        for t in todos do
+            Assert.True(isGone (findTodo store u1 t.Id))
+
+    [<Fact>]
+    let ``CAP-persist.R6 removing a list keeps the todos of another list`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, _ = seedList store u1
+        let other = addList store u1 "Other"
+        let keep = addTodo store u1 other.Id "Keep" None
+        okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
+        Assert.False(isGone (findTodo store u1 keep.Id))
+
+    [<Fact>]
+    let ``CAP-persist.R6 a failure after the second todo leaves the list and all 3 todos`` () =
+        use conn = openDb ()
+        let hook n = if n = 2 then failwith "disk failure"
+        let store = newStore conn (Some hook)
+        let list, todos = seedList store u1
+        let outcome =
+            try
+                Some(remove store (scopeOf u1 "TodoList") listModel children list.Id)
+            with _ ->
+                None
+        match outcome with
+        | Some(Ok()) -> failwith "The remove passed although the hook failed"
+        | _ -> ()
+        Assert.False(isGone (findList store u1 list.Id))
+        for t in todos do
+            Assert.False(isGone (findTodo store u1 t.Id))
+
+    [<Fact>]
+    let ``CAP-persist.R6 u1 removing a list of u2 gives NOT_FOUND and the list stays`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list, todos = seedList store u2
+        let f = errOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
+        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
+        Assert.False(isGone (findList store u2 list.Id))
+        for t in todos do
+            Assert.False(isGone (findTodo store u2 t.Id))
+
+module ``CAP-persist-R7`` =
+
+    let private withTitle (title: string) (r: Record) : Record =
+        { r with Values = r.Values.Add("Title", box title) }
+
+    /// A todo of u1 at the given version, made by saves from version 1.
+    let private todoAt (store: Store) (target: int64) : Record =
+        let list = addList store u1 "Groceries"
+        let mutable t = addTodo store u1 list.Id "Original" None
+        for _ in 1L .. target - 1L do
+            let next = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some t.Version) t)
+            Assert.Equal(t.Version + 1L, next.Version)
+            t <- next
+        t
+
+    [<Fact>]
+    let ``CAP-persist.R7 a save with version 3 on a todo at version 3 passes and gives version 4`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let t = todoAt store 3L
+        Assert.Equal(3L, t.Version)
+        let saved = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
+        Assert.Equal(4L, saved.Version)
+        Assert.Equal(4L, (okOf (findTodo store u1 t.Id)).Version)
+
+    [<Fact>]
+    let ``CAP-persist.R7 a save with version 3 on a todo at version 4 gives STALE_VERSION`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let t = todoAt store 4L
+        let before = okOf (findTodo store u1 t.Id)
+        let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
+        Assert.Equal(ErrorCode.STALE_VERSION, f.Code)
+        let after = okOf (findTodo store u1 t.Id)
+        Assert.Equal(4L, after.Version)
+        Assert.Equal<Map<string, obj>>(before.Values, after.Values)
+
+    [<Fact>]
+    let ``CAP-persist.R7 a new todo is stored at version 1`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let list = addList store u1 "Groceries"
+        let t = addTodo store u1 list.Id "New" None
+        Assert.Equal(1L, t.Version)
+        Assert.Equal(1L, (okOf (findTodo store u1 t.Id)).Version)
+
+    [<Fact>]
+    let ``CAP-persist.R7 a save with no version gives VALIDATION_FAILED on Version`` () =
+        use conn = openDb ()
+        let store = newStore conn None
+        let t = todoAt store 3L
+        let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id None (withTitle "Changed" t))
+        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
+        Assert.Equal(Some "Version", f.Field)
+        Assert.Equal("is required", f.Message)
+        Assert.Equal(3L, (okOf (findTodo store u1 t.Id)).Version)
+
+module ``CAP-persist-R8`` =
+
+    let private v1 () = shapeFor typeof<Todo>
+
+    let private migrationSql (previous: Shape option) (next: Shape) (renames: Rename list) : string =
+        okOf (migration previous next renames)
+
+    [<Fact>]
+    let ``CAP-persist.R8 a renamed column with no renamedFrom gives UNPLANNED_DROP`` () =
+        let f = errOf (migration (Some(v1 ())) (shapeFor typeof<DueRenamed.Todo>) [])
+        Assert.Equal(ErrorCode.UNPLANNED_DROP, f.Code)
+        Assert.Equal("Todo.Due would be dropped. Add renamedFrom or an explicit drop.", f.Message)
+
+    [<Fact>]
+    let ``CAP-persist.R8 renamedFrom Due on DueDate gives sp_rename`` () =
+        let rename = { Model = "Todo"; From = "Due"; To = "DueDate" }
+        let sql = migrationSql (Some(v1 ())) (shapeFor typeof<DueRenamed.Todo>) [ rename ]
+        Assert.Contains("EXEC sp_rename 'Todo.Due', 'DueDate', 'COLUMN';", sql)
+        Assert.DoesNotContain("DROP COLUMN", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R8 one more optional column gives ALTER TABLE ADD`` () =
+        let sql = migrationSql (Some(v1 ())) (shapeFor typeof<WithTags.Todo>) []
+        Assert.Contains("ALTER TABLE [Todo] ADD [Tags] nvarchar(200) NULL", sql)
+
+    [<Fact>]
+    let ``CAP-persist.R8 one more required text column gives UNSAFE_MIGRATION`` () =
+        let f = errOf (migration (Some(v1 ())) (shapeFor typeof<WithCode.Todo>) [])
+        Assert.Equal(ErrorCode.UNSAFE_MIGRATION, f.Code)
+        Assert.Equal("Todo.Code is required and has no default, so existing rows would fail.", f.Message)
+
+    [<Fact>]
+    let ``CAP-persist.R8 no stored shape gives the full schema`` () =
+        let models = [ (todoModel, [], todoAccess) ]
+        let sql = migrationSql None (shapeOf models) []
+        Assert.Equal(schema SqlServer models, sql)
+
+    [<Fact>]
+    let ``CAP-persist.R8 a written shape reads back the same`` () =
+        let dir = tempDir ()
+        try
+            let path = Path.Combine(dir, "shape.json")
+            let shape = v1 ()
+            writeShape path shape
+            Assert.Equal(Some shape, okOf (readShape path))
+        finally
+            Directory.Delete(dir, true)
+
+    [<Fact>]
+    let ``CAP-persist.R8 no shape file reads as None`` () =
+        let dir = tempDir ()
+        try
+            Assert.Equal(None, okOf (readShape (Path.Combine(dir, "shape.json"))))
+        finally
+            Directory.Delete(dir, true)
+
+    [<Fact>]
+    let ``CAP-persist.R8 a shape file that is not JSON gives SHAPE_UNREADABLE`` () =
+        let dir = tempDir ()
+        try
+            let path = Path.Combine(dir, "shape.json")
+            File.WriteAllText(path, "{ this is not json")
+            let f = errOf (readShape path)
+            Assert.Equal(ErrorCode.SHAPE_UNREADABLE, f.Code)
+            Assert.Equal("build/shape.json cannot be read. Delete it to start from an empty shape.", f.Message)
+        finally
+            Directory.Delete(dir, true)
diff --git a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
index a5e5625..f71d127 100644
--- a/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
+++ b/tests/Typed.Engine.Tests/Typed.Engine.Tests.fsproj
@@ -9,9 +9,11 @@
     <Compile Include="CAP-models.Tests.fs" />
     <Compile Include="CAP-access.Tests.fs" />
     <Compile Include="CAP-validate.Tests.fs" />
+    <Compile Include="CAP-persist.Tests.fs" />
   </ItemGroup>
 
   <ItemGroup>
+    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.11" />
     <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
     <PackageReference Include="xunit" Version="2.6.2" />
     <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
````

### tests/Typed.Engine.Tests/CAP-persist.Tests.fs

````
namespace Typed.Engine.Tests

open System
open System.IO
open Microsoft.Data.Sqlite
open Xunit
open Typed.Engine.Model
open Typed.Engine.Overrides
open Typed.Engine.Access
open Typed.Engine.Persist
open Typed.Engine.Tests.ModelsFixtures
open Typed.Engine.Tests.AccessFixtures

/// Models, overrides, and store helpers of the persistence checks. The checks use only these.
module PersistFixtures =

    /// An owned model with a title field, no listSort, and no listView.
    type Note = { Heading: Title; Body: Notes }

    /// Shapes of Todo for the migration checks. The model name is "Todo" in each module.
    module DueRenamed =
        type Todo =
            { List: Ref<TodoList>
              Title: Title
              Notes: Notes option
              Priority: Priority
              DueDate: DateOnly option
              Done: bool
              CompletedAt: DateTimeOffset option }

    module WithTags =
        type Todo =
            { List: Ref<TodoList>
              Title: Title
              Notes: Notes option
              Priority: Priority
              Due: DateOnly option
              Done: bool
              CompletedAt: DateTimeOffset option
              Tags: Title option }

    module WithCode =
        type Todo =
            { List: Ref<TodoList>
              Title: Title
              Notes: Notes option
              Priority: Priority
              Due: DateOnly option
              Done: bool
              CompletedAt: DateTimeOffset option
              Code: Title }

    let accessOf (modelName: string) : Access =
        (rules |> List.find (fun r -> r.ModelName = modelName)).Access

    let todoAccess = accessOf "Todo"
    let listAccess = accessOf "TodoList"
    let userAccess = accessOf "User"
    let noteAccess = (rule<Note> owned).Access

    let todoModel = describeOk typeof<Todo>
    let listModel = describeOk typeof<TodoList>
    let userModel = describeOk typeof<User>
    let noteModel = describeOk typeof<Note>

    let todoItems =
        (overrides<Todo>
            [ listColumns
                  [ col <@ fun (t: Todo) -> t.Title @>
                    col <@ fun (t: Todo) -> t.Due @>
                    col <@ fun (t: Todo) -> t.Priority @>
                    col <@ fun (t: Todo) -> t.Done @> ]
              listSort <@ fun (t: Todo) -> t.Due @> Ascending ])
            .Items

    let listItems =
        (overrides<TodoList>
            [ listView "Active" <@ fun (l: TodoList) -> not l.Archived @>
              listView "Archived" <@ fun (l: TodoList) -> l.Archived @> ])
            .Items

    let userColumnItems =
        (overrides<User> [ column <@ fun (u: User) -> u.Subject @> "ExternalSubject" ]).Items

    let userSensitiveItems =
        (overrides<User> [ sensitive <@ fun (u: User) -> u.Subject @> ]).Items

    /// The three models of the todo app with their overrides and access.
    let appModels =
        [ (listModel, listItems, listAccess)
          (todoModel, todoItems, todoAccess)
          (userModel, [], userAccess) ]

    let sqlServerSchema = schema SqlServer appModels

    // ---- store helpers ----

    let fixedNow () = DateTimeOffset(2026, 10, 19, 12, 0, 0, TimeSpan.Zero)

    let okOf (r: Result<'a, Finding>) : 'a =
        match r with
        | Ok v -> v
        | Result.Error f -> failwithf "Expected Ok, got: %A" f

    let errOf (r: Result<'a, Finding>) : Finding =
        match r with
        | Ok v -> failwithf "Expected an error, got Ok: %A" v
        | Result.Error f -> f

    let scopeOf (user: Guid) (modelName: string) : Scope =
        match scopeFor rules user modelName with
        | Ok s -> s
        | Result.Error f -> failwithf "Expected a scope, got: %A" f

    /// An open in-memory SQLite connection with the schema of the todo app.
    let openDb () : SqliteConnection =
        let conn = new SqliteConnection("Data Source=:memory:")
        conn.Open()
        use cmd = conn.CreateCommand()
        cmd.CommandText <- schema Sqlite appModels
        cmd.ExecuteNonQuery() |> ignore
        conn

    let newStore (conn: SqliteConnection) (hook: (int -> unit) option) : Store =
        Store.create conn Sqlite fixedNow hook

    let listRec (name: string) : Record =
        { Model = "TodoList"
          Id = Guid.Empty
          Owner = None
          Version = 0L
          Values = Map [ "Name", box name; "Archived", box false ] }

    let todoRec (listId: Guid) (title: string) (due: DateOnly option) : Record =
        let baseValues =
            Map [ "List", box listId
                  "Title", box title
                  "Priority", box "Normal"
                  "Done", box false ]
        let values =
            match due with
            | Some d -> baseValues.Add("Due", box d)
            | None -> baseValues
        { Model = "Todo"
          Id = Guid.Empty
          Owner = None
          Version = 0L
          Values = values }

    let addList store (user: Guid) (name: string) : Record =
        okOf (add store (scopeOf user "TodoList") listModel (listRec name))

    let addTodo store (user: Guid) (listId: Guid) (title: string) (due: DateOnly option) : Record =
        okOf (add store (scopeOf user "Todo") todoModel (todoRec listId title due))

    let findTodo store (user: Guid) (id: Guid) =
        find store (scopeOf user "Todo") todoModel id

    let findList store (user: Guid) (id: Guid) =
        find store (scopeOf user "TodoList") listModel id

    let isGone (r: Result<Record, Finding>) : bool =
        match r with
        | Result.Error f -> f.Code = ErrorCode.NOT_FOUND
        | Ok _ -> false

    let pageTodos store (user: Guid) (listId: Guid) (cursor: string option) (size: int option) =
        page store (scopeOf user "Todo") todoModel todoItems todoAccess None (Some listId) cursor size

    // ---- shape helpers ----

    let shapeFor (t: Type) : Shape =
        shapeOf [ (describeOk t, [], todoAccess) ]

    let tempDir () : string =
        let d = Path.Combine(Path.GetTempPath(), "persist-" + Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory d |> ignore
        d

    let hasIndex (cols: string list) (ixs: IndexDef list) : bool =
        ixs |> List.exists (fun i -> i.Columns = cols)

open PersistFixtures

module ``CAP-persist-R1`` =

    [<Fact>]
    let ``CAP-persist.R1 Todo gets a table with the five system columns`` () =
        Assert.Contains("CREATE TABLE [Todo]", sqlServerSchema)
        Assert.Contains("[Id] uniqueidentifier NOT NULL PRIMARY KEY", sqlServerSchema)
        Assert.Contains("[Owner] uniqueidentifier NOT NULL", sqlServerSchema)
        Assert.Contains("[Version] rowversion NOT NULL", sqlServerSchema)
        Assert.Contains("[CreatedAt] datetimeoffset NOT NULL", sqlServerSchema)
        Assert.Contains("[UpdatedAt] datetimeoffset NOT NULL", sqlServerSchema)

    [<Fact>]
    let ``CAP-persist.R1 User with the rule self has no Owner column`` () =
        let sql = createTable SqlServer userModel [] userAccess
        Assert.Contains("CREATE TABLE [User]", sql)
        Assert.DoesNotContain("[Owner]", sql)

    [<Fact>]
    let ``CAP-persist.R1 Todo with an owned rule keeps the Owner column`` () =
        let sql = createTable SqlServer todoModel todoItems todoAccess
        Assert.Contains("[Owner] uniqueidentifier NOT NULL", sql)

    [<Fact>]
    let ``CAP-persist.R1 Todo fields get their SQL types`` () =
        let sql = createTable SqlServer todoModel todoItems todoAccess
        Assert.Contains("[Title] nvarchar(200) NOT NULL", sql)
        Assert.Contains("[Notes] nvarchar(max) NULL", sql)
        Assert.Contains("[Priority] varchar(16) NOT NULL", sql)
        Assert.Contains("[Due] date NULL", sql)
        Assert.Contains("[CompletedAt] datetimeoffset NULL", sql)

    [<Fact>]
    let ``CAP-persist.R1 listSort on Due adds the persisted HasDue column`` () =
        let sql = createTable SqlServer todoModel todoItems todoAccess
        Assert.Contains("[HasDue] AS (CASE WHEN [Due] IS NULL THEN 0 ELSE 1 END) PERSISTED", sql)

    [<Fact>]
    let ``CAP-persist.R1 no listSort means no HasDue column`` () =
        let sql = createTable SqlServer todoModel [] todoAccess
        Assert.DoesNotContain("HasDue", sql)

    [<Fact>]
    let ``CAP-persist.R1 the column override renames Subject to ExternalSubject`` () =
        let sql = createTable SqlServer userModel userColumnItems userAccess
        Assert.Contains("[ExternalSubject] nvarchar(255) NOT NULL", sql)
        Assert.DoesNotContain("[Subject]", sql)

    [<Fact>]
    let ``CAP-persist.R1 the sensitive override leaves the schema as it was`` () =
        let plain = schema SqlServer [ (userModel, [], userAccess) ]
        let withSensitive = schema SqlServer [ (userModel, userSensitiveItems, userAccess) ]
        Assert.Equal(plain, withSensitive)

module ``CAP-persist-R2`` =

    [<Fact>]
    let ``CAP-persist.R2 Todo gets the foreign key FK_Todo_List`` () =
        Assert.Contains(
            "ALTER TABLE [Todo] ADD CONSTRAINT [FK_Todo_List] FOREIGN KEY ([List]) REFERENCES [TodoList] ([Id]);",
            sqlServerSchema
        )

    [<Fact>]
    let ``CAP-persist.R2 foreignKeys lists the key of Todo once`` () =
        let fks = foreignKeys todoModel todoItems todoAccess
        Assert.Equal(1, fks.Length)
        Assert.Contains("[FK_Todo_List]", fks.Head)

    [<Fact>]
    let ``CAP-persist.R2 the schema of the three models has no CASCADE`` () =
        Assert.DoesNotContain("CASCADE", sqlServerSchema, StringComparison.OrdinalIgnoreCase)

    [<Fact>]
    let ``CAP-persist.R2 exactly one Todo index starts with the column List`` () =
        let starts = indexes todoModel todoItems todoAccess |> List.filter (fun i -> i.Columns.Head = "List")
        Assert.Equal(1, starts.Length)

module ``CAP-persist-R3`` =

    [<Fact>]
    let ``CAP-persist.R3 TodoList with two views gets one index on Owner Archived Name Id`` () =
        let ixs = indexes listModel listItems listAccess
        let matching = ixs |> List.filter (fun i -> i.Columns = [ "Owner"; "Archived"; "Name"; "Id" ])
        Assert.Equal(1, matching.Length)

    [<Fact>]
    let ``CAP-persist.R3 Todo with listSort on Due gets one index on Owner List HasDue Due Id`` () =
        let ixs = indexes todoModel todoItems todoAccess
        let matching = ixs |> List.filter (fun i -> i.Columns = [ "Owner"; "List"; "HasDue"; "Due"; "Id" ])
        Assert.Equal(1, matching.Length)

    [<Fact>]
    let ``CAP-persist.R3 User gets a unique index on Subject and no list index`` () =
        let ixs = indexes userModel [] userAccess
        Assert.Equal(1, ixs.Length)
        Assert.Equal<string list>([ "Subject" ], ixs.Head.Columns)
        Assert.True ixs.Head.Unique

    [<Fact>]
    let ``CAP-persist.R3 an owned model with a title and no sort gets an index on Owner Heading Id`` () =
        let ixs = indexes noteModel [] noteAccess
        Assert.True(hasIndex [ "Owner"; "Heading"; "Id" ] ixs)

module ``CAP-persist-R4`` =

    let private todoQuery () =
        (listQuery SqlServer todoModel todoItems todoAccess None false).Sql

    [<Fact>]
    let ``CAP-persist.R4 the Todo list query names its columns`` () =
        Assert.StartsWith("SELECT TOP (@limit) [Id], [Title], [Due], [Priority], [Done] FROM [Todo]", todoQuery ())

    [<Fact>]
    let ``CAP-persist.R4 the Todo list query filters by owner and parent`` () =
        let sql = todoQuery ()
        Assert.Contains("WHERE", sql)
        Assert.Contains("[Owner] = @owner", sql)
        Assert.Contains("[List] = @parent", sql)

    [<Fact>]
    let ``CAP-persist.R4 the Todo list query orders by HasDue descending then Due then Id`` () =
        Assert.EndsWith("ORDER BY [HasDue] DESC, [Due] ASC, [Id] ASC", todoQuery ())

    [<Fact>]
    let ``CAP-persist.R4 the Archived view adds Archived = 1`` () =
        let sql = (listQuery SqlServer listModel listItems listAccess (Some "Archived") false).Sql
        Assert.Contains("[Archived] = 1", sql)
        Assert.Contains("[Owner] = @owner", sql)

    [<Fact>]
    let ``CAP-persist.R4 the Active view does not hold Archived = 1`` () =
        let sql = (listQuery SqlServer listModel listItems listAccess (Some "Active") false).Sql
        Assert.DoesNotContain("[Archived] = 1", sql)

    [<Fact>]
    let ``CAP-persist.R4 TodoList with no listColumns selects Id Name Archived`` () =
        let sql = (listQuery SqlServer listModel listItems listAccess None false).Sql
        Assert.StartsWith("SELECT TOP (@limit) [Id], [Name], [Archived] FROM [TodoList]", sql)

    [<Fact>]
    let ``CAP-persist.R4 Todo with no listColumns does not select the wide Notes column`` () =
        let sql = (listQuery SqlServer todoModel [] todoAccess None false).Sql
        Assert.DoesNotContain("[Notes]", sql)

module ``CAP-persist-R5`` =

    let private d1 = DateOnly(2026, 11, 3)
    let private d2 = DateOnly(2026, 11, 1)
    let private d3 = DateOnly(2026, 11, 2)

    /// 25 todos in one list of u1: 6 with no due date, 19 spread over 3 dates, not in order.
    /// Decoys: another list of u1 and a list of u2.
    let private seed (store: Store) =
        let list = addList store u1 "Groceries"
        let other = addList store u1 "Other"
        let foreign = addList store u2 "Foreign"
        let dates = [| d1; d2; d3 |]
        let dueOf i = if i % 4 = 0 then None else Some dates.[i % 3]
        let todos = [ for i in 1..25 -> addTodo store u1 list.Id (sprintf "Todo %d" i) (dueOf i) ]
        addTodo store u1 other.Id "Decoy one" (Some d1) |> ignore
        addTodo store u2 foreign.Id "Decoy two" None |> ignore
        list, todos

    let private collectAll store (listId: Guid) (size: int) : Guid list =
        let rec loop cursor acc n =
            if n > 30 then failwith "Paging did not end after 30 pages"
            let p = okOf (pageTodos store u1 listId cursor (Some size))
            let acc = acc @ (p.Rows |> List.map (fun r -> r.Id))
            match p.Next with
            | Some c when not p.Rows.IsEmpty -> loop (Some c) acc (n + 1)
            | _ -> acc
        loop None [] 0

    [<Fact>]
    let ``CAP-persist.R5 paging 4 at a time returns each of the 25 todos one time`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seed store
        let got = collectAll store list.Id 4
        Assert.Equal(25, got.Length)
        Assert.Equal(25, (got |> List.distinct).Length)
        Assert.Equal<Guid list>((todos |> List.map (fun t -> t.Id) |> List.sort), List.sort got)

    [<Fact>]
    let ``CAP-persist.R5 paging follows the sort: dated todos by date, then the empty ones`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seed store
        let dueById =
            todos
            |> List.map (fun t ->
                t.Id,
                (match t.Values.TryFind "Due" with
                 | Some v when not (isNull v) -> (0, v :?> DateOnly)
                 | _ -> (1, DateOnly.MinValue)))
            |> Map.ofList
        let keys = collectAll store list.Id 4 |> List.map (fun id -> dueById.[id])
        Assert.Equal<(int * DateOnly) list>(List.sort keys, keys)
        Assert.Equal(6, keys |> List.filter (fun (g, _) -> g = 1) |> List.length)
        Assert.Equal(3, keys |> List.map snd |> List.filter (fun d -> d <> DateOnly.MinValue) |> List.distinct |> List.length)

    [<Fact>]
    let ``CAP-persist.R5 a page of size 1 runs one query`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, _ = seed store
        let before = queryCount store
        let p = okOf (pageTodos store u1 list.Id None (Some 1))
        Assert.Equal(1, p.Rows.Length)
        Assert.Equal(1, queryCount store - before)

    [<Fact>]
    let ``CAP-persist.R5 a page of size 200 runs one query`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, _ = seed store
        let before = queryCount store
        let p = okOf (pageTodos store u1 list.Id None (Some 200))
        Assert.Equal(25, p.Rows.Length)
        Assert.Equal(1, queryCount store - before)

    [<Fact>]
    let ``CAP-persist.R5 a page of 200 holds 200 of 210 todos`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        for i in 1..210 do
            addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
        let p = okOf (pageTodos store u1 list.Id None (Some 200))
        Assert.Equal(200, p.Rows.Length)
        Assert.True p.Next.IsSome

    let private limitError (size: int) =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        errOf (pageTodos store u1 list.Id None (Some size))

    [<Theory>]
    [<InlineData(0)>]
    [<InlineData(201)>]
    [<InlineData(300)>]
    [<InlineData(-1)>]
    let ``CAP-persist.R5 a page size outside 1 to 200 gives VALIDATION_FAILED on limit`` (size: int) =
        let f = limitError size
        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
        Assert.Equal(Some "limit", f.Field)
        Assert.Equal("must be between 1 and 200", f.Message)

    [<Fact>]
    let ``CAP-persist.R5 a page size of 200 and a page size of 1 are accepted`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        addTodo store u1 list.Id "One" None |> ignore
        Assert.Equal(1, (okOf (pageTodos store u1 list.Id None (Some 200))).Rows.Length)
        Assert.Equal(1, (okOf (pageTodos store u1 list.Id None (Some 1))).Rows.Length)

    [<Fact>]
    let ``CAP-persist.R5 no page size gives a page of 50`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        for i in 1..55 do
            addTodo store u1 list.Id (sprintf "Todo %d" i) None |> ignore
        let p = okOf (pageTodos store u1 list.Id None None)
        Assert.Equal(50, p.Rows.Length)
        Assert.True p.Next.IsSome

    [<Fact>]
    let ``CAP-persist.R5 a bad cursor gives VALIDATION_FAILED on cursor`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        let f = errOf (pageTodos store u1 list.Id (Some "not-a-cursor") (Some 4))
        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
        Assert.Equal(Some "cursor", f.Field)
        Assert.Equal("is not valid", f.Message)

module ``CAP-persist-R6`` =

    let private children = [ (todoModel, "List") ]

    let private seedList (store: Store) (user: Guid) =
        let list = addList store user "Old"
        let todos = [ for i in 1..3 -> addTodo store user list.Id (sprintf "Todo %d" i) None ]
        list, todos

    [<Fact>]
    let ``CAP-persist.R6 removing a list removes the list and its 3 todos`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seedList store u1
        okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
        Assert.True(isGone (findList store u1 list.Id))
        for t in todos do
            Assert.True(isGone (findTodo store u1 t.Id))

    [<Fact>]
    let ``CAP-persist.R6 removing a list keeps the todos of another list`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, _ = seedList store u1
        let other = addList store u1 "Other"
        let keep = addTodo store u1 other.Id "Keep" None
        okOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
        Assert.False(isGone (findTodo store u1 keep.Id))

    [<Fact>]
    let ``CAP-persist.R6 a failure after the second todo leaves the list and all 3 todos`` () =
        use conn = openDb ()
        let hook n = if n = 2 then failwith "disk failure"
        let store = newStore conn (Some hook)
        let list, todos = seedList store u1
        let outcome =
            try
                Some(remove store (scopeOf u1 "TodoList") listModel children list.Id)
            with _ ->
                None
        match outcome with
        | Some(Ok()) -> failwith "The remove passed although the hook failed"
        | _ -> ()
        Assert.False(isGone (findList store u1 list.Id))
        for t in todos do
            Assert.False(isGone (findTodo store u1 t.Id))

    [<Fact>]
    let ``CAP-persist.R6 u1 removing a list of u2 gives NOT_FOUND and the list stays`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list, todos = seedList store u2
        let f = errOf (remove store (scopeOf u1 "TodoList") listModel children list.Id)
        Assert.Equal(ErrorCode.NOT_FOUND, f.Code)
        Assert.False(isGone (findList store u2 list.Id))
        for t in todos do
            Assert.False(isGone (findTodo store u2 t.Id))

module ``CAP-persist-R7`` =

    let private withTitle (title: string) (r: Record) : Record =
        { r with Values = r.Values.Add("Title", box title) }

    /// A todo of u1 at the given version, made by saves from version 1.
    let private todoAt (store: Store) (target: int64) : Record =
        let list = addList store u1 "Groceries"
        let mutable t = addTodo store u1 list.Id "Original" None
        for _ in 1L .. target - 1L do
            let next = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some t.Version) t)
            Assert.Equal(t.Version + 1L, next.Version)
            t <- next
        t

    [<Fact>]
    let ``CAP-persist.R7 a save with version 3 on a todo at version 3 passes and gives version 4`` () =
        use conn = openDb ()
        let store = newStore conn None
        let t = todoAt store 3L
        Assert.Equal(3L, t.Version)
        let saved = okOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
        Assert.Equal(4L, saved.Version)
        Assert.Equal(4L, (okOf (findTodo store u1 t.Id)).Version)

    [<Fact>]
    let ``CAP-persist.R7 a save with version 3 on a todo at version 4 gives STALE_VERSION`` () =
        use conn = openDb ()
        let store = newStore conn None
        let t = todoAt store 4L
        let before = okOf (findTodo store u1 t.Id)
        let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id (Some 3L) (withTitle "Changed" t))
        Assert.Equal(ErrorCode.STALE_VERSION, f.Code)
        let after = okOf (findTodo store u1 t.Id)
        Assert.Equal(4L, after.Version)
        Assert.Equal<Map<string, obj>>(before.Values, after.Values)

    [<Fact>]
    let ``CAP-persist.R7 a new todo is stored at version 1`` () =
        use conn = openDb ()
        let store = newStore conn None
        let list = addList store u1 "Groceries"
        let t = addTodo store u1 list.Id "New" None
        Assert.Equal(1L, t.Version)
        Assert.Equal(1L, (okOf (findTodo store u1 t.Id)).Version)

    [<Fact>]
    let ``CAP-persist.R7 a save with no version gives VALIDATION_FAILED on Version`` () =
        use conn = openDb ()
        let store = newStore conn None
        let t = todoAt store 3L
        let f = errOf (revise store (scopeOf u1 "Todo") todoModel t.Id None (withTitle "Changed" t))
        Assert.Equal(ErrorCode.VALIDATION_FAILED, f.Code)
        Assert.Equal(Some "Version", f.Field)
        Assert.Equal("is required", f.Message)
        Assert.Equal(3L, (okOf (findTodo store u1 t.Id)).Version)

module ``CAP-persist-R8`` =

    let private v1 () = shapeFor typeof<Todo>

    let private migrationSql (previous: Shape option) (next: Shape) (renames: Rename list) : string =
        okOf (migration previous next renames)

    [<Fact>]
    let ``CAP-persist.R8 a renamed column with no renamedFrom gives UNPLANNED_DROP`` () =
        let f = errOf (migration (Some(v1 ())) (shapeFor typeof<DueRenamed.Todo>) [])
        Assert.Equal(ErrorCode.UNPLANNED_DROP, f.Code)
        Assert.Equal("Todo.Due would be dropped. Add renamedFrom or an explicit drop.", f.Message)

    [<Fact>]
    let ``CAP-persist.R8 renamedFrom Due on DueDate gives sp_rename`` () =
        let rename = { Model = "Todo"; From = "Due"; To = "DueDate" }
        let sql = migrationSql (Some(v1 ())) (shapeFor typeof<DueRenamed.Todo>) [ rename ]
        Assert.Contains("EXEC sp_rename 'Todo.Due', 'DueDate', 'COLUMN';", sql)
        Assert.DoesNotContain("DROP COLUMN", sql)

    [<Fact>]
    let ``CAP-persist.R8 one more optional column gives ALTER TABLE ADD`` () =
        let sql = migrationSql (Some(v1 ())) (shapeFor typeof<WithTags.Todo>) []
        Assert.Contains("ALTER TABLE [Todo] ADD [Tags] nvarchar(200) NULL", sql)

    [<Fact>]
    let ``CAP-persist.R8 one more required text column gives UNSAFE_MIGRATION`` () =
        let f = errOf (migration (Some(v1 ())) (shapeFor typeof<WithCode.Todo>) [])
        Assert.Equal(ErrorCode.UNSAFE_MIGRATION, f.Code)
        Assert.Equal("Todo.Code is required and has no default, so existing rows would fail.", f.Message)

    [<Fact>]
    let ``CAP-persist.R8 no stored shape gives the full schema`` () =
        let models = [ (todoModel, [], todoAccess) ]
        let sql = migrationSql None (shapeOf models) []
        Assert.Equal(schema SqlServer models, sql)

    [<Fact>]
    let ``CAP-persist.R8 a written shape reads back the same`` () =
        let dir = tempDir ()
        try
            let path = Path.Combine(dir, "shape.json")
            let shape = v1 ()
            writeShape path shape
            Assert.Equal(Some shape, okOf (readShape path))
        finally
            Directory.Delete(dir, true)

    [<Fact>]
    let ``CAP-persist.R8 no shape file reads as None`` () =
        let dir = tempDir ()
        try
            Assert.Equal(None, okOf (readShape (Path.Combine(dir, "shape.json"))))
        finally
            Directory.Delete(dir, true)

    [<Fact>]
    let ``CAP-persist.R8 a shape file that is not JSON gives SHAPE_UNREADABLE`` () =
        let dir = tempDir ()
        try
            let path = Path.Combine(dir, "shape.json")
            File.WriteAllText(path, "{ this is not json")
            let f = errOf (readShape path)
            Assert.Equal(ErrorCode.SHAPE_UNREADABLE, f.Code)
            Assert.Equal("build/shape.json cannot be read. Delete it to start from an empty shape.", f.Message)
        finally
            Directory.Delete(dir, true)
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
    <Compile Include="CAP-validate.Tests.fs" />
    <Compile Include="CAP-persist.Tests.fs" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.11" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.2" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/Typed.Engine/Typed.Engine.fsproj" />
  </ItemGroup>

</Project>
````

