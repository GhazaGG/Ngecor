# Re-review PR #99 at 2b72757 — remove production Disable() on the shared Pour action

## Task summary
Re-reviewed PR #99 (`[NET-002] Synchronize player movement and interaction`, meryzennn) at head `2b72757`. Posted a small Request Changes review and an AI handoff prompt.

## Relevant previous context
`2026-10-10-13-45-rereview-pr-99-compile-check.md` requested two Must Fixes and two Should Fixes:
- Must Fix: add `UnityEngine.UI` to `Ngecor.Multiplayer.asmdef`;
- Must Fix: test evidence from the pushed head, with `git rev-parse HEAD` and an empty `git status --porcelain`;
- Should Fix: remove the `SessionManager.Instance` singleton and wire spawn Transforms in the scene;
- Should Fix: replace the tautological auto-drop tests with real pinch tests.

PR #90, which held the earlier history reports, was merged at 10:51Z, so this report is on a new branch from `main`.

## Changes made
- Posted a CHANGES_REQUESTED review and a handoff prompt on PR #99.
- No source or asset change by the reviewer.

## Findings
- **All previous items are resolved:**
  - the asmdef now references `UnityEngine.UI`;
  - the evidence hash matches the head, with a clean tree and 181/181 passing;
  - the singleton is removed, `GetSafeSpawnPosition` is an instance method with a testable static overload, and `SpawnPoints` (`Spawn_0..3`) is wired in `Playground.unity`;
  - the two new pinch tests in `PlayerGrabTests` exercise the real pinch path, and the same setup proves the pinch happens;
  - `CanReplicate` consolidates the guards;
  - manual host + client results are written per point.
- **Must Fix (new):** commit `7d8c5b0` added `Disable()` calls in `ShovelInput.OnDisable` and `BucketPourInput.OnDisable`, and the author's reply did not mention them.
  - `Shovel.prefab` and `Bucket.prefab` both reference `PlayerPour.inputactionreference`, which is the project-wide `Pour` action.
  - Disabling or destroying any one bucket or shovel therefore disables `Pour` for every bucket and shovel. `BucketPourInput.Update` re-enables it while held, but `ShovelInput` does not, so all shovels stop responding with no Console error.
  - This contradicts the review decisions in #48, #69, and #78 (production code does not toggle project-wide actions).
  - The author's own history shows the `statePtr` test failure persisted after `7d8c5b0` and was fixed by `InputSystem.actions?.Disable()` in fixture Setup (`fb41607`). The production change is a leftover from the first hypothesis.
- **Minor:** the test names quoted in the author's comment differ from the names in code.

## Files affected
- `.development-history/2026-10-10-18-30-rereview-pr-99-shared-pour-action.md`

## Technical decisions
- Kept this as a Must Fix despite its size: the bug is silent, it hits a core tool, and the fix is a deletion that the existing fixture fix already covers.

## Verification performed
- Read the diff `90d4f08..2b72757` and the full PR diff against the merge base `f3fe648`.
- Confirmed through the prefab and `.inputactionreference` GUIDs that both tools reference the same project-wide `Pour` action.
- Confirmed with `git log` that `7d8c5b0` introduced the `Disable()` calls.
- `git merge-tree` shows no conflict with `origin/main` (`d58e6cf`), and `main` changed no scripts or tests since the merge base.
- Did not run Unity or the tests; the 181/181 result is the author's.

## Final result
PR #99: CHANGES_REQUESTED, with one small Must Fix. The PR is ready for code approval after that fix and a green run with a matching hash.

## Known limitations
- The shovel failure is derived from code and asset references, not reproduced in Play Mode.

## Unresolved issues or follow-up work
- Re-review after meryzennn removes the two `Disable()` calls.
