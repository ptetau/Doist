Result: fail

Remit: quality

Problems:
- src/Typed.Engine/Validate.fs:869-876 · The `WholeNumber` branch of `parseCore` holds two copies of the same parse and the same error message, one for `int64` and one for `int`. Only the parser differs. · quality: no unnecessary repetition; the code is as simple as the requirement lets it be.
- src/Typed.Engine/Validate.fs:830 and :864 · The rule "wide text keeps its spaces, other text is trimmed" is written twice, in `textRules` and in `parseCore`. A change to the rule needs two edits. · quality: no unnecessary repetition.
- src/Typed.Engine/Validate.fs:899 · The empty-optional test `not (isWide field.Kind) && text.Trim() = "" || text = ""` repeats the trim rule a third time. It also relies on operator precedence and is hard to read. · quality: names and code are easy to read and change.

Follow-ups:
- The message "cannot be read from text" (line 890) does not tell the person what to do, and the spec has no line for it. A new intent could define a message and a DES-errors code for kinds that the engine cannot parse from text.

Notes:
- The code gives errors as values and does not raise on the paths that the requirements name.
- Names and doc comments are clear. The Rule override and reference handling match R5 and R6.
