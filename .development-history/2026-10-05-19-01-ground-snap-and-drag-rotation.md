# Ground-Snapped Support Preview and Mouse-Drag Rotation

## Task summary

Updated the scaffolding ghost placement controls so a support preview aligns its bottom with the ground, and scaffolding can be rotated continuously by holding the right mouse button and dragging horizontally.

## Relevant previous context

The previous ghost preview used fixed 90-degree rotation steps. This follow-up replaces that input and adds ground alignment for support parts, as requested. The earlier ghost, carry-collision, and breakable-joint behavior remains in place.

## Changes made

- Support parts now raycast beneath their carried position and shift vertically so the bottom of their cached, non-trigger colliders touches the highest qualifying static surface found within the configured distance.
- The ground-snap query reuses the `GrabbableObject` collider cache and a fixed raycast hit buffer instead of allocating a collider array each frame.
- Holding the right mouse button and moving the mouse horizontally rotates carried scaffolding continuously. `SmoothDampAngle` eases the current yaw toward the mouse-drag target.
- The local player's look input is suppressed only while dragging a scaffolding part, so the drag controls the object instead of the camera.
- Inspector settings expose the snap distance, degrees per mouse pixel, and rotation smoothing time.

## Files affected

- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `.development-history/2026-10-05-19-01-ground-snap-and-drag-rotation.md`

## Technical decisions

- Rotation uses horizontal mouse delta with a default sensitivity of 0.25 degrees per pixel and a 0.06-second smoothing time. Both values can be tuned in the Inspector.
- The ground snap uses a downward ray at the support's carried X/Z position, ignores colliders attached to Rigidbodies, and accepts surfaces with an upward normal of at least 0.5. The default search distance is 5 m.
- Catwalk parts keep their carried height; only support parts snap vertically.

## Verification performed

- Unity 6000.3.25f1 script recompilation completed with `failed: false`, `compilationFailed: false`, and no errors.
- `git diff --check` passed for the two modified scripts. A repository-wide diff check reported trailing whitespace on existing modified lines in `Dev_DePo4l.unity`; that scene was not changed for this follow-up.
- Play Mode interaction was not run, so mouse feel and ground alignment still need direct gameplay verification.

## Final result

The code compiles with continuous, smoothed mouse-drag rotation and vertical ground snapping for support previews.

## Known limitations

- Ground alignment follows one vertical ray under the support's carried X/Z position. It does not tilt the support to match sloped ground.
- Only static, sufficiently upward-facing colliders count as ground. If none is found within the configured distance, the support stays at its carried height.
- Sensitivity, smoothing, and snap distance have not been tuned through a Play Mode feel test.

## Unresolved issues or follow-up work

- Verify the support snap and mouse-drag rotation in Play Mode and adjust the Inspector defaults if the feel needs tuning.
