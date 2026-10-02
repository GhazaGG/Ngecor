# Implement NET-001 Multiplayer Session Prototype

## Task summary
Implemented a 2-player local multiplayer host/join session prototype in the Playground scene using Netcode for GameObjects (NGO) 2.x and Unity Transport (UTP), allowing two players to host, join, auto-spawn on Playground, and handle client disconnection without crashing the host.

## Relevant previous context
- `docs/DECISIONS.md`: NGO 2.x + Unity Transport host mode decided on 2026-10-01; exact versions pinned upon installation in NET-001 (#20).
- Issue #20: Goal to create simple multiplayer session for 2 players on Playground with local testability and graceful disconnect handling.
- `docs/PROJECT_STRUCTURE.md`: All team scripts in `Assets/Game/Scripts/<System>/` and settings in `Assets/Game/Settings/`.
- Dependency PLAYER-001 (#7) is still open, so a prototype player prefab (`Player_Prototype.prefab`) was created as the network spawning baseline.

## Changes made
- **Packages & Decisions:**
  - Added `com.unity.netcode.gameobjects`: `2.13.3` and `com.unity.transport`: `2.7.4` to `Packages/manifest.json`.
  - Pinned exact versions in `docs/DECISIONS.md` and removed the open question from the pre-task table.
  - Relocated NGO's generated `DefaultNetworkPrefabs.asset` to `Assets/Game/Settings/` and locked the path in `ProjectSettings/NetcodeForGameObjects.asset` to keep root `Assets/` clean.
- **Session Logic & HUD:**
  - Created `Assets/Game/Scripts/Multiplayer/SessionManager.cs` (`Ngecor.Multiplayer` namespace) to manage Host/Client session lifecycle, configure transport connection data, handle client connect/disconnect events gracefully, provide command-line argument hooks for automation, and offset player spawn positions via connection approval to prevent physical clipping on spawn.
  - Created `Assets/Game/Scripts/Multiplayer/SessionHUD.cs` offering a lightweight, immediate GUI (`OnGUI`) for IP/Port input, Host, Join, Disconnect buttons, and live connection status.
- **Player Prototype Prefab:**
  - Created `Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab` containing a Capsule collider/mesh, `NetworkObject`, and `NetworkTransform`.
  - Registered as the default player prefab in `NetworkManager`.
- **Playground Scene Integration:**
  - Configured `[Network]` GameObject in `Assets/Game/Scenes/Playground.unity` with `NetworkManager`, `UnityTransport`, `SessionManager`, and `SessionHUD`.
  - Wired `NetworkManager.PlayerPrefab` to `Player_Prototype.prefab` and enabled connection approval.

## Files affected
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `docs/DECISIONS.md`
- `ProjectSettings/NetcodeForGameObjects.asset`
- `Assets/Game/Settings/DefaultNetworkPrefabs.asset`
- `Assets/Game/Settings/DefaultNetworkPrefabs.asset.meta`
- `Assets/Game/Scripts.meta`
- `Assets/Game/Scripts/Multiplayer.meta`
- `Assets/Game/Scripts/Multiplayer/SessionManager.cs`
- `Assets/Game/Scripts/Multiplayer/SessionManager.cs.meta`
- `Assets/Game/Scripts/Multiplayer/SessionHUD.cs`
- `Assets/Game/Scripts/Multiplayer/SessionHUD.cs.meta`
- `Assets/Game/Prefabs.meta`
- `Assets/Game/Prefabs/Multiplayer.meta`
- `Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab`
- `Assets/Game/Prefabs/Multiplayer/Player_Prototype.prefab.meta`
- `Assets/Game/Scenes/Playground.unity`
- `docs/superpowers/plans/2026-10-02-net-001-multiplayer-session-prototype.md`
- `.development-history/2026-10-02-14-15-multiplayer-session-prototype.md` (this report)

## Technical decisions
- Used verified versions `com.unity.netcode.gameobjects: 2.13.3` and `com.unity.transport: 2.7.4` for Unity 6000.3.25f1 LTS.
- Added `ConnectionApprovalCallback` in `SessionManager` to calculate an offset spawn position for connecting clients, preventing rigid overlap or physics catapulting upon spawning on the ground.
- Used lightweight `OnGUI` for `SessionHUD` rather than heavy UI Canvas assets to strictly adhere to the performance budget for M1/M3 prototypes.
- Added CLI parameter handling (`-mode host`, `-mode client`, `-ip`, `-port`, `-autoclose`) to `SessionManager` to enable automated headless/batchmode end-to-end multi-process verification.

## Verification performed
- Batchmode Unity compilation checks for all scripts (`SessionManager.cs`, `SessionHUD.cs`, editor setup scripts) with exit code 0.
- Standalone Windows 64-bit build executed via `BuildPipeline.BuildPlayer` into `Build/Windows/Ngecor.exe` (184 MB, exit code 0).
- Multi-client live end-to-end verification running two concurrent instances on `127.0.0.1:7777`:
  - Instance 1 started as Host (`-mode host -port 7777`). Log confirmed: `Host started on 127.0.0.1:7777` and `Client 0 connected to server`.
  - Instance 2 started as Client (`-mode client -ip 127.0.0.1 -port 7777`). Log confirmed: `Connecting to host at 127.0.0.1:7777...` and `Connected to Host as Client (ID: 1)`.
  - Both players spawned on Playground with assigned network IDs.
  - Instance 2 disconnected after 4 seconds (`Shutting down network session... Disconnected from server`).
  - Host logged `Client 1 disconnected from server` and remained running and healthy without errors or crashing.
- Verified working tree: build artifacts in `Build/` ignored by `.gitignore`, unintended shader settings changes restored.

## Final result
All acceptance criteria of Issue #20 are fulfilled and verified.

## Known limitations
- Player object is currently a prototype capsule (`Player_Prototype.prefab`) with `NetworkTransform` without movement input logic, pending integration with PLAYER-001 (#7).
- External WAN networking / Relay / Lobby is out of scope and not implemented; connection is currently LAN / Direct IP (`127.0.0.1`).

## Unresolved issues or follow-up work
- Once PLAYER-001 (#7: Basic player movement) is implemented and merged, attach player movement & camera control components to `Player_Prototype` (or replace with final Player prefab).
- NET-002: Replicate basic player movement over network.
- NET-003: Latency & physics interaction test across network.
