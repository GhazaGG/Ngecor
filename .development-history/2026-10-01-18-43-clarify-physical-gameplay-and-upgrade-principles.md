# Clarify physical gameplay and upgrade principles

## Task summary
Check and strengthen the owner's two core design principles: fully physical gameplay without magic inventory, and improvements that introduce relevant new challenges.

## Relevant previous context
`2026-10-01-18-02-create-pre-unity-team-documents.md` established the design and AI guides in draft PR #37. The current design already mentioned physical logistics and equipment risks, but phrased new problems as an ideal rather than a requirement.

## Changes made
Made the two principles explicit in game design, added practical examples of cargo capacity, wheel chocks, and unsecured pickup cargo, and added concise implementation rules for AI assistants. Recorded the owner's clarification in the project decision register.

## Files affected
`docs/GAME_DESIGN.md`, `AGENTS.md`, `docs/DECISIONS.md`, and this report.

## Technical decisions
Physical capacity and available material must follow world state. Equipment improvements must introduce a relevant new risk or challenge with understandable triggers and player countermeasures. A risk need not cause an accident on every use. Examples illustrate principles and do not expand implementation scope.

## Verification performed
Reviewed relevant development history and current design/agent documents. Checked the clarification in all three affected documents and ran `git diff --check` successfully. The new decision link resolves to an existing local design file. Preserved the pre-existing untracked documentation-review report.

## Final result
The two principles are explicit requirements for future feature design and review, prepared as an update to the existing documentation branch and draft PR #37.

## Known limitations
This is a documentation change. No Unity gameplay or physical behavior was implemented or tested.

## Unresolved issues or follow-up work
Individual tasks must select concrete physics and equipment mechanics while meeting these principles.
