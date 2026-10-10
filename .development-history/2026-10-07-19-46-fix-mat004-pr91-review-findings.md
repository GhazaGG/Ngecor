# Fix MAT-004 PR #91 Review Findings

## Task summary
Address RyoFPS's MAT-004 PR #91 review on the existing feature branch and make the PR ready for human gameplay re-review.

## Relevant previous context
`2026-10-06-22-49-mat004-shovel-pile-implementation.md` records the original MAT-004 implementation and automated checks. `2026-10-06-23-05-open-mat004-pr-for-review.md` records the open PR handoff. The local untracked `2026-10-07-17-21-check-mat004-pr91-review.md` records the initial review inspection. Live PR comments from RyoFPS confirmed the requested CharacterController filter, request/execute split, and checklist relocation. Historical test results were not treated as current verification.

## Changes made
- Reject CharacterController colliders as ground deposit surfaces. Refresh `DepositPoint` when a valid surface is found, before the next integer transfer, to keep the bucket particle destination current.
- Keep shovel requests limited to a local holder, while `ExecuteScoop` and `ExecuteDump` require a held shovel without depending on `LocalCamera`. No networking code was added.
- Add Play Mode regressions for a player capsule below the outlet, destination refresh before transfer, and non-local holder execution versus local request rejection.
- Remove the ticket checklist and two captured PNGs from `docs/`. Copy the PNGs into ignored local `Logs/` before removal. Move the gameplay checklist into the live PR description and identify `Dev_Ghaza.unity` as the prepared test fixture.
- Commit and push the code/docs changes as `d379974` on `feat/mat-004-shovel-piles`; update the PR body with the current evidence and pending gates.

## Files affected
`Assets/Game/Scripts/Material/GroundMaterialDeposit.cs`, `Assets/Game/Scripts/Material/ShovelAction.cs`, `Assets/Game/Tests/Material/ShovelPilePlayModeTests.cs`; removed `docs/MAT-004-Shovel-Pile-Test-Cases.md`, `docs/MAT-004-Piles-PlayMode.png`, and `docs/MAT-004-Profiler-CPU.png`; GitHub PR #91 description; this report. Ignored local evidence: `Logs/Mat004Review*`, `Logs/Mat004PilesPlayMode.png`, and `Logs/Mat004ProfilerCPU.png`.

## Technical decisions
The nearest invalid surface still blocks a deposit rather than falling through to a farther floor. `BulkMaterialContainer` remains quantity authority. The local request guard stays separate from execution so a future host can validate intent without requiring a local camera. `TryGetSurface` updates a visual target only after validating a surface; quantity changes still occur only in `Deposit`.

## Verification performed
- Reviewed the live issue #54, RyoFPS review and follow-up comment, prior history, current project workflow/structure/decisions, affected callers and tests, branch status, and remote head. `.codegraph/` is absent.
- Unity 6000.3.25f1 Play Mode material assembly: **52 passed, 0 failed, 0 skipped**, exit 0 (`Logs/Mat004ReviewMaterialResults.xml`, `Logs/Mat004ReviewMaterialTests.log`).
- Full Unity Play Mode suite: **125 passed, 0 failed, 0 skipped**, exit 0 (`Logs/Mat004ReviewFullResults.xml`, `Logs/Mat004ReviewFullTests.log`). No gameplay warning/error appeared in that log; Unity licensing printed an access-token message during startup.
- `git diff --cached --check` passed. Confirmed remote branch was at prior local head `c7e9c71` before push, push advanced it to `d379974`, and live PR body reflects the new checklist and 125/125 result. No Unity serialized asset or ProjectSettings file was modified.
- Preserved unrelated untracked MAT-002 scenes, earlier reports, and generated `ProjectSettings/SceneTemplateSettings.json`.

## Final result
Code fixes and tests are pushed to open PR #91. Its body now contains the gameplay checklist and test scene guidance. Technical automated verification is green; the PR is not approved or merged.

## Known limitations
Human keyboard playthrough, handling feel, GPU timing, standalone/reference-device and four-player performance remain unverified. This run did not perform a manual gameplay review. Unity licensing produced a startup message unrelated to gameplay.

## Unresolved issues or follow-up work
Team gameplay reviewer should execute the PR checklist, record feel/Console/performance findings, and submit formal approval if acceptable. MIX-003 can consider material-specific shovel fill color. The local screenshots remain under ignored `Logs/`; they are no longer linked from the PR.
