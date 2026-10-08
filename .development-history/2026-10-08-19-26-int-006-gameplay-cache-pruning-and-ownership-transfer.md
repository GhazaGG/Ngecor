# Development History: [INT-006] Gameplay Mesh Cache Eviction & Ownership Transfer Test Fix

## Task Summary
Addressed follow-up feedback from the Team Lead on PR #95:
1. **[P2] Gameplay Mesh Cache Eviction (`InteractionHighlighter.cs`):** Implemented safe automatic eviction (`PruneDeadMeshes`) on gameplay lifecycles (`LateUpdate`, `ClearHighlight`, `GetOrCreateOutlineMesh`, `OnDisable`, `OnDestroy`) to destroy cloned outline meshes when runtime source meshes are destroyed, preventing unbounded native memory growth while strictly preserving meshes still in use by other players.
2. **Refined Two-Player Ownership Transfer Test (`InteractionPromptUITests.cs`):** Updated `TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne` to simulate clean ownership transfer (setting player 1 to non-local when player 2 becomes local) and asserted that exactly 1 player HUD Canvas is active in the scene before and after transition.
3. **Automated Unit Test for Cache Eviction (`InteractionHighlighterTests.cs`):** Added `PruneDeadMeshes_EvictsDestroyedSourceMeshAndDestroysItsClone_WhilePreservingAliveMeshes` proving that destroying a runtime mesh causes its clone to be destroyed and its entry evicted, while persistent meshes used by other players remain completely intact.
4. **Updated PR #95 Metrics:** Updated the PR description with the verified test count: **137 passed, 0 failed, 0 skipped**.

## Relevant Previous Context
- PR #95 fixed the leak when `ClearMeshCache()` was called directly, but in normal gameplay paths, runtime meshes (if created and destroyed repeatedly) would leave dead references in `s_OutlineMeshCache` along with uncollected `HideFlags.DontSave` cloned meshes.
- The previous test `TwoPlayers_...` made player 2 local without making player 1 non-local at the end of the step, leaving both active simultaneously during that specific assertion step.

## Changes Made
1. **`Assets/Game/Scripts/Interaction/InteractionHighlighter.cs`:**
   - Added `s_DeadKeysList` for allocation-free dead key collection.
   - Added `CachedMeshCount` and `IsMeshCached(Mesh source)` public inspection properties.
   - Implemented `PruneDeadMeshes()`: checks for dead source/cloned meshes (`source == null || clone == null`), calls `DestroyClonedMesh(clone)`, removes them from `s_OutlineMeshCache`, with a safety fallback dictionary rebuild if instance ID changes occur.
   - Hooked `PruneDeadMeshes()` into gameplay lifecycles: `LateUpdate()`, `ClearHighlight()`, `GetOrCreateOutlineMesh()`, `OnDisable()`, and `OnDestroy()`.
2. **`Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs`:**
   - Added `PruneDeadMeshes_EvictsDestroyedSourceMeshAndDestroysItsClone_WhilePreservingAliveMeshes` test.
3. **`Assets/Game/Tests/Interaction/InteractionPromptUITests.cs`:**
   - Updated `TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne` to transfer local ownership from player 1 to player 2 and asserted that `CountActiveInteractionCanvases` is exactly 1 before and after the transfer.

## Files Affected
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (Modified)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (Modified)
- `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs` (Modified)
- `.development-history/2026-10-08-19-26-int-006-gameplay-cache-pruning-and-ownership-transfer.md` (New)

## Verification Performed
- Ran the full PlayMode test suite via `unity test . --mode PlayMode`.
- Results: **137 passed, 0 failed, 0 skipped** across all test suites.
- Specifically verified:
  - `PruneDeadMeshes_EvictsDestroyedSourceMeshAndDestroysItsClone_WhilePreservingAliveMeshes`: Passed.
  - `TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne`: Passed.
- `git diff --check`: Passed with zero whitespace warnings on all modified files.

## Final Result
- Outline mesh cache cleanly auto-prunes dead runtime meshes during gameplay without leaking native memory or interrupting persistent meshes.
- Ownership transfer between players is thoroughly proven in tests with exactly 1 active HUD canvas maintained throughout.
- PR #95 description accurately reflects 137 passing tests.
