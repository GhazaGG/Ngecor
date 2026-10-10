# Approve PR #99 after a TL test run; flag PR #93 checkboxes; find a flaky main test

## Task summary
- Checked the open PRs.
- Ran the PlayMode tests on PR #99 (`[NET-002]`, meryzennn) at the merged head `0e89499` and approved it.
- Commented on PR #93 (`[BUILD-001]`, DePo4l) about checkboxes ticked without a basis.

## Relevant previous context
- `2026-10-10-18-30-rereview-pr-99-shared-pour-action.md` requested removing the production `Disable()` calls on the shared `Pour` action.
- `2026-10-10-19-30-rereview-pr-93-plank-folder-owner-checkbox.md` requested unticking the owner-feel checkbox.
- PR #92 was merged to `main` as `7d21c78`.

## Changes made
- **PR #93:** posted a comment. At 12:31 UTC, after the Request Changes review, the PR body was edited to tick "Owner confirms gameplay feel" and "Issue #18 acceptance is approved by review"; asked for both to be unticked.
- **PR #99:** posted an APPROVE review with the TL test evidence.
- Moved the isolated review worktree `C:\Users\Hype\AppData\Local\Temp\ngecor-pr-92-review` to `0e89499`, then to `origin/main` (`7d21c78`), both detached and clean. Unity runs were batchmode only.
- No source or asset change by the reviewer.

## Findings
- **PR #99:**
  - `2a9a210` removed both `Disable()` calls; the author's run on that commit was 181/181 with a matching hash.
  - The author then merged `main` (which includes #92) and resolved a conflict in `PlayerGrabTests.TearDown`. They only compile-checked the result.
  - The resolution keeps `_actionAsset.Disable()` before the asset is destroyed.
- **TL PlayMode run on `0e89499`:** 218 tests, 217 passed, 1 failed (`PlayerMovementTests.MovementPushWithSurfaceNormalPushesAlongTheSurface`), 0 compile errors.
- **Pre-existing on main:** `PlayerMovementTests` alone on `origin/main` `7d21c78` fails 2 of 37:
  - `MovementPushWithSurfaceNormalPushesAlongTheSurface`: velocity 0;
  - `MovementBrakeSlowsABodyFasterThanThePushSpeed`: velocity stays at 8.
- **Cause, from code:** both tests are from #92 and use `WaitForFixedFrames(1)`. That helper waits one rendered frame at `captureFramerate = 60` (1/60 s), while the fixed timestep is 0.02 s, so the frame can pass with no physics step. Which test fails depends on the leftover fixed-time accumulator. Waiting with `WaitForFixedUpdate` would make them deterministic.
- **Scope note:** the wheelbarrow from #92 has no `NetworkObject`, and its hold interaction does not go through RPCs. Multiplayer sync belongs to NET-003 (#22).

## Files affected
- `.development-history/2026-10-10-19-55-approve-pr-99-tl-test-run-and-pr-93-checkboxes.md`

## Verification performed
Unity 6000.3.25f1 batchmode `-runTests -testPlatform PlayMode`:
- full suite on `0e89499`;
- `PlayerMovementTests` filter on `0e89499`: 36/37;
- `PlayerMovementTests` filter on `7d21c78`: 35/37.

## Final result
- PR #99: APPROVED.
- PR #93: still CHANGES_REQUESTED, with an added comment on the checkboxes.

## Known limitations
- The flaky-test cause is inferred from the helper code and the fixed timestep; the fix was not tried.

## Unresolved issues or follow-up work
- Fix the two flaky `PlayerMovementTests` on `main` (owner RyoFPS).
- DePo4l to untick the PR #93 checkboxes and push the two Must Fixes.
