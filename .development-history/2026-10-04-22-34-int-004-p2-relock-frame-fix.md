# INT-004 P2 Cursor Relock Frame Guard Fix

## Task summary
Resolved Team Lead review feedback regarding bug [P2]: cursor relock flag from frame $N$ discarding subsequent throw click on frame $N+1$ when `PlayerGrab.Update` executes before `PlayerMovement.Update`.
Replaced the boolean flag in `PlayerMovement` with a frame-index comparison against `Time.frameCount`. Added automated tests verifying immediate frame expiration and both execution orders (`Movement`-first and `Grab`-first). Batchmode PlayMode suite passed 100% (73/73 tests green).

## Relevant previous context
- PR #78 addressed Must Fix 1-3 and Should Fix 4.
- Team Lead re-review identified an execution-order dependency [P2]:
  `CursorRelockedThisFrame` was a boolean variable reset at the start of `PlayerMovement.Update()`.
  If `PlayerGrab.Update()` ran before `PlayerMovement.Update()` on frame $N+1$, the flag was still `true` from frame $N$'s relock click, causing `PlayerGrab` to discard the throw input.

## Changes made
- `Assets/Game/Scripts/Player/PlayerMovement.cs`:
  - Added `private int _cursorRelockedFrame = -1;`.
  - Converted `CursorRelockedThisFrame` to a frame-check expression: `public bool CursorRelockedThisFrame => _cursorRelockedFrame == Time.frameCount;`.
  - In `HandleCursorInput()`, set `_cursorRelockedFrame = Time.frameCount;` on relock.
  - Removed manual `CursorRelockedThisFrame = false;` assignment from `Update()`.
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`:
  - Added `CursorRelock_BecomesFalseImmediatelyOnNextFrame_BeforeUpdate`: verifies `CursorRelockedThisFrame` evaluates to `false` on frame $N+1$ even before `PlayerMovement.Update()` runs.
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`:
  - Added `ThrowInput_AfterRelock_WhenMovementUpdatesBeforeGrab_ThrowsCarriedObject`: verifies that clicking to throw on frame $N+1$ throws the object when `Movement` updates before `Grab`.
  - Added `ThrowInput_AfterRelock_WhenGrabUpdatesBeforeMovement_ThrowsCarriedObject`: verifies that clicking to throw on frame $N+1$ throws the object even when `Grab` updates before `Movement`.

## Files affected
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-04-22-34-int-004-p2-relock-frame-fix.md`

## Technical decisions
1. **FrameCount Comparison over Boolean State:** By checking `_cursorRelockedFrame == Time.frameCount`, the flag automatically and synchronously expires at the boundary between frames when Unity advances `Time.frameCount`, before any MonoBehaviour's `Update()` executes. This guarantees immunity to script execution order without configuring project-wide Script Execution Order settings.
2. **Explicit Component Update Ordering in Tests:** Tests disable components prior to grab acquisition so `OnDisable()` is not invoked during carrying, and manually invoke `Update()` via reflection in specific orders to deterministically reproduce both execution sequence permutations.

## Verification performed
- **Unity 6000.3.25f1 Batchmode PlayMode Test Suite:**
  - `Ngecor.Interaction.Tests.dll`: **42 passed, 0 failed** (includes both Movement-first and Grab-first ordering tests).
  - `Ngecor.Material.Tests.dll`: **12 passed, 0 failed**.
  - `Ngecor.Player.Tests.dll`: **19 passed, 0 failed** (includes immediate-frame expiration test).
  - Total: **73 passed, 0 failed, 0 skipped (100% green)**.

## Final result
The [P2] input bug is fully resolved. Subsequent throw clicks on frame $N+1$ are guaranteed to process regardless of whether `PlayerMovement.Update()` or `PlayerGrab.Update()` runs first. All 73 automated tests pass.

## Known limitations
- None for offline M1 throw mechanics.

## Unresolved issues or follow-up work
- Await Team Lead re-review and approval on PR #78.
