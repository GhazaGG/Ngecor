# INT-005 P2 Multi-Player Re-Grab Collision Separation Fix

## Task summary
Resolved [P2] review finding from Team Lead on PR #89 (head `0948585`):
1. Fixed collision monitoring in `PlayerGrab` separation routines when a second player re-grabs an object that was dropped while overlapping player A (especially after player A is disabled):
   - In `MonitorSeparationRoutine`, track `playerOwner` (`GameObject`), check if `target.IsHeld && target.CurrentHolder == playerOwner` before aborting. If another player holds the target, separation monitoring continues rather than aborting prematurely.
   - Once the object separates from player A's colliders, restore collision between player A and the object (`SetCollisionIgnored(playerColliders, targetColliders, false)`).
   - In `FixedUpdate`, ensure queue monitoring continues when another player holds the object, while only removing if the local player re-grabbed it.
2. Added an automated two-player PlayMode test verifying that when player A disables `PlayerGrab` while overlapping an object, and player B grabs it and moves away, collision between player A and the object is reliably restored once separated.
3. Executed the full batchmode PlayMode test suite: 120/120 passed (100% green).
4. Updated PR #89 description and submitted review comment response.

## Relevant previous context
- Head `0948585` introduced `MonitorSeparationRoutine` on the active target `GrabbableObject` to handle separation when `PlayerGrab` is disabled.
- Team Lead identified an edge case where if player B re-grabs the object while it still overlaps disabled player A, `target.IsHeld` was causing `MonitorSeparationRoutine` to `yield break` immediately without ever un-ignoring collision for player A.
- Because player B's `PlayerGrab` only manages collision pairs with player B, player A's collision remained permanently ignored with the object even after player B moved away.

## Changes made
1. **Multi-Player Holder-Aware Separation Coroutine (`PlayerGrab.cs`):**
   - Updated `MonitorSeparationRoutine(GameObject playerOwner, Collider[] playerColliders, GrabbableObject target)` to track the original player owner.
   - Inside the while-loop, only abort if `playerOwner != null && target.IsHeld && target.CurrentHolder == playerOwner`. If another player holds the object, keep monitoring separation across fixed updates until colliders no longer overlap.
   - After the loop, restore collision via `SetCollisionIgnored(playerColliders, targetColliders, false)` if `playerOwner` is not currently holding the object.
   - Passed `gameObject` as `playerOwner` from `OnDisable()` and `DetachObject()`.
2. **Local vs Remote Grab Guard in `FixedUpdate` (`PlayerGrab.cs`):**
   - In `FixedUpdate()`, added a guard: if `entry.target.IsHeld && entry.target.CurrentHolder == gameObject`, remove it from `_separatingQueue` without un-ignoring (since local player is actively carrying it). If another player holds it, separation monitoring continues until physically clear.
3. **Automated PlayMode Regression Test (`PlayerGrabTests.cs`):**
   - Added `TwoPlayers_WhenPlayerADisablesWhileOverlapping_AndPlayerBGrabsAndMovesAway_PlayerACollisionRestored`:
     - Creates Player A and Player B.
     - Player A grabs object, positions it deeply overlapping Player A's capsule.
     - Disables Player A (`_playerGrab.enabled = false`); verifies object dropped and collision remains ignored while overlapping.
     - Player B executes grab while still overlapping Player A; verifies Player B is carrying object.
     - Player B moves 10m away; simulates fixed update.
     - Verifies Player A's collision with the object is restored (`Physics.GetIgnoreCollision == false`).
     - Verifies Player B maintains ignored collision while carrying the object (`Physics.GetIgnoreCollision == true`).

## Files affected
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-07-18-01-int-005-p2-multiplayer-regrab-collision-fix.md`

## Technical decisions
1. **Holder-Aware Separation vs Blind IsHeld Check:** Instead of treating any `target.IsHeld` as a signal to abort separation monitoring, `GrabbableObject.CurrentHolder` is compared against `playerOwner`. If another player holds the object, the original player's colliders must still be monitored for separation so that collision is restored when the other player walks away with the prop.
2. **Preserving No-Launch Contract During Multi-Player Handoff:** By continuing separation tracking across fixed updates while held by player B, PhysX penetration impulses between player A and the held prop are prevented during the handoff until physical separation occurs.

## Verification performed
- Full batchmode PlayMode test suite executed:
  - `Ngecor.Interaction.Tests.dll`: 54/54 passed (including the new two-player regression test)
  - `Ngecor.Material.Tests.dll`: 33/33 passed
  - `Ngecor.Player.Tests.dll`: 33/33 passed
  - Total: 120/120 passed (100% green, 0 failed, 0 skipped, 0 inconclusive).
- Verified `git diff --check origin/main...HEAD` is clean.

## Final result
The multi-player re-grab edge case is resolved. Collision between player A and the dropped object is reliably restored once separated even when re-grabbed by another player, and the full suite passes with 120/120 green tests.

## Known limitations
None.

## Unresolved issues or follow-up work
None on this branch. Awaiting Team Lead approval.
