# Re-review PR #99 — NET-002 at head 90d4f08

## Task summary
Team Lead re-review of PR #99 (`[NET-002] Synchronize player movement and interaction`, meryzennn) after commits `545ec6c` and `90d4f08`. Posted a Request Changes review and an AI handoff prompt.

## Relevant previous context
The previous in-depth review at `04c23fa` requested:
- removing the UGUI dependency from `Ngecor.Player`;
- host-only auto-drop with replication;
- test evidence from the pushed head;
- scene spawn points;
- no ClientRpc during despawn.
Earlier rounds also had test evidence that did not match the pushed head: an uncommitted scene at `a524846`, and 17 claimed tests against 15 on the branch at `04c23fa`.

## Changes made
- Posted the GitHub review (CHANGES_REQUESTED) and the handoff comment on PR #99.
- Added this report. No game code was changed.

## Findings
- **Resolved:**
  - auto-drop is decided on the server for all player instances and disabled on non-server peers; `ReplicateDropClientRpc` skips the host;
  - the despawn RPC was removed;
  - `Ngecor.Player` no longer uses `EventSystem`;
  - default spawns are clear of `Ramp_Gentle`;
  - the Multiplayer test count (13 + 4 = 17) matches the branch.
- **Must Fix 1: compile error.** `SessionHUD.cs` (assembly `Ngecor.Multiplayer`) now uses `UnityEngine.EventSystems.EventSystem`, but `Ngecor.Multiplayer.asmdef` does not reference `UnityEngine.UI`.
- **Must Fix 2:** the reported 180/180 and the two-instance results "at `90d4f08`" cannot come from that commit, which makes three rounds of evidence from an uncommitted working tree. Required from now on: an empty `git status --porcelain` plus `git rev-parse HEAD` posted with the results, preferably from a clean clone.
- **Should Fix 3:**
  - `SessionManager.Instance` is a static singleton (AGENTS rule 8), added only so the static `GetSafeSpawnPosition` can read `_spawnPoints`;
  - `_spawnPoints` is not assigned in `Playground.unity`, so the hard-coded fallback is still used.
- **Should Fix 4:** the two new auto-drop tests are tautological; one invokes the handler directly, the other asserts a property it just set. Replace them with real pinch tests in `PlayerGrabTests`.
- **Suggestion:** collapse the repeated replication guard into one `CanReplicate` property.

## Files affected
- `.development-history/2026-10-10-13-45-rereview-pr-99-compile-check.md`

## Technical decisions
- Treated the compile error as blocking, and verified it by compilation rather than inference, because the developer posted a passing screenshot for the same hash.

## Verification performed
- Read the diff `04c23fa..90d4f08`, the PR body, and the developer's comments.
- Confirmed that no `Managed/UnityEngine/*.dll` module of Unity 6000.3.25f1 contains `IsPointerOverGameObject`; it exists only in `UnityEngine.UI.dll`.
- Extracted `Assets/Game/Scripts/{Player,Interaction,Multiplayer}` from `git archive 90d4f08` into the scratchpad. Compiled each assembly with Unity's bundled Roslyn (`DotNetSdkRoslyn/csc.dll`) using exactly the references in its asmdef:
  - `Ngecor.Player`: OK;
  - `Ngecor.Interaction`: OK;
  - `Ngecor.Multiplayer`: `SessionHUD.cs(24,21)` and `(25,21)` error CS0234 ('EventSystems' does not exist in namespace 'UnityEngine').
- Adding `UnityEngine.UI.dll` reduced the errors to 0.
- Reference DLLs came from `C:/NGECORRR/Ngecor/Library/ScriptAssemblies`. NGO IL post-processing was not run, but it is irrelevant to a name-resolution error.
- Unity Editor and Play Mode were not run.

## Final result
PR #99: CHANGES_REQUESTED, with the handoff prompt posted.

## Known limitations
- The standalone compile does not replace a Unity Test Runner run; it only proves that the pushed head cannot compile.

## Unresolved issues or follow-up work
- Re-review after meryzennn pushes the asmdef fix and posts evidence that includes the matching hash.
- Late-join carry state (`NetworkVariable`) remains a NET-003 follow-up, noted in the PR's Known Issues.
