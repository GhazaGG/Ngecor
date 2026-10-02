# Team Lead review of PR #52 (NET-001) and rescope of NET-001/NET-002

## Task summary
Team Lead session. Reviewed meryzennn's PR #52 `[NET-001] Multiplayer session prototype`. Posted a "Request changes" review and a step-by-step AI handoff prompt with fix instructions. Rescoped issues #20 (NET-001) and #21 (NET-002) after the owner chose option A.

## Relevant previous context
- The #20 dependency note said to start with a capsule in the developer's own dev scene, not in Playground. After PLAYER-001 merged, the placeholder was to be replaced with the Player prefab, and Playground integration was to be coordinated with the lead.
- PR #48 (PLAYER-001) and PR #50 (PLAYER-002) are merged. Playground now holds the local Player prefab with its camera. PR #48 left a known issue for NET-001: Camera and AudioListener don't follow local ownership.
- AGENTS.md rule 7 forbids force pushes. Rule 8 forbids global singletons unless an issue needs one. Rule 11 allows NGO code only in `NET-*` tickets.

## Changes made
- **PR #52 review (CHANGES_REQUESTED).**
  - Must Fix:
    1. Resolve conflicts with `git merge origin/main`, taking `main` for `Assets/Game/Prefabs.meta`, `Assets/Game/Scripts.meta`, and `Playground.unity`.
    2. Before the merge, move `[Network]` through the Editor into `Assets/Game/Scenes/Dev/Dev_Mery.unity`.
    3. Delete `docs/superpowers/`.
  - Should Fix:
    - Remove `SessionManager.Instance`.
    - Remove the unused events, `-autoclose`, and the `StartingHost` state.
    - Make the host listen on `0.0.0.0`.
    - Add `Ngecor.Multiplayer.asmdef`.
    - Update the PR description.
  - Suggestions: cap the session at 4 players in connection approval; show `DisconnectReason` in the HUD; player movement in NET-002 needs owner authority.
- **PR #52 comment:** an AI handoff prompt with ordered steps:
  - move `[Network]` additively in the Editor before merging
  - the exact git commands for the merge and for choosing file versions
  - the code changes
  - creating the asmdef
  - a local build using only the dev scene, without committing `EditorBuildSettings.asset`
  - a two-instance retest

  It also lists the done criteria and the conditions for stopping to ask.
- **Issue #20:** AC changed to spawning capsules in a dev scene, with Playground and `Player.prefab` left unchanged. The host listens on all interfaces. Out of scope now covers Player prefab integration and movement sync.
- **Issue #21:** split into part A, networked player (can start after NET-001), and part B, interaction (after INT-003). New AC:
  - the Player prefab is spawned by the NetworkManager
  - Playground has only one player spawn path
  - only the owner reads input and has an active camera and AudioListener
  - owner-authority NetworkTransform

  `PlayerMovement` stays free of NGO, and a small `Ngecor.Multiplayer` component calls `SetLocalPlayer(IsOwner)`.
- Explanatory comments added to #20 and #21.

## Files affected
- This report. Everything else was GitHub reviews, comments, and issue edits.

## Technical decisions
- **Option A, chosen by the owner:** NET-001 proves only the session flow. Integrating the real player belongs with NET-002's movement-sync AC. Wiring the current Playground into it now would create two player paths per instance, and it would touch Ryo's prefab and the shared scene in an unrelated PR.
- **Merge, not rebase,** because rebasing a pushed branch needs a force push, which AGENTS.md forbids.
- **Folder `.meta` files take `main`'s GUIDs.** Folder GUIDs aren't referenced by assets. `main` already has these folders from #48.
- **The host listen address is `0.0.0.0`.** The M3 connection decision is LAN/direct IP/VPN, and a host listening only on `127.0.0.1` can't accept those connections.

## Verification performed
- Read every changed file in `origin/meryzennn/net-001-multiplayer-session-prototype`:
  - `SessionManager.cs`, `SessionHUD.cs`
  - the prefab component fields
  - `DefaultNetworkPrefabs.asset`, `NetcodeForGameObjects.asset`
  - the manifest and lock diff, the `DECISIONS.md` diff
  - the scene additions
- Ran `git merge-tree --write-tree origin/main <branch>`. Conflicts: `Assets/Game/Prefabs.meta` and `Assets/Game/Scripts.meta` (add/add, different GUIDs) and `Playground.unity`. `docs/DECISIONS.md` merges automatically.
- Confirmed `/[Bb]uild/` is gitignored, so the standalone test build isn't committed.
- Confirmed on GitHub: the review state is CHANGES_REQUESTED, the handoff comment exists, and #20 and #21 have the new bodies.
- Unity wasn't run in this session. Two points are inferred from the code and known UTP behavior: the host listening only on loopback, and the effect of `AuthorityMode: 0`. Neither was observed at runtime.

## Final result
PR #52 is blocked on Must Fix 1–3. NET-001 and NET-002 now have non-overlapping scopes.

## Known limitations
- The loopback-only host finding still has to be confirmed by a LAN/VPN join test after the fix.
- The verified status of NGO 2.13.3 and UTP 2.7.4 in Package Manager was taken from the PR, not checked independently.

## Unresolved issues or follow-up work
- After NET-001 merges, meryzennn can start NET-002 part A. Playground integration has to be coordinated with the lead.
- On a LAN join test, Windows Firewall may block the host port.
