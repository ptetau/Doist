Result: fail

Remit: specification

Problems:
- pdlc/changes/CH-0004-persist/frames/40.png:caption · The caption says "All 49 checks of CAP-persist pass". The frame shows "Passed: 0 failed, 51 passed, Total: 51". The caption count does not match the frame. The progress log says 47 checks for the test writer, which is a third number. · remit rule: each frame must show what its caption claims

Follow-ups: none

Notes:
- I read Persist.fs against all eight requirements (R1 to R8) and the Interface. I found no break of an acceptance line.
- Only `src/Typed.Engine/Persist.fs` and the Compile entry in `Typed.Engine.fsproj` changed. This matches "Files". The change adds no package reference, and the code needs none.
- No README is in "Files", so the README rules do not apply.
- The "Writing check" reports "No findings", so there is no finding to judge as a false alarm.
- Frames 1 to 39 each show the output that their caption claims. The frames are cumulative terminal output, and each new output sits at the bottom.
- Frame 4 shows the R4.1 query start only. This is enough for "starts with".
- Frame 10 shows two Todo indexes. Only one starts with `List`, as R2 asks.
