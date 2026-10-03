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

## Third review (same session)
- RyoFPS pushed `d401357`, which changed several things at once: wheel position, leg height and width, handle length, a new empty `PushBar` object, and the center of mass (z 0.45, now saved in the prefab). It added a seventh history report. He had also asked whether the local wheel position (z 1.422) or the reviewed one (z 0.914) should stay, and I answered with a PR comment: restore 0.914, because the wheel at 1.422 sticks about 0.55 m out in front of the tray, and the center of mass would otherwise have to sit near the tray front.
- Computed from the prefab: leg bottoms went from +0.05 (17.5 cm above the wheel bottom at -0.125) to -0.455 (23 cm below the wheel bottom at -0.225). The cart now stands on its legs with the wheel lifted. That fits the reported wobble on the ramp, the sideways tipping, and the cargo spills.
- Posted a third CHANGES_REQUESTED review. Must Fix: restore leg height (legs at least 0.15 m above the wheel bottom) and the wheel at z 0.914; remove or explain the empty `PushBar`, revert the stale `_pushStrength` 1.3 scene override, rename the asset file to `Wheelbarrow.prefab`, and resolve `slop_sederhana_1`.
- Proposed a way to stop the tuning loop: turn `slop_sederhana_1` into a gentle ramp of about 10° for the acceptance run, keep the 20° ramp as a stress test, and fix the done-definition (empty cart on flat ground, empty cart up the gentle ramp at the default strength, three cubes stable on flat ground, spill when tipped, clean Console). Change one parameter per trial and log each trial in the PR instead of in new reports.
- Accepted friction 0.8/0.8 for the feet, since his data showed it was more stable.
- Unity wasn't run; the geometry numbers come from the committed prefab. The 10° ramp idea and the strength range 5 to 6 are estimates.

## Fourth review (same session)
- RyoFPS pushed three more commits. Verified in the committed prefab and scene:
  - wheel at y 0.1, z 0.914; legs at y 0.3 with scale y 0.5 and a track of ±0.65; handles raised to y 0.9; center of mass (0, 0.2, 0.45); mass 4; interpolation on
  - `PushBar` removed, the stale `_pushStrength` 1.3 scene override removed, the asset renamed to `Wheelbarrow.prefab`, `slop_sederhana_1` removed
  - friction 0.8/0.8 on the feet, as his data favored
- The original 20° `Ramp` was turned into `Ramp_Gentle` (about 10°, scale (6, 0.3, 8), low end flush with the floor), instead of keeping the 20° ramp and making `slop_sederhana_1` a gentle one as I had suggested. I accepted that for AC #13 but required it to be documented.
- Reported result: the empty cart reaches the ramp top at strength 5; the loaded cart doesn't, which he and I consider acceptable as a bonus.
- Posted a fourth CHANGES_REQUESTED review with only reporting items: the PR description is stale (old center of mass, "pending" items, 33/33, no checked boxes), results on the final geometry for flat ground, three cubes, spill, and Console are missing, `Box_8` is still 1 kg (and moved again), and there are now 8 history reports. I said I'd approve without another code round once the description and results are in.
- Unity wasn't run; geometry values come from the committed files, results from the author's report.

## Approval (same session)
- RyoFPS updated the PR description (final geometry, the `Ramp_Gentle` change, `Box_8`, per-case results, `Closes #13`) and pushed `9d7e030`, which only sets `Box_8` to 30 kg. `git merge-tree` showed no conflicts with `main`, which now contains INT-002.
- Approved PR #67; GitHub shows APPROVED/CLEAN.
- Non-blocking notes: the PlayMode suite wasn't rerun after the last sync, the gameplay results are developer-reported so the owner should play the wheelbarrow once before closing #13, and #14/#46 should handle load-dependent pushing.
