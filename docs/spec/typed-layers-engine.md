# Typed Layers Engine and Todo App Spec

Oct 18, 2026 · @Patrick Te Tau

## Summary

The engine turns typed data models into a working, secure web app: access control, storage, validation, screens and an API. You write code only for exceptions and new behaviour.

- Fields get their behaviour from their type and name.
- Each type has a default in every engine, written once and shared by all models.
- Overrides change one field in one engine. They should be rare.
- Access rules say who can see and change each model. Every model must have one.
- Hooks hold the small amount of real business logic.
- The build fails when anything is missing or doesn't fit: a type with no default, a model with no access rule, or an override that points at the wrong field or config.

The todo list app is the first test. Its target is 3 access rules, about 10 lines of overrides and 2 hooks, with everything else derived.

### Decisions made

| Topic | Decision |
| --- | --- |
| Language | F# on .NET |
| UI | Server-rendered HTML with htmx, so validation rules run in one place |
| Database | SQL Server |
| Sign-in | External OpenID Connect provider. The API serves only this app's own pages, using the same session. |
| Ownership | Every owned model carries its own `Owner` field, copied from its parent where it has one |
| Deleting | Hard delete. Deleting a parent deletes its children. Archiving is the soft option. |
| API errors | RFC 9457 problem details, with errors listed per field |

## Concepts

| Term | What it is | Example |
| --- | --- | --- |
| Model | A record type describing one kind of thing | `Todo` |
| Semantic type | A small wrapper type that carries meaning and rules | `Title`, `Notes` |
| Kind | The basic shape an engine must handle for every type | see list below |
| Engine | Code that does one job across all models | Access, Persist, Validate, Display, API |
| Default | What an engine does for a type when nothing else is said | `Title` shows as a text box |
| Override | A change to one field in one engine, from a fixed catalogue | label `Due` as "Due date" |
| Access rule | Who can see and change records of a model | only the owner |
| Hook | Hand-written code acting on a whole record | set `CompletedAt` when `Done` becomes true |
| System field | A field the engine adds and sets itself | `Id`, `Owner`, `Version` |
| Generator | Build step that reads models and wires engines to them | reports a missing default as a build error |

**Kinds in version 1:** text, whole number, decimal, boolean, enum, date, timestamp, id, reference, optional. Lists are added later, by the tags test.

Every semantic type is built on one kind. An engine must handle every kind, and may add a more specific default for a semantic type.

**Wide text** is any text type allowing more than 400 characters. It's left out of lists and never indexed.

**Two rules keep config small:**

1. Prefer a new semantic type over a field override. If two fields need the same override, make a type.
2. Prefer an override over a hook. Use a hook only when behaviour depends on other fields, time or events.

## How behaviour is decided

### Per field

For each field, each engine uses the first match in this order:

1. An override for this field in this engine.
2. The default for the field's semantic type.
3. The default for the field's kind.

Names add a few defaults on top. The field name becomes the label, split into words: `CompletedAt` becomes "Completed at". An override replaces it.

### Per record

Hooks act on whole records, after field behaviour is applied. There are four hook points:

| Hook | When it runs | What it can do |
| --- | --- | --- |
| Before save | Inside the save transaction, after field validation passes | Change the record, or reject it with field errors |
| After save | After the transaction commits | Side effects such as notifications. A failure is logged and never undoes the save. |
| Computed value | When a record is read | Add a read-only value, such as "overdue" |
| Delete check | Before a delete | Reject the delete with a reason |

Hooks are plain F# functions over the model type, plus a context that gives the current user, their time zone and the current time. No framework types leak in, so hooks are easy to test.

Hooks never decide who can see or change a record. That is the access rule's job.

## Engines

| Engine | Job | Output |
| --- | --- | --- |
| Access | Decide who can see and change each record | An owner filter on every query, deny by default |
| Persist | Store and load records | Schema, indexes, migrations, data access |
| Validate | Check values and return errors per field | One rule set, run on the server for both forms and API |
| Display | Show lists and edit forms | Server-rendered screens with the right control per field |
| API | Expose records over HTTP | Paged JSON endpoints to create, read, update, delete and list |

### Access rules

Each model declares one access rule. Version 1 has four:

- `owned`: the model gets an `Owner` system field, set to the current user on create. Only the owner can see or change the record.
- `ownedVia <reference field>`: `Owner` is copied from the referenced parent, which must belong to the current user. Copying it keeps every owner filter on a single table.
- `self`: each record is a user. You can read and edit only your own record, at `/me`. No one can list users. Used for `User`.
- `system`: no user can read or change the record. Only the engine and hooks use it.

A model that is `ownedVia` a parent is listed and created under that parent, such as `/lists/{id}/todos`. Its reference field is set from the route and left off forms.

A read-only condition can be added to an owned or ownedVia rule, such as "read-only when the parent list is archived".

Read-only stops users editing or deleting a record directly. Deleting its parent still removes it. Trying to edit or delete a read-only record returns 409 with the reason, such as "This list is archived".

### System fields

The generator adds these to every model. They never appear in models as written, and they are never saved from user input. Forms and API calls send Version back, only so a stale save can be caught.

| Field | Type | Set by |
| --- | --- | --- |
| `Id` | UUID v7 | Engine on create |
| `Owner` | `Ref<User>` | Engine on create, for owned models only |
| `Version` | row version | Database on every save |
| `CreatedAt` | timestamp, UTC | Engine on create |
| `UpdatedAt` | timestamp, UTC | Engine on every save |

The generator also adds a stored `Has<Field>` column for each optional field used in a sort, such as `HasDue`. It's never shown and never read from input.

### Override catalogue

Overrides take a typed field selector, so a wrong field or wrong config type fails the build.

| Engine | Override | Effect |
| --- | --- | --- |
| Display | `label field text` | Replace the label taken from the name |
| Display | `hiddenInForm field` | Leave the field off edit forms |
| Display | `readOnly field` | Show the field but don't allow edits, in forms and API |
| Display | `listColumns [fields]` | Choose and order list columns. Default is every field that isn't wide text. |
| Display | `listEdit field` | Let users change the field straight from the list, such as ticking a checkbox. The save runs validation and hooks as normal. |
| Display | `listSort field direction` | Default list order. Without it, lists sort by the title field. `Id` is always added last as a tie-breaker. |
| Display | `listView name predicate` | A named filter users can pick. The first is the default. Users can't sort or filter any other way in version 1, so every list query has an index. |
| Display | `titleField field` | The field used to name a record in pickers and links. Default is the first field whose type carries the `[<TitleType>]` marker. |
| Validate | `rule field predicate message` | Add a rule that depends only on that field |
| Persist | `sensitive field` | Never written to logs, and masked in audit entries |
| Persist | `column field name` | Use a different column name |
| Any | `unsafe name reason` | A named escape hatch for raw SQL or raw HTML. Each one is listed in the build output for review. |

Rate limits and request size caps are app settings, not overrides.

### Kind defaults

Every engine handles every kind. Semantic types refine these, such as setting a text length.

| Kind | Persist | Validate | Display | API |
| --- | --- | --- | --- | --- |
| text | nvarchar, length from the type | length from the type | text box | string |
| whole number | int | range from the type | number box | number |
| decimal | decimal, precision from the type | range and decimal places | number box | string, to keep precision |
| boolean | bit, default false | none | checkbox | boolean |
| enum | varchar, stored by case name | known case | dropdown | case name |
| date | date | valid date | date picker | ISO date |
| timestamp | datetimeoffset, in UTC | valid time | shown in the user's time zone | ISO timestamp |
| id | uniqueidentifier, UUID v7 | set by the engine | hidden | string |
| reference | foreign key, indexed, no cascade | exists and you can see it | picker by title field | id string |
| optional | nullable | not required | can be left blank | null allowed |

Enums are stored by name so cases can be reordered safely. Renaming a case needs a migration.

### Background jobs

The engine runs scheduled jobs inside the app. Version 1 has two daily jobs:

- Remove audit entries older than 90 days.
- Remove expired sessions.

Each job takes a database lock before it starts, so it runs once even when several copies of the app are running. Each run is logged, and a failed run raises an alert.

## Generator and build checks

### What the generator does

1. Loads the compiled models, access rules and overrides. The build has two stages: the first compiles models and config, the second generates and compiles the engine code.
2. Maps each field to its semantic type and kind.
3. Adds system fields.
4. Emits typed wiring code per model per engine, so nothing uses reflection at runtime.
5. Works out indexes from access rules, references and list overrides.
6. Emits a database migration when the stored shape changes. Renames need an explicit `renamedFrom` note, or they're treated as a drop and an add, which fails the build.

### What fails the build

| Check | Example |
| --- | --- |
| A type has no default in an engine | `Tag list` used before any engine handles lists. The error names the type and each engine. |
| A model has no access rule | `Todo` with no `ownedVia` |
| An override names a missing field | `label <@ fun t -> t.Dew @>` |
| An override has config for the wrong type | date format on a `Notes` field |
| An override targets a system field | `column` on `Version` |
| A reference points at a model that doesn't exist | `Ref<User>` with no `User` model |
| A model has no title field and no `titleField` override, but is referenced | a picker would have nothing to show |
| A migration would drop a column without `renamedFrom` or an explicit drop | renaming `Due` to `DueDate` |

The F# compiler catches a missing field or wrong config type in an override itself, because field selectors are typed. The generator catches the rest.

Warnings, not errors:

- An override that matches the default exactly.
- Each `unsafe` escape hatch, listed with its reason.

## Security

The engines make the secure path the default, so models, overrides and hooks are safe without extra work. This follows the [OWASP Top 10:2025](https://owasp.org/Top10/2025/0x00_2025-Introduction/).

| OWASP 2025 | Engine default | Check |
| --- | --- | --- |
| A01 Broken Access Control | Every model has an access rule, deny by default. Access adds the owner filter to every query, so another user's id returns 404. References are only accepted if they point at records you can see. | Build: model with no access rule. Test: cross-user requests per model. |
| A02 Security Misconfiguration | HTTPS only, HSTS, strict content security policy, CORS off, no debug pages in production. htmx runs with its inline scripts and styles turned off, so the strict policy holds. The app refuses to start with unsafe config. | Startup config test in CI |
| A03 Software Supply Chain Failures | Locked package versions, few dependencies, an SBOM on every build. htmx is served from the app, not a CDN. | CI fails on known high-severity issues |
| A04 Cryptographic Failures | The app stores no passwords. Secrets live in a secret store, never in config files. TLS to the database, encryption at rest. | Secret scan in CI |
| A05 Injection | Persist only issues parameterised queries. Display encodes all output. Semantic types parse input, so a `Title` can't hold a line break. | Build lists every `unsafe` escape hatch |
| A06 Insecure Design | Forms and API only accept fields a user may write. System fields and `readOnly` fields are never saved from input. Version is sent back with each save, only to catch conflicts. Request size caps and rate limits are app settings with safe defaults: 1 MB per request, 120 requests per minute per user, and 10 returns from the sign-in provider per minute per address. | Test: system fields in a request are never saved, and a stale Version is rejected |
| A07 Authentication Failures | Sign-in through OpenID Connect. Session cookies are HttpOnly, Secure and SameSite=Lax. The API serves only this app's own pages, using the same session. Every form post and API call that changes data carries an anti-forgery token. Sessions are stored on the server, in SQL Server. They end after 30 minutes idle or 12 hours in total, and signing out ends them at once. | Tests for expiry and forged form posts |
| A08 Software or Data Integrity Failures | JSON is only read into known model types. `Version` makes stale saves fail with a conflict. Builds are signed. | Concurrency test per model |
| A09 Security Logging and Alerting Failures | An audit log records every create, update and delete with user, time and changed fields. Only the record's owner can read its entries, on a History tab on the record's page and through the API. A daily job removes entries older than 90 days. Failed returns from the sign-in provider and access denials are logged. `sensitive` fields are never logged. Alerts fire when access denials or server errors reach 5 times their usual hourly rate. | Test: sensitive values never reach logs |
| A10 Mishandling of Exceptional Conditions | Fail closed. Validation errors are returned values, not exceptions. Unexpected errors roll back the transaction and return a generic message with a reference id. Every database call has a timeout. | Fault injection tests on Persist |

## Database performance

Persist builds good queries by default. The generator works out indexes from how each model is used, so a sort or filter override also creates the index it needs.

| Area | Default |
| --- | --- |
| Keys | `Id` is a time-ordered UUID v7, so inserts don't fragment the index. |
| Indexes | Every foreign key is indexed. Each list query gets one index: `Owner`, then filter fields, then sort fields, then `Id`. Wide text is never indexed. The audit log is indexed by record then time for the History tab, and by time alone for cleanup. Sessions are indexed by expiry time. |
| Paging | Every list is paged by keyset, using the sort fields plus `Id`. Empty optional values sort last, using a stored "has value" column that leads that field in the sort and the index. Default page size 50, maximum 200. No endpoint returns an unbounded list. |
| Columns | Queries name their columns. List queries skip wide text. |
| Related records | Names for references in a list come from one join, never one query per row. |
| Column types | Sizes come from type defaults, so a `Title` is nvarchar(200), not nvarchar(max). |
| Plan reuse | Parameterised queries let SQL Server reuse query plans. |
| Transactions | One short transaction per request. `Version` handles concurrent edits without holding locks. |
| Deletes | Persist deletes child rows itself, in the same transaction, so each one is audited and runs its delete check. Foreign keys never cascade. |
| Connections | Pooled, async, with a 5 second timeout on every command. |
| Visibility | Queries over 200 ms are logged with their plan. |
| Migrations | Reviewed before release. Index changes on large tables run online. |

## Todo list app

Each user keeps several lists. Each todo has a title, optional notes, a priority and an optional due date. Todos can be ticked off, and lists can be archived.

### Semantic types

```fsharp
[<TitleType>]
type Title      = Title of string      // 1 to 200 chars, one line, trimmed
type Notes      = Notes of string      // up to 5,000 chars, multi-line, wide text
type Priority   = Low | Normal | High  // default Normal
type TimeZoneId = TimeZoneId of string // a valid IANA zone, such as Pacific/Auckland
type ExternalId = ExternalId of string // sign-in provider's user id, up to 255 chars
type Ref<'T>    = Ref of Guid          // link to another record
```

### Models

System fields (`Id`, `Owner`, `Version`, `CreatedAt`, `UpdatedAt`), plus a Has column for each optional sort field such as HasDue, are added by the generator and not written here.

```fsharp
type User =
    { Subject:     ExternalId
      DisplayName: Title
      TimeZone:    TimeZoneId }

type TodoList =
    { Name:     Title
      Archived: bool }

type Todo =
    { List:        Ref<TodoList>
      Title:       Title
      Notes:       Notes option
      Priority:    Priority
      Due:         DateOnly option
      Done:        bool
      CompletedAt: DateTimeOffset option }
```

`User` records are created on first sign-in from the provider's details. On first sign-in, a small script on the welcome page sends the browser's time zone as the starting value. Users can change it at /me.

### Type defaults

| Type | Persist | Validate | Display | API |
| --- | --- | --- | --- | --- |
| `Title` | nvarchar(200) | 1 to 200 chars, no line breaks | text box | string |
| `Notes` | nvarchar(max) | up to 5,000 chars | text area, not in lists | string |
| `Priority` | varchar(16), stored by name | known value | dropdown | name as string |
| `TimeZoneId` | varchar(64) | known IANA zone | dropdown | string |
| `ExternalId` | nvarchar(255), unique | 1 to 255 chars | hidden | not exposed |

All other fields use the kind defaults.

### Access rules

```fsharp
let access =
    [ rule<User>     self
      rule<TodoList> owned
      rule<Todo>     (ownedVia <@ fun t -> t.List @>
                      |> readOnlyWhenParent <@ fun l -> l.Archived @>) ]
```

### Overrides

```fsharp
let todoDisplay =
    overrides<Todo> [
        readOnly    <@ fun t -> t.CompletedAt @>
        listEdit    <@ fun t -> t.Done @>
        listSort    <@ fun t -> t.Due @> Ascending
        listColumns [ <@ fun t -> t.Title @>; <@ fun t -> t.Due @>
                      <@ fun t -> t.Priority @>; <@ fun t -> t.Done @> ]
    ]

let todoListDisplay =
    overrides<TodoList> [
        listView "Active"   <@ fun l -> not l.Archived @>
        listView "Archived" <@ fun l -> l.Archived @>
    ]
```

### Hooks

1. **Completion time (before save).** If `Done` changed to true, set `CompletedAt` to now. If it changed to false, clear it.
2. **Overdue (computed value).** True when `Due` is before today in the user's time zone and `Done` is false. The list highlights overdue todos.

### Derived without extra code

- Tables for all three models, with foreign keys, deleting a list deletes its todos, and migrations.
- Indexes: `TodoList (Owner, Archived, Name, Id)` and `Todo (Owner, List, HasDue, Due, Id)`.
- Paged API endpoints for lists, and for todos under each list.
- List screens and edit forms for lists and todos.
- Field errors shown next to each field, and returned by the API as problem details.
- Audit entries for every change, shown on each record's History tab.

### Change test: add tags

Adding `Tags: Tag list` to `Todo` must fail the build until every engine handles lists of a semantic type. Once it does, tags need no further code on this or any later model.

## Acceptance tests

### Engine

- [ ] Adding a `Title` field to any model needs no code. It appears in the database, API, form and validation.
- [ ] Adding a field of a type with no defaults fails the build, naming the type and each engine.
- [ ] Every row in the build check table produces its error.
- [ ] Invalid input gives the same field errors in the form and in the API's problem details.

### Security

- [ ] Reading, updating or deleting another user's list or todo by id returns 404.
- [ ] Creating a todo under another user's list returns 404. A user can read and edit only their own user record.
- [ ] Sending `Id`, `Owner`, `Version` or `CompletedAt` in a request changes nothing. A stale Version makes the save fail with a conflict.
- [ ] Text containing `<script>` displays as plain text.
- [ ] A form post or API change without a valid anti-forgery token is rejected.
- [ ] A session ends after 30 minutes idle, after 12 hours in total, and at once on sign-out.
- [ ] Unexpected errors return a reference id and no stack trace.
- [ ] `sensitive` field values never appear in logs or audit entries. Only the owner can see a record's History tab, and entries older than 90 days are removed. Each daily job runs once, even with several copies of the app running.

### Data and performance

- [ ] Two saves from stale copies of the same todo: the second fails with a conflict.
- [ ] A list endpoint runs the same number of queries for page sizes 1 and 200.
- [ ] Paging through a list with many equal due dates returns every todo exactly once.
- [ ] Every list query is served by an index, checked against its query plan.
- [ ] Deleting a list deletes its todos in the same transaction, with an audit entry for each.

### Todo app

- [ ] Ticking a todo, in its form or straight from the list, sets `CompletedAt`, and unticking clears it.
- [ ] A todo due yesterday in the user's time zone shows as overdue, even if it's still that date in UTC.
- [ ] Archived lists are hidden by default, and their todos are read-only. Editing one returns 409 with the reason. Deleting an archived list still deletes its todos.
- [ ] The app has no hand-written code outside models, access rules, overrides and the two hooks.

## Build plan

1. **Two-stage build and generator.** Compile models and config, load them, add system fields, and run every build check. Test with no engines.
2. **Validate engine.** Kind and type defaults, plus `rule` overrides.
3. **Persist and Access engines, together.** Schema, derived indexes, owner filters on every query, keyset paging, child deletes, migrations, the audit log, sessions and the daily cleanup jobs.
4. **API engine.** Paged endpoints, nested routes for `ownedVia`, `/me`, problem details.
5. **Display engine.** Server-rendered lists and forms with htmx, list views, anti-forgery tokens and output encoding.
6. **Hooks.** The four hook points and the hook context.
7. **Security baseline.** Headers, sign-in, sessions and sign-out, secret store, CI scans.
8. **Todo app.** Built only from models, access rules, overrides and hooks. All acceptance tests pass.
9. **Tags test.** Add the list kind and confirm the change test passes.

## Language and stack

Use F#. Semantic types take one line each, records and options model the data cleanly, quotations give typed field selectors, and it runs on .NET with SQL Server.

| Language | Semantic types | Missing default as build error | Typed overrides | Main cost |
| --- | --- | --- | --- | --- |
| F# | One line each | Generator | Quotations | Smaller ecosystem. Needs a two-stage generator. |
| TypeScript | Branded types, a bit clunky | Native, with mapped and conditional types | Native | Types vanish at runtime, so models must be schema values |
| Rust | Newtypes, one line each | Native, through derive macros and traits | Macros | Slow to write for a business app |
| C# | Records, verbose | Source generator | Expression trees | Most boilerplate per semantic type |

| Layer | Choice |
| --- | --- |
| Web | ASP.NET Core with Falco |
| UI | Server-rendered HTML with htmx |
| Data access | Generated parameterised SQL over Microsoft.Data.SqlClient |
| Sign-in | ASP.NET Core OpenID Connect |
| Generator | A custom two-stage build step |

Server-rendered forms mean validation runs only on the server, so there is one copy of every rule. Display also adds HTML attributes such as `maxlength` and `required` from the same rules, for quick feedback in the browser.

## Out of scope

- API access for other apps, such as a phone app.
- Users deleting their own account.
- Sharing lists between users, and roles beyond owner.
- Sorting and filtering lists beyond the configured views.
- Real-time updates between browsers.
- Page layouts beyond list and form.
- Soft delete and restore.
- A separate front end such as a single-page app.
