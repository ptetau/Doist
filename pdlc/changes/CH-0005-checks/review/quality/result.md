Result: pass

Remit: quality

Problems: none

Follow-ups: none

Notes:
- Checks.fs is split into clear parts: access, reference, item, and run. Names tell what each part does. The code has no dead code and no unused parameter.
- Messages match the quoted texts in the acceptance lines. The R3 message names Todo.Due and both types.
- Small style points, not problems:
  - Checks.fs:1195 and 1197 differ only in the message text, so one match arm could carry the shared parts.
  - The placement values `-1` (Checks.fs:1195, 1198) and `Int32.MaxValue` (named `last`, Checks.fs:1260) are different ways to say "before the fields" and "after the fields". A named constant for `-1` would read better.
  - The `find f` then `Some field when ...` pattern repeats in the Rule, ListSort, and Label arms.
