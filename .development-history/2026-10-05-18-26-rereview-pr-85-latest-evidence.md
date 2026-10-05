# Re-review PR #85 at head 854ee38

## Task summary

Re-reviewed RyoFPS's PR #85 at `854ee3894500b7e59eb8bca5479316d5d771eca8` against issue #73. Reviewed the changes since `290efd7`, the linked failure and follow-up reports, code, test fixture, manual notes, performance evidence, and previous GitHub reviews. Posted Request Changes for current-head verification and unresolved acceptance evidence.

## Relevant previous context

Read `2026-10-04-23-13-rereview-pr-85-and-78-fixed-regressions.md`, `2026-10-04-20-57-rereview-pr-85-and-86-runtime-regressions.md`, plus current PR branch reports `2026-10-04-21-56-player003-qa-run.md`, `2026-10-05-17-52-player003-manual-playground-results.md`, and `2026-10-05-17-57-player003-focused-playmode-results.md`. Checked their claims against the latest PR description and source diff.

At `290efd7`, the developer reported a 71/73 full-suite run, failing the saturated-probe and destroyed-interaction-target tests. The latest branch adds the two corresponding follow-ups, reports the focused 28/28 PlayerMovement suite and a separate destroyed-target result, and explicitly says the complete suite has not been rerun on `854ee38`.

Read the user-supplied project instructions and current README, WORKFLOW, PROJECT_STRUCTURE, GAME_DESIGN, DECISIONS, and issue #73. No `.codegraph` directory exists. The existing local checkout remains on `docs/history-review-pr-69`; six existing untracked history reports were preserved.

## Changes made

- Fetched current `main` and PR #85 refs and reviewed the incremental code/test changes from `290efd7` to `854ee38`.
- Posted a Request Changes review with the full-suite, sack acceptance, and profiling follow-ups.
- Read the review back from GitHub and confirmed body, author, head commit, state, ID, and URL.
- Created this report. No product changes, Unity tests, commits, pushes, or merges were made.

## Files affected

- Active checkout: this report only.
- Temporary review body: `C:\Users\Hype\AppData\Local\Temp\pr85-854ee38-rereview.md`.
- External change: [PR #85 review 5413771667](https://github.com/GhazaGG/Ngecor/pull/85#pullrequestreview-5413771667), `CHANGES_REQUESTED`, by GhazaGG at 18:26 Asia/Jakarta.

## Technical decisions

The previous probe-saturation fix remains in the branch; the updated regression now asserts that the query has more than eight hits and includes the valid dynamic blocker. `InteractionDetector.HasTarget` correctly treats a destroyed Unity object referenced through `IInteractable` as unavailable. The change stays within existing components, with no dependency, framework, networking or shared project-setting addition. No new code or security defect was established in the diff.

Requested a current-head full PlayMode result because the preceding complete suite had two failures and the current head changes both areas, including `InteractionDetector`. Asked the author to clarify the reported intermittent 25 kg sack behavior against issue #73's slow-movement and stop-on-release criteria. Also requested movement/pushing profiling: the supplied capture is idle-only, and the moving probe's runtime cost remains unmeasured. These are verification and acceptance gaps, not claims of a reproduced new code defect.

## Verification performed

- Inspected the live PR head, author, base, description, reviews, and issue #73 before posting.
- Reviewed the incremental production and test diff, latest manual and focused-test reports, and previous full-suite failure report.
- `git diff --check origin/main...origin/review-pr-85` passed.
- Confirmed the current PR description leaves the complete 73-test suite pending and describes the 28-test focused evidence and unresolved performance measurement.
- Did not run Unity tests or interactive gameplay in this turn. Prior and developer-provided test results are identified as reported evidence, not as current independent results.
- `gh-axi pr review 85 --request-changes --body-file ...` succeeded. Readback matched the prepared body and confirmed state `CHANGES_REQUESTED` at head `854ee38`.
- Active checkout status was checked; existing untracked reports were untouched.

## Final result

The re-review is posted. The prior buffer and destroyed-target issues have targeted follow-up changes, but full-suite validation at the latest head, consistent 25 kg sack acceptance, and movement/pushing performance evidence remain open. No approval or merge was made.

## Known limitations

No current-head Unity test run, keyboard-driven reviewer playtest, warmed-up movement/pushing Profiler capture, weakest-laptop benchmark, Windows build, or multiplayer check was performed. Manual observations and focused results are developer-reported evidence.

## Unresolved issues or follow-up work

- Run the full PlayMode suite on `854ee38` and record per-assembly results and process exit.
- Reproduce and document the 25 kg sack's motion during sustained push and after release, including its resting orientation.
- Capture walking/pushing performance or record why that measurement remains unavailable.
- Complete independent reviewer gameplay verification before approval under `docs/WORKFLOW.md`.
