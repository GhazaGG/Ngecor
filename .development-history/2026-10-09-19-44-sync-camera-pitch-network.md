# Synchronize Camera Pitch Across Network for Remote Player View and Hold Point

## Task Summary
Synchronized camera pitch (vertical look angle) across the network using a client-owner `NetworkVariable<float>` in `NetworkPlayer` to resolve visual desynchronization when holding objects while looking down or up.

## Relevant Previous Context
- In playtesting, when Player 2 looked straight down at the ground while holding a box, Player 2 correctly saw the box held near their feet with proper obstacle depenetration.
- However, Player 1 (observing Player 2) only saw the box slightly displaced horizontally at eye level rather than held downwards, because `_cameraPivot`'s local rotation was only modified locally by `PlayerMovement` and never replicated to the host or other peers.

## Changes Made
1. **`Assets/Game/Scripts/Player/PlayerMovement.cs`**:
   - Exposed `CameraPitch` property (getter and setter) that reads/writes `_cameraPitch` and updates `_cameraPivot.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f)`.
2. **`Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`**:
   - Added `_networkCameraPitch` (`NetworkVariable<float>` with `Owner` write permission and `Everyone` read permission).
   - In `OnNetworkSpawn()`, initialized non-owners to current `_networkCameraPitch.Value`.
   - In `Update()`:
     - For owner (`IsOwner`): syncs `_networkCameraPitch.Value = currentPitch` whenever pitch changes by more than 0.25 degrees.
     - For non-owners (`!IsOwner`): smoothly lerps `_playerMovement.CameraPitch` towards `_networkCameraPitch.Value` (using factor 25 * Time.deltaTime).

## Files Affected
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`
- `.development-history/2026-10-09-19-44-sync-camera-pitch-network.md`

## Technical Decisions
- **Owner-Authority Pitch NetworkVariable**: Follows project Rule 9 exception ("movement dan arah pandang player milik client itu sendiri"). Pitch is directly driven by the local player's mouse look and replicated to observers.
- **Dynamic HoldPoint Orientation**: Since `HoldPoint` is parented under the camera which is under `CameraPivot`, synchronizing `CameraPitch` to the host allows the host to simulate the carried object's position at the exact vertical orientation of the holding player, perfectly matching the carrier's first-person view.

## Verification Performed
- Built C# assemblies via `dotnet build`:
  - `Ngecor.Multiplayer.csproj`: 0 errors.
  - `Ngecor.Player.csproj`: 0 errors.
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors.
  - `Ngecor.Player.Tests.csproj`: 0 errors.
- Verified working tree cleanliness and staged all changes.

## Final Result
Camera pitch and carried object vertical hold point are now synchronized across network clients and host.
