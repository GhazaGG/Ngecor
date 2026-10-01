# Add concrete guardrails for weaker AI assistants

## Task summary
Apply the documentation fixes identified in `2026-10-01-18-36-review-pre-unity-docs-for-weaker-ai.md` before Unity setup begins.

## Relevant previous context
The 18-36 review found that the docs state principles but lack concrete prohibitions that less capable AI assistants follow reliably. Before this task the working tree already held uncommitted edits to `AGENTS.md`, `docs/DECISIONS.md`, and `docs/GAME_DESIGN.md` (two core physical/upgrade principles). Those edits were left intact.

## Changes made
- `AGENTS.md`: added guidance for chat-only AI that cannot read the repo, and a "Larangan keras" list of nine concrete prohibitions (packages, undecided tech choices, moving assets outside the Editor, text-editing serialized files, main scene edits, destructive git, global managers, client-authoritative state, false test claims).
- `docs/WORKFLOW.md`: replaced the optional dev-scene line with a per-developer `Dev_<Nama>.unity` rule and single-owner main-scene edits; added an Editor-only move/rename rule; added a short C# convention section marked as a proposal.

## Files affected
`AGENTS.md`, `docs/WORKFLOW.md`, this report.

## Technical decisions
C# conventions are recorded as a team-changeable proposal, not a decision in `docs/DECISIONS.md`. No Unity version, package, input system, or render pipeline was chosen.

## Verification performed
`git diff --check` reported no whitespace errors. The new relative link to `PROJECT_MANAGEMENT.md` resolves.

## Final result
Docs now carry concrete rules for weaker AI. Changes are uncommitted on `docs/pre-unity-team-guides`.

## Known limitations
No Unity project exists; rules about the Editor and scenes are untested in practice.

## Unresolved issues or follow-up work
- Team must decide Unity version, input system, render pipeline, and networking package and record them in `docs/DECISIONS.md`.
- Setup issues #34–#36 still lack How to Test and Out of Scope sections.
- `main` has no branch protection; PR #37 is still draft.
