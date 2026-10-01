# Root project location, main ruleset, CODEOWNERS, PR ready

## Task summary
Record the owner's approval for a repo-root Unity project, replace `main` branch protection with a ruleset that lets only the team lead bypass via PR, add CODEOWNERS, and mark PR #37 ready for review.

## Relevant previous context
`2026-10-01-19-00-lightweight-performance-and-urp.md` left these three items awaiting the owner's answer. The classic `main` protection from `2026-10-01-18-51` enforced rules on admins with no bypass.

## Changes made
- `docs/DECISIONS.md`: added a "Lokasi project Unity: root repo" row; removed location from the open "Struktur proyek nyata" row.
- `docs/PROJECT_STRUCTURE.md`: changed the root layout from a proposal to a rule, with a warning against subfolders like `My project/`.
- `docs/WORKFLOW.md`: documented CODEOWNERS approval and the team lead's emergency bypass.
- Added `.github/CODEOWNERS`: `ProjectSettings/`, `Packages/`, `.gitignore`, `.gitattributes`, `.github/` owned by @GhazaGG.
- GitHub: created ruleset "Protect main" (id 24306744) on the default branch with rules for deletion, non-fast-forward, and pull requests (1 approval, code owner review required). The only bypass actor is the admin repository role in `pull_request` mode. Deleted the classic branch protection.
- PR #37: updated the description and marked it ready for review.

## Files affected
`docs/DECISIONS.md`, `docs/PROJECT_STRUCTURE.md`, `docs/WORKFLOW.md`, `.github/CODEOWNERS`, this report. Remote: repo ruleset, `main` branch protection, PR #37.

## Technical decisions
- Personal-account repos cannot list individual users as bypass actors, so bypass uses the admin role. GhazaGG is the only admin; the other three collaborators have `write`.
- `pull_request` bypass mode lets the lead merge their own PR without approval, but blocks direct pushes to `main`, including pushes made by AI under the lead's account.

## Verification performed
- `GET /rules/branches/main` returns `deletion`, `non_fast_forward`, `pull_request`.
- The ruleset reports `current_user_can_bypass: pull_requests_only`.
- Did not test a direct push to `main`, to avoid landing unreviewed commits.
- `git diff --check` is clean.

## Final result
`main` is protected by the ruleset, and team lead bypass is limited to PR merges.

## Known limitations
CODEOWNERS takes effect only once it exists on `main`, i.e. after PR #37 merges.

## Unresolved issues or follow-up work
An untracked Unity project exists at `My project/` (Unity `6000.6.3f1`, URP 17.6.0, Input System 1.20.0, about 1.3 GB including `Library/`). It was not touched or committed. It conflicts with the repo-root decision and belongs to SETUP-001 on its own branch.
