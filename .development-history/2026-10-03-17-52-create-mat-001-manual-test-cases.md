# Create MAT-001 Manual Test Cases

## Task summary
Created a tester-facing manual test plan for MAT-001 Cement Bag Physics.

## Relevant previous context
- Issue #15 is open and defines five relevant acceptance criteria: physical prefab/collider stability, five-bag settling, relative mass, three bags falling from a ramp, and Rigidbody sleep.
- Previous MAT-001 history records six bag instances and a ramp in the already modified `Dev_Ghaza` scene, but no reliable Play Mode acceptance results.
- Project rules keep each developer's test work in a dev scene and require Unity serialized scene files to be handled through the Editor.

## Changes made
- Added manual setup guidance and six test cases with step-by-step procedures, expected observations, result labels, and blank evidence fields.
- Kept bag tearing, rain damage, grab/carry, and wheelbarrow integration outside the MAT-001 cases.
- Distinguished observable stillness from proof of the internal Rigidbody sleep state.

## Files affected
- `docs/MAT-001-Cement-Bag-Test-Cases.md`
- `.development-history/2026-10-03-17-52-create-mat-001-manual-test-cases.md`

## Technical decisions
- Used the existing owner dev scene (`Dev_Ghaza`) or an Editor-created isolated copy for the test layout; the shared Playground is not the place to save this test arrangement.
- Listed the previously inspected mass/friction/damping values as a baseline reference to recheck, not fixed acceptance thresholds.
- Did not add scripts, packages, automated tests, or Unity assets.

## Verification performed
- Read the current GitHub issue #15 acceptance criteria and `How to Test` steps.
- Reviewed relevant MAT-001 development history, `README.md`, `docs/PROJECT_STRUCTURE.md`, `docs/WORKFLOW.md`, `docs/GAME_DESIGN.md`, and relevant `docs/DECISIONS.md` guidance.
- Confirmed the documentation path is under the existing `docs/` folder and verified the new test-case document after writing.
- Did not run Unity Play Mode; the user will execute the cases and record results.

## Final result
The test plan is ready for the user to run. No acceptance result is claimed.

## Known limitations
The project does not currently expose a confirmed manual indication of `Rigidbody.IsSleeping()`; visual rest cannot prove the internal sleep state.

## Unresolved issues or follow-up work
- User to execute the cases in Unity and fill in the run summary and evidence references.
- Review any failures or `NOT VERIFIED` criteria after the test results are recorded.
