# Team Lead review of PR #85 (PLAYER-003 human-strength pushing)

## Task summary
Team Lead session. Reviewed RyoFPS's PR #85 at head `854ee38` and added a physics and gameplay analysis to an existing review chain. The PR's review state was already CHANGES_REQUESTED, so this was posted as a COMMENT review.

## Relevant previous context
- Earlier reviews from the owner's account (another session that ran Unity) asked for:
  - per-body dedupe, collision exclusions, and step-probe buffer saturation, all of which were fixed
  - a full suite rerun on `854ee38`
  - repeatable 25 kg sack acceptance
  - a warmed-up movement profiler capture
- No commits since that request.
- `docs/DECISIONS.md` #74: 1 unit = 1 kg, sack 25 kg, human-strength cap of about 250–300 N, the sack must not act as a wall, sack friction about 0.6–0.75.

## Changes made
- Posted a COMMENT review on PR #85:
  - **Approach and logic confirmed:** target-speed push with a 300 N cap, a shared impulse budget, frame-rate consistency, and pure functions tested directly.
  - **Finding 1:** using `CementBag.prefab` values (25 kg, friction 0.75/0.6 with Average combine, 0.18 m tall) and an assumed default floor material, the code's own model gives about 280 N initial force against about 165 N static friction, with a steady speed of about 1 m/s. The sack should always move, so the reported "sometimes stuck or stepped onto" is a defect to diagnose, not acceptance. Candidate causes: the step-probe height threshold flipping at `stepOffset` for a sack on its side, push torque at the contact point, and corner contact rotating the sack.
  - **Finding 2 (after diagnosis):** consider never stepping onto dynamic bodies, which removes the height threshold. Thin objects remain walkable through the slope rule.
  - **Finding 3:** five image files and 12 reports in `.development-history`; attach screenshots to the PR instead.
  - Merge coordination with #78 on `PlayerMovement`, and ordered steps for Ryo's AI.

## Files affected
- This report only.

## Technical decisions
- Posted as COMMENT so the existing CHANGES_REQUESTED stays the decision and the chain isn't duplicated.

## Verification performed
- Read the diff of `PlayerMovement.cs`, `InteractionDetector.cs`, and `Player.prefab`, all earlier review bodies, and issue #73.
- Read the CementBag Rigidbody, collider scale, and physics material values on `main`.
- Unity wasn't run. The friction numbers assume the floor uses the default physics material. The causes in Finding 1 are hypotheses.

## Final result
PR #85 stays at Request changes, with a concrete diagnosis plan for the sack behavior.

## Known limitations
- The steady-speed estimate ignores push torque and contact geometry.

## Unresolved issues or follow-up work
- When #78 and #85 merge, keep both the frame relock guard and the push changes in `PlayerMovement`.

## Re-review at `4390fae` (2026-10-05)
- RyoFPS reproduced the stall with the real CementBag prefab: 0.002–0.004 m in 3 s at friction 0.75/0.6, with the push budget nearly used up. That disproves my 280 N vs 165 N estimate, so the empirical data stands.
- He fixed it by lowering `CementBag.physicMaterial` to 0.25/0.25. He also added a 15-trial regression with the real prefab (flat, side, corner), reported 74/74 on the full suite, and removed the images from the branch.
- Posted CHANGES_REQUESTED:
  - **Must Fix 1:** revert the sack friction. It contradicts DECISIONS #74 (effective friction about 0.6–0.75) and the #73 note not to change sack friction in this ticket, and it affects stacking, wheelbarrow cargo, and scaffold loads.
  - **Experiments to run instead:** first apply the push at center-of-mass height instead of the top-edge contact point (no decision change); if the sack still stalls, raise `_maxPushForce` stepwise (350/400/450 N), which is the decision's designated tuning knob. The TL updates DECISIONS #74 if more than 300 N is needed.
  - **Must Fix 2:** the PR description is stale.
- `DynamicsManager` has no default material, so the floor uses 0.6/0.6 Average, as assumed.

## Re-review at `17cfd79` (2026-10-06)
- RyoFPS ran the requested experiments:
  - restored CementBag friction to 0.75/0.6 (the material diff against `main` is empty)
  - clamped the push point to the center-of-mass height
  - found that at `PushResponseTime` 0.2 s the 25 kg bag's required force (about 280 N) never reached the cap, so he lowered the response time to 0.1 s
  - set `_maxPushForce` to 350 N, the lowest value passing all 15 bag trials (300 N failed the flat trials)
  - suite 74/74; bag peak speed 2.06 m/s
  - tests now read the cap from `Player.prefab`
- Posted a COMMENT review approving the code. Remaining gates:
  - the owner approves 350 N, and the TL updates DECISIONS #74
  - a manual wheelbarrow check in the owner's gameplay review; the 0.1 s response accelerates the 4 kg cart faster, and cargo mass isn't counted in the target speed
- TL decision: the warmed-up Profiler capture doesn't block this PR (one non-allocating capsule cast per moving frame); measure it with the Windows playtest build (#80).
- Nit: a test computes its expected value with a hard-coded 300 N; harmless because the body is 1 kg.
- `git merge-tree` against `main` (now with #78): no conflicts.
