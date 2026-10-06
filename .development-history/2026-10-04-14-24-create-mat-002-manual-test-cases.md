# MAT-002 Manual Test Cases and Scope Clarification

## Task summary

Created a tester-facing checklist for the current sand pile and bucket implementation, and clarified MAT-002's scope. The user prefers a shrinking mound for the cartoon party-game direction and requested test cases for the present feature.

## Relevant previous context

- `2026-10-04-00-50-mat-002-sand-bucket-finalization.md` records the bucket pour adapter, held-input gate, receiver registration, and R binding proposal.
- `2026-10-04-04-05-correct-sand-pile-volume-and-verify-prefab-physics.md` records proportional mound shrinking, empty-pile collision handling, hollow bucket geometry, and the 22-test Play Mode result.
- Reviewed current issues #16 and #62, the project workflow/structure/design/decisions, and the existing MAT-001 manual checklist format.
- Confirmed current source and serialized asset values rather than treating old reports as current-state proof. `.codegraph/` is absent.

## Changes made

- Added an Indonesian manual checklist with 12 primary cases and one conditional throw case.
- Included setup, exact MAT-002 scene object names, baseline quantities, input controls, reset instructions, expected results, evidence fields, and PASS/FAIL/BLOCKED/NOT VERIFIED definitions.
- Mapped all five issue #16 acceptance criteria to test cases.
- Included numerical conservation and receiver-capacity checks, plus visual/collision, carry/drop, pour gating, cancellation, rejection, and tilt tests.
- Distinguished controlled tilt-rule evidence from a player's dynamic drop, and documented the missing throw dependency in the inspected branch.
- Recorded that shovel/scoop, ground spill piles, local depressions, cartoon squash effects, cement textures/final art, and multiplayer sync are outside this current checklist. Suggested cartoon polish has not been implemented.

## Files affected

- `docs/MAT-002-Sand-Pile-Bucket-Test-Cases.md`
- This report

## Technical decisions

- Kept manual tests in the current offline MAT-002 scope and used MAT-005 as the quantity/transfer authority.
- Required actual quantities as evidence for conserved transfer, and kept bucket tilt loss separate from transfer measurements.
- Used an Editor-created isolated dev-scene copy for temporary test configurations; no scene or prefab changes were made while writing the checklist.
- All manual results start at NOT VERIFIED. Throw should be marked BLOCKED when the tested build lacks INT-004; an input binding alone does not prove the mechanic exists.
- Bucket content mass coupling has no agreed bulk unit-to-kilogram mapping; spills may disappear under the explicit MAT-005 interim rule.

## Verification performed

- Read current issues #16 and #62 through `gh-axi`.
- Checked pile/bucket prefabs, named scene instances and receivers, current visual/container/pour/trigger/grab code, and Pour action/binding against the checklist.
- Confirmed the existing result XML reports 22/22 Passed from the earlier 2026-10-04 run. The earlier recorded process exit code is 0; no new Unity test run was performed in this documentation task.
- Reviewed the completed Markdown, confirmed 13 case headings, all cases' initial NOT VERIFIED status, and no trailing whitespace in the new checklist.
- Confirmed tracked diff statistics match the pre-task state. No runtime, Unity asset, package, ProjectSettings, or existing checklist file was changed.

## Final result

The current MAT-002 manual test checklist is available locally, with runnable steps and evidence fields. The documentation task is complete; gameplay acceptance has not been newly verified by writing the document.

## Known limitations

- The manual cases were not executed in this session, and automated tests were not rerun.
- Manual R input, carry/drop feel, and dynamic tipped-drop evidence still need the tester and gameplay reviewer.
- Throw integration requires the appropriate INT-004 implementation in the tested build.

## Unresolved issues or follow-up work

- Run the checklist, attach numerical/screenshot/video evidence, and report FAIL/BLOCKED/NOT VERIFIED results.
- Review gameplay before claiming MAT-002 acceptance complete.
- Treat cartoon reactions or other polish as separate implementation work if later requested.
