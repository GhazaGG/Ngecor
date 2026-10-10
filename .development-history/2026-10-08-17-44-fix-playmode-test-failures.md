# Play Mode Test Failure Follow-up

## Task summary

Address the two failures reported from the 117-test Play Mode run: the second-wheelbarrow interaction assertion and the Input System `statePtr` exception.

## Relevant previous context

- The wheelbarrow interaction changes and their manual gameplay acceptance are recorded in the earlier VEH-002 reports.
- The user reported that the full suite had 117 tests and two failures; this report records the follow-up to that run.
- Existing changes in the branch and the user's dirty Playground scene and Version Control settings were preserved.

## Changes made

- Disabled each test-created `InputActionAsset` before destroying it and before `InputTestFixture.TearDown()` restores the input system. This prevents enabled test actions from retaining controls while the fixture restores test devices.
- Spawned the second wheelbarrow two meters in front of the player's current position in the re-grab test. The first held wheelbarrow can move the player toward its handles before release, so reusing its original world position could place the new test target too close to the player.
- Included the interaction feedback in the re-grab assertion message to make a remaining rejection diagnosable.

## Files affected

- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-08-17-44-fix-playmode-test-failures.md`

## Technical decisions

- Keep both fixes within the existing test fixtures; no runtime gameplay or package changes were needed based on the failure traces and fixture lifecycle.
- Use the existing `InputActionAsset.Disable()` API before teardown instead of suppressing the Input System log as expected output.

## Verification performed

- Inspected the failing test setup and `InputTestFixture` lifecycle in the installed Input System source.
- Ran `git diff --check` for the two edited test files; no whitespace errors were reported.
- Did not rerun Unity tests. Three Unity processes were already active, so a competing test run was avoided.

## Final result

Both reported failures have targeted fixture-level changes. The fixes still need confirmation from a Unity rerun.

## Known limitations

- The second-wheelbarrow test and `CementBagSlidesSlowlyWithoutBeingSteppedOver` have not been rerun after these changes.
- The `statePtr` diagnosis is based on the enabled action asset teardown order and the reported stack trace; Unity runtime confirmation remains pending.

## Unresolved issues or follow-up work

- Rerun the two failing tests in Unity, then rerun the full Play Mode suite if both pass.
