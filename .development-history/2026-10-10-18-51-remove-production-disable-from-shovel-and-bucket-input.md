# Development History: Remove Production Disable Calls in ShovelInput and BucketPourInput

## Task Summary
Addressed Tech Lead re-review findings on PR #99 (`feat/net-002-sync-player-movement-interaction` at head `2b72757`).
Removed redundant and harmful `Disable()` calls on the project-wide `InputSystem_Actions.Pour` action from production components `ShovelInput` and `BucketPourInput`.

## Relevant Previous Context
- In commit `7d8c5b0`, `_useAction.action.Disable()` was added to `ShovelInput.OnDisable()`, and `_pourAction.action.Disable()` was added to `BucketPourInput.OnDisable()`, based on an initial hypothesis for resolving input leaks in PlayMode tests.
- However, as confirmed by commit `fb41607`, the actual root cause of the `ArgumentNullException: statePtr` failure was `InputTestFixture.Setup()` re-enabling `InputSystem.actions` and attempting initial state checks on newly added virtual test devices before their buffers were allocated. That issue was properly and cleanly solved in test fixtures via `InputSystem.actions?.Disable();` in fixture setup.
- Production components `ShovelInput` and `BucketPourInput` share the same project-wide `PlayerPour.inputactionreference` (the `Pour` action in `InputSystem_Actions.inputactions`). Calling `Disable()` on it in production when an object is disabled or destroyed disables the action globally across all shovels and buckets, breaking input responsiveness across the entire game (violating review decisions #48, #69, and #78).

## Changes Made
1. **`Assets/Game/Scripts/Material/ShovelInput.cs`:**
   - Removed the `OnDisable` method that disabled `_useAction.action`.
2. **`Assets/Game/Scripts/Material/BucketPourInput.cs`:**
   - In `OnDisable()`, removed the lines calling `_pourAction.action.Disable()`, retaining only `CancelPour()`.

## Files Affected
- `Assets/Game/Scripts/Material/ShovelInput.cs`
- `Assets/Game/Scripts/Material/BucketPourInput.cs`
- `.development-history/2026-10-10-18-51-remove-production-disable-from-shovel-and-bucket-input.md`

## Technical Decisions
- Adhere strictly to project architecture rules: production components do not toggle or disable shared project-wide input actions.
- Any test isolation or input teardown leaks must be managed at the test fixture boundaries (`Setup` and `TearDown`), not by muting shared production assets.

## Verification Performed
- Ran Roslyn/dotnet builds for all affected projects:
  - `Ngecor.Material.csproj`: 0 errors.
  - `Ngecor.Player.Tests.csproj`: 0 errors.
  - `Ngecor.Interaction.Tests.csproj`: 0 errors.
- Verified clean git diff showing only the removal of `Disable()` calls while preserving `CancelPour()`.

## Final Result & Next Steps
- Production code is clean of all project-wide action disable mutations.
- Ready for developer to run PlayMode Test Runner in Unity Editor and verify shovel/bucket interactions in `Playground.unity`.

## Known Limitations
- None.

## Unresolved Issues or Follow-Up Work
- Run PlayMode Test Runner in Unity Editor to confirm all 181 tests pass cleanly on this commit.
- Quick test in `Playground.unity`: scoop/dump with shovel, pour with bucket.
- Post final comment and update PR description once tests are verified.
