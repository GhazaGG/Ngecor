# Make Scaffolding Catwalk and Legs Grabbable

## Task summary

Added grab and carry support to the catwalk and support-leg parts in the three custom `Scafolding_v2` assemblies in `Dev_DePo4l`.

## Relevant previous context

BUILD-001 (#18) currently excludes player grab/carry and player assembly of scaffolding. The developer explicitly requested carryable catwalk and leg parts in this follow-up. The existing `PlayerGrab` and `GrabbableObject` system was reused without adding input actions or changing player code.

## Changes made

- Added `GrabbableObject` to 10 scaffold part roots: four catwalk bodies and six support-leg assemblies.
- The first assembly's roots had no Rigidbody; Unity added the required Rigidbody through `GrabbableObject`'s existing component requirement. Other part roots already had Rigidbodies.
- Preserved existing FixedJoints, so connected parts in the later assemblies move together when grabbed.
- Saved the changes through the Unity Editor.

## Files affected

- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity`
- `.development-history/2026-10-05-16-18-grabbable-scaffold-parts.md`

## Technical decisions

- Reused the generic interaction pipeline, primitive colliders, and per-part Rigidbody components; no custom carry code, package, or input binding was added.
- Applied the change to the developer scene's custom `Scafolding_v2` objects. Other scaffolding prefab assets and the main scene were left unchanged.
- Kept existing joints intact. This makes the connected copies carry as assemblies rather than adding a new player dismantling flow.

## Verification performed

- Unity 6000.3.25f1 Editor found all 10 `GrabbableObject` targets after saving and reopening the dev scene; the scene was clean after reload.
- In the Editor, the interaction detector selected a catwalk and `PlayerGrab.RequestGrab` changed its Rigidbody to kinematic with gravity disabled. Dropping restored dynamic physics and gravity.
- A support-leg assembly also entered the held kinematic state through `PlayerGrab.ExecuteGrab`.
- Simulated E-key input did not trigger a drop in this CLI session, so normal keyboard-driven input was not confirmed.
- The full BUILD-001 overload and impact checklist was not rerun; this change only covers carrying the custom scene parts.

## Final result

The custom catwalk and support-leg parts in `Dev_DePo4l` are grabbable through the existing system.

## Known limitations

- The change is in the dev scene, not in reusable prefab assets.
- Existing FixedJoints keep connected parts together. Carrying one of those parts can move the connected assembly.
- Keyboard-driven Play Mode behavior still needs a normal manual check in the Game view.

## Unresolved issues or follow-up work

- Confirm whether this expanded carry scope should also be recorded in issue #18, whose current body lists player carry and player assembly as out of scope.
