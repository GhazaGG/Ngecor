# INT-005 P2 Separation Queue Full Capacity & OnDisable Separation Fixes

## Task summary
Resolved two [P2] review findings from Team Lead on PR #89 (head `22e0d46`):
1. Handled full capacity in `PlayerGrab` separation queue dynamically by resizing array on demand, ensuring dropped objects overlapping player capsule are never discarded from separation tracking and collision is reliably restored once separated.
2. Maintained the no-launch physical contract when `PlayerGrab` is disabled while carrying/dropping an overlapping object by delegating separation tracking to a non-allocating coroutine on the active grabbable target, preventing premature collision restoration while overlapping and restoring collision once separated.
3. Added automated regression tests for both scenarios, verified 119/119 PlayMode tests passing, and updated PR description and review comments.

## Relevant previous context
- Team Lead identified two edge cases in head `22e0d46`:
  - `_separatingQueue` had fixed size 8; if more than 8 objects were dropped overlapping simultaneously, excess objects were not queued and collision remained ignored indefinitely.
  - `OnDisable` in `PlayerGrab` called `SetPlayerCollisionIgnored(..., false)` across all queued entries immediately, even if colliders were still deeply penetrating the player capsule. This prematurely re-enabled collision before separation, risking explosive PhysX launch impulses when the component was disabled.

## Changes made
1. **Dynamic Separation Queue Resizing & Re-entry Safety (`PlayerGrab.cs`):**
   - Removed `readonly` from `_separatingQueue` to allow runtime resizing.
   - In `DetachObject()`, checked if target is already present in `_separatingQueue` to prevent duplicates.
   - When queue capacity is reached (`_separatingCount >= _separatingQueue.Length`), dynamically doubled queue capacity (`System.Array.Resize(ref _separatingQueue, Mathf.Max(8, _separatingQueue.Length * 2))`).
   - In `AttachObject()`, removed `target` from `_separatingQueue` if it was previously undergoing separation tracking when re-grabbed.
2. **Safe Separation Delegation on Disable (`PlayerGrab.cs`):**
   - Extracted shared zero-alloc static `CheckOverlapping(playerColliders, targetColliders)` and `SetCollisionIgnored(playerColliders, targetColliders, ignore)` methods to eliminate duplicate logic.
   - Added `MonitorSeparationRoutine(Collider[] playerColliders, GrabbableObject target)`: waits via `WaitForFixedUpdate` until colliders are no longer overlapping, then cleanly restores collision via `SetCollisionIgnored(..., false)`. Safely aborts if target is destroyed or picked up again (`target.IsHeld`).
   - In `OnDisable()`: for all queued entries, if still overlapping, delegated tracking to `target.StartCoroutine(MonitorSeparationRoutine(...))` instead of forcing premature collision restoration. Only entries already separated have collision restored immediately.
   - In `DetachObject()`: if `PlayerGrab.enabled` is false, immediately handed off overlapping dropped targets to `MonitorSeparationRoutine` on the active target GameObject.
3. **Automated Regression PlayMode Tests (`PlayerGrabTests.cs`):**
   - `SeparationTracking_ExceedingInitialQueueCapacity_MonitorsAllAndRestoresCollisionOnceSeparated`: Drops 10 objects overlapping player capsule (exceeding initial capacity 8), verifies ignore-collision is active for all 10, moves player away, and verifies collision is restored for all 10 objects once separated.
   - `OnDisable_WhenCarriedObjectOverlapsPlayer_PreservesIgnoreUntilSeparatedAndPreventsLaunch`: Positions held object overlapping capsule, disables `PlayerGrab`, verifies object is released, verifies collision remains ignored while overlapping, verifies linear velocity remains low (< 2 m/s, no launch), moves player away, and verifies collision is restored once separated.

## Files affected
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-07-10-27-int-005-p2-separation-queue-and-disable-fix.md`

## Technical decisions
1. **Dynamic Array Resizing vs List:** Kept array representation (`_separatingQueue`) with dynamic doubling (`Array.Resize`) rather than `List<SeparatingEntry>` to maintain zero heap allocations during standard gameplay fixed update loops while gracefully scaling to arbitrary capacity under stress.
2. **Target Coroutine Delegation for Disabled Player:** Since `FixedUpdate` stops on disabled `MonoBehaviour` instances, delegating separation monitoring to the active dropped `GrabbableObject` ensures separation checking continues natively until clear, preserving the no-launch contract without requiring artificial managers.

## Verification performed
- Full batchmode PlayMode test suite executed:
  - `Ngecor.Interaction.Tests.dll`: 53/53 passed (including 2 new regression tests)
  - `Ngecor.Material.Tests.dll`: 33/33 passed
  - `Ngecor.Player.Tests.dll`: 33/33 passed
  - Total: 119/119 passed (100% green, 0 failed, 0 skipped, 0 inconclusive).
- Verified zero console errors or warnings.

## Final result
Both [P2] review findings are resolved with full regression coverage. Separation tracking handles unlimited overlapping drops without leaking ignored collisions, and `OnDisable` preserves the no-launch contract under deep penetration.

## Known limitations
None.

## Unresolved issues or follow-up work
None on this branch. Awaiting Team Lead approval.
