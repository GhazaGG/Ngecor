# Team Lead review of PR #50 (PLAYER-002)

## Task summary
Team Lead session. Reviewed RyoFPS's PR #50 `[PLAYER-002] Add basic camera controller` against the rescoped AC of issue #8. Posted a "Comment" review that asks for one confirmation before approval, and added a note to issue #12 (INT-004 Throw).

## Relevant previous context
- `2026-10-02-02-17-review-pr-48-player-movement.md`: PR #48 was merged as `dc6d4d2`, and #8 was rescoped. Its AC are cursor lock and release, camera height checked in Playground, sensitivity tuning, jitter, and a camera reference for INT-001 that doesn't use `Camera.main`.
- The owner decided that gameplay clips are optional.

## Changes made
- **PR #50 review (COMMENTED, not approved yet).** No Must Fix items. The one open question: the AC "camera height checked in Playground" isn't reported. The prefab `CameraPivot` sits at 1.6 m on a 1.8 m capsule.
- Non-blocking notes in the review:
  - The left click that relocks the cursor is also the Throw (`Attack`) binding.
  - Escape and left click are read straight from `Keyboard.current`/`Mouse.current` instead of through Input Actions.
  - `PlayerMovement` now holds movement, look, cursor, and camera access. The next camera feature should split look and cursor out.
  - A standalone build hasn't been tested.
- **Process note in the review:** one `.development-history` report per task, plus one per review-fix round. PR #50 adds 4 reports and PR #48 added 5.
- **Issue #12 comment:** the click that relocks the cursor must not trigger a throw.

## Files affected
- This report. Everything else was a GitHub review and an issue comment.

## Technical decisions
- I didn't request changes. The code meets the AC, and the only gap is reporting one manual check.
- I didn't ask for a component split yet, because the AC don't need one. Doing it now would be speculative refactoring.

## Verification performed
- Read the diff of `origin/feat/basic-camera-controller` against `origin/main`: `PlayerMovement.cs` +53/-2 and the tests +88.
- Confirmed the branch contains `origin/main`, and that commit `d4809d2` only adds a history report. That supports the PR's claim that the implementation didn't change after the last passing suite.
- Read the `CameraPivot` height (1.6) and the capsule height (1.8) from `Player.prefab` on `main`.
- Confirmed on GitHub that the review state is COMMENTED and that the #12 comment exists.
- Unity wasn't run in this session.

## Final result
PR #50 is waiting for RyoFPS to confirm the camera height. After that, the Team Lead approves it.

## Known limitations
- The manual test results (cursor, sensitivity, ramp smoothness, clean Console) come from RyoFPS's own report.

## Unresolved issues or follow-up work
- #12 has to handle the relock-click conflict when it's implemented.
- Cursor behavior in a standalone build should be checked once playtests run from builds.
