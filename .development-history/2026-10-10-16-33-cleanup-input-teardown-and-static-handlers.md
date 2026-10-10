# Development History: Explicit Action Teardown and Static Handler Cleanup in Test Fixtures

## Task Summary
Added defensive cleanup in test fixture `TearDown()` methods (`PlayerGrabTests` and `PlayerMovementTests`) to disable individual input actions, nullify destroyed asset/action references, and reset static delegates (`PlayerMovement.IsCursorOverUIHandler`).

## Relevant Previous Context
- In commit `7d8c5b0`, input action leak fixes were implemented in `BucketPourInput.cs`, `ShovelInput.cs`, and `PlayerGrabTests.cs` (adding `_actionAsset.Disable()` before destroying it), and the artificial `[SetUp]` warm-up in `PlayerMovementTests.cs` was reverted.
- In `TestResults_20261010_160506.xml` (run on commit `cece773`), 180 out of 181 tests passed; the only failure was inside the artificial `Setup()` method in `PlayerMovementTests.cs`.
- Before testing commit `7d8c5b0` in Unity PlayMode, additional defensive cleanup was identified to prevent any subtle dangling references across test assemblies.

## Changes Made
1. **`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`:**
   - In `TearDown()`, explicitly disabled `_interactAction` and `_throwAction` before destroying `_actionAsset`.
   - Set `_interactAction`, `_throwAction`, `_actionAsset`, `_interactReference`, and `_throwReference` to `null` to avoid dangling references.
2. **`Assets/Game/Tests/Player/PlayerMovementTests.cs`:**
   - In `TearDown()`, explicitly disabled `_moveAction` and `_lookAction` before destroying `_actionAsset`.
   - Set `_moveAction`, `_lookAction`, `_actionAsset`, `_moveReference`, and `_lookReference` to `null`.
   - Reset `PlayerMovement.IsCursorOverUIHandler = null` to ensure no cross-fixture delegate leaks.

## Files Affected
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-10-16-33-cleanup-input-teardown-and-static-handlers.md`

## Technical Decisions
- **Disabling actions prior to reference destruction:** Calling `.Disable()` on individual `InputAction` references guarantees that their internal subscriptions to `InputSystem.s_Manager.onBeforeUpdate` are immediately unregistered, preventing orphaned initial state checks from running in subsequent fixtures with null state pointers.
- **Static delegate cleanup:** Resetting static delegates like `PlayerMovement.IsCursorOverUIHandler` in fixture teardown ensures tests remain strictly hermetic.

## Verification Performed
- Ran `rtk dotnet build` across all affected projects:
  - `Ngecor.Player.Tests.csproj`: 0 errors, 0 warnings.
  - `Ngecor.Interaction.Tests.csproj`: 0 errors, 0 warnings.
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors, 0 warnings.
  - `Ngecor.Material.Tests.csproj`: 0 errors, 0 warnings.
- Verified `git diff` for correctness and compliance with project conventions.

## Final Result & Follow-Up
- All assemblies compile cleanly without warnings or errors.
- Changes committed and ready for PlayMode verification in Unity Editor.
