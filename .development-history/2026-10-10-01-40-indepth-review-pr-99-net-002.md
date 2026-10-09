# Team Lead in-depth review of PR #99 at `04c23fa` (NET-002)

## Task summary
The owner asked for an in-depth review of meryzennn's PR #99 after commit `04c23fa`. Verified the claimed fixes against the branch, then reviewed the networking code paths end to end. Posted CHANGES_REQUESTED.

## Relevant previous context
- `2026-10-10-00-24-rereview-pr-99-claims-vs-branch.md`: claimed scene and prefab changes were missing from the pushed branch, and test counts didn't match.

## Changes made
- PR #99 review (CHANGES_REQUESTED). Verified as fixed and committed:
  - `NetworkRigidbody` on the three boxes (`AutoUpdateKinematicState` 1, `UseRigidBodyForMotion` 0, server authority)
  - a `RemoteProxy` child (capsule collider plus kinematic Rigidbody) saved in `Player.prefab`, active in the prefab (so `PlayerGrab._playerColliders` includes it), toggled only for remote players
  - Interaction, Material, and Player test counts match the branch
- Must Fix 1: `PlayerMovement` uses `UnityEngine.EventSystems.EventSystem` (UGUI), but `Ngecor.Player.asmdef` references only `Unity.InputSystem`. `Ngecor.Interaction.asmdef` references `UnityEngine.UI` explicitly for the same reason, so this is likely a compile error, which would invalidate the "180/180" report. Suggested removing the UI dependency from `PlayerMovement` and supplying the check through `IsCursorOverUIHandler` from a UI-owning component.
- Must Fix 2: pinch auto-drop runs in `PlayerGrab.LateUpdate` on every peer and calls `RequestDrop()`. Handlers exist only for the owner, so on the host a remote player's pinch runs a local `ExecuteDrop()` without replicating. The owning client stays `IsCarrying` (slowed, unable to grab), and its later drop RPC is rejected. Fix: let the host decide auto-drop through a server-side handler that replicates, and disable auto-drop on non-server peers.
- Must Fix 3: Multiplayer was reported as 17/17, but the branch has 15 tests. Asked for a full suite run on the pushed commit, with a screenshot in a PR comment.
- Should Fix:
  - the hard-coded spawn point (0, 0.05, 2.5) lies inside the `Ramp_Gentle` footprint (x −3..3, z 0.97..8.85); use scene spawn Transforms instead
  - `ReplicateDropClientRpc` is sent from `OnNetworkDespawn`, which is likely rejected by NGO and unnecessary because the client's `OnDisable` drops
- Suggestions: store the carry relation as a server-written `NetworkVariable` (fixes late-join and desync), and note that the static single-subscriber UI hook is a limitation.

## Files affected
- This report only.

## Verification performed
- Read the `04c23fa` diff, the full `NetworkPlayer.cs` and `NetworkPlayerInteraction.cs` (`OnNetworkDespawn`), `PlayerGrab.LateUpdate` and `RequestDrop`, and the `SessionManager`, `SessionHUD`, and `PlayerMovement` diffs against `main`.
- Read all `.asmdef` references on the branch.
- Read the prefab YAML for `RemoteProxy` (active, collider, kinematic Rigidbody), the scene YAML for the box network components, and the `Ramp_Gentle` transform.
- Counted test attributes, including `[TestCase]`.
- Unity wasn't run. The compile error and the RPC-at-despawn error are high-confidence inferences, not observed.

## Final result
PR #99 isn't mergeable yet. The structure is now right, but the compile risk, the auto-drop desync, and the evidence integrity need fixing.

## Unresolved issues or follow-up work
- NET-003 (#22): consider a carry-state `NetworkVariable` for late join, plus client pushing.
