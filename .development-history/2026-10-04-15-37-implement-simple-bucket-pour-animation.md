# Simple Bucket Pour Animation

## Task summary

Implemented the user's explicitly requested simple pouring animation: the bucket model tips and visible sand grains travel from its lip toward a nearby receiver. Free ground pouring remains deferred to MAT-004, as agreed in the conversation.

## Relevant previous context

- `2026-10-04-15-15-assess-bucket-animation-and-free-ground-pouring.md` records the earlier proposal, which was not implemented at that time. The user subsequently separated free pouring and requested execution of the animation only.
- `2026-10-04-04-05-correct-sand-pile-volume-and-verify-prefab-physics.md` records the five hollow bucket colliders and the previous 22-test material baseline.
- `2026-10-04-14-24-create-mat-002-manual-test-cases.md` records the manual checklist, including still-unverified keyboard input and gameplay feel.
- Reviewed current source, prefab configuration, relevant project rules and performance budget, and the installed URP particle shader. The project has no `.codegraph/` index.

## Changes made

- Added `BucketPourVisual` to animate the model toward a 65-degree pouring pose and back to its neutral pose.
- Exposed the active receiver and a short recent-transfer feedback window from `BucketPourAction`. Feedback follows actual positive transfers; cancel and receiver exit clear it. A short tail lets the final single material unit remain visible.
- Added `Pour Model` to the bucket prefab and reparented its existing walls, bottom, fill visual, and colliders through Unity Editor APIs. The fill reference and five primitive colliders remain intact.
- Added a lip emitter using world-space mesh particles: small gold cubes, 0.3-second lifetime, maximum 32 alive, no per-particle collision or Rigidbody. Its velocity targets the receiver surface while including gravity.
- Created a shared URP Unlit particle material, with particle shadows disabled.
- Added three Play Mode checks for tilt/emission/cancellation/conservation, rejected/full/empty transfers, and visibility of the final single unit. The graphics-enabled run captures actual runtime renders while pouring and after stopping.
- Updated the manual checklist with the new component/hierarchy, visible pour expectation, and TC-MAT002-14. Recorded the authorized scope and particle limit in `docs/DECISIONS.md`.
- Removed the temporary prefab setup helper through `AssetDatabase.DeleteAsset`.

## Files affected

- `Assets/Game/Scripts/Material/BucketPourAction.cs`
- `Assets/Game/Scripts/Material/BucketPourVisual.cs` and Editor-generated `.meta`
- `Assets/Game/Prefabs/Material/Bucket.prefab`
- `Assets/Game/Materials/Material/SandFlow.mat` and Editor-generated `.meta`
- `Assets/Game/Tests/Material/SandPileAndBucketPrefabPlayModeTests.cs`
- `docs/MAT-002-Sand-Pile-Bucket-Test-Cases.md`
- `docs/DECISIONS.md`
- This report
- Generated evidence under ignored `Logs/`: setup/cleanup/test logs, result XML, and two preview PNGs.

## Technical decisions

- Kept quantities and transfer in the existing `BulkMaterialContainer`; its implementation was not changed.
- Animated the child model and its matching wall colliders, rather than adding animation rotation to the root Rigidbody. This avoids automatic pose feedback also triggering the root-based spill rule and removing units twice.
- Cached components and receiver collider references. No package, global manager, event bus, networking, or per-grain physical simulation was introduced.
- Restored the neutral model pose immediately when the bucket is no longer held. Cancellation stops new emission; existing world-space grains finish their short lifetime.
- The 0.2-second feedback window bridges integer transfer ticks and represents the last unit. Particles are visual feedback, not independent material stock.

## Verification performed

- Unity 6000.3.25f1 Editor setup and cleanup both exited with code 0. Prefab reload validation confirmed model/emitter references and exactly five BoxColliders.
- Graphics-enabled Unity Play Mode material suite exited with code 0. `Logs/BucketPourAnimationResults.xml` reports **25/25 Passed**, zero failed, inconclusive, or skipped, at approximately 15:32 WIB.
- Tests verified actual prefab tilt, emitted particles, reset after cancel, no further transfer after cancel, conserved source-plus-receiver totals, no effects for initially rejected/full/empty transfers, completion at empty, and final single-unit feedback.
- Visually inspected `Logs/BucketPourAnimation-Pouring.png`: the bucket is tipped and gold grains visibly exit its lip. Inspected `Logs/BucketPourAnimation-Stopped.png`: the bucket is neutral and the grains are gone. These are actual URP runtime renders from the scripted test fixture, not mockups.
- Existing material and prefab physics checks also passed in the same run.
- Confirmed the temporary Editor helper and Test Runner initialization scene were removed. Scene, input, and assembly-definition diff statistics remained unchanged; only the intended additional decision text changed in tracked files.
- Newly changed runtime/test C# and checklist files have no trailing whitespace. Full checkout `git diff --check` still fails on the eight pre-existing serialized whitespace lines in `Dev_Ghaza.unity`; the scene was preserved.

## Final result

Simple receiver-bound pouring feedback is implemented and verified through Play Mode checks and runtime renders. The updated prefab is ready for the user's manual E/R test. Changes remain local and uncommitted.

## Known limitations

- The new render/checks drive the pour adapter through its API; they do not prove a real interactive R press, player-perceived carry/drop alignment, or gameplay feel. Manual checklist results remain NOT VERIFIED.
- Particle visibility was checked in a controlled render. No weakest-laptop FPS or Profiler capture was performed; the fixed particle cap bounds the added visual load.
- The effect represents active receiver-bound pouring. Passive tilted-drop spill effects and ground pile creation are not added.
- Bulk quantity still does not affect Rigidbody mass. The existing root-tilt spill rule remains unchanged.

## Unresolved issues or follow-up work

- Run TC-MAT002-07/08/14 in the user's dev test scene and review keyboard input, camera alignment, cancel/drop behavior, and visual readability.
- Profile the effect on the team's target low-end hardware before claiming the performance target.
- Implement free pouring and recoverable ground piles under MAT-004 separately.
