# Milestones, priorities, new tickets, reassignment, NGO scope rule

## Task summary
Team Lead session. Set up GitHub milestones and priorities for every issue, filled ticket gaps found in the dependency graph, split MAT-001 so it no longer blocks on other systems, reassigned work by system and skill, and added an AGENTS.md rule that keeps NGO code inside NET tickets.

## Relevant previous context
- `2026-10-02-00-11-merge-42-ruleset-bypass-issue-updates.md`: the M1 decisions are on `main`.
- `docs/PROJECT_MANAGEMENT.md` defines the milestone names, P0–P3, and the rule that if every task is P1 the priorities are wrong.
- Owner input: DePo4l is junior, and meryzennn is the strongest at networking and architecture. RyoFPS is developing #7/#8, and the owner has told him only that the game is first-person.

## Changes made
- **Milestones created:** M0 Foundation, M1 Player Playground, M2 Construction Toys, M3 Multiplayer Playground, M4 Core Loop, M5 Vertical Slice, each with an outcome description.
- **Milestone assignment:**
  - M0: all closed SETUP issues and EPIC-001
  - M1: #7–#12
  - M2: #13–#18, #44, #46
  - M3: #20–#23
  - M4: #19, #24, #25, #45
  - M5: #26–#28
  - Epics #29–#33 are labeled `epic`.
- **Priority** means importance within the issue's own milestone. P2: #12 (throw), #16 (sand/bucket), #25 (result screen). All other tasks are P1.
- **New issues:**
  - #44 VEH-003 Parked pickup cargo bed: physical cargo, a handbrake that fails under overload on a slope, recoverable by chocking the wheels or unloading. Driving is out of scope.
  - #45 MIX-002 Concrete freshness and hardening: deterministic per-batch working time. Hardened concrete can't be poured, and the container can be emptied and reused.
  - #46 MAT-003 Cement bag in the grab + wheelbarrow loop.
- **#15 MAT-001** is now a standalone physics cement bag, blocked only by SETUP-005. Grab and wheelbarrow integration moved to #46.
- **Dependency links updated:** #19 now blocks #45. #26 is now blocked by #44 and #45. EPIC-003 #31 lists #44–#46. #20 NET-001 is no longer blocked by PLAYER-001: it starts with a capsule placeholder in the developer's own dev scene.
- **Assignees:**
  - RyoFPS: #7, #8, #13, #14, #44
  - meryzennn: #9, #10, #11, #20, #21, #22
  - DePo4l: #15, #16, #18, #46
  - GhazaGG: #17, #19, #24, #25, #45
  - Unassigned: #12, #23, #26, #27
- **`AGENTS.md`:** added hard rule 11. No NGO/networking code outside `NET-*` tickets, unless a ticket explicitly asks for it.

## Files affected
- `AGENTS.md`, this report. Everything else was GitHub issues and milestones.

## Technical decisions
- The MVP pickup is parked, not driven. The vertical-slice flow (Unload → Setup → Mix → Transport → Weather) needs no driving, and chaos moment #1 can still happen through a handbrake overload. Driving goes to the backlog.
- Concrete freshness was added because the slice objective depends on it. Without it, rain is the only pressure in the MVP.
- meryzennn starts NET-001 now. Their INT chain is blocked behind #7/#8, so this front-loads the biggest technical risk (network physics) instead of leaving them idle.
- Rule 11 exists because once NGO is on `main`, weaker AI tools tend to add `NetworkBehaviour` to unrelated features.

## Verification performed
- The milestone API shows open/closed counts. No open issue is left without a milestone.
- The issue list was re-read after the assignee changes.
- While checking, I found assignment changes made from the GhazaGG account outside this session: #17 unassigned at 17:53Z, and DePo4l assigned to #19 at 16:39Z. The owner resolved both: #17 goes to GhazaGG, and #19 is GhazaGG only.

## Final result
All open issues have a milestone, priority, and owner, except the deliberately unassigned ones. The new tickets close the pickup and freshness gaps. MAT-001 and NET-001 can start this sprint.

## Known limitations
- The GitHub Project board (Status/Estimate/Iteration fields) was not touched: the gh token lacks the `read:project` and `project` scopes.
- Priorities and assignments are planning judgments and still need confirmation at sprint planning with the team.

## Unresolved issues or follow-up work
- meryzennn sits on the critical path twice (INT and NET). Watch for slippage.
- The lead reviews every PR. If PRs pile up, delegate technical review to RyoFPS or meryzennn.
- A follow-up ticket is needed for the mixer jamming with hardened concrete (chaos moment #4) after #45.
- EPIC-001 #29 stays open until a teammate confirms a fresh clone opens in Unity 6000.3.25f1.
- Tell RyoFPS to merge `main` into his branch and follow the M1 decisions (Input System, no Cinemachine, local-only input).
