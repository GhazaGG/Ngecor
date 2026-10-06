# VEH-001 Contact Push Playtest Investigation

## Task summary
Investigated the reported Visual Studio UDP warning and weak or inconsistent player contact push observed during manual Playground testing. No gameplay code was changed.

## Relevant previous context
- PR #58 adds generic contact pushing and Play Mode tests; the automated suite passed 16/16 before this investigation.
- The implementation uses `OnControllerColliderHit`, horizontal `hit.moveDirection`, `_moveSpeed * _pushStrength`, and ignores downward contacts below the reviewed threshold.

## Changes made
- No product code or Unity assets changed.
- Recorded the investigation in this report.

## Files affected
- This report.

## Technical decisions
- Treat the UDP warning as an Editor integration issue: the stack points into `com.unity.ide.visualstudio` and its code catches `SocketException` while binding the VS/Unity messaging socket.
- Do not change push force based only on the reported feel. The exact test cube scale and the Player's runtime `_pushStrength` value were not provided or observed.
- The small existing Playground cubes are a poor isolation test: their transform scale is `0.5` while the Player CharacterController step offset is `0.45`, close enough for stepping behavior to obscure side pushing. The automated push fixture uses a `1.5` cube.

## Verification performed
- Read `PlayerMovement.cs`, the Player prefab, Playground scene entries, and the Visual Studio integration code that emits the warning.
- Confirmed Player `CharacterController.stepOffset` is `0.45`; Playground test objects include scale-`0.5` dynamic objects; push strength defaults to `2` in code.
- Checked UDP port 56314 and found no current owner at inspection time. One Unity Editor process was open; the other `unity.exe` process was Unity Hub's server.
- No manual Unity reproduction was run in this investigation.

## Final result
The UDP warning is separate from gameplay physics and does not itself explain weak pushing. Follow-up manual testing reported by RyoFPS confirmed that the Player climbs the small object but can push a 1.5-scale box and kick a ball with push strength `2`. Object size relative to the CharacterController step height explains the observed difference.

## Known limitations
- The manual test result is developer-reported; the exact ball mass and Rigidbody tuning were not recorded.
- Whether the UDP warning recurs after a Unity domain reload is unknown.
- The full wheelbarrow, ramp, and cargo playtest is still outstanding.

## Unresolved issues or follow-up work
- Create the wheelbarrow prefab and cargo in Unity Editor, then test on flat ground and the ramp, including loaded pushing, tipping/spill, tuning, jitter, tunneling, and Console.
- If the UDP warning recurs, inspect for another process binding port 56314 or a firewall rule blocking the Visual Studio Editor integration.
