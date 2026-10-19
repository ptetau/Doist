Result: pass

Remit: quality

Problems: none

Follow-ups:
- Unused opens in Persist.fs (`System.Data`, `Microsoft.FSharp.Reflection`, `Microsoft.FSharp.Quotations`) and the repeated "first error of two results" match in `comparison` and `combine`. A tidy-up intent could remove them. No acceptance line asks for it.
- `page` on an `ownedVia` model with `parent = None` binds NULL and returns an empty page with no error. A new intent could return `VALIDATION_FAILED` on `parent`.
- `listQuery` raises with `failwithf` for an unknown view or an unsupported view. A new intent could return a `Finding` instead.

Notes:
- Names follow DES-names (Persist, Shape, Store) and error codes follow DES-errors.
- The error messages for UNPLANNED_DROP, UNSAFE_MIGRATION, SHAPE_UNREADABLE, and the limit and cursor errors match the spec text.
- The code splits cleanly into pure schema, query, store, and migration parts. Shape rendering is shared between `schema` and `migration`.
