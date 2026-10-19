Result: fail

Remit: specification

Problems:
- pdlc/changes/CH-0003-validate/change.md:225 · The Build round 1 entry in the progress log has 26 words in one sentence (it starts "Build round 1: the builder found that `knownZone`..."). Split it into two sentences. · Remit rule: the change spec text follows DES-writing-style (6.3, a descriptive sentence has at most 25 words), and the Writing check finding is not a false alarm.

Follow-ups:
- `parseField` gives the message "cannot be read from text" for a decimal, a timestamp, an id, or a reference. No acceptance line asks for it. A new intent could add acceptance lines for these kinds, or a DES-errors entry for them.

Notes:
- All 8 requirements (R1 to R8) are met by `Validate.fs`. I traced each acceptance line against the code, for example trim for short text, no trim for wide text, the exact zone match, `Ref Guid.Empty` giving "is required", `rule` predicates, enum cases in order, and the boolean values.
- Only `src/Typed.Engine/Validate.fs` and the Compile entry in `Typed.Engine.fsproj` changed. Both are in "Files". No README is in "Files", and the spec names no user-set seam ("Test seams: none").
- The code comments are short and in the active voice.
- All 32 frames show what their captions claim. Each frame shows the output of its step at the bottom of the screen. Frame 32 shows 32 passed checks, filtered to CAP-validate.
- Frame 27 shows the date as "10/18/2026", which is the default DateOnly print. The value is the date 2026-10-18, so it meets the line.
