# Make Scaffolding Catwalks and Supports Connectable

## Task summary

Added physical assembly for the custom scaffolding catwalk and support parts. Releasing a carried part while a complementary part is touching it creates a `FixedJoint` at the measured contact position without moving either part.

## Relevant previous context

The preceding follow-up made the custom parts grabbable in `Dev_DePo4l`. BUILD-001 (#18) still lists player assembly as out of scope. The developer explicitly requested assembly in this task, so the implementation extends the dev scene and related custom prefabs without changing the issue.

## Changes made

- Added `ScaffoldingPart` with `Catwalk` and `Support` roles and a per-part connection tolerance.
- Updated `PlayerGrab` to try a connection after releasing a carried scaffolding part. Only complementary roles connect; existing links are not duplicated.
- Added short carry feedback indicating that releasing a scaffolding part connects it when close enough.
- Marked the 10 custom parts in `Dev_DePo4l` and the custom catwalk/support prefabs with their roles.
- Saved the dev scene through the Unity Editor.

## Files affected

- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Interaction/ScaffoldingPart.cs` and its Unity-generated `.meta`
- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity`
- `Assets/Game/Prefabs/Construction/Cat_Walk.prefab`
- `Assets/Game/Prefabs/Construction/Cat_Walk (1).prefab`
- `Assets/Game/Prefabs/Construction/Kaki_scafolding.prefab`
- `.development-history/2026-10-05-17-04-scaffold-part-assembly.md`

## Technical decisions

- The catwalk owns the `FixedJoint`, connected to each touching support body. The two anchors are calculated from collider closest points and the current transforms are left unchanged, so there is no forced snapping.
- Primitive collider bounds are used as a broad phase; the default connection tolerance is 0.08 m and can be tuned in the Inspector.
- Reused the existing grab/drop action and Rigidbody setup. No new input binding, package, or scene was added.

## Verification performed

- Unity 6000.3.25f1 completed script recompilation.
- In Play Mode, called `PlayerGrab.ExecuteGrab` followed by `ExecuteDrop` on temporary primitive parts in contact. Verified that one FixedJoint connected the support Rigidbody, the catwalk returned to dynamic physics, and its position and joint anchors remained unchanged. Exited Play Mode and confirmed the temporary objects were gone.
- After the final carry-feedback/cache change, repeated the direct grab/drop flow in the loaded Editor; it passed.
- In the Editor, repeated the connector check with a 0.05 m gap; it created the joint without moving the catwalk.
- The scene remained clean after verification. Keyboard-driven Interact input was not simulated.

## Final result

Players can carry a custom catwalk or support, place it against the complementary part, and press Interact to drop and attach it physically.

## Known limitations

- Assembly only connects a catwalk to supports. It does not connect two catwalks or two supports, and there is no detach action.
- The full Play Mode check for a small gap timed out in the Unity CLI; the 0.05 m tolerance was verified directly in the Editor instead.

## Unresolved issues or follow-up work

- BUILD-001's current scope excludes player assembly. The developer requested this extension here; issue #18 was not edited.
- Confirm the E/Interact flow manually in the Game view.
