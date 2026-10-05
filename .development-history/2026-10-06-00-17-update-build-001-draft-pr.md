# Update the BUILD-001 Draft Pull Request

## Task summary

Published the current scaffolding follow-up to the existing BUILD-001 draft PR targeting `main`.

## Relevant previous context

Draft PR #84 already covered issue #18 from `feat/build-001-scaffolding`. The current local work branch was `feat/scaffolding-ladder-climb`. Remote `main` advanced to `508af14` with INT-004 throw support while the PR was being prepared.

## Changes made

- Committed the scaffolding parts, placement behavior, custom prefabs, developer scene changes, and related history reports in `d99fca2`.
- Merged `origin/main` at `508af14` with merge commit `d802b41`; Git merged without conflicts.
- Added `a68ca2a` so throwing a carried scaffolding part also ends its placement preview.
- Pushed the updated work to `origin/feat/build-001-scaffolding`, updating PR #84.
- Updated the PR description with the new behavior, manual Play Mode steps, known scope expansion, and untested status. PR #84 remains a draft.
- Kept unrelated local `ProjectSettings/VersionControlSettings.asset`, `ProjectSettings/SceneTemplateSettings.json`, `verify_physics.log`, and PR #86 review history out of the PR.

## Files affected

- Scaffolding scripts, prefabs, and `Assets/Game/Scenes/Dev/Dev_DePo4l.unity` from the implementation commit.
- `.development-history/2026-10-06-00-17-update-build-001-draft-pr.md`

## Technical decisions

- Reused PR #84 because it is already an open draft for issue #18 and targets `main`; this avoids a duplicate PR for the same issue.
- Merged the latest `main` into the task branch rather than rebasing it.
- Kept the PR in draft because the Play Mode acceptance steps remain unverified.

## Verification performed

- Confirmed `origin/main` advanced from `d271570` to `508af14`; merging it completed without conflicts.
- Confirmed PR #84 is open, draft, targets `main`, and is mergeable. The PR reports 32 changed files, 11,331 additions, and 6 deletions; no checks are reported.
- A Unity recompile had completed without compile errors before merging the latest `main`. A new recompile was attempted after the merge, but no Unity Editor Pipeline instance was available. Play Mode checks were not run.
- The issue has no project items returned by `gh issue view`; `gh project list` could not read project data because the current GitHub token lacks the `read:project` scope. The board status was not changed.

## Final result

The new scaffolding work is available in draft PR #84 against `main`, including the latest `main` merge and updated review instructions.

## Known limitations

- The PR diff is large because it includes Unity scene and prefab serialization for the custom scaffolding setup.
- Gameplay feel, ground alignment, steps, overload failure, and part reinstallation still need Play Mode review.
- Issue #18 board status could not be updated with the current GitHub token permissions.

## Unresolved issues or follow-up work

- Run the PR's Play Mode checklist in Unity and update the acceptance checklist with observed results.
- Update the issue board status when project access is available.
