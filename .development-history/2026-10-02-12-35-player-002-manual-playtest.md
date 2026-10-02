# PLAYER-002 Manual Playtest Results

## Task summary
Recorded the developer's manual Unity Play Mode observations for issue #8 after implementing the basic camera controller.

## Relevant previous context
The implementation and automated verification are documented in `2026-10-02-11-59-player-002-camera-controller.md`. That report left physical input, cursor behavior, sensitivity feel, and movement/look jitter for manual validation.

## Changes made
- Recorded the reported cursor, movement, look, sensitivity, and Console observations.
- No code or Unity serialized assets were changed in this follow-up.

## Files affected
- `.development-history/2026-10-02-12-35-player-002-manual-playtest.md`

## Technical decisions
Kept the serialized sensitivity at 0.1. The reported micro-stepping occurred only at a test value of 2, twenty times the default. The existing controller multiplies mouse delta directly by sensitivity, so a high value magnifies small input steps. This is a likely explanation, not a confirmed diagnosis of the mouse or display pipeline.

## Verification performed
Developer-reported Unity Play Mode results:
- Initial cursor remained visible until the Game view was clicked; after focusing the Game view, the cursor locked.
- WASD movement while looking felt smooth. Deliberately fast movement/look also remained stable.
- At sensitivity 2, very slow mouse movement appeared to have small stepping; no such issue was reported at the normal setting.
- Escape released the cursor and made it visible again.
- Unity Console had no reported issues.
- `git diff --check` passed during this follow-up.

## Final result
Manual results support smooth movement/look at the normal sensitivity and correct Escape-to-release behavior. The high-sensitivity observation is recorded for tuning; it does not currently demonstrate jitter during movement.

## Known limitations
- Startup cursor lock before focusing the Unity Editor Game view was not observed. The Editor Game view may need focus to capture input; standalone player startup behavior has not been tested.
- The report does not include an exact comfortable sensitivity value selected by the tester. Keep 0.1 as the recorded default until the team selects and records a final value in the PR.
- Flat-ground and ramp tests were not separately identified in the observation, so those individual acceptance checks remain unconfirmed.

## Unresolved issues or follow-up work
- If startup locking must be evaluated independently of Editor focus, verify it in a standalone build.
- Confirm the final sensitivity value and explicitly check the ramp during review playtest.
