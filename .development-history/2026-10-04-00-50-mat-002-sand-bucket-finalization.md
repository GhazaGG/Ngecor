# MAT-002 Sand Pile and Bucket — Finalization

## Task summary
Finalized the local Stage A and Stage B implementation for MAT-002 (#16) on `feat/sand-bucket`. `origin/main` is still `eb4a0dac14182e324650f5e493872d8736f148f3`, which includes merged INT-003 (#11), PR #75.

## Relevant previous context
- MAT-005 (#62) owns bulk quantity, capacity, acceptance, conserved transfer, fill visuals, and tilt spill. Its API was sufficient and its core code was not changed.
- INT-002 and merged INT-003 provide the existing generic carry/drop path. `PlayerGrab` and related interaction code were not changed.
- `.development-history/2026-10-04-00-46-mat-002-sand-bucket.md` is an interim report from before final prefab wiring and the final test run. This report supersedes its implementation and verification details: the pour input is on the bucket prefab, with the reference persisted after prefab reload.
- Existing unrelated untracked project files were preserved.

## Changes made
- Created static sand-pile and dynamic bucket prefabs with URP materials. The bucket uses the existing bulk container and generic grab components.
- `BucketPourAction` records pour intent on the bucket and transfers via MAT-005. `DebugPourReceiverTrigger` registers the receiver while any bucket collider overlaps its trigger; it no longer starts transfer on proximity alone.
- Added `BucketPourInput` to the bucket prefab. It polls `Player/Pour` only while the bucket is held by a local player, requests transfer while held, and cancels on release.
- Added the `Player/Pour` Input System action with `R` binding and a `PlayerPour` reference asset. Recorded `R` as a binding proposal in `docs/DECISIONS.md`; Interact remains `E`, Throw remains left mouse.
- Updated `Dev_Ghaza.unity` with the static sand pile, a bucket configured with 12 sand units, sand and rejecting receivers, taller pour triggers, and the local player aimed toward the bucket. `Playground.unity` was not changed.
- Added six MAT-002 Play Mode tests, including compound bucket collider overlap tracking.

## Files affected
- `Assets/Game/Materials/Material/Bucket.mat` and `.meta`
- `Assets/Game/Materials/Material/Sand.mat` and `.meta`
- `Assets/Game/Prefabs/Material/Bucket.prefab` and `.meta`
- `Assets/Game/Prefabs/Material/SandPile.prefab` and `.meta`
- `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`
- `Assets/Game/Scripts/Material/BucketPourAction.cs` and `.meta`
- `Assets/Game/Scripts/Material/BucketPourInput.cs` and `.meta`
- `Assets/Game/Scripts/Material/DebugPourReceiverTrigger.cs` and `.meta`
- `Assets/Game/Scripts/Material/Ngecor.Material.asmdef`
- `Assets/Game/Settings/InputSystem_Actions.inputactions`
- `Assets/Game/Settings/PlayerPour.inputactionreference.asset` and `.meta`
- `Assets/Game/Tests/Material/BucketPourPlayModeTests.cs` and `.meta`
- `Assets/Game/Tests/Material/Ngecor.Material.Tests.asmdef`
- `docs/DECISIONS.md`

## Technical decisions
- The bucket owns the pour adapter because it can gate requests through `GrabbableObject.IsHeld` and `CurrentHolder`; local-player verification uses the holder's existing `PlayerMovement.LocalCamera`.
- Transfer amounts and target contents remain in `BulkMaterialContainer`; pour input only expresses intent.
- Receiver registration counts overlaps per collider so one side of a compound bucket can leave while another side remains in range.
- No package, manager, event bus, or networking code was added.

## Verification performed
- Unity 6000.3.25f1 Play Mode batch run: `Ngecor.Material.Tests` passed **18/18** tests, including all six MAT-002 tests.
- Unity Editor batch APIs verified the pile has no Rigidbody, the bucket has its required components and 12 configured sand units, the receiver accepts sand, the reject receiver does not, and the pour action reference survives prefab save/reload and is present on the Dev scene bucket instance.
- `git diff --check` passed. Final `origin/main` check returned `eb4a0dac14182e324650f5e493872d8736f148f3`.

## Final result
Stage A and Stage B code and serialized setup are present locally, with the full material Play Mode suite passing. The branch is ready for gameplay review; no commit, push, or PR was created.

## Known limitations
- The headless Unity Test Runner did not drive a synthetic keyboard event through the `InputAction` phase. Automated tests cover the carried-bucket request, receiver gating, transfer, and cancel path; the `R` binding and prefab reference were verified through Editor APIs.
- The interactive `R` press, bucket carrying/pour alignment, gameplay feel, screenshot, and performance capture have not been checked in the Unity Editor.

## Unresolved issues or follow-up work
- Run the interactive `Dev_Ghaza` playtest and record carry/pour feel.
- Open the Stage B PR after gameplay review, use the repository template, and include `Closes #16` only on that Stage B PR. Fill `Not Tested` with the remaining interactive checks.
