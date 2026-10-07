# INT-005 P3 Separation Coroutine Per-Tick GC Allocation Fix

## Task summary
Resolved [P3] review finding from Team Lead on PR #89 (head `7131585`):
- Eliminated heap allocation in `MonitorSeparationRoutine` (`PlayerGrab.cs:554-566`) by instantiating `WaitForFixedUpdate` once before the `while (CheckOverlapping(...))` loop and yielding the cached instance on each physics tick.
- Verified zero regressions across the full PlayMode test suite (120/120 tests passed).

## Relevant previous context
- Head `7131585` had `yield return new WaitForFixedUpdate();` inside the while loop of `MonitorSeparationRoutine`.
- While an object is held overlapping a disabled player, the coroutine ran every physics tick, creating a new `WaitForFixedUpdate` instance every tick and generating avoidable GC allocations contrary to the zero-allocation carry/drop requirement in issue #76.

## Changes made
- In `PlayerGrab.cs:MonitorSeparationRoutine`, created `var waitForFixedUpdate = new WaitForFixedUpdate();` prior to the loop and yielded `waitForFixedUpdate` inside the loop.

## Files affected
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `.development-history/2026-10-07-18-56-int-005-p3-separation-coroutine-alloc-fix.md`

## Technical decisions
- Cached a single `WaitForFixedUpdate` local instance per separation routine instead of allocating per fixed update tick, satisfying the zero-alloc requirement without introducing unnecessary abstraction.

## Verification performed
- Full batchmode PlayMode test suite executed:
  - `Ngecor.Interaction.Tests.dll`: 54/54 passed
  - `Ngecor.Material.Tests.dll`: 33/33 passed
  - `Ngecor.Player.Tests.dll`: 33/33 passed
  - Total: 120/120 passed (100% green, 0 failed, 0 skipped, 0 inconclusive).
- Verified `git diff --check origin/main...HEAD` is clean.

## Final result
All P2 and P3 review items from Team Lead are resolved. The separation coroutine runs with zero per-tick allocations.

## Known limitations
None.

## Unresolved issues or follow-up work
Awaiting gameplay reviewer testing in Unity before final merge approval.
