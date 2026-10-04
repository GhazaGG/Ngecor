# PLAYER-003 Step Probe Buffer Saturation Fix

## Task summary

Fixed the PLAYER-003 review finding where a full `CapsuleCastNonAlloc` result buffer could hide a valid low dynamic blocker behind colliders that are filtered out later. Added a PlayMode regression fixture and pushed the change to PR #85.

## Relevant previous context

- PR #85 is on `feat/player-003-human-strength`, linked to issue #73.
- GhazaGG's review on head `7c16b3b` reported that eight ignored colliders could fill the cached probe array before a valid low dynamic blocker was returned.
- The working tree already contained local changes to `Playground.unity`, `VersionControlSettings.asset`, a dev scene, and history/settings files. Those files were preserved and excluded.
- The Unity 6000.3.25f1 Editor for this checkout was already in Play Mode when implementation began.

## Changes made

- In `PlayerMovement.ShouldBlockStepOverDynamicBody`, retry the capsule query whenever its hit count fills the current cached array, doubling that array and repeating until the result count is below capacity. Existing layer, ignored-pair, self-collider, and dynamic-body filters remain in place.
- Added `StepProbeRetriesWhenIgnoredCollidersFillHitBuffer`, which creates 12 ignored colliders ahead of a valid low dynamic blocker, asserts the blocker is detected, and checks the grown array is reused on the next query.
- Committed and pushed `a533ba1` (`fix: retry saturated player step probes`). Updated PR #85 to distinguish the previous 72/72 test report and earlier Console screenshot from current-head evidence.
- Did not stop Play Mode, save the scene, or edit scene, prefab, project-setting, or asset files.

## Files affected

- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-04-21-29-player003-step-probe-buffer-fix.md` — this report.
- PR #85 description on GitHub.

## Technical decisions

- Keep `Physics.AllLayers` and the existing post-query collision exclusions. These filters cannot prevent ignored collider pairs from saturating the physics result array, so the query must retry after growing the cache.
- Grow the cache only when the query returns exactly the current capacity. Normal unsaturated queries keep reusing the same array without per-frame allocation.
- Do not change the push-force cap or unrelated movement behavior.

## Verification performed

- `git diff --check` passed before committing.
- Confirmed commit `a533ba1` contains only the player movement script and its test file, and the branch push succeeded.
- Confirmed PR #85 points to `a533ba1`; its checklist marks current-head automated, Console, and Game View evidence as pending.
- The Unity Editor log recorded a successful managed assembly reload after the edits. PlayMode tests were not run: the Editor was already in Play Mode, and it was left running to preserve the user's active session.
- Confirmed pre-existing local scene, project-setting, and untracked files remained outside the commit.

## Final result

The saturated probe now retries with a larger reusable cache, and PR #85 contains a regression test for the reported scenario. The fix has not yet received automated PlayMode test results or current-head manual gameplay validation.

## Known limitations

- `StepProbeRetriesWhenIgnoredCollidersFillHitBuffer`, the 28-test PlayerMovement suite, and the full PlayMode suite have not been run on this head.
- Current-head Game View checks and a walking/pushing Profiler capture remain outstanding.
- PR #85 remains `CHANGES_REQUESTED`; GhazaGG has not been re-requested after this change.

## Unresolved issues or follow-up work

- Run the new regression, focused PlayerMovement suite, and full PlayMode suite in Unity 6000.3.25f1.
- Complete current-head Game View checks for a light prop, a 25 kg bag and release, five-bag stack, ordinary static traversal, empty wheelbarrow on a ramp, and exactly three cargo bodies on flat ground.
- Capture warmed-up walking-flat and low-object-push Profiler intervals if issue #73 still requires that evidence.
- Update the PR checklist with observed results, then request GhazaGG's re-review.
