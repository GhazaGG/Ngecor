# Team Lead re-review of PR #67 (VEH-001 wheelbarrow tuning)

## Task summary
Team Lead session. Re-reviewed RyoFPS's PR #67 after PR #58 merged. Answered his question about push strength tuning with a geometry-based analysis, and posted a second "Request changes" review plus an AI handoff prompt.

## Relevant previous context
- `2026-10-02-21-43-review-pr-58-67-veh-001.md`: the first review. PR #58 was then fixed and merged with an impulse-based push (default strength 5).
- Ryo reported on 2026-10-03: strength 8 lets the empty cart and `Box_8` reach the ramp top; with one cargo cube the cart stalls at 8 and needs 20; with three cubes it needs 20; 20 is too strong for `Box_8`. He asked whether prefab tuning is still in scope or whether load-based push stays deferred to #14/#46.

## Changes made
- **PR #67 re-review (CHANGES_REQUESTED).**
  - The answer: prefab tuning is in scope (AC #13 says "mass dan handling dapat dituning"). Don't raise the global push strength to 20. A loaded cart climbing the ramp is not an AC.
  - Analysis from `WheelBarrow.prefab`: wheel at z = +0.914, legs at z = -0.25, center of mass at z = 0. The cart rests on those two supports, so the legs carry about 79% of the weight, and they use friction 0.8. A pushed cart isn't lifted, so the legs drag. A rough force table for the 20° ramp (about 37 N empty, 46 N with one cube, 64 N with three) matches all three observations.
  - `Box_8` is 1 kg for a 1.5 m cube, which explains why 20 felt brutal.
  - Must Fix: run an isolation experiment, move the center of mass to z ≈ +0.45, split the leg friction (static 0.6, dynamic 0.3), pick the lowest strength that lets the empty cart climb and set it as the default, revert the stale scene override of `_pushStrength` (1.3), set `Box_8` to 30 kg, report the missing flat-ground, spill, and Console results, and clean up `slop_sederhana_1`.
  - Should Fix: rename `WheelBarrow.prefab` to `Wheelbarrow.prefab`; stop adding history reports per step.
- **PR #67 comment:** AI handoff prompt with 12 ordered steps, a per-case playtest list, and conditions to stop and ask.

## Files affected
- This report only.

## Technical decisions
- The tuning question was treated as a physics diagnosis rather than a push-strength choice. The legs, not the push force, are the likely cause.
- I followed the PR's statement that the owner confirmed deferring controlled descent to VEH-002. I didn't verify that with the owner in this session.

## Verification performed
- Read the current branch: default `_pushStrength` 5, the scene override 1.3 still present, `Box_8` and `slop_sederhana_1` still in the scene, the prefab name unchanged, six history reports, no conflicts with `main`.
- Read the prefab transforms for the wheel, legs, tray, and handles, the Rigidbody values (mass 4, center of mass (0, 0.2, 0), interpolate on), and the material GUIDs.
- Computed the support-point load split and the force table by hand.
- Confirmed on GitHub: the review state and the handoff comment.
- Unity wasn't run. The leg-drag explanation is an estimate that fits Ryo's three observations. It must be confirmed by the isolation experiment in step 1 of the handoff.

## Final result
PR #67 stays blocked, but the remaining work is narrow and has a concrete plan.

## Known limitations
- The proposed values (COM z 0.45, static 0.6, dynamic 0.3) are starting points, not measured results.

## Unresolved issues or follow-up work
- #14 and #46: a target-speed push (bounded force) and a mass convention.
- Whether the owner formally confirmed deferring controlled descent to VEH-002 hasn't been recorded in the issues yet.
