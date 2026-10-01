# Record lightweight performance goal and URP

## Task summary
The owner asked for the game to be very lightweight and for this to be recorded as a decision.

## Relevant previous context
`2026-10-01-18-51-git-workflow-unity-6-and-setup-issues.md` left the render pipeline open and blocking SETUP-001; URP was recommended in conversation.

## Changes made
- `docs/DECISIONS.md`: added "Performa" and "Render pipeline: URP" rows, removed the open render pipeline row, added a decision log entry and an "Anggaran performa" section (target hardware and FPS, graphics/art/physics/code/network rules, how to check).
- `AGENTS.md`: added an implementation rule pointing to the performance budget.
- `docs/WORKFLOW.md`: technical review now checks the performance budget.
- GitHub issue #34: the render pipeline note now says URP with the Universal 3D template; the "Blocked by" render pipeline decision was removed.

## Files affected
`docs/DECISIONS.md`, `AGENTS.md`, `docs/WORKFLOW.md`, this report. Remote: issue #34.

## Technical decisions
- The owner said "very lightweight"; the concrete numbers are mine and marked as starting points to revise with Profiler data: 60 FPS at 1080p with 4 players on integrated-GPU laptops (Iris Xe / Vega 8 class, 8 GB RAM), with the weakest team laptop as the reference until a device is chosen.
- URP chosen over HDRP (too heavy) and Built-in (Unity steers new projects to URP).

## Verification performed
`git diff --check` is clean. The `#anggaran-performa` anchor matches its heading. Read issue #34 back to confirm the edited lines.

## Final result
Performance goal and URP are recorded; SETUP-001 is no longer blocked by a pipeline decision.

## Known limitations
The budget numbers are not validated on hardware. The issue #34 link targets `main`, which will resolve only after PR #37 merges.

## Unresolved issues or follow-up work
Confirm the target hardware; target platform, input system, and networking remain open.
