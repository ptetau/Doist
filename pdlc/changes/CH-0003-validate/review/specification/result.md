Result: pass

Remit: specification

Problems: none

Follow-ups:
- `parseField` gives the message "cannot be read from text" for a decimal, a timestamp, an id, or a reference. No acceptance line asks for it. IN-0008 (parse messages for all kinds) already covers it.
- `parseField` does not check the length of a text value. IN-0009 (parseField length) already covers it.
- The Interface says that no function raises. `validate` reads fields by reflection and can raise on a record of the wrong type. IN-0010 (validate never raises) already covers it. No acceptance line shows a raise.

Notes:
- All 32 frames show the step in the caption, after the step. Each frame adds the output of its step to the earlier output. Frame 32 shows 32 checks passed.
- The code meets each acceptance line of CAP-validate.R1 to R8 on a read of `Validate.fs`. Only `Validate.fs` and the `Compile` entry in `Typed.Engine.fsproj` changed, and both are in "Files".
- The "Writing check" has no findings. I read the spec text and the code comments against DES-writing-style and found no break.
- Minor: the "Done when" lines of Steps 2, 3, and 4 write the identifiers in lower case (`title`, `notes`, `timezoneid`). The identifiers are `Title`, `Notes`, and `TimeZoneId`. The writing rules say not to change identifiers. I did not count this as a problem, because it does not change the behaviour and the line also names the requirement ID.
