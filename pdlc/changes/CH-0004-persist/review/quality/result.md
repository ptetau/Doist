Result: pass

Remit: quality

Problems: none

Follow-ups:
- `listQuery` calls `failwith` for an unknown view or an unsupported view. A caller gets an exception, not a `Finding`. A new intent could return a `Result` (already filed as IN-0012).
- `viewToSql` repeats one `Call(None, mi, [a; b]) when mi.Name = ...` line for each comparison operator. A table of operator names would be shorter. A new intent could tidy this (IN-0014 covers persist tidy).
- `page` builds a `Record` inline, and `find` builds one with `toRecord`. A new intent could share the code.

Notes:
- Names follow DES-names (Persist, model, access rule, system field). Error codes match DES-errors. The messages for `UNPLANNED_DROP`, `UNSAFE_MIGRATION`, `SHAPE_UNREADABLE`, and the `limit` and `cursor` errors match the acceptance lines word for word.
- Minor: the `Optional _` arm of `sqlType` cannot run, because `innerKind` removes it first. The `open System.Data` and `open Microsoft.FSharp.Reflection` lines look unused. Neither one breaks a spec line.
- I read the diff and the first part of the full file. The rest of the full file repeats the diff.
