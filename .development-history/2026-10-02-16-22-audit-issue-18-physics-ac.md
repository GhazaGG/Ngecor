# BUILD-001: Audit Remaining Physics Acceptance Criteria

## Task summary

Audited the remaining Issue #18 acceptance criteria in the current Playground and prepared the scene for a clean manual Player test.

## Relevant previous context

- The combined scaffolding module, two-level showcase, static ramp, cargo fixtures, and merged PLAYER-001 prefab had already been added.
- Issue #18 requires dynamic Play Mode checks for settling, sleep, off-center/uneven tipping, and player traversal.
- `TestPlayerDummy` was an older Rigidbody-based test object whose walker script reads the disabled legacy Input Manager.

## Changes made

- Disabled only `TestScaffoldingWalker` on the existing `TestPlayerDummy` through Unity Editor; preserved the dummy GameObject and its other components.
- Kept `Player_Test` enabled as the local CharacterController player and preserved its PlayerMovement prefab override.
- Saved the scene with the legacy walker disabled to prevent its repeated Input Manager exceptions during a Player test.

## Files affected

- `Assets/Game/Scenes/Playground.unity`
- This development-history report

## Technical decisions

- Did not edit `PlayerMovement.cs` or `InputSystem_Actions.inputactions`.
- Static configuration matches the requested access limits: the Player prefab has step offset 0.45 m and slope limit 50 degrees; the generated ramp is static and 45 degrees.
- The two modules start aligned at local Y 0 and 1.88 m. The module Rigidbody mass is 65 kg; cargo fixtures are 25 kg and 125 kg. The root transform and existing friction material remain Inspector-editable.

## Verification performed

- Unity Editor inspection confirmed the Player prefab's CharacterController settings and the Player_Test local-player override.
- During an initial Play Mode interval, module transforms remained close to their authored positions (the largest observed offset was under 0.0005 m). A Rigidbody sleep state was not captured.
- That initial Play Mode run produced repeated exceptions from the old `TestScaffoldingWalker` calling `UnityEngine.Input.GetAxisRaw`; the component was disabled afterward.
- A later Editor play-state inspection showed Play Mode had stopped, so no post-fix movement, no-jitter, centered-load, or tipping result is claimed.
- No code tests were added.

## Final result

The static setup needed for the remaining tests is present, and the known legacy input-error source is disabled. The dynamic physics and movement acceptance criteria remain unverified.

## Known limitations

- Rigidbody sleep, imperfect placement tolerance, uneven-base behavior, heavy off-center tipping, player traversal, and jitter have not been confirmed in a sustained Play Mode test.
- Cargo fixtures begin beside the scaffold; place them above the deck in Edit Mode before starting each load test.

## Unresolved issues or follow-up work

- Run Issue #18's manual Play Mode steps with `Player_Test` after this audit. Compare centered 25 kg cargo with 125 kg cargo placed overhanging the deck edge, and repeat the straight stack after it settles.
- If the two-level stack cannot settle without joints/snapping, or the CharacterController jitters on the Rigidbody platform, stop and ask the lead as required by GhazaGG's Issue #18 prompt.
