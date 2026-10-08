# Development History: [INT-006] Address Team Lead Review Feedback on PR #95

## Task Summary
Addressed all 4 review items (1 P1, 3 P2) from the Team Lead's review on PR #95 (`d8e8375`):
1. **[P1] Asset Generation on Domain Reload (`INT006Setup.cs`):** Removed `[InitializeOnLoadMethod]` so the asset generator script never executes automatically on domain reloads, project load, or test runs. Kept only the manual menu item `[MenuItem("Ngecor/INT-006 Setup Assets (Manual)")]`.
2. **[P2] HUD Isolation for Non-Local Players (`InteractionPromptUI.cs`, `PlayerMovement.cs`):** Gated HUD creation and display strictly on local player ownership. Prevented `ScreenSpaceOverlay` Canvas from instantiating in `Awake` for non-local player prefabs (`_isLocalPlayer = false`), handled dynamic transitions via `SetLocalPlayer`, and added an automated 2-player test verifying the non-local player displays zero crosshairs/prompts while the local player displays exactly one.
3. **[P2] Mesh Leak in Outline Cache (`InteractionHighlighter.cs`):** Fixed `ClearMeshCache()` to destroy cloned meshes with `Object.DestroyImmediate` (while keeping source meshes intact). Added `OnDisable()` and `OnDestroy()` to clean up procedural materials and state, and added an automated lifecycle regression test.
4. **[P2] Documentation Attribution & Conventions (`docs/DECISIONS.md`, `docs/PROJECT_STRUCTURE.md`):** Restored the missing INT-005 `Owner/source` line, accurately recorded INT-006's `Owner/source` based on project owner direction, and recorded the convention that `IInteractable` belongs on root GameObjects with solid (non-trigger) colliders, ignoring trigger colliders.

## Relevant Previous Context
- PR #95 implements single plus (`+`) crosshair, inverted-hull edge outline (white, 0.02 m), and interaction prompt text below crosshair.
- In `Player.prefab`, `_isLocalPlayer` defaults to `0` (false), and ownership is assigned at spawn/runtime.
- Unconditional `ScreenSpaceOverlay` Canvas creation in `InteractionPromptUI.Awake()` resulted in non-local players rendering additional crosshairs onto the local screen.
- In `InteractionHighlighter.cs`, `GetOrCreateOutlineMesh` cached instantiated `Mesh` objects with `HideFlags.DontSave`, but `ClearMeshCache` only cleared dictionary references without destroying the native mesh clones.

## Changes Made
1. **`Assets/Game/Scripts/Editor/INT006Setup.cs`:**
   - Removed `[InitializeOnLoadMethod]` and `OnDomainReload()`.
   - Renamed menu item to `[MenuItem("Ngecor/INT-006 Setup Assets (Manual)")]`.
2. **`Assets/Game/Scripts/Player/PlayerMovement.cs`:**
   - Added `public bool IsLocalPlayer => _isLocalPlayer;`.
   - Added `public event System.Action<bool> LocalPlayerChanged;`.
   - Invoked `LocalPlayerChanged` in `SetLocalPlayer(bool isLocalPlayer)`.
3. **`Assets/Game/Scripts/Interaction/InteractionPromptUI.cs`:**
   - Added `using Ngecor.Player;`.
   - Added `IsLocalPlayer` property resolving against `PlayerMovement`.
   - Subscribed to `LocalPlayerChanged` in `Awake()` and unsubscribed in `OnDestroy()`.
   - Avoided Canvas creation during `Awake()` if non-local; deactivated Canvas if serialized.
   - Handled dynamic transition in `OnLocalPlayerChanged`: creates/activates Canvas on `true`, deactivates and clears prompts on `false`.
   - In `LateUpdate()` and `UpdatePrompt()`, early-exited if non-local.
   - Updated `Crosshair` property to return null if `_crosshairRoot` is inactive in hierarchy.
4. **`Assets/Game/Scripts/Interaction/InteractionHighlighter.cs`:**
   - Added `OnDisable()` and `OnDestroy()` for cleanup.
   - In `ClearMeshCache()`, iterated through `s_OutlineMeshCache` and destroyed all cloned meshes (`clonedMesh != sourceMesh`) using `DestroyImmediate` in Editor and `Destroy`/`DestroyImmediate` in runtime.
5. **`Assets/Game/Tests/Interaction/InteractionPromptUITests.cs`:**
   - Added `TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne` test covering both simultaneous non-local + local instances and `SetLocalPlayer(true)` transition.
6. **`Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs`:**
   - Added `ClearMeshCache_DestroysClonedMeshesWithoutDestroyingSourceMesh` test verifying cloned mesh destruction while preserving source mesh.
7. **`docs/DECISIONS.md` & `docs/PROJECT_STRUCTURE.md`:**
   - Restored INT-005 `Owner/source` line.
   - Added interactive object conventions (root placement with non-trigger collider; triggers ignored).
   - Updated INT-006 `Owner/source` attributing direction to project owner.

## Files Affected
- `Assets/Game/Scripts/Editor/INT006Setup.cs` (Modified)
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (Modified)
- `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs` (Modified)
- `Assets/Game/Scripts/Player/PlayerMovement.cs` (Modified)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (Modified)
- `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs` (Modified)
- `docs/DECISIONS.md` (Modified)
- `docs/PROJECT_STRUCTURE.md` (Modified)
- `.development-history/2026-10-08-18-27-int-006-address-lead-review-fixes.md` (New)

## Verification Performed
- Ran full Unity PlayMode test suite using Unity 6000.3.25f1 via `unity test . --mode PlayMode`.
- Results: **136 passed, 0 failed, 0 skipped**.
- Specifically verified:
  - `TwoPlayers_NonLocalPlayerHasNoCrosshairOrPrompt_LocalPlayerHasExactlyOne`: Passed.
  - `ClearMeshCache_DestroysClonedMeshesWithoutDestroyingSourceMesh`: Passed.
- Verified formatting and whitespace integrity with `git diff --check` (0 issues).

## Final Result
- All 4 review items from Team Lead resolved completely.
- Zero asset pollution on domain reloads.
- Non-local players produce zero UI on local screens.
- Outline mesh cache cleanly deallocates cloned meshes without leaking native memory.
- Documentation accurately reflects decisions and conventions.
