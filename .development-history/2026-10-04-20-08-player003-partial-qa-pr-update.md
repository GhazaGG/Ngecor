# PLAYER-003 Partial QA Evidence Added to PR #85

## Task summary

Added the developer's partial current-head QA report and screenshots to PR #85, and updated the PR description to distinguish the idle-only Profiler and Console evidence from the still-unverified Game View acceptance checks.

## Relevant previous context

- PR #85 is `feat/player-003-human-strength`, linked to issue #73, targeting `main`.
- The current code head before this QA evidence update was `268af23`. The branch already contained unrelated local changes, including `Assets/Game/Scenes/Playground.unity`; those changes were preserved.
- Current-head visible Game View checks and movement/push profiling remained outstanding. The developer supplied an idle-only Profiler capture and final Console screenshot, and said they prefer not to perform the Game View scenarios themselves.

## Changes made

- Committed the manual QA report and its two screenshots as `6ba388f` (`docs(player): add partial manual QA evidence`) and pushed them to the PR branch.
- Updated PR #85's description with the idle-only metrics, EditorLoop outlier caveat, combined PlayerLoop sample limitation, allocation summary, GPU Usage module limitation, and current-head Console result.
- Kept the Game View acceptance and gameplay-feel checklist items unchecked. Kept the earlier gameplay observations labeled as preceding the review-fix commit.
- Did not edit gameplay code, scenes, prefabs, project settings, or assets.

## Files affected

- `.development-history/2026-10-04-19-42-player003-manual-gameview-profiler.md` — developer-provided partial QA report.
- `.development-history/2026-10-04-player003-profiler-idle-summary.jpg` — idle Profiler screenshot.
- `.development-history/2026-10-04-player003-console-final.jpg` — final Console screenshot.
- `.development-history/2026-10-04-20-08-player003-partial-qa-pr-update.md` — this report.
- PR #85 description on GitHub.

## Technical decisions

- The idle-only Profiler Highlights capture is recorded as a partial baseline, not as the requested walking/pushing performance measurement. The combined `PlayerLoop` and `ScriptRunBehaviourUpdate` samples do not establish the per-frame capsule-sweep cost.
- The Console screenshot is recorded as showing 0 logs, 0 warnings, and 0 errors at the end of the current-head attempt.
- No claim is made that current-head Game View push, stack, traversal, or wheelbarrow feel acceptance passed.

## Verification performed

- Confirmed branch `feat/player-003-human-strength` and pre-update head `268af23` matched PR #85.
- Staged only the supplied QA report and two screenshots; `git diff --cached --check` passed.
- Commit `6ba388f` pushed successfully, including its Git LFS screenshot objects.
- Confirmed PR #85 now points at `6ba388f`, its description contains the partial QA evidence and remaining gaps, and GhazaGG remains requested for review.
- No Unity tests were rerun because this follow-up only recorded QA evidence and updated PR documentation.

## Final result

PR #85 now includes the partial idle Profiler and clean Console evidence without presenting the incomplete Game View checks or the capsule-sweep measurement as verified.

## Known limitations

- Current-head Game View cases 1–5 remain unverified.
- Controlled warmed-up walking-flat and low-object-push Profiler captures were not obtained. The capture was not made on a confirmed weakest team laptop, and the dedicated GPU Usage module could not be enabled.
- PR #85 still has a `CHANGES_REQUESTED` review decision while GhazaGG is requested; this evidence update does not clear that review state.

## Unresolved issues or follow-up work

- GhazaGG can perform or request the outstanding current-head Game View gameplay review.
- If issue #73 still requires a controlled performance sample, capture warmed idle, flat-walk, and low-object-push intervals with CPU/GPU data on the weakest available team laptop.
