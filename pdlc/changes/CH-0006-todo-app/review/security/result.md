Result: pass

Remit: security

Problems: none

Follow-ups:
- Add a check that `Subject` (`ExternalId`) is not empty and has no control characters. The spec sets only the length, unique, and hidden. A new intent could add this check.
- Add a check that `Notes` has no control characters other than line breaks. The spec sets only the length.

Notes:
- The app holds only data, pure hooks, and access rules. It builds no command, query, path, or HTML from input.
- Access rules match the spec: `User` is `self`, `TodoList` is `owned`, `Todo` is `ownedVia List` and read-only when the list is archived. Each rule denies by default.
- `ExternalId` is hidden and unique. No secret appears in code, logs, or errors.
- The only dependency is the project reference to `Typed.Engine`.
- `knownZone` compares the zone ID by exact equality with the system zones. The result depends on the host zone data. This is not a security risk.
