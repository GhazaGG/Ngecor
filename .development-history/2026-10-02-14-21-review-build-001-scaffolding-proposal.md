# Team Lead review of DePo4l's BUILD-001 scaffolding proposal

## Task summary
Team Lead design-discussion session. Collected the game design context, then reviewed DePo4l's implementation proposal for BUILD-001 (#18), "Scaffolding dari Papan, Paku, dan Palu" (a text file shared by the owner, not in the repo). The owner approved every recommendation: rescope #18, move the plank/nail idea to a backlog ticket, and post an AI handoff prompt for DePo4l.

## Relevant previous context
- `2026-10-02-01-17-milestones-priorities-new-tickets-assignments.md`: DePo4l is junior. #18 was kept standalone (blocked only by SETUP-005) so DePo4l doesn't wait on INT.
- `2026-10-02-13-04-review-pr-50-camera-controller.md`: the left click (`Attack`) is both Throw (#12) and the cursor relock.
- `docs/GAME_DESIGN.md`, principle 2: every tool must create a believable new risk. The concept brief lists scaffolding as "harus dirakit, bisa goyang, overload, roboh", which is chaos moment #5.
- The old #18 had no Why and no How to Test, and "dirakit" was undefined.

## Changes made
- **Proposal review (verdict: Revise; scope change rejected).** Must Fix items:
  1. The proposal rewrote the ticket into a general plank + nail + hammer construction system.
  2. It added a second, local-only grab/control system that duplicates INT-001/002 and conflicts with `Attack`. It also assumes a `Jump` that `PlayerMovement` doesn't have.
  3. Nailed joints that never fail add no new risk, which breaks principle 2.
  4. It didn't address jitter from long Rigidbody joint chains under a client-owned `CharacterController`.
- **Issue #18 body rewritten.** It now has Goal, Why with Solves/Creates, AC, Technical Notes, How to Test, Dependency, and Out of Scope.
  - Modular prefabs, each with one Rigidbody and primitive colliders, no joints and no snapping.
  - Modules stack at least 2 high.
  - Uneven bases or heavy off-center cargo make the stack tip over, driven by physics only.
  - A static ramp or stairs gives access, within the 0.45 m step and 50° slope limits.
  - Load comes from cargo only.
- **New issue #51:** BUILD-003 "Plank and nail improvised joints", labeled P3 with no milestone. It's backlog and not Ready, blocked by #10, #11, and #18. Joint `breakForce` is noted as the natural risk.
- **Issue #18 comment:** a structured AI handoff prompt for DePo4l. It asks for a short implementation plan before coding and says to keep the existing local "rangka modular" work.

## Files affected
- This report. Everything else was GitHub issue edits and comments.

## Technical decisions
- Player weight doesn't load the scaffolding in the MVP. `CharacterController` applies no force to Rigidbodies, and making it do so would need host-side work. Cargo load is enough for chaos moment #5.
- Scaffolding is placed in the Editor for now. Moving it with grab becomes an integration ticket after INT-002/003, following the same pattern as MAT-003 #46.
- Each module is a single Rigidbody with no joints. This keeps the body count low for the performance budget and limits the known client-on-host-body jitter risk.

## Verification performed
- Read `origin/main`. `PlayerMovement` has no jump or climb. `Player.prefab` has step offset 0.45, slope limit 50, and height 1.8. `Attack`, `Interact`, and `Jump` exist in `InputSystem_Actions`.
- Confirmed there's no remote branch or PR from DePo4l for #18, so the proposal's "current modular prototype" exists only locally.
- Confirmed the #18 body links #51, that #51 has the P3 label, and that the handoff comment was posted.
- Unity wasn't run. This was a design and proposal review only.

## Final result
#18 is ready to restart with a sharp scope. The plank/nail idea is kept in #51 for after M2.

## Known limitations
- The AC numbers (2 tiers, which cargo counts as "heavy") still need tuning in Play Mode, and the stack's tipping behavior hasn't been tried in Unity yet.

## Unresolved issues or follow-up work
- Review DePo4l's implementation plan, which the handoff requested, before coding starts.
- Integration ticket: moving scaffolding modules with grab, after INT-002/003.
- Possible ticket: player weight loading physics bodies (host-side), if playtests need it.
- Other design gaps are still open: a shared contract for bulk-material quantity (#16/#17/#19/#45), the recipe (gravel/water), cement bag into mixer, how heavy grab feels (#10), and players pushing props.
