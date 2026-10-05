# Post PR #78 Request Changes review

## Task summary

Published the completed review of Ryzen's PR #78, `[INT-004] Throw carried object`, after the user's follow-up asking about posting it to the PR.

## Relevant previous context

Read `2026-10-04-18-36-review-pr-78-throw-relock-order.md` and the existing GitHub review. The completed investigation found one P2 cursor-relock flag defect; the three previous Must Fix items and the action-lifecycle Should Fix had been resolved. The original PR passed 70/70 tests, the added diagnostics reproduced one failure with a passing control, and a temporary candidate correction passed 73/73. No new tests were run during this posting task.

## Changes made

- Submitted a GitHub Request Changes review as `GhazaGG`.
- Explained the stale frame flag, reproduction steps, and the frame-number correction that was previously verified in a temporary clone.
- Acknowledged completed earlier fixes and distinguished original PR results from candidate-fix results.
- Recorded the limits of synthetic input, performance inspection, and gameplay-feel verification.
- Created this report. No product files were changed, committed, pushed, or merged.

## Files affected

- This development report.
- Temporary review body: `C:\Users\Hype\AppData\Local\Temp\ngecor-pr78-review-yf5fncit\pr78-request-changes-review.md`.
- External change: [GitHub review 5405984341](https://github.com/GhazaGG/Ngecor/pull/78#pullrequestreview-5405984341).

## Technical decisions

Kept Request Changes because the next throw click can be discarded when Grab updates before Movement. The review makes clear that the first relocking click is protected in both orders and that the defect affects the immediately following click. Heavy-throw tuning and carry-collision changes remain follow-ups under #46 and #76.

## Verification performed

- Checked the active branch and working tree, preserving all existing untracked reports.
- Rechecked GitHub: PR open and unmerged; head `f235b228f968b6fc3b4a7a6ee2bb0d92c93fe49c` and base `eb4a0dac14182e324650f5e493872d8736f148f3` matched the reviewed snapshot.
- Checked existing reviews to avoid duplicating a newly posted outcome and confirmed the authenticated identity as `GhazaGG`.
- `gh-axi pr review 78 --request-changes --body-file ...` completed with exit 0.
- Read back the submitted review through GitHub API and confirmed its body, author, reviewed commit, state `CHANGES_REQUESTED`, ID `5405984341`, URL, and submission time `2026-10-04T12:06:00Z` (19:06 Asia/Jakarta).

## Final result

The review is published successfully: https://github.com/GhazaGG/Ngecor/pull/78#pullrequestreview-5405984341. No approval or merge was performed.

## Known limitations

This task published previously verified findings. It did not repeat Unity tests, interactive gameplay checks, or performance measurements.

## Unresolved issues or follow-up work

The author should fix the frame-specific relock state and validate both Update orders before another review. Gameplay-feel verification remains outstanding for the reviewer.
