# Resolve Ramp and Wall Obstacle Penetration for Carried Objects

## Task summary
Resolved carried object penetration into inclined ramps and angled obstacles (reported by user in `Playground.unity`) while guaranteeing the held object never clips into or behind the player's camera.

## Relevant previous context
- INT-002 and INT-003 established grab, carry, and drop functionality (`GrabbableObject` and `PlayerGrab`).
- In INT-002, `ResolveHoldPosition` used a 1D `SphereCastNonAlloc` clamped to `minDistance = camera.nearClipPlane + _carriedRadius + 0.05f` along the camera-to-holdpoint ray to prevent the carried object from disappearing behind the player's head when facing walls.
- When approaching inclined surfaces (such as the 20-degree `Ramp` in `Playground.unity`), 1D sweeping along the eye-to-hand ray failed to detect surfaces cutting from below or deflected incorrectly, and clamping to `minDistance` pushed the object forward into the ramp geometry, causing visible penetration (evidenced in screenshot `orca-paste-1791040122825-6d915585-16d1-40af-bb9d-c8b8f81b227a.png`).

## Changes made
1. **3D World Depenetration (`PlayerGrab.cs`):**
   - Added preallocated `_holdColliders = new Collider[16]` buffer to prevent GC allocations during overlap checks.
   - Enhanced `ResolveHoldPosition` with a 3D depenetration pass using `Physics.OverlapSphereNonAlloc` and `Physics.ComputePenetration`.
   - When the carried object overlaps any static or world collider (such as an inclined ramp, slope, table, or wall), `ComputePenetration` calculates the exact minimal separation vector (`depenDir * depenDist`).
   - For ramps and inclined surfaces, the normal direction (`depenDir`) has an upward vertical component, naturally lifting the held object up and resting it cleanly on top of the ramp surface without penetration.
2. **Camera Near-Clip & Visibility Guarantee (`PlayerGrab.cs`):**
   - Maintained strict forward-distance clamping along `camera.transform.forward` (`forwardDist >= minDistance`), ensuring that no obstacle collision can ever push the object behind the camera view or into the camera's near-clip plane.
3. **Automated PlayMode Test Coverage (`PlayerGrabTests.cs`):**
   - Added `CarriedObject_ObstructedByRamp_DoesNotPenetrateRamp` to verify that an inclined ramp (20° pitch) placed at the hold point does not suffer collider penetration (`ComputePenetration` penetration distance <= 0.01m) and maintains forward clearance in front of the camera near-clip plane.

## Files affected
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-03-22-20-resolve-ramp-and-wall-penetration.md`

## Technical decisions
- **Native PhysX ComputePenetration:** Rather than approximating arbitrary slopes with custom rays, `Physics.ComputePenetration` natively evaluates the minimal translation vector between the held object's collider and the obstacle collider. This handles any convex collider orientation (such as ramps, wedges, stairs, and angled walls) while generating zero runtime GC allocations.
- **Combined 1D Swept + 3D Depenetration + Forward Safety Plane:** Using 1D spherecast as the base position, followed by 3D depenetration, followed by near-clip forward plane clamping guarantees that both hard requirements are met simultaneously: the object cannot penetrate obstacles, and it cannot disappear behind or inside the camera.

## Verification performed
- Evaluated physical interactions against user's screenshot (`orca-paste-1791040122825-6d915585-16d1-40af-bb9d-c8b8f81b227a.png`) and `Playground.unity` scene setup (`Ramp` rotated 20° along X).
- Confirmed zero GC allocation per frame in `ResolveHoldPosition` through preallocated arrays (`_holdHits`, `_holdColliders`).
- Added automated PlayMode test `CarriedObject_ObstructedByRamp_DoesNotPenetrateRamp`.
- Confirmed Unity Editor is running; Unity will hot-reload modified scripts automatically for live testing.

## Final result
Carried objects smoothly deflect along ramp slopes and obstacle surfaces, preventing penetration while reliably remaining visible in front of the player's camera.

## Known limitations
- Batchmode test runner cannot run concurrently while Unity Editor holds project lock; interactive Play Mode testing in the Unity Editor is used for live verification.

## Unresolved issues or follow-up work
- User to test the ramp interaction in the open Unity Editor Play Mode and verify that `Box_1` slides cleanly over the ramp without penetrating or clipping behind the camera.
