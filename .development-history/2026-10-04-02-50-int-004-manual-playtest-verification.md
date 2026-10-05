# INT-004 Manual Playtest Verification & PR Readiness

## Task summary
Verified manual playtest in Unity Editor Play Mode for issue #12 ([INT-004] Throw carried object). Confirmed that interactive gameplay feel is smooth, physically responsive, and bug-free. Updated PR #78 checklist and readiness for team review.

## Relevant previous context
- Issue #12 ([INT-004] Throw carried object) implementation and automated PlayMode tests were completed in PR #78 (`feat/int-004-throw-carried-object`).
- Automated tests passed 100% (69/69 green), but interactive Editor GUI verification remained pending developer confirmation.
- Developer conducted manual PlayMode testing in Unity Editor (`Playground.unity`) following the test plan:
  1. Light object throw (`Box_1`, 1 kg): launches far and responsively along the camera look direction.
  2. Heavy object throw (`Box_8` 30 kg / 25 kg cement bag): drops close by naturally due to $v = J / m$.
  3. Running throw: horizontal movement velocity is smoothly inherited by the thrown object.
  4. Cursor re-lock protection: left-clicking to re-lock after `Escape` reliably locks the cursor without triggering an accidental throw.

## Changes made
- Conducted and verified developer manual playtest confirmation: all scenarios verified with zero bugs and smooth feel.
- Updated PR #78 description checklist to mark gameplay feel as verified:
  `- [x] Gameplay feel sudah cukup untuk milestone saat ini (diverifikasi playtest interaktif di Unity Editor oleh developer)`
- Updated verification status in PR #78 documentation.

## Files affected
- `.development-history/2026-10-04-02-50-int-004-manual-playtest-verification.md`

## Technical decisions
- Marked acceptance checklist item for gameplay feel as complete based on developer verification.
- Maintained clean separation of offline physics throw and future multiplayer networking (`NET-*`).

## Verification performed
- Interactive PlayMode test in Unity 6000.3.25f1 (`Playground.unity`):
  - Picked up light object (`Box_1`) and threw with LMB: clean trajectory aligned with crosshair / camera forward.
  - Picked up heavy object and threw with LMB: realistic short drop without jitter or penetration.
  - Threw while moving: smooth forward and lateral velocity inheritance.
  - Unlocked cursor (`Escape`) and clicked LMB: cursor locked back without throwing object. Second click threw object.
- Automated PlayMode tests: 69/69 passed (100% green).
- PR #78 status: ready for review.

## Final result
All acceptance criteria for issue #12 and team review guidelines are satisfied and verified both through automated batchmode tests and interactive Editor playtest. PR #78 is updated and ready for team review.

## Known limitations
- Multiplayer replication is deferred to networking tickets (`NET-002` / `NET-003`).

## Unresolved issues or follow-up work
- Await peer review and approval on PR #78 prior to merge into `main`.
