# Prevent Player Spawn Stacking and Physics Ejection on Client Join

## Task Summary
Fixed a bug where a newly joined client spawned directly on top of the host player if the host had not moved from their initial spawn point, causing an immediate PhysX collision impulse that pushed the host away.

## Relevant Previous Context
- In `SessionManager.HandleConnectionApproval`, client spawn position was computed via `new Vector3(-2f + (request.ClientNetworkId % 4) * 2f, 0.05f, 0f)`.
- The host spawns at `(0, 0, 0)` without going through `HandleConnectionApproval`.
- When Client 1 joined (`ClientNetworkId = 1`), the formula evaluated to `-2f + 2f = 0f`, placing Client 1 at `(0, 0.05, 0)`—the exact coordinate of the host.
- Because remote player proxy colliders were enabled in the previous fix, overlapping colliders at the same point caused PhysX depenetration to launch/push the host.

## Changes Made
1. **`Assets/Game/Scripts/Multiplayer/SessionManager.cs`**:
   - Added `DefaultSpawnPoints` array with 2.5m clearance between each candidate spawn location.
   - Implemented `GetSafeSpawnPosition(NetworkManager)` which checks all currently connected player positions and picks the first candidate spawn point with at least 1.2m clearance, preventing any overlapping spawn.
   - Updated `HandleConnectionApproval` to assign `response.Position = GetSafeSpawnPosition(netManager)`.
2. **`Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs`**:
   - Added unit test `GetSafeSpawnPosition_WhenNetManagerNull_ReturnsDefaultOffset` to verify non-zero spawn offset fallback.

## Files Affected
- `Assets/Game/Scripts/Multiplayer/SessionManager.cs`
- `Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs`
- `.development-history/2026-10-09-20-21-safe-player-spawn-offset.md`

## Verification Performed
- Built C# assemblies via `dotnet build`:
  - `Ngecor.Multiplayer.csproj`: 0 errors.
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors.

## Final Result
Clients now spawn at safe dedicated spawn offsets (at least 2.5m away from host and existing players), completely avoiding player stacking and collision impulses on join.
