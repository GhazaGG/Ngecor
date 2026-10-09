# Development History: NET-002 PR Re-review Fixes (Grab Line-of-Sight & Remote Proxy Kinematic Rigidbody)

## Task Summary
Addressed Team Lead re-review items on Pull Request [#99: [NET-002] Synchronize player movement and interaction](https://github.com/GhazaGG/Ngecor/pull/99) (Issue [#21](https://github.com/GhazaGG/Ngecor/issues/21)) on branch `feat/net-002-sync-player-movement-interaction`.
Specifically tackled:
- **Must Fix 1 & 2:** Verification of client kinematic body flow in `PlayerGrab` with `NetworkRigidbody`, and test run/two-instance test guidance.
- **Should Fix 3:** Host line-of-sight validation in `ValidateGrabTarget` before accepting grab intents via `RequestGrabServerRpc`, complete with unit tests for blocked and visible targets.
- **Should Fix 4:** Remote player proxy kinematic Rigidbody configuration (`isKinematic = true`, `useGravity = false`, `detectCollisions = !isOwner`) in `NetworkPlayer` to prevent PhysX "moving static collider" penalties without disrupting local player's `CharacterController`.
- Note: Pushing physics objects as client player is explicitly excluded from this task and deferred to NET-003 ([#22](https://github.com/GhazaGG/Ngecor/issues/22)).

## Relevant Previous Context
- Earlier commit `1ba889e` solved:
  - Host authority over carried object transforms (`UpdateCarriedTransform` in `PlayerGrab`).
  - Throw impulse input sanitation (rejection of non-finite/zero directions, unit vector normalization).
  - Restoration of INT-005 drop separation contract (`Physics.IgnoreCollision` until separated).
- TL re-review requested:
  - Line-of-sight raycast from `CameraPivot` to target in `ValidateGrabTarget`.
  - Rigidbody with `isKinematic = true`, `useGravity = false` on `Player.prefab` with collision detection toggled for remote proxies only.
  - Adding `NetworkRigidbody` to interactive boxes in `Playground.unity` via Unity Editor.
  - Unity Test Runner (PlayMode) numbers and two-instance playtesting on the final head.

## Changes Made
1. **CameraPivot Accessor ([`PlayerMovement.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Player/PlayerMovement.cs)):**
   - Exposed `public Transform CameraPivot => _cameraPivot;` with fallback search `transform.Find("CameraPivot")` if unassigned.
2. **Grab Line-of-Sight Validation ([`NetworkPlayerInteraction.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs)):**
   - Implemented `GetEyePosition()` querying `PlayerMovement.CameraPivot.position` (fallback to camera position or eye height offset).
   - In `ValidateGrabTarget()`, added `Physics.SyncTransforms()` followed by `Physics.RaycastAll(eyePos, dir, maxDistance, ~0, QueryTriggerInteraction.Ignore)`.
   - Sorted hits by distance: colliders belonging to the player's own transform hierarchy are ignored. The first non-player collider hit must belong to the target `GrabbableObject` (or child); if hit by any other object (e.g. wall/obstacle), the grab request is rejected.
3. **Remote Proxy Kinematic Rigidbody ([`NetworkPlayer.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs)):**
   - Added `Rigidbody _playerRigidbody` field and ensured presence in `EnsureComponentReferences()`.
   - In `ApplyOwnership(bool isOwner)`, configured:
     - `_playerRigidbody.isKinematic = true;`
     - `_playerRigidbody.useGravity = false;`
     - `_playerRigidbody.detectCollisions = !isOwner;`
     - Local player (`isOwner == true`): collision detection is disabled on the Rigidbody so `CharacterController` maintains full exclusive control over movement and depenetration without PhysX solver conflicts.
     - Remote proxies (`isOwner == false`): `detectCollisions` is active so physics objects interact with the proxy as a moving kinematic body rather than an expensive moving static collider.
4. **Unit & Integration Tests ([`NetworkPlayerInteractionTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs), [`NetworkPlayerTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs)):**
   - Added `ValidateGrabTarget_WhenTargetBehindWall_ReturnsFalse()`: Spawns an obstacle between player eye and target, asserts grab is rejected.
   - Added `ValidateGrabTarget_WhenTargetVisible_ReturnsTrue()`: Asserts direct line-of-sight grab is accepted.
   - Added `ApplyOwnership_ConfiguresRigidbody_KinematicAndCollisionsForRemoteOnly()`: Asserts Rigidbody kinematic, gravity, and collision detection settings for local vs remote player.

## Files Affected
- Modified:
  - [`Assets/Game/Scripts/Player/PlayerMovement.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Player/PlayerMovement.cs)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayer.cs)
  - [`Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs`](file:///D:/coding/Ngecor/Assets/Game/Scripts/Multiplayer/NetworkPlayerInteraction.cs)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerInteractionTests.cs)
  - [`Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs`](file:///D:/coding/Ngecor/Assets/Game/Tests/Multiplayer/NetworkPlayerTests.cs)
- Added:
  - [`.development-history/2026-10-09-23-35-net-002-pr-rereview-fixes.md`](file:///D:/coding/Ngecor/.development-history/2026-10-09-23-35-net-002-pr-rereview-fixes.md)

## Technical Decisions
- **Line-of-Sight Authority on Host:** The server must not trust that the client has an unblocked view of the object. Running `Physics.RaycastAll` with `QueryTriggerInteraction.Ignore` from `CameraPivot` towards target center prevents grabbing through walls or around corners.
- **Kinematic Rigidbody with Ownership-based `detectCollisions`:** Unity CharacterController can have unpredictable physics solver behavior when sharing a GameObject with an active Rigidbody detecting collisions. Setting `detectCollisions = false` when `isOwner == true` eliminates this conflict while providing remote observers with a proper kinematic body.

## Verification Performed
- Built all assemblies using `rtk dotnet build`:
  - `Ngecor.Player.csproj`: 0 errors
  - `Ngecor.Interaction.csproj`: 0 errors
  - `Ngecor.Multiplayer.csproj`: 0 errors
  - `Ngecor.Multiplayer.Tests.csproj`: 0 errors
  - `Assembly-CSharp.csproj`: 0 errors
- Executed `rtk git diff --check`: 0 whitespace issues.

## Known Limitations & Follow-Up Work
- In accordance with AGENTS.md Rule 4 (no text-editing `.unity` and `.prefab` files), component additions for `Playground.unity` (`NetworkRigidbody` on `Box_1`, `Box_3`, `Box_Heavy`) and `Player.prefab` (`Rigidbody` with `isKinematic = true`, `useGravity = false`) must be performed directly in Unity Editor by the developer.
- Unity Test Runner (PlayMode) execution across all assemblies must be executed directly in Unity Editor as per AGENTS.md Rule 10.
- Two-instance manual verification (player movement sync, visual grab/drop/throw near wall, matching box positions in Inspector, and 100-150ms network simulator latency) must be recorded in the PR description.
- Client pushing dynamic physics objects is deferred to NET-003 ([#22](https://github.com/GhazaGG/Ngecor/issues/22)).
