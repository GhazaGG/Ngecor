# PR #92 Wheelbarrow Review Fixes

## Task summary

Addressed the latest PR #92 review comments for wheelbarrow interaction in `RyoFPS/veh-002-wheelbarrow-interaction`.

## Relevant previous context

- Earlier PR #92 comments had already been addressed in commit `7dd24d4` on this branch.
- The latest review requested safe alignment to the handle side, wheelbarrow-oriented pushing and steering, and a hold that persists until explicit release or an invalid/extreme-separation condition.
- Existing ramp playtest follow-up reports were read. Their reported manual gameplay observations were not repeated as part of this change.

## Changes made

- Align the player to a collision-checked grip pose behind the configured handles, with clear feedback when alignment is too far away or blocked.
- Keep the wheelbarrow Rigidbody dynamic. Route W/S input through the existing mass-aware push limits and apply bounded yaw steering from A/D input while disabling normal player strafing during the hold.
- Keep the hold active when follow movement is obstructed. Release on invalid interaction state or extreme separation, with feedback; pressing E again remains the normal release path.
- Add PlayMode coverage for the real wheelbarrow prefab, both handle sides, blocked alignment, W/D/S control, normal hold/release, extreme separation, and disabling the interaction component.

## Files affected

- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`

No scene, prefab, asset, metadata, package, or networking files were edited by this task.

## Technical decisions

- Use `CharacterController.Move` for short collision-resolved alignment and a capsule cast to reject blocked paths before moving.
- Reuse `PlayerMovement`'s mass-aware push calculation; input strength scales the existing speed and force limits.
- Keep steering on the dynamic Rigidbody with a bounded torque controller and a bounded target yaw speed.
- Treat follow obstruction as a reason to keep the current hold rather than an automatic release. The hold still ends on invalid state or separation greater than twice the configured grab distance.

## Verification performed

- Unity `6000.3.25f1` PlayMode batch suite: **117 passed, 0 failed**.
- The suite included the original `Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab` and verified both configured handles remain usable while the Rigidbody stays dynamic.
- `git diff --check` passed.
- Unity's batch log contained licensing-token and D3D12 info-query warnings; the test runner completed with exit code 0.

## Final result

The requested PR review behavior is implemented locally on `RyoFPS/veh-002-wheelbarrow-interaction`. No commit, push, or GitHub review response was made.

## Known limitations

- Manual Game View acceptance was not performed. Empty/loaded cargo feel, ramp handling, jitter, and visual feedback still need a human Unity playtest.
- The worktree already contained unrelated edits to `Assets/Game/Scenes/Playground.unity` and `ProjectSettings/VersionControlSettings.asset`, plus untracked history/settings files. They were left untouched.

## Unresolved issues or follow-up work

- Run the requested manual Playground checks for straight push, turns, reverse, release, empty and loaded cargo, and ramp/jitter behavior before gameplay acceptance.
