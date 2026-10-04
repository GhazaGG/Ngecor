# PLAYER-003 Manual Game View and Profiler QA

## Task summary

Attempted the requested current-head Game View and Profiler QA on `feat/player-003-human-strength` at `268af23`, using the `Playground` scene in Unity 6.3 LTS (`6000.3.25f1`). The Game View was set to Full HD (1920x1080). Play Mode was stopped at the end, and no scene changes were saved.

## Relevant previous context

The linked QA evidence report identified manual force/stack/traversal/cart checks and profiling as outstanding. The working tree already contained user changes before this session, including `Assets/Game/Scenes/Playground.unity`; those changes were left intact.

## Changes made

- Added this QA report and two evidence screenshots under `.development-history/`.
- No gameplay code, project settings, scene, prefab, or asset was edited or saved during this QA attempt.

## Files affected

- `.development-history/2026-10-04-19-42-player003-manual-gameview-profiler.md` — this report.
- `.development-history/2026-10-04-player003-profiler-idle-summary.jpg` — Profiler idle capture summary.
- `.development-history/2026-10-04-player003-console-final.jpg` — final Console view.

## Technical decisions

- Preserved all pre-existing dirty and untracked files; did not revert or overwrite the modified `Playground` scene.
- Did not treat brief key presses as equivalent to holding W for the requested durations. The computer input interface exposed only a momentary key press, so the physical push and traversal acceptance cases could not be evaluated reliably.
- Kept Deep Profile disabled for timing observations.

## Verification performed

- Confirmed the checked-out branch and commit matched the requested PR head: `feat/player-003-human-strength` at `268af23`.
- Opened `Playground`, selected the Full HD (1920x1080) Game View setting, and opened Unity Profiler in Play Mode.
- Captured one idle-only Profiler Highlights summary. It contains 7,024 frames; the nominal capture span at 60 FPS is about 117 seconds, longer than the requested 10 seconds and not a controlled benchmark interval.
- Profiler Highlights reported CPU over-target frames at 1% and GPU over-target frames at 0%. Frame-time summary: median 7.908 ms, minimum 6.440 ms, maximum 78.432 ms. The longest selected frame was dominated by `EditorLoop` at 75.755 ms, so that peak is Editor/Profiler activity and not a gameplay frame cost.
- In the selected outlier frame, the Profiler showed `PlayerLoop` at 2.40 ms and `ScriptRunBehaviourUpdate` at 0.10 ms. These are combined measurements; no separate `PlayerMovement.Update` or capsule-cast sample was visible. Do not interpret 0.10 ms as the movement method or sweep cost.
- Profiler allocation summary showed 22,774 allocation events across the capture; the highest observed frame had 137 events and 4.8 KB at frame 6656.
- The dedicated GPU Usage module showed a warning and could not be enabled. The saved image is the Profiler Highlights overview, which includes CPU/GPU bottleneck indicators, not a dedicated GPU Usage timeline screenshot.
- Final Unity Console counters showed 0 logs, 0 warnings, and 0 errors.
- Stopped Play Mode without saving the scene.

## Final result

Only the idle baseline and final Console state were observed. Profiler Highlights stayed within the 16.67 ms target for its median and indicated 1% CPU / 0% GPU over-target frames, but the long capture and EditorLoop spike prevent treating this as a clean 10-second performance result. The requested object-push, traversal, and cart feel checks remain unverified.

## Known limitations

- The computer input API available in this session cannot continuously hold W for 2–5 seconds. As a result, cases 1–5 were not performed; no claims about prop speed, bag friction/stack stability, traversal feel, or cart load/ramp behavior are made.
- The walking-flat and pushing-low-object Profiler captures were not obtained. There are no current-head measurements for `PlayerMovement.Update` or its per-frame sweep under those conditions.
- The GPU Usage module warning prevented inspection of dedicated GPU timeline samples. The computer used for this run was not identified as the team's weakest laptop.
- No video was captured; the screenshots and notes document the partial evidence.

## Unresolved issues or follow-up work

- Complete Game View cases 1–5 with held movement input, resetting Play Mode between cases and never saving Play Mode scene changes.
- Capture separate warmed 10-second idle, flat-walk, and low-object-push Profiler intervals with CPU and GPU data on the target weakest laptop. Re-check why the GPU Usage module is unavailable.
- Repeat the final Console check after those gameplay cases.

## Evidence

![Profiler idle CPU and GPU summary](2026-10-04-player003-profiler-idle-summary.jpg)

![Final Unity Console counters](2026-10-04-player003-console-final.jpg)
