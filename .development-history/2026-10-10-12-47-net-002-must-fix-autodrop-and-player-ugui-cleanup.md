# Development History: NET-002 Host Auto-Drop Desync Fix & Player UGUI Cleanup

## Task Summary
Addressed Team Lead in-depth review on head `04c23fa` for PR [#99: [NET-002] Synchronize player movement and interaction](https://github.com/GhazaGG/Ngecor/pull/99) (Issue [#21](https://github.com/GhazaGG/Ngecor/issues/21)) on branch `feat/net-002-sync-player-movement-interaction`.

Specifically resolved:
- **Must Fix 1 (Assembly Decoupling):** Removed `UnityEngine.EventSystems.EventSystem` from `PlayerMovement.cs` so `Ngecor.Player` is completely decoupled from UGUI (`UnityEngine.UI`). Documented `IsCursorOverUIHandler` as a single static delegate and wired pointer-over-UI checks via `SessionHUD.cs`.
- **Must Fix 2 (Host-Authoritative Auto-Drop & Desync Prevention):** Added `AutoDropEnabled` and `AutoDropHandler` to `PlayerGrab.cs`. On server, `NetworkPlayerInteraction` assigns `AutoDropHandler = ExecuteDropAndReplicate` and keeps `AutoDropEnabled = true`; on clients, `AutoDropEnabled = false`. When pinch drop occurs on host for a remote player, host drops the object and broadcasts `ReplicateDropClientRpc`, keeping the owner and all observers fully synchronized. Added unit tests in `NetworkPlayerInteractionTests.cs`.
- **Must Fix 3 (Test Count Alignment):** Verified exact counts across all PlayMode suites on the committed head (Interaction 75, Material 55, Player 33, Multiplayer 19, total 182 tests).
- **Should Fix 4 (Safe Configurable Spawn Points):** Added `[SerializeField] Transform[] _spawnPoints` to `SessionManager.cs` to allow scene-based spawn point configuration. Updated default fallback coordinates away from the `Ramp_Gentle` footprint (`z` placed in safe negative coordinates: `(2.5, 0.05, 0)`, `(-2.5, 0.05, 0)`, `(0, 0.05, -2.5)`, `(2.5, 0.05, -2.5)`).
- **Should Fix 5 (Despawn RPC Cleanup):** Removed `ReplicateDropClientRpc()` from `NetworkPlayerInteraction.OnNetworkDespawn()`. Host drops carried object via `_playerGrab.ExecuteDrop()` without emitting RPCs on an unspawning network object, eliminating console warnings on disconnect.

## Relevant Previous Context
- Head `04c23fa` successfully committed `Playground.unity` with 3× `NetworkRigidbody` and isolated the remote proxy to child `RemoteProxy` on `Player.prefab`.
- TL identified that `PlayerMovement` referenced `EventSystem` without `UnityEngine.UI` in `Ngecor.Player.asmdef`.
- TL revealed that `PlayerGrab.LateUpdate` auto-drop called un-replicated `ExecuteDrop()` on host for remote players, leaving the owner in a permanent desynced carrying state.

## Changes Made
1. **PlayerMovement UGUI Decoupling ([`PlayerMovement.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Player/PlayerMovement.cs)):**
   - Removed `EventSystem` call from `HandleCursorInput()`.
   - Added code documentation on `IsCursorOverUIHandler` regarding single static delegate behavior.
2. **SessionHUD UI Check ([`SessionHUD.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/SessionHUD.cs)):**
   - In `OnEnable()`, updated `IsCursorOverUIHandler` to check both `_isMouseOverHud` and `EventSystem.current.IsPointerOverGameObject()`.
3. **Pinch Auto-Drop Authority ([`PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs), [`NetworkPlayerInteraction.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs)):**
   - Added `AutoDropEnabled` and `AutoDropHandler` to `PlayerGrab`.
   - In `NetworkPlayerInteraction.OnNetworkSpawn()`, server sets `AutoDropEnabled = true` and `AutoDropHandler = ExecuteDropAndReplicate`; client sets `AutoDropEnabled = false` and `AutoDropHandler = null`.
   - In `ExecuteDropAndReplicate()`, drops object on server and calls `ReplicateDropClientRpc()`.
   - In `OnNetworkDespawn()`, removed `ReplicateDropClientRpc()`.
4. **Scene & Fallback Spawn Points ([`SessionManager.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/SessionManager.cs)):**
   - Added `_spawnPoints` Transform array and `Instance` singleton accessor.
   - Replaced candidate spawn point `(0, 0.05, 2.5)` with `(2.5, 0.05, -2.5)` to avoid `Ramp_Gentle` bounds.
5. **Unit Tests ([`NetworkPlayerInteractionTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs)):**
   - Added `AutoDrop_WhenPinchedOnServer_DropsCarriedObjectViaAutoDropHandler`.
   - Added `AutoDrop_WhenDisabledOnClient_PreventsClientSideAutoDrop`.

## Files Affected
- Modified:
  - [`Assets/Game/Scripts/Player/PlayerMovement.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Player/PlayerMovement.cs)
  - [`Assets/Game/Scripts/Interaction/PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs)
  - [`Assets/Game/Scripts/Multiplayer/SessionHUD.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/SessionHUD.cs)
  - [`Assets/Game/Scripts/Multiplayer/SessionManager.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/SessionManager.cs)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs)
- Added:
  - [`.development-history/2026-10-10-12-47-net-002-must-fix-autodrop-and-player-ugui-cleanup.md`](file:///D:/coding/Ngecor/.development-history/2026-10-10-12-47-net-002-must-fix-autodrop-and-player-ugui-cleanup.md)

## Verification Performed
- Built all assemblies with `rtk dotnet build`: 0 errors.
- Ran `rtk git diff --check`: 0 whitespace issues.
- Counted test totals per assembly: Interaction 75, Material 55, Player 33, Multiplayer 19 (182 total).
