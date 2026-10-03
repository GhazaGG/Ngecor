# Scaffolding rescope, BUILD ID fix, bulk container split to lead

## Task summary
Team Lead session. Rewrote BUILD-001 so the scaffolding is placed in the world rather than assembled. Added a re-erect ticket. Fixed a duplicate BUILD-003 ID. Split the generic bulk material container out of MAT-002 into a new lead-owned ticket. Moved MAT-001 back to DePo4l.

## Relevant previous context
- `2026-10-02-01-17-milestones-priorities-new-tickets-assignments.md`: the board baseline.
- `docs/DECISIONS.md`: "Kontrak material bulk" and "Player mendorong objek fisika lewat kontak", both 2026-10-02.
- The owner focuses on review and game design. DePo4l can make progress again. meryzennn finished NET-001 (#20 closed manually after PR #52) and moves on to INT-001 (#9).

## Changes made
- **#15 MAT-001:** reassigned to GhazaGG while DePo4l was blocked, then back to DePo4l.
- **#18 BUILD-001 rewritten:** the scaffolding is pre-placed. It holds 1 player + 3 cement bags, a casual player bump doesn't topple it, it collapses on overload or a hard impact, the parts stay physical, it has at most 6 Rigidbodies, and the ticket now has a How to Test section.
- **#61 created:** re-erect collapsed scaffolding (M2, P1, unassigned). The owner proposes a recovery method, and the lead approves it before implementation. It blocks LEVEL-001 #26.
- **#61 renamed** from BUILD-003 to BUILD-004, because #51 already uses BUILD-003. References in #18, #26, and #31 updated.
- **#62 MAT-005 created:** a generic bulk material container (M2, P1, GhazaGG). It covers integer units per type, capacity, single/multi-type mode, conserved rate-limited transfer, type rejection, tilt spill, and a fill visual. It has no INT dependency.
- **#16 MAT-002 rescoped** to sand pile + bucket + pour action built on MAT-005, and raised from P2 to P1. It was P2 while blocking P1 tickets #54 and #55.
- **EPIC-003 #31** now lists #62 and the new MAT-002 title.
- **`docs/DECISIONS.md` bulk contract:** the container is now "dibuat di MAT-005 #62", and MAT-005 is added to Applies from.

## Files affected
- `docs/DECISIONS.md`, this report. The rest were GitHub issues.

## Technical decisions
- The container foundation moved from a junior's ticket to the lead. Five tickets (#54, #55, #19, #45, #17) build on it, and the playbook gives architecture to the lead. It also gives the lead a ticket that can start now; all other lead tickets are blocked behind MAT-002/INT-002.
- MAT-005 supports multi-type from the start, because the contract defines the mixing spot and the mixer as multi-material containers.

## Verification performed
- Re-read the #16, #18, #26, and #31 bodies after editing. Checked that the `BUILD-004` text is present in #18.
- Confirmed #20 is closed as completed, with milestone and assignee intact.
- Confirmed PR #58 (VEH-001) is a draft, so its review was deferred.

## Final result
The lead has a startable P1 ticket (#62). The scaffolding tickets are unambiguous. The priority inversion on #16 is fixed.

## Known limitations
- #54, #55, #19, #45, and #17 still list their blockers through MAT-002/MAT-004. MAT-005 reaches them only transitively.
- The team test norm (automated vs. manual) is still an open lead decision. MAT-005's How to Test uses a debug trigger in a dev scene.

## Unresolved issues or follow-up work
- Handoff INT-001 → INT-002 for meryzennn.
- Review PR #58 once it leaves draft.
- Remind developers to use `Closes #<issue>` in PRs (#20 was closed manually).
