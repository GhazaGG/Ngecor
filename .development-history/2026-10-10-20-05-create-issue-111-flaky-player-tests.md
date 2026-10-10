# Create issue #111 for flaky PlayerMovementTests on main

## Task summary
At the owner's request, created issue #111, `[PLAYER-006] Fix flaky push and brake PlayMode tests on main` (bug, P1, M2, assigned to RyoFPS).

## Relevant previous context
`2026-10-10-19-55-approve-pr-99-tl-test-run-and-pr-93-checkboxes.md` found that `MovementPushWithSurfaceNormalPushesAlongTheSurface` and `MovementBrakeSlowsABodyFasterThanThePushSpeed` fail on `main` `7d21c78` when `PlayerMovementTests` runs alone (35/37).

## Changes made
- Created issue #111 with:
  - the reproduction;
  - the suspected cause: `WaitForFixedFrames(1)` waits one 1/60 s rendered frame against a 0.02 s fixed timestep;
  - acceptance criteria: confirm the cause, wait on real physics steps, 37/37 three times in a row, the full suite green on a matching hash, and no production change.

## Files affected
- `.development-history/2026-10-10-20-05-create-issue-111-flaky-player-tests.md`

## Verification performed
- Confirmed label, milestone, and `PLAYER-*` numbering against existing issues before creating it.

## Final result
Issue #111 is open and assigned to RyoFPS.

## Unresolved issues or follow-up work
- RyoFPS to fix the tests.
