# Publish MAT-002 Pull Request

## Task summary

Published `feat/sand-bucket` and opened PR #86 for technical and gameplay review after the user asked to proceed toward PR creation.

## Relevant previous context

- Reviewed the `16-12` animation acceptance/readiness report, the `16-16` PR readiness report, and the repository PR template.
- Confirmed the original three MAT-002 milestone commits, clean tracked working tree, unrelated untracked inventory, current issue #16, and absence of an existing branch PR.
- The animation implementation report and existing result XML record 25 passing Play Mode checks. Full manual acceptance and hardware performance remain pending.

## Changes made

- Merged the current `origin/main` into `feat/sand-bucket` using a normal merge, without conflicts. The incoming commit `d271570` changed documentation only: rope decisions, the TOOL prefix, and related development history.
- Committed the two existing MAT-002 readiness reports as `f8d28de` (`docs: record mat-002 review readiness`).
- Pushed `feat/sand-bucket` and established its remote upstream.
- Created the non-draft PR [#86: MAT-002 Sand pile, bucket transfer and pour animation](https://github.com/GhazaGG/Ngecor/pull/86), targeting `main` and linking `Closes #16`.
- Used the repository template, including actual test evidence, pending reviewer reproduction, unverified acceptance items, the R binding proposal, affected systems, and MAT-004 scope boundaries.
- Created this report. No runtime code or Unity serialized assets were edited during publication.

## Files affected

- `.development-history/2026-10-04-16-12-assess-mat-002-readiness-after-animation-acceptance.md`
- `.development-history/2026-10-04-16-16-confirm-mat-002-pr-readiness.md`
- This report.
- Incoming main documentation: `docs/DECISIONS.md`, `docs/PROJECT_MANAGEMENT.md`, and `2026-10-04-03-59-rope-in-mvp-tool-001.md`.
- Ignored temporary PR body: `Logs/MAT002-PR-body.md`.

## Technical decisions

- Preserved the existing gameplay implementation and used a normal merge rather than rebase or force push.
- Corrected the earlier readiness interpretation: matching the live remote main hash to cached `origin/main` did not prove the feature branch contained its latest commit. A separate ancestry check found one missing documentation commit; the merge now includes it.
- Checked only the locally executed Play Mode test item in the PR acceptance checklist. User animation acceptance does not establish every manual criterion or reviewer approval.
- Kept free pouring and recoverable ground piles in MAT-004 (#54).

## Verification performed

- Live main remained `d271570a63d75bf7b2bb431a0283e3b92c411e6a` before publication.
- Inspected incoming main changes and confirmed the merge affected documentation only. `git merge-base --is-ancestor origin/main HEAD` subsequently exited 0.
- Read the existing XML: 25 passed, zero failed, skipped, or inconclusive, from the 2026-10-04 15:32 WIB run. No Unity tests were rerun.
- Push exited 0 and created the remote feature branch. Tracked working tree and upstream were synchronized afterward; unrelated untracked files were preserved.
- PR creation exited 0. Read back the complete body and confirmed the PR is open, non-draft, targets review, and remains unmerged.
- GitHub reports no CI checks configured, rather than a CI pass.
- Attempted to inspect the project board for the required In Review transition. `gh-axi project list --owner GhazaGG` was denied because the token lacks `read:project`. No board mutation, token scope change, or authentication flow was performed.

## Final result

PR #86 is open for technical and gameplay review with scope and verification limits documented. No merge was performed and issue #16 was not manually closed.

## Known limitations

- Complete manual acceptance, real player keyboard/carry/drop interaction evidence, and low-end hardware performance remain pending.
- Runtime screenshots are local ignored artifacts and were not uploaded; the PR explicitly states this.
- The previously documented Unity serialized trailing whitespace remains unchanged.
- Project board status could not be checked or moved with the available token permissions.

## Unresolved issues or follow-up work

- Move issue #16 to In Review using an account with project access.
- Obtain technical and gameplay review, record pending manual results, and address review findings before merging.
- Complete target hardware performance verification before claiming the performance target.
- Continue MAT-004 free pouring separately.
