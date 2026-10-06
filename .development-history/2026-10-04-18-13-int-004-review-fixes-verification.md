# INT-004 Review Fixes & Box_Heavy Playtest Verification

## Task summary
Addressed Team Lead review feedback on PR #78 ([INT-004] Throw carried object). Fixed Must Fix items 1-3 and Should Fix 4:
1. Removed `docs/superpowers` directory from the repository.
2. Created `Assets/Game/Settings/PlayerThrow.inputactionreference.asset` referencing action `Player/Attack`.
3. Assigned `_throwAction` to `PlayerThrow` in `Assets/Game/Prefabs/Player/Player.prefab` without altering `_isLocalPlayer`.
4. Removed `Mouse.current` fallback from `PlayerGrab.Update` and stripped all `Enable()` calls from production code (`OnEnable` and `Update`). Tests now explicitly enable `playerMap` in `SetUp()`. Added test verifying no throw occurs when `ThrowAction` is null.
5. Added dedicated 25 kg test object `Box_Heavy` (cube 0.5 m, Rigidbody mass 25, `GrabbableObject`) in `Assets/Game/Scenes/Playground.unity` next to `Box_1`.
6. Verified automated test suite: 70/70 PlayMode tests passed (0 failed, 0 skipped).
7. Verified interactive playtest in `Playground.unity`: `Box_1` launches far, `Box_Heavy` plops down in front of player, running throw inherits player momentum, cursor re-lock guard functions as intended, and console log is clean. Recorded physics feel notes for future discussion in #46.

## Relevant previous context
- PR #78 implemented the throw mechanics for carried objects.
- Team Lead review requested Must Fix 1 (delete `docs/superpowers/`), Must Fix 2 (add reproducible `Box_Heavy` 25 kg object to `Playground.unity` instead of referencing un-grabbable `Box_8` or `CementBag`), Must Fix 3 (wire `PlayerThrow` asset to `Player.prefab` and remove hardcoded `Mouse.current.leftButton` fallback), and Should Fix 4 (remove production `Enable()` calls in favor of test setup `playerMap.Enable()`).
- Team Lead also recommended keeping current impulse mechanics for INT-004 while recording notes on the feel of throwing 25 kg objects for subsequent discussion in MAT-003 (#46).

## Changes made
- `docs/superpowers/`: Removed entire directory and plan via `git rm -r`.
- `Assets/Game/Settings/PlayerThrow.inputactionreference.asset` (+ `.meta`): Created Input Action Reference targeting `InputSystem_Actions.inputactions` action `Attack` (ID `6c2ab1b8-8984-453a-af3d-a3c78ae1679a`).
- `Assets/Game/Prefabs/Player/Player.prefab`: Assigned `_throwAction` to `PlayerThrow`. Verified `_isLocalPlayer` remained `0`.
- `Assets/Game/Scenes/Playground.unity`: Added `Box_Heavy` test object (0.5 m cube, Rigidbody mass 25 kg, `GrabbableObject`) near `Box_1`.
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`:
  - Removed `OnEnable()` and all calls to `action.Enable()`.
  - Removed fallback to `Mouse.current.leftButton`. Throw input is strictly processed through `_throwAction.action.WasPressedThisFrame()`.
  - Preserved cursor re-lock guard logic (`canProcessThrow = _playerMovement.IsCursorLocked && !_playerMovement.CursorRelockedThisFrame`).
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`:
  - Added `_throwAction` (Attack) and `_throwReference` assigned to `_playerGrab.ThrowAction`.
  - Enabled `playerMap` in `SetUp()`.
  - Cleaned up references in `TearDown()`.
  - Added `Update_WithoutThrowAction_DoesNotProcessThrowInput` test verifying no throw occurs when `ThrowAction` is null.

## Files affected
- `docs/superpowers/plans/2026-10-04-int-004-throw-carried-object.md` (deleted)
- `Assets/Game/Settings/PlayerThrow.inputactionreference.asset`
- `Assets/Game/Settings/PlayerThrow.inputactionreference.asset.meta`
- `Assets/Game/Prefabs/Player/Player.prefab`
- `Assets/Game/Scenes/Playground.unity`
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-04-18-13-int-004-review-fixes-verification.md`

## Technical decisions
1. **Strict Input Action Binding:** Dropped hardcoded mouse fallback completely in production code to conform with project input decisions ("Binding input minimum M1"). Input is exclusively consumed through `_throwAction`.
2. **Standardized Action Map Lifecycle:** Production components no longer manage action enable states per-component, adhering to Unity Input System project-wide active action standards. Test fixture manages its own mock action map activation (`playerMap.Enable()`).
3. **Dedicated 25 kg Verification Object:** Created `Box_Heavy` in `Playground.unity` with identical dimensions to `Box_1` (0.5 m) and 25 kg mass to provide an easily reproducible test bed for the 1 kg vs 25 kg impulse ratio without modifying out-of-scope assets (`Box_8` or `CementBag.prefab`).

## Verification performed
- **Unity 6000.3.25f1 Batchmode PlayMode Test Suite:**
  - `Ngecor.Interaction.Tests.dll`: **40 passed, 0 failed** (includes new `Update_WithoutThrowAction_DoesNotProcessThrowInput` test).
  - `Ngecor.Material.Tests.dll`: **12 passed, 0 failed**.
  - `Ngecor.Player.Tests.dll`: **18 passed, 0 failed**.
  - Total: **70 passed, 0 failed, 0 skipped (100% green)**.
- **Interactive PlayMode Playtest in `Playground.unity`:**
  - `Box_1` (1 kg): launches far and responsively along the camera look vector.
  - `Box_Heavy` (25 kg): drops immediately in front of the player due to mass-scaled impulse ($\Delta v = 0.4\text{ m/s}$), providing clear tactile feel of heavyweight cargo.
  - Moving throw: object properly inherits player horizontal running velocity when released.
  - Cursor re-lock protection: verified via automated test and in-game behavior (clicking to focus/lock cursor after Escape does not trigger an accidental throw).
  - Console: verified clean, 0 warnings, 0 errors.

## Final result
All Must Fix (1-3) and Should Fix (4) requirements from the Team Lead review are resolved and verified. The PR is fully reproducible from committed scene assets.

## Known limitations
- Throwing a 25 kg object with 10 N·s impulse causes it to drop almost vertically ($\Delta v \approx 0.4\text{ m/s}$). While meeting the INT-004 requirement that 25 kg lands substantially shorter than 1 kg, throwing heavy items into cargo targets (e.g. wheelbarrow) may feel too weak. This feel observation is logged for discussion in MAT-003 (#46).

## Unresolved issues or follow-up work
- Discussion on impulse model tuning vs human-force limits in issue #46 (MAT-003).
- Carried object collision guidelines under discussion in issue #76.
