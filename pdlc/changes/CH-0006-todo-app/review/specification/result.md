Result: pass

Remit: specification

Problems: none

Follow-ups:
- `Defaults.fs` gives `Title` a check that rejects line breaks, and gives `TimeZoneId` a check that rejects unknown zones. No acceptance line asks for either check. A new intent could add them as validation requirements. The zone check reads the system zone list, so a new intent must also say how the check behaves on a host with a different list.

Notes:
- Only the four `.fs` files and the `.fsproj` (listed as assumed) changed. The `.fsproj` compile order is Model, Defaults, Schema, Hooks.
- R1 to R6 match the code and frames 2 to 15. Frame 16 shows 206 checks passing, which matches the progress log.
- "Writing check" reports no findings. The two code comments in `Hooks.fs` follow the writing rules.
- Frame 6 shows the Active view then the Archived view, as R3 asks. Each frame shows the screen after its step, and none is blank.
- No README is in "Files", so no README rule applies.
