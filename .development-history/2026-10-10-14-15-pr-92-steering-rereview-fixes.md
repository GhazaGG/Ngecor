# PR #92 Steering Re-review Fixes (A/D Direction, Roll, Player-Led Steering, Grounded Grip)

## Task summary

Address the two changes-requested reviews GhazaGG posted on PR #92 (`[VEH-002] Wheelbarrow player interaction`) against head `31ce142` (re-review at 2026-10-08 14:49 UTC and gameplay re-review at 15:07 UTC):

1. A/D mapping was reversed relative to the owner's confirmed control: A must move the player/handle left and swing the nose right; D the opposite.
2. Sideways force applied at the (high) handle point rolled the barrow so one side lifted; the player was still only a follower of the cart.
3. The wheel's lateral "ground grip" counter-force ran even when the wheel did not touch the ground.
4. Tests used a synthetic barrow without a `Wheel` child and only checked yaw sign and total displacement.

## Relevant previous context

- `.development-history/2026-10-08-21-10-wheelbarrow-steering-fulcrum-pivot.md` introduced handle-point lateral force with a wheel contact fulcrum. That report recorded the opposite A/D mapping; the owner's later playtest on `31ce142` superseded it.
- `docs/DECISIONS.md` fixes the human push limit at 350 N (`_maxPushForce` on `Player.prefab`) and describes the Playground "three cargo" as loose cubes (`Cargo_1..3`, 1 kg each in the Playground scene).
- Reviews also noted the headless fixture never reaches ground contact; that is why a prefab-based test was required.

## Changes made

`Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- **Mapping:** the handle is pulled toward the player's real sideways position; A steps the player and handle left, nose turns right (positive yaw); D the opposite.
- **Player leads:** in `LateUpdate` the player takes a lateral step (`CharacterController.Move`, so walls stop it) up to a 0.5 m "lead" leash from the grip pose. The follow step now only restores the lateral part beyond that leash. `FixedUpdate` then applies a leash spring-damper at the handle toward the player. A blocked player has no lead, so the cart is not pushed away from the grip.
- **No roll:** steering force and the wheel grip force are applied at centre-of-mass height (horizontal handle/contact position, COM height). Steering is also skipped above 15 degrees of tilt.
- **Grounded grip only:** the wheel lateral counter-force and the no-input lateral damping run only when `TryGetWheelGroundContact` succeeds. Steering at the handle still runs without contact.
- **Stability and bounds:** spring and damper are capped to what is numerically stable for the handle point's effective mass (yaw inertia about the wheel / lever arm squared); an earlier uncapped version made yaw flip sign every physics step. Force is also limited per step so yaw stays under `MaxSteeringSpeed` (2 rad/s), with braking above it. Maximum steering force is 350 N, matching the player prefab's push limit.
- Cached the `Wheel` transform lookup; removed the unused `PushResponseTime`.

`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- Replaced the D and A synthetic tests with one shared routine that checks signed player shift, signed handle shift, nose yaw direction, bounded yaw and roll.
- Added `BlockedPlayerSteeringDoesNotPushTheWheelbarrow` (wall beside the player).
- Added prefab tests on a real ground surface using the original `Wheelbarrow.prefab`: D and A, empty and with three 1 kg cargo cubes. They assert the front wheel actually touches the ground, player/handle shift direction, nose yaw, roll, bounded peak yaw, and that wheel travel stays under half of handle travel.
- Added `WheelbarrowPrefab_LiftedWheelHasNoArtificialGroundGrip` and `WheelbarrowPrefab_WPushesForwardWithThreeCargo`.
- Steering tests pin `Time.captureFramerate` to 60 (reset in `TearDown`) because headless frames are sub-millisecond, which makes each sideways step smaller than `CharacterController.minMoveDistance`. The prefab tests set the test player's `_maxPushForce` to the prefab's 350 N.

No `.unity`, `.prefab`, `.asset` or `.meta` file was edited.

## Files affected

- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-10-14-15-pr-92-steering-rereview-fixes.md`

## Technical decisions

- Leash model instead of speed-chasing: a force from lead (spring) needs no knowledge of cargo mass, so loaded barrows simply need a bigger lead and respond slower. The Rigidbody mass (4 kg) excludes loose cargo, so mass-based force formulas under-drive a loaded barrow.
- Force at COM height rather than handle height removes the roll moment while keeping the handle's horizontal lever arm for yaw, as the review suggested, without locking the Rigidbody or teleporting it.
- The grip counter-force keeps its mass/dt gain; it is only gated on real ground contact.

## Verification performed

Unity 6000.3.25f1 batchmode PlayMode run on a scratch copy of the project (the project's own Editor was open, so batchmode could not use it), after a clean Library import:
- Full suite: 139 tests, 138 passed, 0 failed, 1 skipped (`PourPreviewCreatesMissingDirectoryAndCanCaptureAgain`, needs a graphics device).
- 18 wheelbarrow-related tests pass, including the new prefab tests. In the prefab runs (empty / three 1 kg cargo, 25 physics steps of A or D): handle moved about 0.34-0.39 m toward the player's side, player moved about 0.72-0.75 m, nose yawed about 10-11 degrees in the expected direction, tilt change under 1 degree, front wheel travel 1-3 cm.
- Per-step yaw trace after the stability fix showed smooth yaw (about -0.5 rad/s) with no step-to-step sign flips.
- `git diff --check` on the two modified C# files is clean.

## Final result

The code and automated tests address all four review points. Manual feel in `Playground` was **not** run by me.

## Known limitations

- Not verified manually in Playground: A/D feel, W+A/D, S, E release, ramps (`Ramp_Gentle`, `Ramp_20Degree`), obstacle near the handle, Console warnings. Review gameplay gate remains open.
- Three 75 kg cement bags in the tray (a heavier setup than the Playground's cargo) made even forward push (W) stall in my fixture; that W behavior is unchanged by this work and was not tuned for.
- No Profiler run; per-FixedUpdate work added is a few vector operations and one downward raycast.
- PR body still lists head `31ce142` and the earlier test counts and needs updating after push.

## Unresolved issues or follow-up work

- Owner/reviewer Playground playtest of this head.
- Commit and push (not done), then update the PR body and reply to the review.
- Uncommitted local changes unrelated to this task exist in the worktree (`Playground.unity`, `VersionControlSettings.asset`, `SceneTemplateSettings.json`, `test-results.xml`, `unity-test.log`) and were left untouched.
