# MAT-002 Sand Pile Shape and Collider Review

## Task summary

Reviewed and corrected the sand-pile shape and the collision geometry used by the MAT-002 prefabs on `feat/sand-bucket`.

## Relevant previous context

- `.development-history/2026-10-04-00-50-mat-002-sand-bucket-finalization.md` records the current MAT-002 implementation and its automated checks, but no interactive visual or feel review.
- `.development-history/2026-10-02-23-13-complete-mat-005-issue-62.md` confirms MAT-005 supplies quantity and tilt-removal logic. The project decision expects spilled material to become a pile, while MAT-002 explicitly defers that behavior to MAT-004 (#54).
- Current MAT-002 issue #16 requires a visible mound and a carried bucket; granular simulation and spill piles are out of scope.

## Changes made

- Added a low-poly radial mound mesh with a flat base and gently irregular slope, and assigned it to the sand-pile visual and bucket fill visual.
- Replaced the sand pile's oversized box collider with a static mesh collider matching the mound. The pile still has no Rigidbody.
- Replaced the bucket's solid full-volume box collider with five primitive colliders on its bottom and walls, leaving the top and interior open.
- Preserved the existing bulk-container and pour behavior. The temporary Unity Editor helper used to update the prefab assets was removed through the Editor after use.

## Files affected

- `Assets/Game/Art.meta`
- `Assets/Game/Art/Models/Material.meta`
- `Assets/Game/Art/Models/Material/SandPileMound.asset` and `.meta`
- `Assets/Game/Prefabs/Material/SandPile.prefab`
- `Assets/Game/Prefabs/Material/Bucket.prefab`
- This report

## Technical decisions

- A static mesh collider is appropriate for the static pile and follows the project's allowance for mesh colliders on static objects. The bucket remains a dynamic Rigidbody with primitive compound colliders.
- No per-grain physics or new material-transfer behavior was introduced.
- The existing bucket contents still do not add to `Rigidbody.mass`; the project has no per-material bulk mass mapping. Tilt spill still removes units without creating a ground pile, which MAT-002 defers to MAT-004.

## Verification performed

- Unity 6000.3.25f1 batch Editor compiled the project, generated the mesh asset, saved both prefabs, and passed Editor API assertions for the pile's mesh/collider/no-Rigidbody state and the bucket's Rigidbody, material/grab components, five colliders, and shared fill mesh.
- Opened `Dev_Ghaza` in Unity Play Mode and visually confirmed the pile reads as a sloped mound and the bucket has an open top and interior. Stopped Play Mode without saving runtime state.
- `git diff --check` passed.
- Attempted the standard headless Play Mode test runner, but it produced no result XML; this is inconclusive and is not reported as a test pass.
- No controlled carry, pour, tilt, or physics-feel review was completed.

## Final result

The sand pile now has mound-shaped geometry and matching static collision. The bucket collision now represents an open container. The pending MAT-002 changes remain uncommitted on the existing task branch.

## Known limitations

- The visual review only covered the scene appearance in Play Mode; interaction and physics feel were not tested.
- Sand remains an abstract bulk quantity. A full bucket has the same configured Rigidbody mass as an empty one, and tilted material does not yet appear as a ground pile.

## Unresolved issues or follow-up work

- Playtest the `Dev_Ghaza` scene in Unity and review the mound appearance, bucket handling, and collision feel.
- Decide a per-material bulk mass mapping and implement its Rigidbody effect in the shared material-container scope if desired.
- Implement visible spill piles under MAT-004 (#54), or update the project decision and ticket if the intended behavior is to discard tipped contents.
