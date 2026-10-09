# Development History: NET-002 Must Fix RemoteProxy Separation and Playground Scene Sync

## Task Summary
Addressed Team Lead re-review feedback on PR [#99: [NET-002] Synchronize player movement and interaction](https://github.com/GhazaGG/Ngecor/pull/99) (Issue [#21](https://github.com/GhazaGG/Ngecor/issues/21)) on branch `feat/net-002-sync-player-movement-interaction`:
- **Must Fix 1:** Staged and committed `Playground.unity` containing 3× `NetworkRigidbody` on `Box_1`, `Box_3`, and `Box_Heavy` (ensured `git show HEAD:Assets/Game/Scenes/Playground.unity | grep -c NetworkRigidbody` equals 3).
- **Must Fix 2:** Recorded complete PlayMode test numbers per assembly across the entire project (180 total tests: Interaction 75, Material 55, Player 33, Multiplayer 17).
- **Must Fix 3:** Refactored remote player proxy architecture: eliminated `Rigidbody` from root `Player` GameObject (leaving only `CharacterController`), and isolated `CapsuleCollider` + kinematic `Rigidbody` (`isKinematic = true`, `useGravity = false`) onto a separate child GameObject (`RemoteProxy`). Updated `NetworkPlayer` to toggle `RemoteProxy.SetActive(!isOwner)` and verified that thrown physics objects bounce off the local player's `CharacterController` without penetration.
- **Known Issues:** Documented carried object latency on client (~1 RTT lag and server-interpolated following) as an expected outcome of the approved host-authority placement decision to be revisited in NET-003.

## Relevant Previous Context
- On head `a524846`, TL noted that `Playground.unity` had not been committed with the added `NetworkRigidbody` components.
- TL identified that the previously reported test count omitted full suites (`PlayerMovementTests` alone has 33 tests, `Material` has 55 tests).
- TL advised against placing `Rigidbody` on the same GameObject as `CharacterController` due to depenetration solver risks for local player collision, proposing a dedicated `RemoteProxy` child GameObject instead.

## Changes Made
1. **Scene Synchronization ([`Playground.unity`](file:///D:/coding/Ngecor/Assets/Game/Scenes/Playground.unity)):**
   - Staged `Playground.unity` with `NetworkRigidbody` added to `Box_1`, `Box_3`, and `Box_Heavy`.
2. **Child `RemoteProxy` Separation ([`Player.prefab`](file:///D:/coding/Ngecor/Assets/Game/Prefabs/Player/Player.prefab), [`NetworkPlayer.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs)):**
   - Removed `Rigidbody` and `NetworkRigidbody` from root `Player` GameObject in `Player.prefab`.
   - Created child GameObject `RemoteProxy` in `Player.prefab` with `CapsuleCollider` (radius 0.35, height 1.8, center `(0, 0.9, 0)`) and kinematic `Rigidbody` (`isKinematic = true`, `useGravity = false`).
   - In `NetworkPlayer.cs`, updated references to manage `_remoteProxy`, activating it strictly when `!isOwner` (`_remoteProxy.SetActive(!isOwner)`). Local player keeps only `CharacterController` active.
3. **Unit Tests ([`NetworkPlayerTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs)):**
   - Updated `ApplyOwnership_ConfiguresRemoteProxy_ActiveOnlyForNonOwner` to assert that root player does not have a `Rigidbody`, and that the `RemoteProxy` child GameObject with kinematic Rigidbody and CapsuleCollider is activated only for non-owner clients.
4. **PlayMode Test Suite Verification:**
   - Evaluated all 4 test assemblies:
     - `Ngecor.Interaction.Tests`: 75/75 passed
     - `Ngecor.Material.Tests`: 55/55 passed
     - `Ngecor.Player.Tests`: 33/33 passed
     - `Ngecor.Multiplayer.Tests`: 17/17 passed

## Files Affected
- Modified:
  - [`Assets/Game/Prefabs/Player/Player.prefab`](file:///D:/coding/Ngecor/Assets/Game/Prefabs/Player/Player.prefab)
  - [`Assets/Game/Scenes/Playground.unity`](file:///D:/coding/Ngecor/Assets/Game/Scenes/Playground.unity)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs)
- Added:
  - [`.development-history/2026-10-10-01-26-net-002-must-fix-remote-proxy-and-scene-sync.md`](file:///D:/coding/Ngecor/.development-history/2026-10-10-01-26-net-002-must-fix-remote-proxy-and-scene-sync.md)

## Technical Decisions
- **Clean Separation of Player Movement and Remote PhysX Proxy:** Unity's `CharacterController` and `Rigidbody` on the same GameObject introduce solver ambiguity, especially when `detectCollisions` is toggled. Moving the proxy to a dedicated child GameObject ensures the local player has an unencumbered `CharacterController`, while remote proxies present a proper kinematic body to PhysX.
- **Strict Adherence to Host Authority for Carried Objects:** Client-side delay and following interpolation of held objects is retained without ad-hoc client-side position hacks, aligning with `docs/DECISIONS.md`.

## Verification Performed
- Built all assemblies with `rtk dotnet build`: 0 errors.
- Verified in Unity Play Mode:
  - Thrown box strikes the local player on Host and rebounds without penetrating.
  - Carried objects follow peer representations smoothly.
  - Scene contains 3 `NetworkRigidbody` components.
