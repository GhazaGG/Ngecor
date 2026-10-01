# Review pre-Unity docs for teammates using weaker AI

## Task summary
Assess whether the pre-Unity documentation is sufficient for four developers whose AI assistants may be less capable than Codex or Claude.

## Relevant previous context
`2026-10-01-18-02-create-pre-unity-team-documents.md` created the docs under review (draft PR #37).

## Changes made
Created this report only. No docs were changed.

## Files affected
- Added: `.development-history/2026-10-01-18-36-review-pre-unity-docs-for-weaker-ai.md`

## Technical decisions
None. Findings only.

## Verification performed
Read every tracked Markdown file, issues #34–#36, and PR #37 status. Checked `main` branch protection via GitHub API: "Branch not protected".

## Final result
Process and design direction are adequate. Gaps for weaker AI are concrete guardrails, not more prose:
1. Technical choices (Unity version, input system, networking package, render pipeline) are all still open; weaker AI will guess regardless of "don't guess" instructions.
2. No rule to move/rename assets only inside the Unity Editor (GUID/`.meta` breakage).
3. No minimal C# convention (namespace, naming, script location).
4. No guidance for chat-only AI that cannot read the repo (what to paste).
5. Scene-editing rule is soft ("bila perlu"); no per-developer dev scene rule.
6. Setup issues #34–#36 do not follow the repo's own ticket format (no How to Test / Out of Scope).
7. PR #37 is still draft and `main` is unprotected.

## Known limitations
No Unity project exists; nothing was tested in Unity.

## Unresolved issues or follow-up work
Apply the guardrail additions if the user approves; merge PR #37; enable branch protection on `main`.
