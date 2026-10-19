Result: pass

Remit: specification

Problems: none

Follow-ups:
- `fieldName` calls `failwithf` when a selector is not a property read. The spec asks for no error code here. A new intent could add a named error for a selector that is not a field.
- `describe` gives a field with a type such as `int` or `Guid` a built-in kind and no check. The spec lists only the todo-app cases. A new intent could state the built-in kinds in a requirement.

Notes:
- All 30 frames show the step in their caption, and each frame includes the output of that step. Frame 30 shows 30 checks passing.
- The Writing check has no findings. Nothing to judge as a false alarm.
- Changed files are `.gitignore`, `Model.fs`, `Overrides.fs`, and `Typed.Engine.fsproj`. Each appears in "Files". No other file changed.
- The change has no README entry in "Files", and "Test seams" is none. No old wording survives in the title, summary, or cross-references.
- The `MISSING_DEFAULT` message in the code matches the acceptance line exactly.
- Code comments are short and in the active voice.
