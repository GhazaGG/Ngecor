# Verify MAT-005 in interactive Play Mode

## Task summary

Complete the interactive Unity verification for issue #62 and hand PR #64 to the team for review.

## Relevant previous context

`2026-10-02-19-20-implement-mat-005-bulk-container.md` records the implementation and six direct Unity Editor checks. `2026-10-02-19-45-open-mat-005-draft-pr.md` records the draft PR and its pending interactive Play Mode gate. Their claims were checked against the current branch, issue, PR, and Unity scene before this follow-up.

## Changes made

- Opened `Assets/Game/Scenes/Dev/Dev_Ghaza.unity` in Unity 6000.3.25f1 and tested transfer, rejection, and tilt in Play Mode.
- Updated PR #64 with exact reproduction steps and observed results, then marked it ready for team review.
- Stopped Play Mode; temporary Inspector rotation and material changes reverted.

## Files affected

This report only. PR #64's description and draft status changed on GitHub. No gameplay asset, scene, or source file was edited.

## Technical decisions

The previous ordinary headless Test Runner license failure remains disclosed. Its six test methods had passed when invoked directly in Unity Editor batchmode. Local acceptance checks support ready-for-review status; a teammate's technical and gameplay review is still required before merge under `docs/WORKFLOW.md`.

## Verification performed

- In Play Mode, a one-second sand transfer moved 10 units: `Sand Source` 12 to 2, `Mixed Receiver` sand 0 to 10, total 12 to 12.
- A one-second cement transfer moved 6 units: `Cement Source` 6 to 0, `Mixed Receiver` cement 0 to 6, pair total 16 to 16 after the sand transfer.
- After restarting Play Mode, cement transfer to `Sand Only` moved 0 while the source still held 6; source remained 6, target 0, total 6 to 6. The earlier rejection attempt after emptying the source was discarded as inconclusive.
- Rotating the Rigidbody-backed `Sand Source` to 90 degrees in Play Mode drained its 12 units to 0 and visibly reduced its fill level. The Inspector showed the count changing; the Console showed 0 warnings and 0 errors after these interactions.
- After stopping Play Mode, the scene's source returned to 12 units and rotation 0. `git status` showed no tracked file changes. PR readback confirmed it is open, non-draft, unmerged, and has no reviews or CI checks configured.

## Final result

Issue #62 is implemented and locally verified in Unity. PR #64 is ready for team review.

## Known limitations

The ordinary headless Unity Test Runner did not start because its licensing client reported no valid Editor license; direct Editor checks passed instead. Spill units disappear until MAT-004 provides piles, as issue #62 permits. The demo uses simple gray primitive meshes, so final gameplay art and feel were not evaluated.

## Unresolved issues or follow-up work

An independent developer must review the code and play the dev scene, approve PR #64, and merge it according to `docs/WORKFLOW.md`. The issue remains open until that process completes. Existing unrelated untracked history files and `debug.log` were left untouched.
