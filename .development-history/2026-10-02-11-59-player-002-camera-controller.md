# PLAYER-002 Basic Camera Controller

## Task summary
Implemented the issue #8 first-person cursor controls and a local-player camera accessor on `feat/basic-camera-controller`.

## Relevant previous context
PLAYER-001 already owns mouse yaw/pitch through `PlayerMovement` and `CameraPivot`. The Player prefab has a Camera below the pivot at local height 1.6 m; its CharacterController is 1.8 m tall. The previous handoff recorded a visible cursor as the camera follow-up. Camera shake, third-person behavior, and network camera ownership remain out of scope.

## Changes made
- Local players lock and hide the cursor at startup, release it with Escape, and relock it with a left click. Look input is ignored while unlocked. Disabling the local movement component releases the cursor, and refocusing reapplies a still-requested lock.
- Cached the child Camera in `Awake` and exposed it through `LocalCamera` only while this player is local, so interaction code can use its transform without `Camera.main`.
- Added Play Mode coverage for cursor transitions, ignoring look while unlocked, missing Move action behavior, and local-only camera access.
- Kept the existing serialized sensitivity at 0.1; it was visible in the Playground Inspector but could not be genuinely tuned by feel with the available synthetic desktop input.
- Preserved Unity-generated ProjectSettings changes in the `player002-editor-settings` stash. No serialized Unity asset was edited.

## Files affected
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-02-11-59-player-002-camera-controller.md`

## Technical decisions
Kept camera look in `PlayerMovement` as requested by the issue, avoiding a second component or package. The existing child Camera is discovered and cached once; `LocalCamera` returns null for non-local players. Native `Cursor` APIs and the already-installed Input System handle lock, Escape, and mouse-click input.

## Verification performed
- Unity 6000.3.25f1 Play Mode suite: 12 passed, 0 failed, 0 skipped.
- `git diff --check` passed.
- Opened Playground in the Unity Editor and entered Play Mode. The first-person view rendered from the player camera at the pivot/head position. Console showed 0 errors and one pre-existing Visual Studio/Unity messaging warning: UDP port 56158 was unavailable.
- The desktop automation marked keyboard and mouse actions as synthetic and unverified. As a result, real cursor lock/relock, sensitivity feel, and jitter while moving and looking on flat ground and ramp remain for a human Play Mode check.

## Final result
The code and automated regression coverage for cursor handling and local camera access are implemented. The camera is connected to `PlayerMovement` through the existing pivot and the issue branch is ready for review.

## Known limitations
- The serialized look sensitivity remains 0.1 and has not been tuned through a reliable human playtest.
- Automated tests verify the requested cursor state and transitions; OS cursor lock and pointer feel were not confirmed with physical input.
- Combined movement/look feel and jitter on both flat ground and ramp need a human Play Mode pass.
- No PR was opened as part of this task.

## Unresolved issues or follow-up work
Before marking issue #8 fully accepted, playtest Playground with physical keyboard/mouse input: verify cursor starts locked/hidden, Escape releases it, a Game view click relocks it, tune and record sensitivity in the PR, and move while looking on flat ground and the ramp. Record the results and the Console warning in the PR.
