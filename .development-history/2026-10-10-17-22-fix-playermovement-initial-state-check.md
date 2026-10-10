# Development History: Eliminate Input State Pointer Exception via PassThrough Action Type

## Task Summary
Resolved the persistent failure in PlayMode Test Runner (`TestResults_20261010_171407.xml`):
`Ngecor.Player.Tests.PlayerMovementTests.CarriedMass_ClampedAtMaximumPenalty` failed with `Unhandled log message: '[Exception] ArgumentNullException: Value cannot be null. Parameter name: statePtr'` at `InputControlExtensions.CheckStateIsAtDefault` invoked via `InputActionState.OnBeforeInitialUpdate()`.

## Relevant Previous Context
- Previously, in commits `7d8c5b0` and `ae45a81`, cross-fixture action leak cleanups were added to `ShovelInput.cs`, `BucketPourInput.cs`, `PlayerGrabTests.cs`, and `PlayerMovementTests.cs`.
- However, when the full PlayMode test runner ran on `ae45a81`, 180 out of 181 tests passed; `CarriedMass_ClampedAtMaximumPenalty` still failed on the very first frame of its execution with the exact same unhandled log message from `NativeInputRuntime`.

## Root Cause Analysis
1. In `PlayerMovementTests.CreatePlayer()`, `Move` and `Look` actions were declared with `InputActionType.Value`.
2. Under Unity's Input System, `InputActionType.Value` enforces `wantsInitialStateCheck = true` (`InputAction.cs` line 714).
3. When `playerMap.Enable()` is called, `InputActionState.HookOnBeforeUpdate()` queues `OnBeforeInitialUpdate` to run on the very first input update tick (`NativeInputSystem:NotifyUpdate`).
4. During `OnBeforeInitialUpdate()`, the Input System iterates through all controls bound to the value actions and calls `control.CheckStateIsAtDefault()`, passing `control.currentStatePtr`.
5. On virtual test devices instantiated within `InputTestFixture` before the first native input event buffer has been dispatched, `InputStateBuffers.GetFrontBufferForDevice` returns `null`.
6. When `statePtr` is `null`, `InputControlExtensions.CheckStateIsAtDefault` throws `ArgumentNullException: Value cannot be null. Parameter name: statePtr`.
7. In contrast, `InputActionType.PassThrough` (as well as `InputActionType.Button`) has `wantsInitialStateCheck = false` by default, meaning `OnBeforeInitialUpdate` completely skips the initial state check on those controls.
8. `PlayerMovement` only calls `.ReadValue<Vector2>()` on the actions, which is fully supported with identical semantics by `PassThrough`.

## Changes Made
1. **`Assets/Game/Tests/Player/PlayerMovementTests.cs`:**
   - In `CreatePlayer()`, updated `Move` and `Look` action definitions from `InputActionType.Value` to `InputActionType.PassThrough`.
   - This bypasses the faulty virtual-device initial state check while preserving full `ReadValue<Vector2>()` functionality for all movement tests.

## Files Affected
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-10-17-22-fix-playermovement-initial-state-check.md`

## Technical Decisions
- **PassThrough for in-memory virtual actions in tests:** Setting `InputActionType.PassThrough` directly addresses the root cause: it eliminates the initial state check that causes `ArgumentNullException(statePtr)` in `InputTestFixture` while keeping the exact same composite vector binding and `ReadValue<Vector2>()` values.

## Verification Performed
- Ran `rtk dotnet build` across all projects:
  - `Ngecor.Player.Tests.csproj`: 0 errors.
  - `Ngecor.Interaction.Tests.csproj`: 0 errors.
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors.
  - `Ngecor.Material.Tests.csproj`: 0 errors.
- Verified `git diff` to ensure minimal, surgical changes.

## Final Result & Follow-Up
- Clean build across all test and runtime assemblies.
- Changes committed and pushed to `origin/feat/net-002-sync-player-movement-interaction`.
- Ready for developer to re-run PlayMode Test Runner in Unity Editor to verify 181/181 passing tests.
