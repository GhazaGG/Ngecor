# NET-003: Synchronize core physics objects (Issue #22)

## Task Summary
Implemented the host-authoritative synchronization of the wheelbarrow and carried objects, a client-side push-as-intent path, and RTT/offset measurement in the session HUD. Work is on branch `feat/net-003-sync-core-physics-objects`, based on `main` at `3f03002`. The cement bag part (plan Task 10) is **not done**: it is blocked by MAT-003 (#46), which was still open when the work was done.

## Relevant Previous Context
- NET-002 (#21, PR #99) reported two limitations for NET-003: the wheelbarrow was not synchronized, and carried state travelled by ClientRpc so a late joiner never received it.
- `docs/DECISIONS.md` entries used: "Networking dan batas authority" (host is source of truth, client sends intent, revisit after the NET-003 latency test), "Player mendorong objek fisika lewat kontak" (NET-003 must let client players push physics objects without deciding their position), "Tenaga dorong player dan massa sak semen" (350 N push cap), INT-005 item 6, and VEH-002 leg friction.
- Issue #22 carries a TL comment from the PR #53/#57 review: a client must still be able to push physics objects (intent to the host).
- Checked against the current repo: `Playground.unity` was not changed. `Dev_Mery.unity` already had a NetworkManager, but its Player Prefab was `Player_Prototype.prefab` (a bare capsule), so it had to be pointed at `Player.prefab` to be a usable test scene.

## Changes Made
Commits on the branch (each with its tests where logic is testable):
1. `9e7546b` `PlayerMovement`: `SetRemoteMovementInput` (clamped, NaN/infinity become zero), non-local players can push with the same force law, per-frame push budget also refilled for non-local players. 4 tests.
2. `09f957a` `PlayerGrab`: `BeginInteractionRequestHandler`, `ExecuteBeginInteraction`, `ExecuteEndInteraction`, `HeldInteractable`, `InteractionEnded`, `ExecuteReplicatedGrab` (no distance check, refuses an object held by someone else). 5 tests.
3. `1ac842a` `WheelbarrowInteraction`: split into "simulates the body" (host or offline) and "drives the player" (the owning client). A kinematic client copy follows the handle without forces. On the host the replica colliders of a remote holder are ignored against the wheelbarrow while held. A second player is rejected without a movement lock. 5 tests.
4. `5e0922d` `NetworkPlayerInteraction`: host-written `NetworkVariable<ulong>` mirror of the carried object, applied on clients at spawn and on change with retry. The three Replicate ClientRpcs were removed.
5. `e9278ab` `NetworkPlayerInteraction`/`NetworkPlayer`: hold-interaction routing through the host (ServerRpc begin/end plus a host-written mirror of the held interactable), owner W/A/S/D replicated as an owner-written `NetworkVariable<Vector2>`. `FakeHoldInteractable` test double. 2 tests.
6. `9b39e37` `PlayerMovement.KinematicContactPushHandler` and new `NetworkPlayerPush` (throttled ServerRpc, host-side validation, 0.15 s intent). 7 tests.
7. `a0a755b` `SessionHUD`: RTT per connection and "Carried offset from hold point".
8. `09503ac` Editor work by the developer: `Wheelbarrow.prefab` (NetworkObject, NetworkTransform with Server authority and scale sync off, NetworkRigidbody), `Player.prefab` (`NetworkPlayerPush`), `DefaultNetworkPrefabs.asset` (Unity auto-registered the wheelbarrow), `Dev_Mery.unity` (arena copied from Playground, host-owned `Platform` at the top end of `Ramp_Gentle`, a second wheelbarrow on it, Player Prefab set to `Player.prefab`). Plus a test helper `InstantiateWheelbarrowPrefab` that makes the unspawned body dynamic again.

## Files Affected
- Scripts: `Assets/Game/Scripts/Player/PlayerMovement.cs`, `Interaction/PlayerGrab.cs`, `Vehicle/WheelbarrowInteraction.cs`, `Multiplayer/NetworkPlayer.cs`, `NetworkPlayerInteraction.cs`, `SessionHUD.cs`, new `Multiplayer/NetworkPlayerPush.cs`.
- Tests: `Tests/Player/PlayerMovementTests.cs`, `Tests/Interaction/PlayerGrabTests.cs`, `Tests/Multiplayer/NetworkPlayerInteractionTests.cs`, new `NetworkPlayerPushTests.cs`, new `FakeHoldInteractable.cs` (each with Unity-generated `.meta`).
- Assets: `Prefabs/Vehicle/Wheelbarrow.prefab`, `Prefabs/Player/Player.prefab`, `Settings/DefaultNetworkPrefabs.asset`, `Scenes/Dev/Dev_Mery.unity`.
- Docs: `docs/DECISIONS.md` (proposal entry), this report.
- Not changed: `Playground.unity`, `ProjectSettings/`, `Packages/`, `EditorBuildSettings.asset`.

## Technical Decisions
- Host stays the source of truth. The client sends intent only: grab/drop/throw (NET-002), begin/end hold, contact push, and its own W/A/S/D. Every state the host decides is mirrored through server-written `NetworkVariable`s.
- Push intent is validated on the host (finite values, horizontal direction, contact within 1.5 m horizontally of the player and −0.5 to 2.5 m vertically, body non-kinematic on the host) and applied with the existing 350 N law. Walking into another player sends nothing, because a remote player's replica is a kinematic body without its own `NetworkObject`.
- The wheelbarrow is simulated only where its body is dynamic. A network client's copy is kinematic and only the player follows the handle.
- Scene choice: the developer decided to test in `Dev_Mery` (the issue does not mention the main scene, AGENTS rule 6). `Playground.unity` was not saved.
- No new packages. Netcode API usage is limited to `Assets/Game/Scripts/Multiplayer/`.

## Verification Performed
- **Automated, Unity 6000.3.25f1 batchmode, PlayMode:** baseline on `main` `3f03002` was 242/242. After the last commit the full suite is **265/265** (23 new tests). One existing test, `PlayerMovementTests.MovementBrakeSlowsABodyFasterThanThePushSpeed`, failed once and passed on re-run and when run alone. It waits one frame at a fixed 60 fps, which does not guarantee a 0.02 s physics step, so it is timing-sensitive and not caused by this change.
- **Two Editors on one PC:** host in the project, client in a `git worktree` of the same commit, joined over `127.0.0.1:7777`. RTT shown in the HUD was 7 ms on the host and 9 ms on the client (no simulated latency).
- **Reported by the developer from manual play (not observed by the assistant):** the client joins and the HUD shows RTT; scenarios S3 to S8 from the plan all behaved as expected (client cannot grab an object another player holds, a client that joins late sees the carried object, disconnect while carrying drops it on the host, client contact push moves objects and the wheelbarrow on both peers, client holds the wheelbarrow handles, client stands on the host-owned platform and pushes a wheelbarrow).
- **Carried-object lag:** a client carrying a cube and walking backward sees the cube trail the hold point ("ghost" feel). The developer judged it acceptable as the expected result of host-simulated carried objects. HUD "Carried offset" values seen: 0.01 m on the client and 0.20 m on the host, at about 7 to 9 ms RTT. The 0.20 m host value was not recorded with its context and may include the INT-005 pull-back from obstacles, so it is not a pure network figure.

## Final Result
Offline logic and the main two-instance scenarios work. The acceptance criteria that depend on the cement bag cannot be closed until MAT-003 lands. The latency and platform criteria are only partially measured (see below).

## Known Limitations
- Cement bag: not networked, not tested (plan Task 10 and scenarios C1 to C6). `CementBag.prefab` has no `GrabbableObject` until MAT-003. `ManualMixingSpot` consumes a bag with `Destroy` (MIX-003), which needs checking once bags are networked, and its material amounts are not synchronized.
- No feedback shown to a client whose wheelbarrow request the host rejected, and the "Push" prompt still shows on a wheelbarrow another player holds.
- Carried object trails the hold point on a client by about one RTT plus interpolation, as described above.
- `Wheelbarrow.prefab` is now a network object, but the wheelbarrow instance in `Playground.unity` has no scene-saved network id because Playground was not saved. Hosting Playground may need that scene re-saved.
- `NetworkRigidbody` keeps unspawned bodies kinematic, so a networked prop used without a session (no Start Host) stays frozen.
- Two audio listeners are reported by Unity in `Dev_Mery` (scene `Main Camera` plus the local player camera). Not changed here.
- Unity auto-registers `Player_Prototype.prefab` into `DefaultNetworkPrefabs.asset` when a project imports from scratch, so two Editors can end up with different prefab lists and the host rejects the client with "disconnected by server". Seen in the client worktree and fixed locally by restoring the file. This predates NET-003 and needs a TL decision.

## Unresolved Issues or Follow-Up Work
- Not tested: plan scenarios S1, S2 measurements with numbers, S9, S10, S11; any test at 100 to 150 ms RTT (Debug Simulator); 3 or 4 players; Build and Run; any run in `Playground`.
- Decision needed from the TL on whether `Playground.unity` goes into this PR (minimal change, wheelbarrow only) or is left to NET-004 (#23).
- Authority revisit (client holder simulates the carried object, host validates) should be decided from the measured numbers.
- Plan Task 10 after MAT-003 merges: network `CementBag.prefab`, check kinematic fill behavior, and run C1 to C6.
- Coordinate with RyoFPS (VEH-002) because `WheelbarrowInteraction` was restructured (offline feel code moved, not changed), and with DePo4l (MAT-003/MIX-003) before touching `CementBag.prefab`.
- Decide how to keep the default network prefab list identical across machines.
