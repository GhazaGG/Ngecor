# Re-review PR #92 at fb45d48 — retract push-height finding, leg friction pending owner

## Task summary
Code re-review of PR #92 (`[VEH-002] Wheelbarrow player interaction`, RyoFPS) at head `fb45d48`, and reopened Unity on that head for the owner's feel re-check. Posted a COMMENT review: no verdict until the owner re-checks feel and decides on leg friction.

## Relevant previous context
`2026-10-10-15-22-pr-92-owner-feel-reverse-lift.md` requested two fixes:
- Must Fix 1: apply the W/S push at COM height to stop the front wheel lifting on S;
- Must Fix 2: a roll-only steering tilt gate.

## Changes made
- Posted the COMMENT review on PR #92.
- Moved the isolated review worktree to `fb45d48` (detached, clean) and launched Unity; the log shows 0 `error CS`.
- Left the unrelated batchmode Unity test run on `C:/NGECORRR/Ngecor`, from another session, untouched.
- No source or asset change.

## Findings
- **Retracted my Must Fix 1.** `PlayerMovement.ApplyContactPush` already sets `point.y = body.worldCenterOfMass.y`, so the W/S push was always at COM height. My previous review did not read that function. Acknowledged on the PR.
- **More likely cause of the S front-wheel lift on flat ground:** leg friction. `FeetHighFriction` is 0.8 with combine Maximum, and the legs act at ground level below the COM. When pulling back, the forward friction at the legs makes a nose-up couple, which shifts load onto the legs and raises friction further. W gives the opposite, nose-down moment. This is consistent with:
  - Ryo's measurement: reverse 0.86 m/s with the original friction vs 3.04 m/s with low friction;
  - the developer's "dragging a table" report;
  - the 25 kg sack pushing problem.
- **Verified correct:**
  - the roll-only gate `GetRollAngle`, with 20° slope tests;
  - steering and wheel grip moved into the plane through the COM perpendicular to the body's up axis (the test fails on the old code: 19° roll);
  - the ground-plane W/S push removes the ~78 N lift component on 20° ramps;
  - Inspector-tunable steering values;
  - 147 tests (146 passed, 1 skipped) from a clean Library.
- **Should Fix:** `ApplyGroundPlanePush` duplicates the push law with hard-coded `MaxPushForce = 350f` and `PushResponseTime = 0.1f`, and bypasses the player's per-frame push budget (`_remainingPushImpulse`, `_pushedBodies`). The earlier "do not change PlayerMovement" restriction came from the TL. Suggested an optional surface-normal parameter on `ApplyMovementPush`, since Ryo owns PlayerMovement.

## Files affected
- `.development-history/2026-10-10-16-44-rereview-pr-92-fb45d48-leg-friction.md`

## Technical decisions
- Posted a COMMENT instead of a verdict, because the remaining blocker (leg friction) needs an owner decision and a feel re-check.

## Verification performed
- Read the diff `ee4db26..fb45d48` (`WheelbarrowInteraction.cs`, test list and assertions), Ryo's update comment, and `PlayerMovement.ApplyContactPush` at the head.
- Unity Editor log for the review worktree: 0 compile errors after load.
- The leg-friction explanation is reasoning from code, prefab materials, and Ryo's measurements; no reviewer measurement.

## Final result
PR #92: COMMENTED. The previous CHANGES_REQUESTED stands until the owner decides. Unity is open on `fb45d48` for the feel re-check.

## Known limitations
- The owner's flat-ground reverse lift has not been reproduced by Ryo or the reviewer.

## Unresolved issues or follow-up work
- Owner feel re-check on `fb45d48`.
- Owner decision on leg friction: A (asset friction), B (low friction while held), or C (lift the handles while held).
- Then a handoff prompt for Ryo.
