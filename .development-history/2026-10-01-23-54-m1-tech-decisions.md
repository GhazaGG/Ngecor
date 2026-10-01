# Record M1 technical decisions (platform, input, camera, networking, authority)

## Task summary
The owner (Team Lead) asked to discuss the three open technical decisions blocking M1 before sprint planning. Options and trade-offs were presented; the owner chose the recommended option for each. The decisions are recorded in the docs.

## Relevant previous context
- `docs/DECISIONS.md` listed target platform/perspective, input system, and networking package as open. PLAYER-001 (#7) AC requires "input system yang dipilih tim"; PLAYER-002 (#8) needs a perspective; workflow says multiplayer must not be postponed.
- `Packages/manifest.json` already contains `com.unity.inputsystem` 1.20.0 and `com.unity.multiplayer.center` 1.0.1; no netcode package, no Cinemachine.
- AGENTS.md rule 9 said client only sends intent, without addressing player movement.

## Changes made
- `docs/DECISIONS.md`: new log entries "Platform, input, dan kamera" and "Networking dan batas authority" (with revisit conditions); summary table rows added; multiplayer row clarified; open-questions table reduced to what is still open (NGO/UTP exact version, Relay/Lobby, minimum bindings); performance-budget network row links to the decision.
- `AGENTS.md`: rule 9 and the multiplayer implementation rule now state the single exception (owner-client player movement/look) and link the decision.
- `docs/WORKFLOW.md`: multiplayer paragraph states the exception and links the decision.

## Files affected
- `docs/DECISIONS.md`, `AGENTS.md`, `docs/WORKFLOW.md`, this report.

## Technical decisions
- Windows PC, online co-op one PC per player, no split-screen (4 cameras break the iGPU perf budget).
- Input System package (already installed), keyboard + mouse only required; gamepad backlog. Input read only by the locally owned player.
- First-person camera; revisit with vehicle/wheelbarrow-only third-person camera if VEH-002 gameplay review finds pushing awkward.
- Netcode for GameObjects 2.x + Unity Transport, host mode. Exact version pinned when installed in NET-001 (#20), not before. LAN/direct IP (or VPN mesh outside repo) for M3; Relay/Lobby deferred.
- Authority split: host owns physics objects, grab results, materials, project state; owning client owns its player movement/look. M1 interaction separates intent from execution (no framework).
- Known risk recorded as revisit trigger: held objects lagging by RTT, and client-owned players standing on or pushing host-simulated bodies (pickup bed, scaffolding, wheelbarrow) jittering/desyncing. Must be tested in NET-003 (#22).

## Verification performed
- Re-read edited sections; anchors follow GitHub heading slug rules (`#2026-10-01--platform-input-dan-kamera`, `#2026-10-01--networking-dan-batas-authority`). Not rendered on GitHub yet.
- No Unity, package, or project-setting change; nothing to test in Play Mode.

## Final result
Decisions recorded on branch `docs/m1-tech-decisions` (local commit). Not pushed; PR pending owner approval.

## Known limitations
- NGO behaviour claims (no built-in physics prediction, Multiplayer Play Mode for multi-client testing) are from general knowledge, not verified in this project.

## Unresolved issues or follow-up work
- Update issues: #7 (input decided; bindings owner), #8 (first-person), #10 (technical note: separate intent from execution), #20 (package chosen; pin version), #22 (add desync test cases to AC).
- Then: milestones/priorities on issues and rebalancing assignments (WIP, lead bottleneck).
