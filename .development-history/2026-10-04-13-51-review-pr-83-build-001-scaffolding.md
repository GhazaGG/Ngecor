# Team Lead review of PR #83 (BUILD-001 scaffolding, DePo4l)

## Task summary
Team Lead session. The owner asked for an especially careful review because DePo4l is junior. Reviewed PR #83 `[BUILD-001] Scaffolding overload breakup prototype` against issue #18, the game design, and the repo rules. Posted a "Request changes" review that explains the reason for every point, and a step-by-step AI handoff prompt that rebuilds the work on a fresh branch.

## Relevant previous context
- Issue #18: a world-placed scaffolding for reaching floor 2 that wobbles and then collapses under overload. Its How to Test says to test in a dev scene. Technical notes cap Rigidbodies at 6 and suggest breaking joints. Re-erecting belongs to #61.
- `docs/DECISIONS.md`: Input System only, contact push (#58), human-strength push (#73/#74), performance budget.
- `AGENTS.md`: risks need a plausible trigger and a way to prevent or recover; avoid failures that remove player agency; don't change the main scene unless the issue says so.

## Changes made
- **PR #83 review (CHANGES_REQUESTED).** The core idea is kept: one Rigidbody while intact, four physical parts after the break (1 to 4 bodies), tunable values. A YAML parse showed that every child except the load probe survives the break.
  - Must Fix:
    1. The vertical ladder climbing mechanic (`LadderClimbable` plus `PlayerMovement` changes) works against the ticket's purpose of carrying material to floor 2. AC only asks for stairs or a ramp in the prefab, and `PlayerMovement` belongs to another system. Replace it with primitive step stairs (step ≤ 0.3 m given `stepOffset` 0.45).
    2. The scaffolding shatters instantly at 150 kg with no wobble. AC says "goyang lalu roboh" and AGENTS asks for preventable risks. Add a cancellable wobble phase with two thresholds and a duration.
    3. The PR changes `Playground.unity` by about 2000 lines, while #18 asks for a dev scene.
    4. Load counting: `OnCollisionStay` isn't sent for sleeping Rigidbodies, so resting cargo stops counting. Stacked cargo and a player standing on cargo aren't counted. Use one overlap probe for all load.
    7. The branch is based on #41 and has files manually copied from `main`, causing 7 conflicts. Its old `PlayerMovement` lacks the #58 contact push and reintroduces `Enable()`, and its Playground lacks newer objects.
    8. `TestScaffoldingWalker` uses `UnityEngine.Input` while `activeInputHandler` is 1 (Input System only), so it throws every frame. It also adds a second player character.
    9. `ScaffoldingPrototypeBuilder` (548 lines) opens, rewrites, and saves `Playground.unity` and prefabs from code.
  - Should Fix: make the scaffolding kinematic while intact (a 65 kg dynamic body may be pushed by the human-strength contact push); the 400 N·s impact threshold can't be reached with game objects; 15 history reports, including an unrelated tooling one.
- **PR #83 comment:** AI handoff with 9 ordered steps:
  - a new branch from `main`, carrying only the verified asset set
  - prefab edits in the Editor
  - load-probe and wobble-phase code changes
  - a dev scene
  - a per-point playtest list
  - a new PR that supersedes #83

## Files affected
- This report only.

## Technical decisions
- Recommended restarting from `main` instead of resolving conflicts. The conflicting files are copies of other systems, and resolving them wrongly would silently drop features.
- The file set to carry over was checked by resolving every GUID referenced by `ScaffoldingModule.prefab`: the wood material, the friction physics material, and `ScaffoldingLoadFailure.cs`.

## Verification performed
- Read `ScaffoldingLoadFailure.cs`, `LadderClimbable.cs`, `TestScaffoldingWalker.cs`, and the outline of `ScaffoldingPrototypeBuilder.cs`.
- Diffed the branch's `PlayerMovement.cs` against `main`.
- Compared GUIDs of the duplicated files with `main`: the Player files share GUIDs but have older content; the folder metas differ.
- Compared scene object sets with `main`.
- Parsed the prefab hierarchy: 36 GameObjects, 1 Rigidbody of 65 kg, 16 BoxColliders, 4 break parts.
- `git merge-tree` against `main`: 7 conflicts.
- Checked `ProjectSettings` `activeInputHandler` (1).
- Confirmed on GitHub: review CHANGES_REQUESTED and the handoff comment exist.
- Unity wasn't run, and the PR author also hasn't run Play Mode. The sleeping-body behavior of `OnCollisionStay` comes from Unity's documented behavior. The kinematic recommendation and the impact numbers are estimates.

## Final result
PR #83 needs rework on a new branch. The core breakup approach is kept.

## Known limitations
- The suggested thresholds (150/190 kg, 2 s) are starting points for tuning.

## Unresolved issues or follow-up work
- Moving material up to floor 2 may rely on rope or pulley (#81); stairs are only for players.
- Issue #18 shows a comment from 2026-10-02 saying the ticket moved to GhazaGG, but the assignee is DePo4l. The owner should confirm who owns it.
