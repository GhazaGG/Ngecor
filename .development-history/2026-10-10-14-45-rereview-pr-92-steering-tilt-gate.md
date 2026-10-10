# Re-review PR #92 — VEH-002 steering at head ee4db26

## Task summary
Code-only Team Lead re-review of PR #92 (`[VEH-002] Wheelbarrow player interaction`, RyoFPS) after commit `ee4db26`. The owner asked for no Unity session; the owner will run the feel test later. Posted a Request Changes review and an AI handoff prompt.

## Relevant previous context
The previous reviews at `31ce142` requested:
- the owner-confirmed A/D mapping (A: player/handle left, nose right);
- no roll from the steering force, and player-led lateral motion;
- no wheel grip while the wheel is airborne;
- grounded tests on the real prefab.
See `2026-10-08-22-07-review-pr-92-owner-steering-roll.md`.

## Changes made
- Posted the GitHub review (CHANGES_REQUESTED) and the handoff comment on PR #92.
- Added this report. No game code was changed.

## Findings
- **Resolved:**
  - Traced the A/D sign chain: `right = Cross(up, -outward)`; A gives a negative lead, the handle force goes left, and the nose turns right with positive yaw.
  - Steering and wheel-grip forces act at centre-of-mass height, so they produce yaw only.
  - The player leads through `CharacterController.Move`, and the handle chases like a spring leash; a blocked player produces no push.
  - Grip and damping run only while the wheel is grounded.
  - Real-prefab tests cover empty and 3-cargo cases for A and D, plus lifted-wheel, blocked-player, and loaded-W cases.
- **Must Fix:** the `MaxSteeringTilt` gate uses total tilt, `Vector3.Angle(transform.up, up)`, which includes pitch. `Ramp_20Degree` in Playground is 20° (quaternion x = 0.1736) and `Ramp_Gentle` is 10°, so A/D is disabled on the steep ramp and has little margin on the gentle one. Proposed a roll-only measure: `|90° - Angle(transform.right, up)|`.
- **Should Fix:** expose `SteeringLeadSpeed`, `MaxSteeringLead`, and `MaxSteeringSpeed` as `[SerializeField]` so the owner can tune them during the feel test.
- **Suggestion:** a serialized wheel reference instead of `transform.Find("Wheel")`.
- **Owner notes (not blockers):**
  - With three 25 kg cement sacks in the tray, the barrow cannot be pushed (Ryo's own finding). The legs use `FeetHighFriction` (0.8, combine Maximum) and stay on the ground while held, so friction likely exceeds the 350 N push cap. The "3 cargo" tests use 1 kg cubes. This conflicts with VEH-003 (#44) and the economy decision of 2026-10-09; recommended a separate follow-up (e.g. handles lifted while held).
  - Steering forces at COM height mean hard turns can never tip a loaded barrow. That is fine for VEH-002 but would be a separate design decision if tipping should become a risk.

## Files affected
- `.development-history/2026-10-10-14-45-rereview-pr-92-steering-tilt-gate.md`

## Technical decisions
- Kept the sack-pushing problem out of #92's scope, because the PR does not change W behavior and the AC cargo is the Playground's 1 kg cubes.

## Verification performed
- Read the diff `31ce142..ee4db26`, the full `WheelbarrowInteraction.cs` at head, the steering tests in `PlayerGrabTests.cs`, Ryo's PR comment, and issue #14.
- Extracted ramp rotations from `Playground.unity` at head: `Ramp_Gentle` x = 0.0872 (10°), `Ramp_20Degree` x = 0.1736 (20°).
- Read `Wheelbarrow.prefab` (mass 4, leg and wheel physic materials) and the `FeetHighFriction`/`WheelLowFriction` values.
- Unity was not opened, by owner request. No tests were run by the reviewer.

## Final result
PR #92: CHANGES_REQUESTED for one small fix, with the handoff prompt posted. The code is otherwise ready for the owner's feel test.

## Known limitations
- The tilt-gate impact on ramps is derived from scene values and code, not from Play Mode.
- The leg-friction explanation for the stuck 75 kg load is an estimate, not a measured force.

## Unresolved issues or follow-up work
- Owner feel test in Playground after the fix: A/D from rest, W+A/D, S, E release, empty and 3 cargo, both ramps, an obstacle near the handle, and the Console.
- Owner decision on a follow-up issue for pushing heavy loads (cement sacks).
