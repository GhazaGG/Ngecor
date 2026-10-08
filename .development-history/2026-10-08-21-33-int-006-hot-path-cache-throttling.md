# Development History: [INT-006] Hot-Path Mesh Cache Throttling & Zero-Scan Lookup

## Task Summary
Addressed feedback on head `1644a79` regarding redundant static cache scanning on rendering hot paths:
1. **Removed Hot-Path Scans (`InteractionHighlighter.cs`):** Eliminated calls to `PruneDeadMeshes()` from hot rendering paths:
   - Removed prune from `GetOrCreateOutlineMesh(source)` (restoring pure O(1) dictionary lookups without scanning the cache per-mesh).
   - Removed prune from `ClearHighlight()`.
   - Removed redundant duplicate prune calls from `OnDisable()` and `OnDestroy()`.
2. **Introduced Throttled Periodic Eviction (`MaybePruneDeadMeshes`):** Scheduled cache pruning to execute at most once every `EvictionIntervalSeconds` (3.0 s) in `LateUpdate()`. Steady-state per-frame overhead is reduced to a single floating-point timestamp check, completely independent of the number of players or submesh count.
3. **Automated Unit & Steady-State Test (`InteractionHighlighterTests.cs`):** Added `HotPath_MultiMeshTargetAndIdle_DoesNotScanCacheEveryFrameOrLookup` verifying that for multi-mesh targets (composite objects with child `MeshFilter` components) and idle frames, steady-state rendering generates zero dictionary scans per-lookup or per-frame.
4. **Updated PR #95 Metrics:** Updated PR body to reflect **138 passed, 0 failed, 0 skipped**.

## Relevant Previous Context
- In commit `1644a79`, `PruneDeadMeshes()` was called per-frame in `LateUpdate()`, on every `ClearHighlight()`, and inside `GetOrCreateOutlineMesh()`. For an interactable with multiple submeshes across multiple players, this resulted in numerous redundant O(N) dictionary scans per frame.

## Changes Made
1. **`Assets/Game/Scripts/Interaction/InteractionHighlighter.cs`:**
   - Added `EvictionIntervalSeconds` (default 3.0 s), `s_LastEvictionTime`, and `PruneInvocationCount`.
   - Added `MaybePruneDeadMeshes(float? timeOverride = null)` which throttles background pruning.
   - Removed `PruneDeadMeshes()` calls from `GetOrCreateOutlineMesh()`, `ClearHighlight()`, `OnDisable()`, and `OnDestroy()`.
   - Replaced `PruneDeadMeshes()` in `LateUpdate()` with `MaybePruneDeadMeshes()`.
   - Reset `s_LastEvictionTime = -1f` and `PruneInvocationCount = 0` in `ClearMeshCache()`.
2. **`Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs`:**
   - Added `HotPath_MultiMeshTargetAndIdle_DoesNotScanCacheEveryFrameOrLookup`.
3. **`scratch/pr_body_updated.md`:**
   - Updated test metrics to 138 total passed tests.

## Verification Performed
- Ran the full PlayMode test suite via `unity test . --mode PlayMode`.
- Results: **138 passed, 0 failed, 0 skipped**.
- Specifically verified:
  - `HotPath_MultiMeshTargetAndIdle_DoesNotScanCacheEveryFrameOrLookup`: Passed.
  - `PruneDeadMeshes_EvictsDestroyedSourceMeshAndDestroysItsClone_WhilePreservingAliveMeshes`: Passed.
  - `TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne`: Passed.
- `git diff --check`: Clean (zero errors).

## Final Result
- Cache lookup in `GetOrCreateOutlineMesh` is strictly O(1) with zero scanning on render paths.
- Background eviction runs at a throttled interval (3.0 s), effectively preventing memory leaks while keeping steady-state frame times minimal.
- All tests pass (138/138).
