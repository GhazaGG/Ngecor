# Publish MAT-004 Draft PR

## Task summary

Publish the verified MAT-004 branch and prepare the review handoff without merging.

## Relevant previous context

`2026-10-06-22-49-mat004-shovel-pile-implementation.md` records implementation, final Unity verification, media, retained local work, and remaining gameplay/GPU gates. `2026-10-06-21-17-commit-mat004-implementation.md` records the earlier implementation commits preserved during this task.

## Changes made

- Committed the mesh query correction, warning regression, adapter-request check, review checklist, media, and implementation report as `c48267a`.
- Pushed `feat/mat-004-shovel-piles`, including its two LFS media objects.
- Created draft PR **#91**, `[MAT-004] Shovel scoop/dump and recoverable ground piles`, targeting `main`, with `Closes #54`.
- Updated the PR's checklist/report/media links to absolute branch URLs and re-read the live PR body.

## Files affected

The prior commit contains the six paths listed in its diff. This report adds delivery evidence. GitHub PR body and feature-branch refs were published; no unrelated local files were staged.

## Technical decisions

Keep the PR in draft until team technical/gameplay review is complete. Preserve the feature-branch history and all existing stash/untracked work. No main push, force push, or merge was performed.

## Verification performed

- Final Unity 6000.3.25f1 full Play Mode: **123 passed, 0 failed, 0 skipped**, process exit **0**; `Logs/Mat004FullResults.xml` and `Logs/Mat004FullTests.log`.
- Final source/test/documentation staged diff check passed; the implementation report distinguishes Unity-generated serialized whitespace from source checks.
- Live PR view confirms **open**, **draft**, **unmerged**, complete intended body, and no submitted reviews.
- PR checks report **no CI checks configured**; this is not a green-CI claim.
- Local feature branch tracks the published origin branch. Existing MAT-002 scenes, history files, stash, and generated untracked SceneTemplateSettings remain excluded.

## Final result

Draft PR: https://github.com/GhazaGG/Ngecor/pull/91

MAT-004 is published for review with implementation, automated evidence, local keyboard-path observations, and profiling media. It has not been approved or merged.

## Known limitations

Human gameplay/feel review, remaining keyboard cases, usable GPU timing, standalone performance, and four-player/reference-device acceptance remain unverified as documented in the PR. No CI checks are configured.

## Unresolved issues or follow-up work

Complete the gameplay checklist, obtain remaining performance evidence, address review findings, and obtain team approval before marking ready or merging. Resolve the generated SceneTemplateSettings file separately.
