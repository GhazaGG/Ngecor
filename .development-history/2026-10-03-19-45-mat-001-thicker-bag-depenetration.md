# MAT-001 Thicker Bag and Depenetration Limit

## Task summary
User playtest after the previous tuning: dropping now feels right, but the bag looked too thin compared to a reference photo of a plump 40 kg sack. It also felt too light: an object moved into the stack with the Scene-view move gizmo (red X arrow) sent bags flying even when moved slowly.

## Relevant previous context
- `2026-10-03-19-10-mat-001-apply-cement-bag-tuning.md`: prefab scale (0.7, 0.14, 0.45), mass 40, `CementBag` component, 3 PlayMode tests passing.

## Root cause (flying bags)
Gizmo drags teleport the object's transform. The object overlaps the bag, and PhysX pushes the bag out at up to the project default max depenetration velocity (10 m/s; not overridden in `ProjectSettings/DynamicsManager.asset`), independent of the bag's mass. Grab/carry in MAT-003 could cause the same thing.

## Changes made
- `CementBag.cs`: new `_maxDepenetrationSpeed` (default 0.5 m/s), applied to `Rigidbody.maxDepenetrationVelocity` in `Awake`. Only cement bags are affected; the global physics setting is unchanged (shared settings need team agreement).
- `CementBag.prefab` (via a temporary batchmode editor script that deleted itself): scale (0.65, 0.18, 0.45), `_endThickness` 0.55, `_sideThickness` 0.85, for a fuller, rounder sack.
- `CementBagTests.cs`: new `OverlappingObjectDoesNotLaunchBag` (a static cube teleported 0.1 m into a resting bag; bag speed < 1 m/s after 5 steps).

## Files affected
- `Assets/Game/Scripts/Material/CementBag.cs`
- `Assets/Game/Prefabs/Material/CementBag.prefab` (via Unity API)
- `Assets/Game/Tests/Material/CementBagTests.cs`
- `.development-history/2026-10-03-19-45-mat-001-thicker-bag-depenetration.md`

## Verification performed
- Setup batchmode: exit 0, "CementBagSetup: prefab thickened."; the prefab YAML shows the new values; the temporary script was removed.
- PlayMode tests: 37/37 passed (4 CementBag tests).
- Not verified: the new test was not run with the 10 m/s default, so it is not proven to fail without the fix. The visual shape and feel need the user's playtest.

## Known limitations
- Moving objects with the Scene gizmo is still non-physical (teleport). The bag now separates gently, but a real push should come from the player or a moving Rigidbody.

## Unresolved issues or follow-up work
- User playtest of the thicker shape against the reference.
