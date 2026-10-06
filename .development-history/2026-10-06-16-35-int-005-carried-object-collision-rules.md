# INT-005 Carried Object Collision Behavior & Mass Penalties

## Task summary
Resolved GitHub Issue #76 ([INT-005] Carried object collision behavior). Established unified physics rules and collision contracts for carried objects, replaced disruptive artificial depenetration on drop with a zero-allocation ignore-collision-until-separated mechanism, applied mass-scaled speed penalties to `PlayerMovement`, prioritized world obstacle clearance over camera viewport near-clip, implemented pinch auto-drop, filtered non-trigger compound colliders, and eliminated explosive kinematic impulses on dynamic props.

## Relevant previous context
- Issue #76 highlighted five critical problems:
  1. Carried objects clipping through walls when moving close due to artificial camera near-clip enforcement.
  2. `DepenetrateOnDrop` causing jarring snaps, side teleportation, and physics glitches.
  3. Carried mass having zero impact on player movement speed despite physics-heavy cargo game design.
  4. Compound colliders or irregular shapes clipping through obstacles.
  5. Carried kinematic objects imparting infinite impulses into dynamic props.
- Documented in `docs/DECISIONS.md` under section `2026-10-06 — Aturan tabrakan objek yang dibawa player (INT-005)`.

## Changes made
1. **Architectural & Design Decisions:**
   - Added `2026-10-06 — Aturan tabrakan objek yang dibawa player (INT-005)` to `docs/DECISIONS.md` defining mass penalty factor, world priority over viewport, pinch auto-drop threshold, ignore-collision-until-separated drop mechanics, compound collider bounds calculation, and kinematic interaction constraints.
2. **Player Movement Mass Penalties:**
   - Modified `Assets/Game/Scripts/Player/PlayerMovement.cs` to expose `CarriedMass` with linear scaling `_massSpeedPenaltyFactor = 0.012f` and minimum multiplier clamp `_minMassSpeedMultiplier = 0.6f`.
   - Updated `Assets/Game/Scripts/Interaction/PlayerGrab.cs` (`AttachObject` and `DetachObject`) to synchronize `_playerMovement.CarriedMass` on grab and drop/throw.
3. **World Priority & Pinch Auto-Drop:**
   - In `PlayerGrab.ResolveHoldPosition`, bounded sweep radius `Mathf.Clamp(_carriedRadius, 0.05f, 0.25f)` to avoid false floor/ramp overlaps.
   - Checked obstacle surface normals (`Vector3.Dot(normal, camera.forward) < -0.2f`) for true forward wall obstructions when initial sweep overlap occurs.
   - Clamped forward distance to avoid falling behind camera only when no forward wall obstructs (`nearest >= minDistance`).
   - Implemented soft auto-drop (`isPinched = forwardDist < 0.15f`) when an obstacle presses a carried object into the player capsule.
4. **Ignore-Collision-Until-Separated Drop Mechanics:**
   - Deleted the 124-line heuristic `DepenetrateOnDrop` in `PlayerGrab.cs`.
   - Added zero-GC allocation `_separatingQueue` struct array tracking up to 8 separating objects.
   - In `FixedUpdate`, maintained `Physics.IgnoreCollision(player, carried, true)` while overlapping capsule using non-allocating `Physics.ComputePenetration`, cleanly restoring collision once clear.
5. **Compound Colliders & Dynamic Prop Impulse Handling:**
   - Filtered non-trigger enabled colliders (`!col.isTrigger && col.enabled`) when calculating `_carriedRadius` in `AttachObject`.
   - Cleared linear and angular velocity before setting `isKinematic = true` on grab.
   - Preserved low/zero kinematic impulses when held objects touch dynamic rigidbodies.
6. **Testing:**
   - Expanded `Assets/Game/Tests/Player/PlayerMovementTests.cs` and `Assets/Game/Tests/Interaction/PlayerGrabTests.cs` with TDD tests covering mass speed penalties, wall priority over viewport, pinch auto-drop, separation tracking lifecycle, compound collider trigger filtering, and dynamic prop collision impulses.

## Files affected
- `docs/DECISIONS.md`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-06-16-35-int-005-carried-object-collision-rules.md`

## Technical decisions
1. **Zero-Allocation Separation Queue:** Used a fixed array `_separatingQueue` of size 8 with zero heap allocations during gameplay frames, satisfying project performance budget constraints.
2. **Surface Normal Differentiation:** Distinguished between forward blocking walls and ground/ramp surfaces using `Vector3.Dot(hit.normal, camera.forward) < -0.2f` during zero-distance sweep conditions.
3. **Host-Authoritative Intent Separation:** Retained `RequestGrab`/`ExecuteGrab` and `RequestDrop`/`ExecuteDrop` boundaries to prepare for upcoming `NET-002`/`NET-003` tickets.

## Verification performed
- Verified PlayMode test suites:
  - `PlayerMovementTests`: all mass penalty speed reduction tests passed.
  - `PlayerGrabTests`: separation tracking tests, wall obstruction, ramp clearance, compound collider trigger exclusion, and dynamic prop impulse tests passed.
- Verified absence of GC allocations in `FixedUpdate` and `LateUpdate`.

## Final result
All requirements for issue #76 [INT-005] are fulfilled. Carried objects behave physically without wall clipping, viewport glitches, or launch impulses on release.

## Known limitations
- If more than 8 objects overlap the player simultaneously upon release in a single frame, additional objects beyond the queue capacity of 8 immediately restore collision. In practice, a single player can only carry and drop 1 object at a time.
