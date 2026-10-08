# Commit MAT-004 Implementation

## Task summary

Commit the reviewable MAT-004 work already present on `feat/mat-004-shovel-piles` in focused commits.

## Relevant previous context

- `2026-10-06-20-15-prepare-mat004-shovel-task.md` records the task branch and existing untracked Unity scenes and stash.
- The ongoing MAT-004 implementation had created material, shovel, prefab, scene, test, and decision-document changes before this commit task began.

## Changes made

- Committed recoverable ground deposit, bucket integration, materials, and related regressions as `e4838db`.
- Committed shovel scoop/dump, its prefab, dev-scene trial, and focused tests as `bbb48f1`.
- Committed the MAT-004 controls and pile decision as `8938207`.
- Left the two temporary Editor helpers, existing untracked history and MAT-002 scenes, and unexpected `ProjectSettings/SceneTemplateSettings.json` out of the commits.

## Files affected

- The three MAT-004 commits contain 29 product and documentation paths under `Assets/Game/` and `docs/DECISIONS.md`.
- This development report.

## Technical decisions

- Kept related source, Unity assets, and regressions together in each implementation commit.
- Preserved Editor-generated serialized files without text editing, including their generated trailing spaces.
- Did not commit temporary verification code or unrelated local files.

## Verification performed

- Inspected branch status and staged file lists before commits.
- Rechecked the existing Unity full Play Mode result XML: 122 passed, 0 failed, 0 skipped, process exit 0.
- `git diff --cached --check` reported only trailing spaces in Unity-generated asset and metadata fields in the first commit.
- Confirmed the remaining working-tree files are untracked and outside the three implementation commits.

## Final result

Three local MAT-004 commits are on `feat/mat-004-shovel-piles`. No push or PR was made in this side task.

## Known limitations

- Keyboard/gameplay and Profiler verification were still ongoing in the parent task; this side task did not complete or claim them.
- Unity-generated trailing spaces remain in serialized assets; no manual text edits were made to Unity assets.

## Unresolved issues or follow-up work

- Continue the parent task's live playtest, review, cleanup of temporary Editor helpers, final verification, and PR preparation.
- Inspect the unexpected ProjectSettings file before deciding its disposition.
