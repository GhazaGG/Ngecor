# PLAYER-001 Review Fixes

## Task summary
Addressed the code and documentation changes requested in the Team Lead review of PR #48 on `feat/player-movement`.

## Relevant previous context
PR #48 implements first-person movement using the existing project-wide Input System actions and a native `CharacterController`. The review accepted this approach and requested removal of redundant action lifecycle control, stronger deterministic tests, removal of out-of-structure planning documents, and a manual Playground playtest by RyoFPS.

## Changes made
- Removed `EnableControls`, `DisableControls`, `_controlsEnabled`, and action enable/disable lifecycle hooks. `Update` still returns before reading actions for non-local players.
- Made test field injection assert that each private field exists and replaced assembly/type discovery with `AddComponent<PlayerMovement>()`.
- Enabled the test action map independently of player ownership to model the project's startup-enabled action asset; the non-local test now confirms input is available but does not move that player.
- Routed movement test frame intervals through a 60 FPS `Time.captureFramerate` helper that restores the prior value in `finally`.
- Removed the redundant `docs/superpowers/` plans and specs.
- Updated PR #48's description with the review fixes, honest manual-test status, and the non-local Camera/AudioListener handoff to NET-001.

## Files affected
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `docs/superpowers/plans/2026-10-01-basic-player-movement.md` (deleted)
- `docs/superpowers/specs/2026-10-01-player-movement-design.md` (deleted)
- PR #48 description

## Technical decisions
Input actions remain enabled by the project's startup asset, so the component must gate local input reads rather than changing shared action state. The test fixture explicitly enables its isolated action map for the same reason. Manual gameplay validation remains a developer-owned task per the review.

## Verification performed
- Unity 6000.3.25f1 batch-mode Play Mode suite: 8 passed, 0 failed, 0 skipped.
- `git diff --check` passed.
- Review diff confirms there are no changes to scenes, prefabs, packages, or ProjectSettings from this fix. Pre-existing local ProjectSettings changes were preserved and excluded.

## Final result
All code/documentation review items that can be completed without human gameplay input are addressed. PR #48 remains Draft until RyoFPS completes the required manual Play Mode checklist and supplies a short clip.

## Known limitations
- WASD, arrow keys, mouse look, collision, speed tuning, interactive Console cleanliness, and gameplay feel have not been manually verified in Playground by RyoFPS.
- No manual gameplay clip or device details are available yet.
- Active Camera/AudioListener components on non-local player instances remain a known multiplayer handoff to NET-001.

## Unresolved issues or follow-up work
RyoFPS must playtest the Playground scene on the target machine at speeds 5 and 2.5 m/s, verify keyboard/mouse controls and flat/ramp/wall behavior, inspect the Console, and add a short clip. Then update the PR with device/results, mark it ready, and request Team Lead re-review.
