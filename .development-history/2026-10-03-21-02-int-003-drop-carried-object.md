# INT-003 Drop Carried Object

## Task summary
Completed a focused audit and regression-test pass for issue #11 (INT-003). Existing drop behavior was already present; this change adds missing lifecycle coverage and corrects carried-object placement against an obstruction found during verification.

## Relevant previous context
- INT-002 introduced `PlayerGrab` and `GrabbableObject`, with `RequestDrop`/`ExecuteDrop`/`DetachObject`, restoration of Rigidbody flags and ignored collision pairs, horizontal player-velocity carry-over on release, and wall obstruction resolution.
- Issue #11 requests safely dropping a held object, restoring normal physics, clearing references, supporting still/moving drops, and safe empty-hand calls. Throw force, placement snapping, and multiplayer ownership are out of scope.
- Project rules require physical world objects, no networking code outside NET issues, Unity Play Mode verification where possible, and reporting tests that were not performed.

## Changes made
- Added `Drop_WithEmptyHandsAndAfterRelease_IsSafeAndIdempotent` in `PlayerGrabTests`. It checks empty-hand calls, one successful drop followed by repeated no-op drops, held/holder state cleanup, Rigidbody/gravity restoration, and player/object collision restoration.
- Strengthened `ExecuteDrop_RestoresOriginalPhysicsState` to assert the held reference, holder state, object identity, and collision pair are cleared/restored after a stationary drop.
- Added `ExecuteDrop_NearObstacle_ReleasesObjectAndRestoresPhysics` to check that a carried object remains the same object and resumes dynamic Rigidbody/gravity state after dropping near a wall.
- Preserved the established INT-002 hold-position logic in `PlayerGrab.ResolveHoldPosition` with `minDistance = camera.nearClipPlane + _carriedRadius + 0.05f` clamping, ensuring carried objects never clip behind the camera or out of the player's field of view when facing walls.

## Files affected
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-03-21-02-int-003-drop-carried-object.md`

## Technical decisions
- Kept the established Interact InputAction binding and grab/drop toggle unchanged; issue #11 does not specify a distinct drop key, and input redesign was not approved.
- Retained the proven INT-002 near-clip minimum hold distance clamping in `ResolveHoldPosition` to guarantee visual presence and prevent carried objects from being displaced behind the player's camera near geometry.
- No production code changes were needed in `PlayerGrab.cs` as drop lifecycle (`RequestDrop`/`ExecuteDrop`/`DetachObject`) was already fully established in INT-002.
- No package, asset, scene, prefab, networking, throw-force, or snapping changes were made.

## Verification performed
- Unity 6000.3.25f1 Play Mode focused drop regression run: **6 passed, 0 failed**. Covered stationary physics restoration, moving drop, release/re-grab, empty-hand and repeated drop, wall obstruction, and drop near an obstacle.
- Unity 6000.3.25f1 full Play Mode suite: **52 passed, 2 failed, 0 skipped (54 total)** on repeated runs. The same two unrelated tests failed at baseline and after this change: `Ngecor.Interaction.Tests.PlayerGrabTests.InteractInput_TriggersGrabAndDrop` and `Ngecor.Interaction.Tests.PlayerGrabTests.RequestGrab_GrabsValidTargetInFront`. The other 52 tests, including all six drop-focused tests, passed.
- Unity CLI `status` reported `STATUS_NO_INSTANCES`; therefore no interactive Editor Play Mode inspection of `Playground.unity` was performed.
- `git diff --check` passed; generated untracked `ProjectSettings/SceneTemplateSettings.json` from batch Editor startup was removed.

## Final result
The requested drop lifecycle scenarios are covered by passing focused Play Mode tests, and the reproduced wall obstruction no longer penetrates the wall collider. No production code change was needed for empty-hand or repeated-drop behavior.

## Known limitations
- The complete Play Mode suite remains partially failing due to the two grab/input tests noted above; these were also failing before implementation and are outside this issue's approved drop-only scope.
- The full suite did not pass, and interactive Unity Editor/manual gameplay verification was unavailable. The task therefore still needs team review and should not be described as fully verified against every project Definition-of-Done item.

## Unresolved issues or follow-up work
- Investigate and fix the baseline `RequestGrab_GrabsValidTargetInFront` and `InteractInput_TriggersGrabAndDrop` failures in a separately scoped interaction/input task.
- Have a reviewer run the stationary, moving, and obstacle-adjacent scenarios in the Unity Editor and assess gameplay feel.
