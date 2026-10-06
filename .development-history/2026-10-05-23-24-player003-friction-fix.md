# PLAYER-003 Sack Push Friction Fix

## Task Summary

Address the review report that a 25 kg CementBag sometimes stayed stuck during a sustained push. The measured failure came from the bag's floor friction in the tested CharacterController setup, not from the step-probe height boundary.

## Relevant Previous Context

Issue #73 caps push force at 300 N and expects a 25 kg sack to move slowly while W is held. The review requested repeated three-second pushes with the real prefab in flat, side, and corner orientations, followed by a PlayMode regression test and a full suite run.

## Changes Made

- Lowered the CementBag physics material's static and dynamic friction from 0.75/0.6 to 0.25/0.25; friction combine remains Average.
- Added a PlayMode regression test using the actual CementBag prefab. It repeats flat, side, and side-corner pushes five times each and checks sack movement, player height, and the 2.3 m/s speed limit.
- Removed five screenshot files from the branch. Their prior commit remains in Git history; the Markdown history reports were preserved under the project's append-only history rule.

## Files Affected

- `Assets/Game/Materials/Material/CementBag.physicMaterial`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-04-player003-console-after-tests.jpg` (removed)
- `.development-history/2026-10-04-player003-console-final.jpg` (removed)
- `.development-history/2026-10-04-player003-profiler-idle-summary.jpg` (removed)
- `.development-history/2026-10-04-profiler-idle.jpg` (removed)
- `.development-history/2026-10-05-player003-focused-playmode-results.png` (removed)

## Technical Decisions

The 300 N cap and PlayerMovement step-probe logic remain unchanged. With the original material, the actual flat prefab moved at most about 0.02 m in three seconds despite consuming nearly the full push impulse budget. A zero-friction diagnostic reached the intended target speed, while 0.25/0.25 coefficients produced repeatable slow motion without freezing rotation. The selected material values preserve the existing Average combine rule and avoid changing global physics settings.

## Verification Performed

- Unity 6000.3.25f1 PlayMode regression: passed 15 three-second trials (five each for flat, side, and side-corner orientations).
- Full PlayMode suite at this head: 74 passed, 0 failed.
- `git diff --check`: passed.
- Flat-orientation diagnostics: approximately 1.28 m movement over three seconds; peak speed approximately 0.55 m/s; player stayed at floor height.

## Final Result

The reproduced flat-sack stall is fixed within the issue's speed cap, and the full automated PlayMode suite is green.

## Known Limitations

A usable warmed-up walking/pushing Profiler capture and human Game View feel check were not obtained. The existing idle Profiler images do not count as that evidence. The final PR should keep gameplay/performance acceptance pending until those are recorded.

## Unresolved Issues or Follow-up Work

Capture and attach a warmed-up Profiler/FPS sample while walking into and pushing the sack in Game View. Historical Markdown reports remain in `.development-history` per the project instruction not to overwrite or discard prior reports.
