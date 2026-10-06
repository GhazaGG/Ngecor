# INT-004 Throw Carried Object

## Task summary
Implemented and verified issue #12 ([INT-004] Throw carried object). Player can throw carried physics objects in the camera look direction using a tunable impulse force. Heavy objects (such as the 25 kg cement bag) naturally travel much shorter than 1 kg light objects due to physical impulse mechanics ($v = J / m$). Left-mouse button input is protected so that clicking to re-lock the cursor after Escape never triggers an unintentional throw.

## Relevant previous context
- INT-002 (#10) and INT-003 (#11, PR #75) established the grab, hold follow, depenetration, and drop lifecycle in `PlayerGrab` and `GrabbableObject`.
- Issue #12 requested tunable throw force, consistent look-direction reference, natural mass-scaled distance (25 kg cement bag landing far shorter than 1 kg object), physics and collision restoration, empty-hand safety, and stable feel.
- Team Lead review note on PR #50 (PLAYER-002) added a critical acceptance criterion: Left Mouse Button is used to re-lock the cursor, and clicking to re-lock must never trigger a throw; throw input is only recognized when the cursor was already locked before that click.
- `docs/DECISIONS.md` prescribes first-person camera, M1 minimum input binding (Throw = LMB / Action `Attack`), separation of intent and execution (`RequestThrow` and `ExecuteThrow`), no networking code outside NET tickets, and pure-logic automated test coverage.

## Changes made
- `Assets/Game/Scripts/Player/PlayerMovement.cs`:
  - Exposed `public bool IsCursorLocked => _cursorLocked;` to allow query of the current cursor lock state.
  - Added `public bool CursorRelockedThisFrame { get; private set; }` reset to `false` at the start of `Update()`.
  - In `HandleCursorInput()`, when the cursor is unlocked and left-click is pressed, set `CursorRelockedThisFrame = true` along with `SetCursorLocked(true)`.
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`:
  - Added `[SerializeField] private InputActionReference _throwAction;` and `public InputActionReference ThrowAction { get; set; }`.
  - Added `[SerializeField, Min(0f)] private float _throwForce = 10f;` and `public float ThrowForce { get; set; }`.
  - Enabled `_throwAction.action` in `OnEnable()`.
  - Implemented `public bool RequestThrow()` returning `ExecuteThrow()` (preserving intent/execution boundary for future multiplayer host routing).
  - Implemented `public bool ExecuteThrow()`:
    - Early exits if not carrying (`!IsCarrying`).
    - Obtains camera look direction from `_playerMovement.LocalCamera` (or player forward as fallback).
    - Calls `DetachObject()` to restore kinematic/gravity flags, re-enable player collision, perform depenetration, and inherit horizontal player movement velocity.
    - Directly calculates and adds the physical impulse velocity change: $\Delta \vec{v} = \vec{d}_{\text{throw}} \cdot \frac{F_{\text{throw}}}{\max(0.0001, m)}$.
  - In `Update()`, added throw input handling with the cursor re-lock guard:
    - Evaluates `bool canProcessThrow = _playerMovement.IsCursorLocked && !_playerMovement.CursorRelockedThisFrame;`.
    - Reads `_throwAction.action.WasPressedThisFrame()` or falls back to `Mouse.current.leftButton.wasPressedThisFrame`.
    - Triggers `RequestThrow()` only when carrying and `canProcessThrow` is true.
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`:
  - Added `CursorRelock_SetsCursorRelockedThisFrame_ForSingleFrame` PlayMode test checking lock state and single-frame relock flag lifetime.
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`:
  - Added `ExecuteThrow_WithCarriedObject_AppliesImpulseInLookDirection`: verifies look-direction impulse application, physics restoration (`isKinematic == false`, `useGravity == true`), and forward velocity $\approx 10$ m/s for 1 kg object.
  - Added `ExecuteThrow_HeavyObjectVsLightObject_HeavyObjectTravelsFarShorter`: verifies that with identical throw force, a 25 kg object receives $\approx 1/25$ the velocity of a 1 kg object ($v_{\text{light}} > 15 \times v_{\text{heavy}}$).
  - Added `ExecuteThrow_WithEmptyHands_ReturnsFalseWithoutErrors`: verifies safety and idempotency when hands are empty.
  - Added `ExecuteThrow_WhileMoving_InheritsPlayerHorizontalVelocity`: verifies horizontal player movement velocity inheritance combined with forward throw impulse.
  - Added `ThrowInput_WhenCursorAlreadyLocked_TriggersThrow`: verifies that pressing LMB while carrying and cursor locked triggers throw.
  - Added `ThrowInput_WhenClickReLocksCursor_DoesNotTriggerThrow`: verifies that pressing LMB to re-lock cursor does not throw the carried object, and only a subsequent click after locking throws it.

## Files affected
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-04-02-22-int-004-throw-carried-object.md`

## Technical decisions
1. **Physical Impulse Mechanics ($v = J / m$):** Rather than artificial mass branching, impulse velocity change is computed as $\Delta \vec{v} = \vec{d} \cdot \frac{F_{\text{throw}}}{m}$. For a 1 kg object with 10 N·s force, $\Delta v = 10$ m/s; for a 25 kg cement bag, $\Delta v = 0.4$ m/s. This directly fulfills AC 4 via emergent physics.
2. **Direct Linear Velocity Integration:** Applying the impulse directly to `rb.linearVelocity` (on top of the horizontal velocity inherited during `DetachObject()`) ensures immediate velocity integration, avoiding PhysX accumulator lag prior to fixed update while guaranteeing identical mechanics to `ForceMode.Impulse`.
3. **Cursor Relock Frame Guard:** To satisfy the TL PR #50 review requirement without coupling input action maps, `PlayerMovement` flags `CursorRelockedThisFrame = true` during the exact frame a left-click re-locks the cursor. `PlayerGrab` suppresses throw detection when `CursorRelockedThisFrame` is true or when the cursor is unlocked.
4. **Fallback Mouse Binding:** If `_throwAction` is not assigned in the Inspector/prefab, `PlayerGrab` cleanly falls back to checking `Mouse.current.leftButton.wasPressedThisFrame`, ensuring out-of-the-box operation without requiring text edits to serialized `.asset` / `.prefab` files.

## Verification performed
- **Unity 6000.3.25f1 Batchmode PlayMode Test Suite:**
  - `Ngecor.Interaction.Tests.dll`: **39 passed, 0 failed** (includes all 6 new throw tests and 33 existing interaction tests).
  - `Ngecor.Material.Tests.dll`: **12 passed, 0 failed**.
  - `Ngecor.Player.Tests.dll`: **18 passed, 0 failed** (includes new cursor relock test and 17 existing player tests).
  - Total: **69 tests passed, 0 failed, 0 skipped (100% green)**.
- **TDD Workflow:** Watched tests fail first (CS1061 missing members, assertion failures on relock and velocity) before minimal implementation, and then watched all tests pass.
- Verified working tree: no serialized `.asset`, `.unity`, or `.meta` files were modified via text editor.

## Final result
All acceptance criteria for issue #12 and the TL review note are met. Player can throw carried objects with consistent look reference and tunable force, 25 kg objects behave realistically heavy, empty hands are safe, and cursor re-locking via left click is guarded against accidental throwing. Full PlayMode test suite is 100% passing (69/69).

## Known limitations
- Interactive manual feel check in Editor GUI (throwing `Box_1` vs `Box_8` into targets in `Playground.unity`) was not performed because tests were executed via batchmode.
- Network replication of throw is out of scope for M1 (intent/execution methods are separated ready for NET-002/003).

## Unresolved issues or follow-up work
- Developer to open `Playground.unity` in the Unity Editor to inspect feel and optionally assign `_throwAction` reference in the `Player.prefab` inspector.
- Coordinate with Issue #76 ([INT-005]) if carried-object collision rules require any subsequent updates to the shared release pipeline.
