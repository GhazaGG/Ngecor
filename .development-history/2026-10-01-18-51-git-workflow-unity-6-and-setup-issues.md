# Git workflow, Unity 6 decision, setup issues, and main protection

## Task summary
Record the owner's Unity 6 choice, review Codex's core-principle edits, document a step-by-step Git workflow, rewrite setup issues #34–#36 in the repo ticket format, and protect `main`.

## Relevant previous context
- `2026-10-01-18-44-add-guardrails-for-weaker-ai.md` listed these as follow-up work.
- `2026-10-01-18-43-clarify-physical-gameplay-and-upgrade-principles.md` (Codex) added two core principles to `GAME_DESIGN.md`, `AGENTS.md`, and `DECISIONS.md`.

## Changes made
- Reviewed Codex's principle edits: consistent with the concept brief, examples marked as illustrations. Added one clarification to `GAME_DESIGN.md` that "physical" does not mean particle simulation, so weaker AI does not over-simulate sand or concrete.
- `docs/DECISIONS.md`: recorded Unity 6 (`6000.x`) as decided; the exact patch version stays open for SETUP-001.
- `docs/WORKFLOW.md`: added "Langkah Git" (branch naming table, commit message format, per-task command sequence, syncing with `main` by merge instead of rebase, Unity conflict handling, squash merge and cleanup).
- GitHub issues #34, #35, #36: rewrote the bodies with Goal, Why, Acceptance Criteria (unchanged), Technical Notes, How to Test, Dependency, and Out of Scope.
- GitHub `main` branch protection: PR required, 1 approval, enforced for admins, force push and deletion blocked.

## Files affected
`docs/GAME_DESIGN.md`, `docs/DECISIONS.md`, `docs/WORKFLOW.md`, this report. Remote: issues #34–#36 and `main` protection settings.

## Technical decisions
- Sync branches with `git pull origin main` (merge) so no one needs force push.
- Squash merge documented as the team merge method. The repo still allows all merge types; the setting was not changed.
- After a squash merge, local branches are deleted with `git branch -D`, because `-d` does not see squashed commits as merged.
- SETUP-001 now depends on a render pipeline decision, since the Unity template selects the pipeline. It also warns that its PR must not commit `Library/` even though `.gitignore` belongs to SETUP-003.

## Verification performed
- `git diff --check` and `git diff --cached --check` are clean.
- The `#langkah-git` anchor matches its heading.
- `gh issue edit` returned URLs for all three issues.
- Read the protection back through the GitHub API: `enforce_admins=true`, `required_approving_review_count=1`, force push and deletion disabled.

## Final result
The docs, setup issues, and repo protection are ready for Unity setup, pending a render pipeline decision.

## Known limitations
The Git command sequence was written, not executed end to end. No Unity project exists yet.

## Unresolved issues or follow-up work
- Decide the render pipeline (blocks SETUP-001).
- Choose the exact Unity 6 patch version in SETUP-001.
- Input system and networking package remain open until M1 and M3.
- With admin enforcement on, PR #37 needs approval from another collaborator before it can merge.
