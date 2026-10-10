# Development History: Disable Project-Wide Actions in Input Test Fixtures

## Task Summary
Resolved the failure in PlayMode Test Runner (`TestResults_20261010_175129.xml`):
`Ngecor.Player.Tests.PlayerMovementTests.CarriedMass_ClampedAtMaximumPenalty` failed with `Unhandled log message: '[Exception] ArgumentNullException: Value cannot be null. Parameter name: statePtr'` at `InputControlExtensions.CheckStateIsAtDefault` invoked via `InputActionState.OnBeforeInitialUpdate()`.

## Relevant Previous Context
- In commit `f54d1e4`, local actions `Move` and `Look` in `PlayerMovementTests.CreatePlayer()` were set to `PassThrough`.
- However, the error still occurred on the very first update tick of `CarriedMass_ClampedAtMaximumPenalty`.

## Deep Root Cause Analysis
1. In `InputTestFixture.Setup()`, `m_StateManager.SaveAndReset(...)` calls `InputTestStateManager.Reset(...)`.
2. Inside `InputTestStateManager.Reset(...)`, line 99 calls `InputSystem.TestHook_EnableActions()`.
3. `TestHook_EnableActions()` calls `EnableActions()`, which calls `InputSystem.actions.Enable()`.
4. `InputSystem.actions` points to the project-wide action asset (`Assets/Game/Settings/InputSystem_Actions.inputactions`), which contains `Move` and `Look` with `"type": "Value"` and `"initialStateCheck": true`.
5. When `InputSystem.actions.Enable()` runs during `base.Setup()`, it enables the project-wide action asset.
6. When the test method starts and calls `InputSystem.AddDevice<Keyboard>()`, `InputSystem.actions` binds to the newly added virtual devices and schedules an initial state check (`initialStateCheckPending = true`).
7. On the first frame update tick, `InputActionState.OnBeforeInitialUpdate()` runs on `InputSystem.actions`.
8. Because the virtual device was newly added and has not completed a native update cycle, `control.currentStatePtr` is null, causing `CheckStateIsAtDefault` to throw `ArgumentNullException(statePtr)`.
9. The tests in `PlayerMovementTests` and `PlayerGrabTests` do NOT use `InputSystem.actions`; they construct their own isolated in-memory action assets. However, `InputSystem.actions` was running concurrently in the background because `InputTestFixture` automatically enabled it.

## Changes Made
1. **`Assets/Game/Tests/Player/PlayerMovementTests.cs`:**
   - Overrode `Setup()` to invoke `base.Setup();` followed immediately by `InputSystem.actions?.Disable();`. This unhooks the project-wide actions from `onBeforeUpdate` and disables their initial state checks during test execution.
2. **`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`:**
   - Added `InputSystem.actions?.Disable();` in `Setup()` right after `base.Setup();` for consistency and hermetic isolation.

## Files Affected
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-10-17-56-disable-project-wide-actions-in-test-fixtures.md`

## Technical Decisions
- **Disable project-wide actions in fixture setup:** Disabling `InputSystem.actions` immediately after `base.Setup()` prevents the project-wide action asset from listening to test devices or scheduling initial state checks, completely eliminating the null pointer dereference while allowing tests to safely use their own isolated action maps.

## Verification Performed
- Built all test and runtime assemblies:
  - `Ngecor.Player.Tests.csproj`: 0 errors.
  - `Ngecor.Interaction.Tests.csproj`: 0 errors.
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors.
  - `Ngecor.Material.Tests.csproj`: 0 errors.
- Verified diff across all modified files.

## Final Result & Follow-Up
- All assemblies compile cleanly.
- Changes committed and pushed to `feat/net-002-sync-player-movement-interaction`.
- Ready for developer to re-run PlayMode Test Runner in Unity Editor.
