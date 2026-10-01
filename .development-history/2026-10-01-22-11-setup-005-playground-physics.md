# SETUP-005 Playground physics content and second code owner

## Task summary
Owner chose to build SETUP-005 (#5) via Unity batchmode instead of a teammate, and to add RyoFPS as a second code owner before removing the admin bypass (#6).

## Relevant previous context
- `2026-10-01-22-05-resolve-setup-001-005-issues.md`: #5 rewritten; #6 pending owner decision.
- `2026-10-01-19-08-root-location-ruleset-codeowners.md`: ruleset and CODEOWNERS origin.

## Changes made
- #5 body: AC changed from "done by non-owner" to "reviewed and tried in Unity 6000.3.25f1 by a developer other than the author"; push test removed from How to Test.
- PR #40 (`chore/codeowners-second-owner`): `@RyoFPS` added to every CODEOWNERS line; `docs/WORKFLOW.md` no longer allows admin self-merge; five pending history reports committed. Review requested from RyoFPS.
- Branch `feat/playground-physics`: `Assets/Game/Scenes/Playground.unity` now contains `Ground` (Plane, scale 5, static), `Ramp` (Cube 4x0.3x8, 20° descending away from camera, static), `Walls` (4 static low cubes on the ground edge), `PhysicsObjects` (3 boxes + 3 balls dropped on the ramp, 1 box + 1 ball stacked on flat ground, all 0.5 scale with Rigidbody). Main Camera moved to (0, 6, -8), rotation X 20°.

## Files affected
- `Assets/Game/Scenes/Playground.unity`
- `.github/CODEOWNERS`, `docs/WORKFLOW.md` (PR #40)
- This report

## Technical decisions
- Scene built with a temporary editor script (`PlaygroundBuilder.cs`) run through `Unity.exe -batchmode -executeMethod`, so the YAML was produced by Unity, not hand-edited. The script, its `.meta`, and the `Scripts/` folder metas were deleted before commit (never committed, GUID not referenced in the scene).
- Walls added after the first simulation showed a ball rolling off the 50 m plane edge.
- Ramp direction flipped and camera raised so rolling objects stay in view.
- Unity reserialized `ProjectSettings/DynamicsManager.asset` (serializedVersion 13→24, no value change) and created `ProjectSettings/SceneTemplateSettings.json`; both reverted to keep the PR out of CODEOWNERS paths.
- Admin bypass not removed yet: removing it before PR #40 merges would leave the CODEOWNERS PR unmergeable.

## Verification performed
- Batchmode exit 0, no compile errors; only log "errors" were Unity licensing token messages.
- `Physics.Simulate` for 10 s (500 × 0.02) in edit mode after saving, not saved: boxes landed on the ramp and settled at y 2.40; balls rolled down and stopped against `Wall_North` at z 24.5, y 0.25; ground stack settled at y 0.25/0.75; no object below y -1.
- Scene YAML: 8 Rigidbody, 1 MeshCollider (Ground), 0 references to the temp script GUID.

## Final result
#5 content exists on `feat/playground-physics`; PR opened for review.

## Known limitations
- Play Mode was not run and nothing was viewed visually; Physics.Simulate in edit mode is not identical to Play Mode.
- Editor Console after opening the scene was not checked.
- No FPS/Profiler numbers.

## Unresolved issues or follow-up work
- A teammate must review #5 in Unity (proves #34 same-version AC).
- After PR #40 merges: remove admin bypass from ruleset "Protect main", then have a non-admin attempt a direct push to `main` to close #6.
