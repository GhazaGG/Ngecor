# Development History: Address TL Re-Review on Head 90d4f08 (Asmdef UGUI, Scene SpawnPoints, True Pinch Auto-Drop Tests, Unified CanReplicate)

## Task Summary
Addressed all Must Fix, Should Fix, and Suggestion review items from Team Leader re-review on head `90d4f08`:
1. **Must Fix 1:** Added `"UnityEngine.UI"` reference to `Ngecor.Multiplayer.asmdef` to ensure clean standalone Roslyn compilation of `SessionHUD.cs` across all build pipelines.
2. **Should Fix 3:** Removed `public static SessionManager Instance` (and Awake/OnDestroy lifecycle hooks) to adhere to AGENTS rule 8 (no global managers/singletons). Refactored `GetSafeSpawnPosition` into an instance method backed by `_spawnPoints` from scene hierarchy and supporting custom candidate overrides. In `Playground.unity`, created `SpawnPoints` GameObject at `(0, 0, 0)` with 4 children clear of `Ramp_Gentle`, and wired them into `SessionManager._spawnPoints`.
3. **Should Fix 4:** Replaced synthetic unit tests in `NetworkPlayerInteractionTests.cs` with real physical pinch simulation tests in `PlayerGrabTests.cs` (`Ngecor.Interaction.Tests`):
   - `CarriedObject_WhenAutoDropDisabled_PushedTooCloseAgainstPlayer_ExceedingPinchDelay_DoesNotDrop`: verifies that with `AutoDropEnabled = false`, pinch exceeding `PinchDropDelay` leaves `IsCarrying == true`.
   - `CarriedObject_WhenAutoDropHandlerConfigured_PushedTooCloseAgainstPlayer_ExceedingPinchDelay_InvokesHandlerExactlyOnce`: verifies that configured `AutoDropHandler` is called exactly once when pinch delay expires.
4. **Suggestion:** Unified replication guard condition into `private bool CanReplicate => IsServer && IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;` in `NetworkPlayerInteraction.cs`.

## Relevant Previous Context
- On head `90d4f08`, PlayMode tests had passed locally, but clean clone Roslyn compilation revealed missing `UnityEngine.UI` in `Ngecor.Multiplayer.asmdef`.
- Static `SessionManager.Instance` was flagged as a singleton violation under AGENTS rule 8.
- Scene spawn points had not yet been populated in `Playground.unity`.

## Changes Made
- `Assets/Game/Scripts/Multiplayer/Ngecor.Multiplayer.asmdef`: Added `"UnityEngine.UI"` to references array.
- `Assets/Game/Scripts/Multiplayer/SessionManager.cs`: Removed static `Instance`. Added candidate-based and instance-based `GetSafeSpawnPosition`.
- `Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs`: Added `CanReplicate` property and updated replication callers.
- `Assets/Game/Scenes/Playground.unity`: Created `SpawnPoints` hierarchy with 4 child transforms at `(2.5, 0.05, 0)`, `(-2.5, 0.05, 0)`, `(0, 0.05, -2.5)`, `(2.5, 0.05, -2.5)` and assigned to `SessionManager._spawnPoints`.
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`: Added 2 physical pinch auto-drop tests.
- `Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs`: Removed synthetic auto-drop tests.
- `Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs`: Updated `GetSafeSpawnPosition` test calls and added custom candidates test.

## Verification Performed
- Built `Ngecor.Player.csproj`, `Ngecor.Interaction.csproj`, `Ngecor.Interaction.Tests.csproj`, `Ngecor.Multiplayer.csproj`, `Ngecor.Multiplayer.Tests.csproj`, `Ngecor.Player.Tests.csproj`: 0 errors.
- Inspected git diffs of code, asmdef, and scene YAML.
- Scene reflects 4 Transform references in `_spawnPoints` under `[Network]`.

## Final Result & Follow-Up
- All code and scene fixes committed and pushed to `feat/net-002-sync-player-movement-interaction`.
- Ready for full Test Runner PlayMode run in Unity and multi-instance manual verification.
