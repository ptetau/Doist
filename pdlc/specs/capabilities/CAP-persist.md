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