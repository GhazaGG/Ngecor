# PR #92 Leg Friction While Pushing, Shared Push Function and Downhill Brake

## Task summary

Implement the project owner's decision on PR #92 (head `fb45d48`): option B for the barrow's foot friction, plus the TL's follow-up steps:

1. Remove the duplicated push law from `WheelbarrowInteraction` by giving `PlayerMovement.ApplyMovementPush` a surface normal.
2. Legs use a low-friction material only while the barrow is held and the player pushes; otherwise they keep their original material.
3. A downhill brake so the barrow does not outrun the player.
4. Play Mode tests for these, and a `docs/DECISIONS.md` entry.

## Relevant previous context

- `2026-10-10-16-25-pr-92-ramp-roll-and-ground-plane-push.md` added `ApplyGroundPlanePush` with a private copy of the push law (`MaxPushForce`, `PushResponseTime`) because `PlayerMovement` was off limits at that time.
- `2026-10-10-17-30-pr-92-roll-gate-and-reverse-investigation.md` left slow, uneven reverse on flat ground unexplained; the PR comment measured 0.86 m/s reverse in `Playground` with `FeetHighFriction` (0.8) legs.
- The TL's latest PR comment (2026-10-10 10:05 UTC) records the owner's decision (option B) and allows `PlayerMovement` changes for the push function.

## Changes made

`Assets/Game/Scripts/Player/PlayerMovement.cs`
- `ApplyMovementPush` and the private `ApplyContactPush` take an optional `surfaceNormal` (zero means world up). The push direction is projected onto that plane and the force acts on the line through the centre of mass inside the plane. With the default the behaviour is unchanged (same as the old `point.y = centre of mass`).
- New `ApplyMovementBrake`: when a body is faster along the push direction than the push target speed, applies a force against the motion. It reuses `CalculatePushTargetSpeed` and `CalculatePushForce` (mirrored) and the same `_maxPushForce * strength` limit. It never pushes and never slows a body below the target. Response time is one frame so the brake holds the target rather than a proportional lag above it.

`Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- Removed the `MaxPushForce` and `PushResponseTime` constants. `ApplyGroundPlanePush` now calls `ApplyMovementPush` and `ApplyMovementBrake` with the ground normal from the wheel raycast.
- New serialized fields `_legColliders` and `_movingLegMaterial`. While held and W or S is pressed the legs' `sharedMaterial` is switched to the moving material; the original materials are cached lazily and restored when W/S is released and in `StopInteraction` (E release, extreme separation, disable, invalid state).

`Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab` (serialized by Unity, not text-edited)
- `_legColliders` = the `Leg_Left` and `Leg_Right` box colliders, `_movingLegMaterial` = `WheelLowFriction`. Unity also wrote the three steering feel fields with their default values (2, 1.5, 0.5), which had not been serialized yet. `FeetHighFriction` and the barrow mass were not changed.

`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `SpawnAndHoldPrefab` takes `cargoMass`, `downhill` and `hold`. For `downhill` the barrow is turned 180 degrees on the 20 degree ramp and the player starts slightly uphill of the handles (see limitations).
- Reverse tests also assert a peak speed (> 2.7 m/s empty, > 2.5 m/s with three cargo cubes) and log peak speeds.
- New tests: three 25 kg loads moved by W on flat ground; held without input on a 20 degree ramp stays within 5 cm in 2 s; not held on the ramp stays within 5 cm in 2 s; W downhill on the 20 degree ramp (empty and three cargo cubes) stays within 110 percent of the push target speed without releasing the hold; legs slide only while held and pushing (W, S; not idle, not A) and recover after E release; legs restore when the interaction is disabled mid push.

`Assets/Game/Tests/Player/PlayerMovementTests.cs`
- The reflection call to `ApplyContactPush` passes the new argument. New tests: push with a surface normal adds nothing along the normal; the brake slows a fast body toward the target and leaves a slow body alone.

`docs/DECISIONS.md`
- New entry "Friksi kaki gerobak saat dipegang (VEH-002)".

## Files affected

- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `docs/DECISIONS.md`
- `.development-history/2026-10-10-17-38-pr-92-leg-friction-and-downhill-brake.md`

## Technical decisions

- **Legs slide on W/S only, not on A/D (deviation from the TL prompt, which says W/S/A/D).** The first implementation also lowered friction for A/D. The existing slope steering tests then failed (`..._ASteersHandleLeftAndNoseRightOnTwentyDegreeSlope`: wheel travel 0.330 m against the limit 0.278 m; the D test 0.334 against 0.321), because with sliding legs the barrow is no longer pivoting on a fixed wheel contact on a 20 degree ramp. The TL prompt also says the A/D feel is approved and the existing steering tests must keep passing, so A/D keeps the original friction. The likely cause (gravity along the ramp on frictionless legs) was not measured separately. Flat-ground A/D tests passed with either variant. Needs the owner's confirmation; the DECISIONS entry says so.
- Brake uses the Rigidbody mass of the barrow, like the push. Cargo that is loose in the tray is not part of that mass, so the brake law sees a 4 kg body. With three 1 kg cubes it holds the target; for three 25 kg loads going downhill it was not tested and may not hold (see limitations).
- Brake response time is one frame (a proportional 0.1 s lag let a 79 kg load settle about 29 percent above the target in a hand calculation; that arithmetic was not run in Unity).
- Material swap uses `Collider.sharedMaterial` so the prefab asset is never modified at runtime.
- Prefab wiring was done by a throwaway Editor script (`PrefabUtility.LoadPrefabContents`, `SerializedObject`, `SaveAsPrefabAsset`) executed in Unity batchmode on a scratch copy, then only the resulting `Wheelbarrow.prefab` was copied into the worktree. The script was not committed. The diff against the old prefab is the seven added YAML lines only. If the team prefers it, the same two fields can be assigned by hand in the Inspector and will serialize the same way.

## Verification performed

Unity 6000.3.25f1 batchmode PlayMode on a scratch copy of the project (the project Editor is open elsewhere). Sources in the scratch copy were byte-identical to the worktree files (`cmp`). Local `Playground.unity` edits in the worktree were not used.
- Full suite on the final code: **157 tests, 156 passed, 0 failed, 1 skipped** (`PourPreviewCreatesMissingDirectoryAndCanCaptureAgain`, needs a graphics device). Run on the working tree state before committing, so the commit hash is not part of the run (it only adds these files).
- Peak speeds in the test fixture (prefab on flat ground, 1 s): reverse S 3.15 m/s empty and 3.06 m/s with three 1 kg cubes; forward W 4.90 and 4.80 m/s. These are fixture numbers; the 0.86 m/s figure in the PR comment came from the real `Playground` scene and is not directly comparable.
- Downhill 20 degrees, W for 2.5 s: peak speed 5.08 m/s empty and 5.14 m/s with three cargo cubes against a 5.00 m/s target; the hold was not released.
- Mutation checks, in the scratch copy only: without the brake call the downhill tests fail (peak 10.4 and 10.0 m/s); with the legs never sliding the 25 kg test fails (0.003 m travel) and the leg-material test fails; reverse peak drops to 2.29 m/s empty and 1.00 m/s with cargo.
- An earlier full run reported 36 failures: 32 `PlayerMovementTests` failed with a missing-script warning while loading `Player.prefab` (import-order artefact in the freshly synced scratch copy; they passed on the next run without changes to those tests) and 4 real ones, which led to the A/D decision above and the downhill fixture change below. A later run caught the reflection call in `PlayerMovementTests` that needed the new argument.
- `git diff --check` on the changed C# files and `docs/DECISIONS.md` is clean.

## Final result

Items 1 to 6 of the TL prompt are implemented and the automated suite passes on the final working tree. Steps 8 and 9's `Playground` hand test and the PR comment/body update were **not** done: I cannot run the Editor here, and nothing was pushed or posted.

## Known limitations

- Not hand-tested in `Playground`: W/S/A/D on flat ground (empty and three cargo), reverse from rest and after moving forward, both ramps up and down, idle hold on a ramp, release on a ramp, Console. The leg-friction feel, the brake feel and the downhill behaviour in particular need the owner. Profiler not run.
- The three 25 kg W test uses loose rigidbody cubes as cargo, like `Playground`'s cargo; real cement bags were not used.
- Downhill with heavy loose cargo (for example three 25 kg) was not tested. Brake and push use the barrow's own 4 kg mass, so a heavy load pushing through the tray may still outrun the player.
- Grabbing a barrow that faces downhill on a 20 degree ramp from beside and behind it can fail with "The grip position is blocked" when the player stands level with the handles, because the horizontal grip path casts into the rising ground. The downhill test starts the player 0.8 m uphill to avoid it. This is existing behaviour of `IsGripPathClear`, not changed here, and may deserve its own issue.
- A/D on a ramp keeps the high-friction legs (see decisions). A barrow held with only A/D pressed on a ramp therefore behaves as before this change.
- `MaxSteeringForce` (350 N) in `WheelbarrowInteraction` is still a constant that has to follow `_maxPushForce` of `Player.prefab`.
- The worktree has unrelated uncommitted changes (`Playground.unity`, `ProjectSettings/VersionControlSettings.asset`, `ProjectSettings/SceneTemplateSettings.json`, `test-results.xml`, `unity-test.log`). They were not committed.
- `main` has three newer commits (MAT-004, MIX-003, INT-006) that are not merged into this branch.

## Unresolved issues or follow-up work

- Owner to hand-test and decide whether A/D should also release the legs (and how to keep ramps safe), then tick the feel items on the PR.
- Post the PR comment (hash, test counts, speeds, per-point hand-test results once they exist) and update the PR body head and counts.
- Decide on the downhill-grab issue and on heavy loose cargo (option C, handle lift, was kept out of scope).
- Merge or rebase `main` before review.
