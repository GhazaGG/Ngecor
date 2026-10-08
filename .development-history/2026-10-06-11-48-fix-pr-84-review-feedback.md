# BUILD-001 PR #84 Review Follow-up

## Task summary

Address the requested changes on PR #84 for scaffolding overload and impact failure, load-query saturation, and the missing development-scene test setup. Preserve the board-only wooden access steps requested by the developer.

## Relevant previous context

- The latest issue #18 and review by GhazaGG identify hard impacts that only wobble without breaking, an undersized non-alloc overlap buffer, repeated parent-component lookups, and a development scene without the scaffolding prefab.
- The project uses a single Rigidbody with primitive child colliders for the intact module. Breakage detaches four configured parts.
- The module prefab already provides six wooden board steps (`AccessStep_1` through `AccessStep_6`); the developer clarified that the access stairs should use boards only.

## Changes made

- A hard impact now remains a failure cause through the wobble interval and breaks the module when that interval ends.
- The serialized impact threshold is 8 N·s, below the PlayerGrab throw impulse of 10 N·s and above the normal player-push impulse observed in PlayerMovement (at most about 0.5 N·s per contact).
- The load query doubles its overlap and scratch buffers and retries when the result fills the buffer, avoiding silent truncation.
- Rigidbody deduplication now precedes the parent-component lookup, so each unique cargo body incurs that lookup once.
- Updated the prefab impact threshold and added the module, three platform bags, a fourth ground bag, and an impact ramp to `Dev_DePo4l` for in-Editor review. The Player starts at the wooden steps. No separate stair material or rail was introduced.

## Files affected

- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`
- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab`
- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity`
- `.development-history/2026-10-06-11-48-fix-pr-84-review-feedback.md`

## Technical decisions

- Keep the existing all-wood board steps; this satisfies the board-only clarification without adding ladder parts or a climb system to this review fix.
- Grow the arrays only after a saturated query. They remain at the larger capacity afterward.
- Keep impact threshold, wobble duration, masses, and overload thresholds Inspector-tunable.

## Verification performed

- Reviewed the latest issue/review context and inspected the throw/push force paths.
- Used Unity Editor CLI to update the prefab and dev scene; confirmed the scene contains the module prefab and the wooden-step access prefab.
- Unity Console reported `compilationFailed: false`; `git diff --check` passed.
- A direct Edit Mode query of the load calculation returned 150 kg for one player plus three bags and 175 kg after adding a fourth bag. This did not run the physics loop and is not a Play Mode stability result.
- Attempted a Play Mode physics simulation, but the Unity pipeline main-thread operation timed out at its 5-second limit. A filtered async `ExecuteThrow` test run remained in `running` state and was cancelled without results. No scaffolding-specific automated tests are present in the discovered test list.

## Final result

The source, prefab setting, and development-scene setup are updated for PR #84. The review findings are addressed in code, but Play Mode evidence is still needed before marking gameplay behavior verified.

## Known limitations

- The actual thrown-object collision, overload collapse, four-part settling, and player traversal over the wooden steps were not verified in a running Play Mode session.
- The 8 N·s impact threshold is based on the existing throw/push configuration; its feel should be confirmed with the real interaction in Play Mode.

## Unresolved issues or follow-up work

- Run the reviewer-requested Play Mode checks in `Dev_DePo4l`: one player plus three bags stable, overload wobble and breakup, a thrown bag triggering breakup, and player ascent over the board steps.
- Request a new technical/gameplay review after these changes reach PR #84.
