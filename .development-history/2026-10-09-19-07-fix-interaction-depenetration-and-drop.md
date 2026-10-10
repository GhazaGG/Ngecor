# Fix Interaction Depenetration, Drop Camera Clipping, and Disconnect HUD

## Task Summary
Resolved three critical issues identified during manual multiplayer playtesting of issue #21 ([NET-002] Synchronize player movement and interaction):
1. **Drop Camera Clipping**: Dropping a box with `E` while moving forward caused the box to clip through the player capsule into the first-person camera.
2. **Held Boxes Collision & Pushback Desync**: When Player 1 and Player 2 collided with carried boxes, obstacle depenetration and pinch mechanics did not trigger for Player 2, and remote players lacked physical colliders.
3. **Clean Disconnect Error Message in HUD**: Disconnecting displayed an internal Netcode `TransportShutdown` error message in red text on the HUD.

## Relevant Previous Context
- PR #99 introduced synchronized grab, drop, and throw RPCs as well as mouse-look interaction handling.
- Player movement uses owner-authority `NetworkTransform`, whereas grabbable objects use server-authority `NetworkTransform`.
- `PlayerGrab` implements obstacle depenetration (`Physics.ComputePenetration`) and pinch detection (`ResolveHoldPosition`).

## Changes Made
1. **`Assets/Game/Scripts/Interaction/PlayerGrab.cs`**:
   - In `DetachObject()`, added a safe forward horizontal displacement check (minimum 0.75m from player center) so dropped objects are released cleanly outside the player capsule.
   - In `FixedUpdate()`, added a 10-frame limit to `SeparatingEntry` so `Physics.IgnoreCollision` cannot become permanently active if the player pushes directly into the dropped object.
   - Refactored `ResolveHoldPosition` to accept `Transform cameraTransform` and `float nearClipPlane = 0.3f` rather than requiring a non-null `Camera` component.
   - In `LateUpdate()` and `AttachObject()`, resolved camera transform falling back to the player's child camera or root transform for remote player instances and on the host server.
   - Ensured `isPinched` variable is properly assigned before ternary evaluation.

2. **`Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`**:
   - Added `CapsuleCollider _proxyCollider` enabled only for remote players (`!isOwner`). This ensures remote player avatars have physical presence in PhysX (height 1.8m, radius 0.35m) so other players and held boxes register collisions and trigger depenetration.

3. **`Assets/Game/Scripts/Multiplayer/SessionHUD.cs`**:
   - Filtered out `netManager.DisconnectReason.Contains("TransportShutdown")` from displaying as a red error label when the session is closed normally.

## Files Affected
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`
- `Assets/Game/Scripts/Multiplayer/SessionHUD.cs`
- `.development-history/2026-10-09-19-07-fix-interaction-depenetration-and-drop.md`

## Technical Decisions
- **Host-Authoritative Depenetration for Carried Objects**: In accordance with `DECISIONS.md` (item 6), held objects are simulated on the host. Because `PlayerMovement.LocalCamera` is null for remote players on the host, `PlayerGrab` was previously bypassing `ResolveHoldPosition` entirely and broadcasting raw `holdPoint.position`. Providing `camTransform` and `nearClip` ensures the host executes full obstacle depenetration and pinch detection for all players before syncing positions via `NetworkTransform`.
- **Proxy Capsule Collider**: Local players use `CharacterController`, while non-owners disable `CharacterController`. Without a proxy collider, remote players were non-physical ghosts in PhysX. A standard `CapsuleCollider` enabled for `!isOwner` provides collision hulls for sweeps and raycasts without interfering with the local `CharacterController`.

## Verification Performed
- Built C# assemblies via `dotnet build`:
  - `Ngecor.Interaction.csproj`: 0 errors.
  - `Ngecor.Multiplayer.csproj`: 0 errors.
  - `Ngecor.Interaction.Tests.csproj`: 0 errors.
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors.
- Verified working tree clean with only target script modifications.
- Editor playtesting deferred to developer per request (*"gw aja yang test manual bro jadinya lu cuma cek code aja"*).

## Final Result
Code compilation succeeded without errors. Core interaction physics desync and camera clipping fixes are staged and ready for manual playtesting.

## Known Limitations & Follow-Up Work
- Camera pitch synchronization across the network currently relies on root yaw rotation; full remote pitch replication will be evaluated in future tickets if vertical aiming indicators are needed.
