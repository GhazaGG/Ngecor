# Merge PR #42, add admin PR bypass, update issues for M1 decisions

## Task summary
The owner asked to merge the M1 technical-decisions PR right away. The merge was blocked by the ruleset, so on the owner's explicit instruction an admin PR-only bypass was added. The PR was then merged, and the affected issues were updated to match the decisions.

## Relevant previous context
- `2026-10-01-23-54-m1-tech-decisions.md`: decisions recorded on `docs/m1-tech-decisions`.
- `2026-10-01-22-11-setup-005-playground-physics.md`: the admin bypass was intentionally removed after PR #40, which left ruleset "Protect main" with no bypass actors.
- The owner said RyoFPS is developing #7 and #8. No branch of his was on the remote, so his work could not be checked for conflicts. My PR touched only docs, so a Git conflict was unlikely.

## Changes made
- Opened PR #42.
- `gh pr merge --squash --admin` was refused: "At least 1 approving review is required by reviewers with write access."
- The owner chose to add a bypass instead of waiting for RyoFPS's approval. Ruleset 24306744 "Protect main" now has bypass actor `RepositoryRole` id 5 (Admin), `bypass_mode: pull_request`. Its rules (deletion, non_fast_forward, pull_request with 1 approval and code-owner review) and enforcement (active) are unchanged.
- PR #42 squash-merged as `e9a3c05`, and the remote branch was deleted.
- Issue bodies updated:
  - #7: AC now specifies Input System + KB/M. Technical Notes added: local-only input, bindings recorded by the owner, no legacy Input Manager. Gamepad added to Out of Scope.
  - #8: AC now specifies first-person. Technical Notes added: no Cinemachine, local-only look. A third-person vehicle camera added to Out of Scope.
  - #10: Technical Note added to separate grab intent from execution, without a framework, keeping the object physical.
  - #20: Technical Notes added: NGO 2.x + UTP host, pin the verified version in DECISIONS, LAN/direct IP only, Multiplayer Play Mode needs lead approval first. Relay/Lobby added to Out of Scope.
  - #22: AC added for the held-object RTT lag test, the client-on-host-object / wheelbarrow-push desync test, and reporting results for the authority revisit.

## Files affected
- This report only. The other changes were to GitHub settings and issues.

## Technical decisions
- Bypass mode `pull_request` rather than `always`: admins can merge their own PRs without approval, but a direct push to `main` is still blocked.
- Issue links to DECISIONS use absolute GitHub URLs, because relative links don't resolve in issue bodies.

## Verification performed
- The ruleset PUT response showed the new bypass actor, the same three rules, and enforcement active.
- `gh pr view 42` reported MERGED, merge commit `e9a3c05`.
- Re-read the edited bodies of #7 and #22 on GitHub.
- A first edit attempt used an unquoted heredoc, so the backtick-quoted text in #7 and #20 was dropped and bash briefly executed `docs/DECISIONS.md` as a script. It hit a syntax error at line 7 and ran nothing meaningful. `git status` was clean afterwards, and the three affected lines were repaired.

## Final result
The decisions are on `main`. Issues #7, #8, #10, #20, and #22 reflect them. Admins can merge PRs without approval.

## Known limitations
- No reviewer other than the owner has read the decisions yet.
- The bypass weakens the "review before merge" rule for admins. The repo has a single admin, the owner.
- A direct push to `main` with the new bypass was not tested.

## Unresolved issues or follow-up work
- Ask RyoFPS to `git merge origin/main` into his #7/#8 branch, confirm his work matches first-person + Input System, and push a draft PR.
- Board hygiene: milestones and priorities on issues, rebalance assignments (WIP, lead bottleneck).
- The gh token lacks `read:project`, so the board can't be read.
