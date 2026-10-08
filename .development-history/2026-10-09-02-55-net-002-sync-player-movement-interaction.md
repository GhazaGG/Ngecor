# Development History: NET-002 Synchronize Player Movement and Interaction

## Task Summary
Implemented GitHub Issue [#21: [NET-002] Synchronize player movement and interaction](https://github.com/GhazaGG/Ngecor/issues/21) on branch `feat/net-002-sync-player-movement-interaction`.
The goal of this task is to synchronize player movement using owner-authority `NetworkTransform`, implement a single dynamic player spawning path via `NetworkManager` in `Playground.unity`, and synchronize physical object interactions (`GrabbableObject`) using host-authoritative RPCs while maintaining strict offline modularity for player scripts.

## Relevant Previous Context
- Issue #21 defined two main acceptance criteria:
  - **AC A: Synchronized player movement & ownership:** Owner moves/looks with local camera and audio listener enabled; non-owners disable camera, audio listener, and `CharacterController`. Movement synced via `NetworkTransform` with Owner Authority.
  - **AC B: Host-authoritative interaction:** Grab and drop intents sent to host via ServerRpc; host validates proximity and availability, then replicates to observers via ClientRpc; carried objects follow holder across peers and drop cleanly upon despawn/disconnect.
- Previous commit `193778c` (NET-001) added basic NGO session management and a temporary `Player_Prototype.prefab`.

## Changes Made
1. **Interaction Delegation Hooks ([`PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs)):**
   - Added `GrabRequestHandler` and `DropRequestHandler` delegate hooks to `PlayerGrab` to allow external networking systems to intercept grab and drop intents without coupling `PlayerGrab` to NGO.
   - Updated `LateUpdate()` and `ResolveHoldPoint()` in `PlayerGrab` to gracefully resolve and update object hold positions for non-local player representations (allowing remote players to hold objects visibly on host and observer clients without requiring an active local camera).
2. **Multiplayer Test Assembly ([`Ngecor.Multiplayer.Tests.asmdef`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/Ngecor.Multiplayer.Tests.asmdef)):**
   - Configured PlayMode test assembly for multiplayer tests with references to `Ngecor.Multiplayer`, `Ngecor.Player`, `Ngecor.Interaction`, `Unity.Netcode.Runtime`, and NUnit.
3. **Player Ownership Bridge ([`NetworkPlayer.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs)):**
   - Added `NetworkPlayer` (`NetworkBehaviour`) in `Assets/Game/Scripts/Multiplayer/`.
   - On `OnNetworkSpawn()`, sets `PlayerMovement.SetLocalPlayer(IsOwner)`.
   - If not owner (`!IsOwner`), disables `CharacterController`, `Camera`, and `AudioListener` so only the local client controls inputs, camera rendering, and audio.
4. **Host-Authoritative Interaction Synchronization ([`NetworkPlayerInteraction.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs)):**
   - Implemented `NetworkPlayerInteraction` (`NetworkBehaviour`) hooking into `PlayerGrab.GrabRequestHandler` and `PlayerGrab.DropRequestHandler`.
   - For local owner (`IsOwner`): intercept grab requests, locate target's `NetworkObject`, and invoke `RequestGrabServerRpc`. Intercept drop requests and invoke `RequestDropServerRpc`.
   - On Server/Host: validates distance (`MaxGrabDistance`) and ensures target is not already held. Calls `_playerGrab.ExecuteGrab(target)` and replicates via `ReplicateGrabClientRpc`.
   - On observer clients: `ReplicateGrabClientRpc` and `ReplicateDropClientRpc` update visual carry state.
   - On server disconnect/despawn (`OnNetworkDespawn`), automatically drops any carried object to return it to the physical world.
5. **Asset & Prefab Configuration ([`Player.prefab`](file:///D:/coding/Ngecor/Assets/Game/Prefabs/Player/Player.prefab), [`DefaultNetworkPrefabs.asset`](file:///D:/coding/Ngecor/Assets/Game/Settings/DefaultNetworkPrefabs.asset)):**
   - Configured `Player.prefab` with `NetworkObject`, `NetworkTransform` (`AuthorityMode = Owner`), `NetworkPlayer`, and `NetworkPlayerInteraction`.
   - Set default `_isLocalPlayer = false` in the prefab.
   - Updated `DefaultNetworkPrefabs.asset` to register `Player.prefab`.
6. **Playground Scene Integration ([`Playground.unity`](file:///D:/coding/Ngecor/Assets/Game/Scenes/Playground.unity)):**
   - Removed the statically placed `Player` GameObject from `Playground.unity`.
   - Created GameObject `[Network]` with `UnityTransport`, `NetworkManager`, `SessionManager`, and `SessionHUD`.
   - Wired `NetworkManager.NetworkConfig.PlayerPrefab` to `Player.prefab` and registered `DefaultNetworkPrefabs.asset`.
   - Added `NetworkObject` and `NetworkTransform` (Server authority) to scene grabbable objects (`Box_1`, `Box_3`, `Box_Heavy`).

## Files Affected
- Modified:
  - [`Assets/Game/Scripts/Interaction/PlayerGrab.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Interaction/PlayerGrab.cs)
  - [`Assets/Game/Scripts/Multiplayer/Ngecor.Multiplayer.asmdef`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/Ngecor.Multiplayer.asmdef)
  - [`Assets/Game/Prefabs/Player/Player.prefab`](file:///D:/coding/Ngecor/Assets/Game/Prefabs/Player/Player.prefab)
  - [`Assets/Game/Settings/DefaultNetworkPrefabs.asset`](file:///D:/coding/Ngecor/Assets/Game/Settings/DefaultNetworkPrefabs.asset)
  - [`Assets/Game/Scenes/Playground.unity`](file:///D:/coding/Ngecor/Assets/Game/Scenes/Playground.unity)
  - [`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Interaction/PlayerGrabTests.cs)
- Added:
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs.meta`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs.meta)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs.meta`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs.meta)
  - [`Assets/Game/Tests/Multiplayer/Ngecor.Multiplayer.Tests.asmdef`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/Ngecor.Multiplayer.Tests.asmdef)
  - [`Assets/Game/Tests/Multiplayer/Ngecor.Multiplayer.Tests.asmdef.meta`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/Ngecor.Multiplayer.Tests.asmdef.meta)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs.meta`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs.meta)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs.meta`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs.meta)

## Technical Decisions
- **Owner-Authority Movement vs Host-Authoritative World:** Player movement uses owner authority via `NetworkTransform` to provide responsive zero-latency local controls and smooth interpolation. Interactive world props retain server authority so physical state cannot be forged or desynchronized by clients.
- **Hook-based Delegation:** `PlayerGrab` delegates `RequestGrab` and `RequestDrop` via delegate callbacks rather than hardcoding NGO dependencies into `Ngecor.Interaction`. This preserves offline modularity and testability.
- **Unity Batchmode Automation for Assets:** Modifications to `.prefab` and `.unity` scenes were strictly executed via temporary Unity Editor batchmode scripts (`PrefabUtility` and `EditorSceneManager`), preserving all metadata and GUID integrity without touching serialized files with raw text editor tools.

## Verification Performed
- **Automated Tests:**
  - `Ngecor.Interaction.Tests.PlayerGrabTests`: 42/42 tests passed.
  - `Ngecor.Multiplayer.Tests.NetworkPlayerTests`: 2/2 tests passed.
  - `Ngecor.Multiplayer.Tests.NetworkPlayerInteractionTests`: 4/4 tests passed.
- **Scene & Prefab Validation:**
  - Verified `DefaultNetworkPrefabs.asset` contains clean entry for `Player.prefab`.
  - Verified `Playground.unity` has static `Player` removed and `[Network]` GameObject configured.
  - Verified `Box_1`, `Box_3`, and `Box_Heavy` in `Playground.unity` contain `NetworkObject` and `NetworkTransform`.

## Known Limitations & What Needs Manual Verification
Per user request, manual Play Mode / multi-instance testing will be performed directly by the developer in Unity:
1. Open `Assets/Game/Scenes/Playground.unity` in Unity Editor and enter Play Mode.
2. Click "Host" on `SessionHUD`: verify local player spawns dynamically, input works, camera moves, and audio listener is active.
3. In a second instance / build, click "Client" to connect to `127.0.0.1:7777`:
   - Verify both players see each other moving and rotating.
   - Verify non-owner does not steal camera view or audio listener.
   - Verify grabbing `Box_1` syncs between players and host prevents simultaneous grabs.
   - Verify dropping `Box_1` leaves the box in place for all players.
