Result: pass

Remit: specification

Problems: none

Follow-ups: none

Notes:
- The diff changes only `src/Typed.Engine/Checks.fs` and the Compile entry in `Typed.Engine.fsproj`. Both are in "Files".
- Each of R1 to R13 has code for its acceptance lines. The messages match the quoted texts. The codes match `DES-errors`.
- The Writing check shows "No findings". The code comments are short and in the active voice.
- All 26 frames show the step in their caption, after the step. None is blank. Frame 26 shows 176 checks pass.
- Frame 10 (R5) shows a second finding, a `WRONG_CONFIG_TYPE` for `readOnlyWhenParent` on the missing `TodoList`. The line says that the findings "hold" `DANGLING_REFERENCE`, so the line is met. The extra finding follows from the rule that R7 asks for.
- Frame 24 (R13) shows a long reason after "Unsupported list view for Todo:". The line requires only the start of the message.
- The change has no README in "Files". The remit asks for README changes only when "Files" lists a README.
