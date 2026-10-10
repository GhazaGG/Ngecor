# Development History: Fix Leaked Input Actions Across Test Assemblies

## Task Summary
Resolved the failure in PlayMode Test Runner (`TestResults_20261010_160506.xml`):
`Ngecor.Player.Tests.PlayerMovementTests.CarriedMass_ClampedAtMaximumPenalty` threw `ArgumentNullException: Value cannot be null. Parameter name: statePtr` during `InputSystem:Update()` inside `Setup()`.

## Relevant Previous Context
- Previously, an attempt was made to add `InputSystem.Update()` to `PlayerMovementTests.Setup()` to warm up buffers.
- However, when the suite ran across all assemblies, `InputSystem.Update()` failed immediately inside `Setup()` on the first test executed (`CarriedMass_ClampedAtMaximumPenalty`).
- All other 32 tests in `PlayerMovementTests` passed.

## Root Cause Analysis
1. In Unity's Input System, calling `action.Enable()` or `actionMap.Enable()` registers internal callbacks (`InputActionState.OnBeforeInitialUpdate`) to `InputSystem.onBeforeUpdate`.
2. When tests finish and destroy the action asset via `DestroyImmediate()`, the unmanaged/internal callback delegate is NOT automatically unregistered unless `action.Disable()` or `actionMap.Disable()` / `asset.Disable()` is explicitly called.
3. Multiple preceding test fixtures and components leaked active input actions:
   - `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`: Instantiated and enabled a mock `_actionAsset` in `Setup()`, but in `TearDown()` it called `Object.DestroyImmediate(_actionAsset)` without disabling it first.
   - `Assets/Game/Scripts/Material/ShovelInput.cs`: Enabled `_useAction.action` in `OnEnable()`, but lacked an `OnDisable()` implementation to disable it.
   - `Assets/Game/Scripts/Material/BucketPourInput.cs`: Enabled `_pourAction.action` in `OnEnable()`, but `OnDisable()` did not disable the action.
4. When `Ngecor.Interaction.Tests` and `Ngecor.Material.Tests` concluded, virtual input devices were torn down and removed from `InputSystem`. The leaked `InputActionState` callbacks remained active in `InputSystem.onBeforeUpdate`, now referencing controls on non-existent devices whose `currentStatePtr` was `null`.
5. Upon starting `Ngecor.Player.Tests`, the very first call to `InputSystem.Update()` (whether in `Setup()` or during the first frame of the first alphabetical test) executed `onBeforeUpdate`, invoking the leaked callbacks and throwing `ArgumentNullException(statePtr)`.

## Changes Made
1. **Disabled `_actionAsset` in `PlayerGrabTests.TearDown()`:**
   - Wrapped asset destruction with `_actionAsset.Disable()` prior to `Object.DestroyImmediate(_actionAsset)`, matching the pattern already used in `PlayerMovementTests.TearDown()`.
2. **Added Action Teardown in `ShovelInput.cs`:**
   - Added `OnDisable()` that calls `_useAction.action.Disable()` when the component is disabled or destroyed.
3. **Added Action Teardown in `BucketPourInput.cs`:**
   - Added `_pourAction.action.Disable()` inside `OnDisable()`.
4. **Cleaned up `PlayerMovementTests.cs`:**
   - Removed artificial `[SetUp] Setup()` and redundant `InputSystem.Update()` calls from `CreatePlayer()` and individual test cases.

## Files Affected
- `Assets/Game/Scripts/Material/BucketPourInput.cs`
- `Assets/Game/Scripts/Material/ShovelInput.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-10-16-13-fix-input-action-leaks-across-test-fixtures.md`

## Technical Decisions
- **Full lifecycle parity for input actions:** Following the Lazy Senior Dev (Ponytail) root-cause principle, fixed all producers of leaked action states across components and test fixtures rather than patching downstream symptoms in `PlayerMovementTests`.

## Verification Performed
- Ran `rtk dotnet build` across all 4 csproj files (`Ngecor.Player.Tests`, `Ngecor.Interaction.Tests`, `Ngecor.Material.Tests`, `Ngecor.Multiplayer.Tests`): all compiled with 0 errors.
- Verified diffs across all modified files.

## Final Result & Follow-Up
- Clean working tree ready to be committed, pushed, and verified via PlayMode Test Runner in Unity Editor.
