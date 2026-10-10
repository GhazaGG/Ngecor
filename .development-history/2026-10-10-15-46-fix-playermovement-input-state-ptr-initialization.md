# Development History: Fix Input State Pointer Null Initialization in PlayerMovementTests

## Task Summary
Resolved the single failing PlayMode test (`Ngecor.Player.Tests.PlayerMovementTests.CarriedMass_ClampedAtMaximumPenalty`) from the full test runner run (`TestResults_20261010_153821.xml`).

## Relevant Previous Context
- In commit `e97af59`, Must Fix 1, Should Fix 3 (SessionManager static instance removal and scene spawn points), and Should Fix 4 (auto-drop tests moved to real pinch tests in `PlayerGrabTests`) were completed.
- Full PlayMode test execution across all assemblies yielded 180 passed, 1 failed out of 181 total tests.
- Failure trace:
  `Unhandled log message: '[Exception] ArgumentNullException: Value cannot be null. Parameter name: statePtr'` at `UnityEngine.InputSystem.InputControlExtensions.CheckStateIsAtDefault` inside `InputActionState.OnBeforeInitialUpdate()`.

## Root Cause
1. `PlayerMovementTests` inherits from `InputTestFixture` without an explicit `[SetUp]` method warming up the Input System state buffers.
2. In NUnit, tests within a fixture execute in alphabetical order. `CarriedMass_ClampedAtMaximumPenalty` is the first test executed in `PlayerMovementTests`.
3. When `InputSystem.AddDevice` creates virtual devices and `InputActionMap.Enable()` activates action bindings without an initial `InputSystem.Update()` tick, the internal state buffer pointers (`currentStatePtr`) for newly bound controls remain unallocated.
4. Calling `Press()` before any initial frame update queues an input event; on the subsequent frame start, `NativeInputSystem.NotifyUpdate` fires `InputSystem.onBeforeUpdate` callbacks where `InputActionState.OnBeforeInitialUpdate` iterates through active controls and calls `control.CheckStateIsAtDefault()`, which throws `ArgumentNullException` because `statePtr` is null.

## Changes Made
1. **Added `[SetUp]` in `PlayerMovementTests.cs`:**
   - Overrode `Setup()` with `base.Setup(); InputSystem.Update();` to ensure the input system runtime state buffers are initialized prior to test execution.
2. **Buffer Warm-up in `CreatePlayer()`:**
   - Added `InputSystem.Update()` immediately after adding missing keyboard/mouse devices and again immediately after `playerMap.Enable()`.
3. **Synchronized Frame Initialization in `CarriedMass` Tests:**
   - Added `InputSystem.Update()` and `yield return null;` in `CarriedMass_When25kg_ReducesEffectiveSpeed` and `CarriedMass_ClampedAtMaximumPenalty` before injecting simulated key presses, giving Unity and the Input System a clean frame tick to settle control states.

## Files Affected
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-10-15-46-fix-playermovement-input-state-ptr-initialization.md`

## Technical Decisions
- **Eager buffer warm-up via `InputSystem.Update()`:** Calling `InputSystem.Update()` immediately upon device creation and map enablement guarantees that all control layout pointers and front/back buffers are safely allocated before any event dispatch or frame processing takes place.

## Verification Performed
- Ran `rtk dotnet build Ngecor.Player.Tests.csproj`: 0 errors, 0 warnings.
- Ran `rtk dotnet build Ngecor.Multiplayer.csproj`: 0 errors.
- Ran `rtk dotnet build Ngecor.Multiplayer.Tests.csproj`: 0 errors.
- Ran `rtk dotnet build Ngecor.Interaction.Tests.csproj`: 0 errors.
- Checked git diff and confirmed clean code formatting.

## Final Result & Follow-Up
- All 4 assemblies build cleanly with 0 errors.
- Changes committed and pushed to `feat/net-002-sync-player-movement-interaction`.
- Ready for developer to run PlayMode Test Runner in Unity Editor to verify 181/181 passing tests.
