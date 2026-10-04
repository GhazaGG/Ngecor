# Scaffold Supports as a Vertical Ladder

## Task summary

Changed the scaffold access design so the scaffold's side supports serve as the vertical ladder: the existing upright posts act as rails and seven wooden rungs span between them on each module.

## Relevant previous context

- The earlier diagonal stair flight did not match the requested vertical ladder.
- The developer selected a climb mechanic for the vertical ladder.
- Ladder movement and the lower module's climb trigger were added in the preceding work; this update integrates the ladder geometry into the scaffold modules.

## Changes made

- Added seven visual wooden rung primitives to the scaffolding module prefab, spanning the existing side supports.
- Removed colliders from the rung visuals so they do not obstruct CharacterController climb movement.
- Removed the separate ladder rails and the slanted access steps from the showcase scene.
- Kept one climb trigger attached to the lower scaffold module so it moves with that module.

## Files affected

- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab`
- `Assets/Game/Scenes/Playground.unity`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Player/LadderClimbable.cs`
- This development history report.

## Technical decisions

- Reused each module's existing side support posts as the ladder rails; only the cross rungs are added.
- Rungs are visual geometry without colliders. The PlayerMovement climb mode handles vertical movement while inside the trigger.
- Kept the climb trigger on the lower module so it follows the physical scaffold. No package or joint was added.

## Verification performed

- Confirmed in Unity Editor that `SupportLadderRung_01` exists in the prefab source and on both scaffold levels.
- Confirmed there is one `LadderClimbable` trigger in the showcase scene and no `AccessSteps_Static` object.
- Entered and exited Play Mode. Unity Pipeline reported zero compile errors and zero console errors.
- A controlled PlayerMovement update with queued W input moved the player slightly upward; the S check did not produce a meaningful measured descent because editor update time was too short. Normal keyboard simulation did not move the player in this editor session.
- Did not verify full ascent to the upper platform, normal physical trigger entry, or walking stability on the top deck. No Unity Test Runner tests were run.

## Final result

The visible ladder is now built into the scaffold supports, with the climb trigger retained for the vertical climb mechanic.

## Known limitations

- Full climb, physical trigger entry, and traversal feel remain unverified in Game View. The editor keyboard simulator accepted key events but did not sustain movement between frames.

## Unresolved issues or follow-up work

- Manually enter Play Mode and climb from the lower level to the upper platform using W/S. Confirm the player mounts the ladder trigger, reaches the upper deck, and stands/walks without jitter or penetration.

