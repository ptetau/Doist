Result: pass

Remit: quality

Problems: none

Follow-ups: none

Notes:
- The three repeats from round 1 are gone. `textToCheck`, `required`, `unwrapOption`, `textOf`, and `idOf` each hold one rule.
- Small point, not a problem: `parseField` and the `Text` branch of `parseCore` both test for blank input with `textToCheck`. The two tests have different jobs (empty optional gives `None`, empty required gives an error), so the code stays as simple as the requirement lets it be.
- Small point, not a problem: `isNarrow` in the `WholeNumber` branch reads backwards in the first guard (`not isNarrow`). A name such as `isInt64` would read better.
- Names follow DES-names (module `Validate`). The error messages match the Interface and tell the user what to fix.
