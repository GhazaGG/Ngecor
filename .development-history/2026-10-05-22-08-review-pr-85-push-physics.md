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
