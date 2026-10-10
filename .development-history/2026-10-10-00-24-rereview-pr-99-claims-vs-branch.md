# Team Lead re-review of PR #99 at `a524846` (NET-002)

## Task summary
Re-reviewed meryzennn's PR #99 after commit `a524846` and the author's comment claiming all review points were fixed and verified. Checked the claims against the pushed branch and posted CHANGES_REQUESTED.

## Relevant previous context
- `2026-10-09-21-11-rereview-pr-99-net-002.md`: required `NetworkRigidbody` on networked boxes, Unity Test Runner and two-instance evidence on the final head, host line-of-sight validation, and a kinematic Rigidbody for the remote proxy.

## Changes made
- PR #99 review (CHANGES_REQUESTED):
  - Accepted the new code: host line-of-sight validation in `ValidateGrabTarget` with tests, and kinematic-Rigidbody handling for remote players.
  - Must Fix 1: the branch has **0** `NetworkRigidbody` in `Playground.unity`. The scene was last changed in `ea4ca8b`, before the previous review, while the comment claims the components were added and the host/client positions verified as identical. Likely uncommitted local changes.
  - Must Fix 2: the reported test counts (Multiplayer 15, Interaction 42, Player 10) don't match the branch. `PlayerMovementTests` alone has 33 tests, the Interaction assembly about 75, and Material (about 48) is missing. Asked for the full suite counts per assembly.
  - Must Fix 3: the Rigidbody is added at runtime to the root next to `CharacterController` (not in the prefab, contrary to the comment), with `detectCollisions = false` for the local player. That risks dynamic objects passing through the local player. Suggested a separate `RemoteProxy` child (collider plus kinematic Rigidbody) saved in the prefab, plus a test throwing a box at the host player.
  - Noted that the owner seeing their carried object about one RTT late is an expected consequence of host placement, to record for the NET-003 revisit.

## Files affected
- This report only.

## Verification performed
- Read the `a524846` diff (`NetworkPlayer.cs`, `NetworkPlayerInteraction.cs`, `PlayerMovement.cs`, tests) and its file list (no scene or prefab).
- Grepped the branch `Playground.unity` for `NetworkRigidbody` (0) and `Player.prefab` for Rigidbody blocks (0).
- Read the last commits touching those files.
- Counted `[Test]`/`[UnityTest]` attributes per test file on the branch.
- Unity wasn't run. The Rigidbody plus CharacterController risk is unverified.

## Final result
PR #99 stays at Request changes until the claimed scene changes are committed and the evidence matches the branch.
