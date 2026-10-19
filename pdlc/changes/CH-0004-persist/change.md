# CH-0004 · Persist

Intent: IN-0001 · Scope: CAP-persist · Status: testing · Ticket: none · Branch: pdlc/CH-0004-persist · Base: pdlc/CH-0003-validate
Tests-Locked: 1e2b637

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
  memory. The Interface settled the curried `Store.create`, the stored forms in
  `Record.Values`, and the record shapes. Red: the checks fail to compile, because
  `Typed.Engine.Persist` does not exist yet. Locked at 1e2b637.
