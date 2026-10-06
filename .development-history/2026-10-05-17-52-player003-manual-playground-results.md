# PLAYER-003 Manual Playground Results and Automated Follow-up

## Task summary

Recorded developer-reported manual gameplay observations for PLAYER-003 in the `Playground` scene and kept them separate from the outstanding automated PlayMode failures.

## Relevant previous context

- PR #85 is on `feat/player-003-human-strength` at `290efd7`.
- The current-head QA report `.development-history/2026-10-04-21-56-player003-qa-run.md` recorded 71 passing and 2 failing PlayMode tests. The failures were `StepProbeRetriesWhenIgnoredCollidersFillHitBuffer` and `CarriedObject_HandlesExternalDestructionGracefully`.
- After that test run, local uncommitted changes were made to guard destroyed Unity targets and to make the buffer regression fixture verify its own hit count and blocker hit. These changes have not been rerun in PlayMode.

## Changes made

- Added this report with the developer's manual observations and the current verification boundary.
- No scene, prefab, project setting, or gameplay source was changed in response to the manual observations.
- Existing local code and scene changes were preserved.

## Files affected

- `.development-history/2026-10-05-17-52-player003-manual-playground-results.md`
- Existing uncommitted changes to `Assets/Game/Scripts/Interaction/InteractionDetector.cs`, `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`, and `Assets/Game/Tests/Player/PlayerMovementTests.cs` remain outside this report's commit scope.

## Technical decisions

- Treat the following as developer-reported manual evidence on the current PLAYER-003 checkout, not as an automated test result.
- Record the single-bag stepping/resistance behavior as acceptable to the developer for a 25 kg sack lying on its side.
- Keep the automated failures unresolved until the updated focused tests and full PlayMode suite are run.

## Verification performed

- The developer reported that pushing `Cube_8` at 1 kg did not make it launch or exceed player walking speed.
- The developer reported that a wheelbarrow containing exactly three cargo bodies worked on flat ground, and an empty wheelbarrow climbed the ramp.
- The developer reported that a single 25 kg cement sack was sometimes stepped onto and sometimes felt stuck or too heavy to push; this was considered plausible for the sack's mass and orientation.
- The developer reported that pushing a stack of five sacks initially felt like hitting a barrier, then caused the stack to topple.
- The developer reported a clean Unity Console with no errors after these scenarios.
- `git diff --check` passed for the current local code changes. Unity reimported the changed scripts without compiler errors in the Editor log.
- PlayMode tests were not rerun after the local follow-up changes.

## Final result

The reported manual feel checks for the light cube, wheelbarrow, single sack, and five-sack stack were acceptable. The automated suite is not yet clean or current against the uncommitted follow-up changes.

## Known limitations

- The ordinary static low-obstacle traversal case was not included in the developer's latest observations.
- No current walking-flat or low-object-push Profiler capture was reported. The earlier idle-only capture remains the only profiler evidence.
- No new Game View screenshot or video accompanied these observations.

## Unresolved issues or follow-up work

- Rerun the updated saturated-buffer regression and destroyed-target regression in Unity Test Runner.
- Rerun the focused `PlayerMovement` suite and full PlayMode suite after the focused tests pass.
- Complete the static traversal check and obtain current walking/pushing Profiler captures if GhazaGG still requires them for review.
