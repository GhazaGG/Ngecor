# VEH-002 Wheelbarrow Playtest Follow-up

## Task summary

Applied a follow-up to the user's Play Mode findings on PR #92: side alignment, slow steering, and rough reverse motion.

## Relevant previous context

- The prior PR review fixes are recorded in `2026-10-08-16-08-pr-92-wheelbarrow-review-fixes.md`.
- The user manually reported that a side approach did not start the interaction, steering was very slow, and reverse lifted the front wheel and stuttered. They also reported that cargo felt heavier, ramps worked with and without cargo, and the Unity Console had no errors. These observations describe the prior implementation, not this follow-up.
- The active Unity Editor had an unsaved Playground scene when this follow-up was inspected.

## Changes made

- Route a side approach to a waypoint behind the handles, then move the player to the grip position.
- Increase the bounded steering target speed from 1.2 to 2 radians per second and maximum steering torque from 15 to 25.
- Apply half push strength while reversing to reduce abrupt reverse acceleration.
- Update PlayMode cases to start beside the handles and check the bounded reverse speed.

## Files affected

- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`

No prefab, scene, package, or metadata asset was edited.

## Technical decisions

- Side alignment still uses capsule casts and the existing grab-distance limit; the route passes behind the handle ends before moving laterally.
- Steering remains bounded and uses the existing Rigidbody torque path.
- Reverse remains available but uses half the existing movement strength to soften acceleration.
- The prefab's contact geometry was left unchanged because Unity Editor is required for serialized asset edits, and the open Editor contained unsaved scene changes.

## Verification performed

- `git diff --check` passed.
- Unity PlayMode tests did not start. A separate temporary checkout first failed to copy the built-in `com.unity.shadergraph` cache; after using the existing local cache, Unity failed before test execution with `Failed to create CoreCLR, HRESULT: 0x80004005`. The Editor also displayed `0x8007000E` (insufficient memory). The temporary checkout was removed.
- The original Unity Editor was left open and its unsaved Playground scene was not saved or changed.

## Final result

The side approach and control adjustments are present locally on `RyoFPS/veh-002-wheelbarrow-interaction`. No commit, push, or GitHub response was made.

## Known limitations

- The side alignment and control changes have not been compiled or run in Unity in this follow-up.
- The reverse strength reduction is intended to lessen wheel lift, but ground contact was not verified on the physical prefab.
- The user's reports about cargo weight, ramp handling, and Console cleanliness remain their manual observations from before these edits.

## Unresolved issues or follow-up work

- Re-test starting beside both handles, forward motion, steering response, and reverse wheel contact in Play Mode.
- Recheck empty and loaded cargo on a ramp and inspect the Console after the control changes.
- If reverse still lifts the wheel, inspect and adjust prefab collider/contact geometry through the Unity Editor, then repeat the physical test.
