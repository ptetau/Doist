Result: pass

Remit: specification

Problems: none

Follow-ups:
- `page` returns `NOT_FOUND` for the engine-only scope, and `listQuery` raises an exception for an unknown view name. The spec asks for neither. A new intent could name the result for each case.
- The SQLite schema holds no foreign keys. The spec does not cover this. A new intent could state it, so that checks on SQLite do not expect foreign-key refusals.

Notes:
- The change edits only `src/Typed.Engine/Persist.fs` and the `Compile` entry in `Typed.Engine.fsproj`. Both are in "Files". The fsproj lists no package reference, and the spec does not need one in this project.
- I read the code against the acceptance lines of R1 to R8. I found no break.
- The writing check reported "No findings", and no false alarm needed a ruling. The code comments are short, active, and clear.
- I read all 40 frames. Each caption matches the output that the frame shows after its step. Frame 40 now says "49 checks, 51 runs", and it shows "Passed: 51". The round 1 problem is fixed.
- The change adds no README, and "Files" does not list one. The spec title and summary show the behaviour as it is now.
