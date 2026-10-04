# Side Stairs Attached to Scaffold Support

## Task summary

Repositioned the static stair flight onto the scaffold's front support plane and added a wooden brace that connects the upper inner stringer to the vertical frame post. The test player now starts aligned with the stair approach.

## Relevant previous context

- Issue #18 requires static access to the upper platform without jump or climb behavior.
- The Player CharacterController is configured with a 0.45 m step offset and a 50 degree slope limit.
- Earlier work created ten wooden steps and two diagonal stringers, but the flight sat through the scaffold footprint and was not aligned to the frame support.
- The project restrictions leave `PlayerMovement.cs` and `InputSystem_Actions.inputactions` unchanged.

## Changes made

- Shifted `AccessSteps_Static` to local Z = -1.4 so the inner stringer sits at Z = -0.91, beside the front support post plane at Z = -0.9.
- Added a primitive `UpperRail_Brace` beneath the upper tread. It spans the gap between the inner stringer and the upper front-left post.
- Repositioned `Player_Test` to (-12.45, 0, -1.4), facing the stair approach.
- Assigned the existing wood material to the brace and saved the scene through Unity Editor.

## Files affected

- `Assets/Game/Scenes/Playground.unity`
- This development history report.

## Technical decisions

- Kept the existing ten-step flight and its static primitive colliders.
- Each tread rises 0.376 m over a 0.5 m run, giving an approximate 36.9 degree pitch.
- The upper tread surface aligns with the upper platform at Y = 3.76 m.
- Kept the stair independent of the scaffold Rigidbody so the access structure remains static.

## Verification performed

- Inspected all ten tread transforms in Unity Editor: every run is 0.5 m and every rise is 0.376 m.
- Confirmed the first and last tread use non-trigger BoxColliders.
- Confirmed the rise is below the Player step offset (0.45 m) and the calculated pitch is below the slope limit (50 degrees).
- Confirmed the stair hierarchy contains no Rigidbody, MeshCollider, or Joint.
- Confirmed the brace uses the existing wood material and its BoxCollider is non-trigger.
- Confirmed the Player CharacterController settings and existing PlayerInput action map/control scheme.
- Unity's test discovery reported zero existing tests. The standalone Unity test command could not run while the project Editor was open.
- Entered Play Mode, but the Pipeline connection dropped before runtime movement could be inspected. Returned the Editor to Edit Mode. No traversal or feel test is claimed.

## Final result

The stair flight is positioned along the scaffold's front support plane, with a primitive brace connecting its upper inner rail to a vertical post. Static geometry meets the configured step and slope limits.

## Known limitations

- Keyboard traversal, top-platform walking, and jitter/penetration behavior remain unverified in Play Mode.
- Tread BoxColliders still use Unity's default physics material; the existing scaffolding friction material was not successfully assigned to them.

## Unresolved issues or follow-up work

- In Play Mode, walk from the marked Player_Test start to the upper deck, walk across the deck, and descend. Confirm no input failure, catching, jitter, or penetration.
- If runtime verification continues to disconnect Pipeline, run the same checks manually in the Game view and record the result.

