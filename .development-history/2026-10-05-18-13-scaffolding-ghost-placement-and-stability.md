# Add a Rotatable Scaffolding Ghost and Breakable Support Joints

## Task summary

Added a mesh-only ghost preview for carried scaffolding parts, yaw rotation before placement, collision-free carrying, upright placement orientation, and breakable physical joints so an installed support can topple from a strong impact and be reconnected.

## Relevant previous context

The earlier player assembly work connected touching catwalk and support parts with FixedJoints. Issue #18 currently lists player assembly and re-erecting fallen scaffolding as out of scope. The developer explicitly requested this follow-up behavior; the issue itself was not edited.

## Changes made

- `PlayerGrab` builds a translucent mesh-only ghost while a scaffolding part is carried. The ghost follows the carried part pose and changes color to show whether it can connect to a complementary part.
- Pressing R rotates the ghost and carried part by 90 degrees around world vertical. The preview keeps scaffolding parts level.
- Colliders on any carried object are disabled while held and restored on release, so the held object cannot physically push other objects. The source renderers are hidden while the ghost is shown and restored on release.
- Picking up an installed scaffolding part removes its joints to other scaffolding parts so the connected pieces are not dragged along.
- Scaffolding FixedJoints now have Inspector-tunable break force and break torque, defaulting to 2000 N and 350 N m. A strong physical impact can break a support joint; the fallen support remains a grabbable object and can be connected again using the existing assembly interaction.

## Files affected

- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Interaction/ScaffoldingPart.cs`
- `.development-history/2026-10-05-18-13-scaffolding-ghost-placement-and-stability.md`

## Technical decisions

- The ghost copies only MeshFilter and MeshRenderer data. It has no collider, Rigidbody, or gameplay scripts.
- The real carried colliders are briefly re-enabled only while checking contact against other scaffolding parts, then disabled again before the next physics step.
- FixedJoint break limits use the lower force and torque values from the connected catwalk and support, so either part can tune that connection.
- No prefab, scene, package, or ProjectSettings files were changed in this follow-up.

## Verification performed

- Unity 6000.3.25f1 script recompilation completed.
- In Play Mode, a direct grab smoke check confirmed that the mesh ghost was created, the real part renderers were hidden, and its colliders were disabled while carried.
- After releasing the part, confirmed the player was no longer carrying it and its renderer was restored.
- Exited Play Mode and confirmed the development scene was not dirty.
- `git diff --check` passed for `PlayerGrab.cs`.
- A simulated R key press did not verify the rotation input. The support impact, topple, and reinstallation flow were not exercised end to end.

## Final result

Scaffolding now has a rotatable ghost placement preview, and its installed support connections can break under configured physical force and torque. The code compiles, but rotation input and impact tuning still need a direct gameplay playtest.

## Known limitations

- Rotation is around the vertical axis in 90-degree steps.
- Break thresholds are starting values and need feel testing with the project throw or impact speeds.
- The Unity CLI smoke check did not confirm the R key event or simulate a heavy thrown-object impact.

## Unresolved issues or follow-up work

- In Game view, verify R rotation, placement against a matching part, collision-free carrying, stability under normal load, and break/reinstall after a heavy thrown-object impact.

