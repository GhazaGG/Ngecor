# Add Static Steps to the Custom Scaffolding

## Task summary

Added static stair access to the developer's custom `Scaffolding_` prefab, following issue #18's requirement for a ladder or ramp that works with normal CharacterController movement. No climb mechanic or PlayerMovement code was added.

## Relevant previous context

The plan and cleanup are recorded in `2026-10-04-17-13-prepare-static-scaffold-ladder-plan.md`. The developer then directed implementation. GitHub issue #18 and GhazaGG's comment require static access with `stepOffset = 0.45 m`, `slopeLimit = 50°`, and no jump/climb behavior. The scene has two instances of the custom prefab. The existing custom scaffold also contains multiple Rigidbodies and FixedJoints, which remains a separate mismatch with the comment's single-Rigidbody/no-joint decision.

## Changes made

- Reshaped the four existing thin rungs in each ladder flight into walkable BoxCollider treads and added one primitive bottom step per flight.
- Set each flight to five treads: 0.12 m thick, 0.44 m deep, and 1.14 m wide. The existing treads are offset outward by 0.38 m per rise; their existing center heights remain 0.643, 1.064, 1.448, and 1.766 m. The new bottom step is centered at 0.30 m.
- Spread the two vertical side rails outward by 0.11 m per side to give the 0.35 m-radius CharacterController room to pass.
- Applied the prefab changes through the Unity Editor, so both scene instances use the updated source prefab.
- Kept the previously disabled aggregate ladder-root colliders disabled; the individual rail and tread colliders remain enabled. No new Rigidbody, joint, script, input action, or friction/mass setting was added.
- Saved `Dev_DePo4l.unity`, then reloaded it to restore the saved Player position after the temporary Play Mode traversal.

## Files affected

- `Assets/Game/Prefabs/Construction/Scaffolding_.prefab`
- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity`
- This history report

## Technical decisions

- The platform top is approximately 2.11 m high. The lowest tread's top is approximately 0.36 m; adjacent tread rises are at most 0.42 m, and the top tread is about 0.29 m below the deck. These heights stay within the 0.45 m step offset.
- Each tread overlaps its neighbor horizontally by about 0.06 m, preventing gaps between the BoxCollider surfaces. The two existing vertical rails remain on the sides.
- The new cubes have primitive BoxColliders and remain children of the existing ladder bodies. This adds colliders, not Rigidbody bodies.

## Verification performed

- Unity 6000.3.25f1 recompile completed with zero compiler errors and warnings.
- Confirmed all four ladder roots across the two prefab instances contain `Step_Base`; its BoxCollider is enabled and not a trigger.
- In Play Mode, reset the Player to the foot of one flight and invoked 40 CharacterController moves of 0.08 m toward the deck. It reached position `(-5.78, 2.18, 1.65)`. Twelve further 0.08 m moves across the platform reached `(-5.78, 2.18, 0.69)` with `isGrounded = true`.
- Stopped Play Mode and reloaded the saved dev scene. The scene is clean and the Player is back at its saved position `(-5.73, 3.09, 1.50)`.
- Keyboard W input was not verified: the Editor did not advance normal runtime frames during this session, so the CharacterController geometry was checked directly rather than through the input action.

## Final result

Both ends of the custom scaffold now have a static five-tread flight while retaining the vertical side rails. CharacterController movement reached and crossed the deck in Play Mode. Keyboard feel still needs a normal interactive Play Mode check.

## Known limitations

- The custom scaffold still contains multiple Rigidbodies and FixedJoints. This change only addresses ladder access and does not make the complete prefab conform to the separate single-Rigidbody/no-joint direction in GhazaGG's comment.
- The aggregate ladder-root BoxColliders remain disabled because their dimensions came from the earlier climb-zone calculation; the child primitive colliders provide the visible rails and steps.
- Actual keyboard-driven ascent and descent were not verified in the Editor.

## Unresolved issues or follow-up work

- Try W/S ascent and descent in the Unity Game view to confirm input feel and clearance.
- Have the lead resolve the prefab's Rigidbody/FixedJoint mismatch before treating all of issue #18 as complete.
