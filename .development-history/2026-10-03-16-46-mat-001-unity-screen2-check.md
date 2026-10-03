# MAT-001 Unity Screen 2 Check

## Task summary
Continued the MAT-001 cement bag task by opening the development scene in Unity and inspecting the existing physical test setup using only the non-primary display designated as screen 2.

## Relevant previous context
- `2026-10-03-15-16-cement-bag-progress-check.md` recorded the local cement bag prefab, Physic Material, six scene instances, and missing Play Mode acceptance evidence.
- Earlier MAT-001 reports kept puncture/spill behavior outside issue #15 and limited the work to a discrete physical bag.
- The scene, material, prefab, settings, debug log, and unrelated history files were already present as local worktree changes before this continuation; they were preserved.

## Changes made
- No Unity scene, prefab, material, script, or settings asset was edited.
- Temporarily changed the Unity Editor workspace to the built-in Default layout while inspecting the hierarchy, then restored the prior 2 by 3 layout before ending the session. Unity saved the workspace at `UserSettings/Layouts/default-6000.dwlt`.
- Attempted a brief Play Mode observation. The Editor toolbar state changed as focus moved between Orca operations, so the runtime result could not be confirmed.
- Added this development report.

## Files affected
- `.development-history/2026-10-03-16-46-mat-001-unity-screen2-check.md` (added).
- `UserSettings/Layouts/default-6000.dwlt` (Unity workspace layout; ignored by Git).
- No tracked gameplay asset or code file was changed by this continuation.

## Technical decisions
- Kept MAT-001 scoped to the physical bag; puncture, tearing, and loose cement remain outside this issue.
- Kept all Unity window inspection and input on the non-primary display at x=1920..3456. Windows enumerates that display as `DISPLAY1`; it is the user's designated screen 2.
- Did not edit Unity serialized assets outside the Editor and did not touch `Playground.unity`.

## Verification performed
- Confirmed the active branch remains `feat/cement-bag`; the pre-existing modified and untracked worktree paths remained present.
- Opened Unity 6.3.25f1 with `Dev_Ghaza.unity` active. Orca reported the Editor bounds at x=1925..3446, fully inside the designated screen 2.
- Confirmed `UserSettings/Layouts/default-6000.dwlt` is ignored by Git and restored the Editor to its prior 2 by 3 layout at the end of the session.
- Expanded the scene hierarchy and observed six `CementBag` instances and one `Ramp` in `Dev_Ghaza`.
- Captured Editor screenshots before and after the short Play Mode attempt. The objects appeared in a spread-out arrangement; no five-bag stack was established, and no reliable movement, settling, jitter, sleep, or ramp result was observed.
- The Inspector remained too narrow to read the selected object's properties. No acceptance criterion is claimed as passed.

## Final result
MAT-001 remains incomplete and local. The scene has a visible six-bag and ramp test setup, but physical acceptance is still unverified.

## Known limitations
- Orca could enumerate and screenshot the Unity Editor on screen 2, but direct clicks were unsupported and keyboard focus was intermittent.
- The Play Mode state was unclear after focus changes; screenshots did not establish a repeatable physics observation.
- Rigidbody mass, damping, sleep behavior, five-bag stacking, and ramp sliding still need Editor inspection and Play Mode verification.

## Unresolved issues or follow-up work
- Continue using the Unity Editor on screen 2 when reliable controls are available.
- Inspect the prefab's Rigidbody and Physic Material values in Inspector.
- Arrange the five-bag stack and ramp checks in `Dev_Ghaza`, run Play Mode, and record settling, jitter, sleep, contact, and ramp behavior.
- Preserve unrelated worktree changes and do not modify `Playground.unity`.
