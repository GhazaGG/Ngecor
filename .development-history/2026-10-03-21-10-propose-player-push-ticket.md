# Propose PLAYER-003 Human-Strength Contact Push; Pause MAT-001

## Task summary
In the Player collision test in Dev_Ghaza, the cement bag stack did not move or topple. The user agreed the cement bag itself behaves correctly, paused MAT-001 at its current state, and asked for a new ticket proposal for player push.

## Findings
- `PlayerMovement.OnControllerColliderHit` applies an impulse of `_moveSpeed * _pushStrength * dt` at `hit.point`. That is a constant ~25 N (5 x 5), independent of mass.
- At the bag's friction (0.9, Maximum combine), sliding one 40 kg bag needs ~353 N, and a 5-bag stack ~1766 N. 25 N cannot move even one bag. Raising `_pushStrength` globally makes light props launch (PR #67: Box_8 at strength 20 felt "brutal").
- The TL review on PR #67 already deferred target-speed / force-limited push to #14/#46.

## Changes made
- None to code or assets. The proposal text was given to the user in chat. No GitHub issue was created.

## State left
- MAT-001 is paused, with work uncommitted on `feat/cement-bag` (CementBag.cs, tests, prefab, physic material, Dev_Ghaza changes, history reports). The friction-to-0.6 suggestion was not applied.

## Follow-up
- The user posts the PLAYER-003 proposal. It should be scheduled after PR #67 merges, because both touch push tuning.
- Resume MAT-001 PR prep: file selection, manual ramp/player checks.
