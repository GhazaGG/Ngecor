# Re-review PR #85 and PR #78 after regression fixes

## Task summary

Reviewed the latest revisions of PR #85 and PR #78, verified the previously reported defects, inspected logic, approach, integration, security, performance and tests, and published a new COMMENT review on each PR.

| PR | Author | Reviewed head | Base |
| --- | --- | --- | --- |
| #85 PLAYER-003 | RyoFPS | `290efd7d65fb4a1c8f0c4737a5ec92efbf12cc0f` | `d271570a63d75bf7b2bb431a0283e3b92c411e6a` |
| #78 INT-004 | meryzennn | `7a2015d7e44115f67024bfbfbfbc7d1665e4a644` | `eb4a0dac14182e324650f5e493872d8736f148f3` |

## Relevant previous context

Read the relevant local reports:

- `2026-10-04-20-57-rereview-pr-85-and-86-runtime-regressions.md`: #85's eight-hit probe could miss a valid blocker when ignored contacts filled the buffer.
- `2026-10-04-18-36-review-pr-78-throw-relock-order.md` and `2026-10-04-19-06-post-pr-78-request-changes-review.md`: #78's Boolean relock flag could discard a new click if Grab updated first on the following frame.

Read repository instructions, RTK guidance, Ponytail, README, workflow, folder structure, relevant design/decision sections, live issues #73/#12, complete PR descriptions and existing reviews/comments. Read each new regression-fix history report from its current branch. Used project memory's advice to verify live state and distinguish runtime fixtures from interactive acceptance. No CodeGraph index exists.

## Changes made

- Fetched main and both current PR refs without changing the active checkout.
- Updated the existing isolated #85 review clone and created a new detached #78 clone.
- Ran both unchanged Unity PlayMode suites. Added one temporary #78 imported-input diagnostic, ran it, and restored the test file byte-for-byte.
- Checked a synthetic combined tree with `git merge-tree`; no merge or combined runtime run was performed.
- Posted and read back both review bodies, confirming exact content, author, commit, state and URL.
- Created this report. No product fixes, commits, pushes, approvals or merges were performed.

## Files affected

Active workspace: this report only. All five pre-existing untracked history reports were preserved on branch `docs/history-review-pr-69`.

Temporary evidence projects:

- #85: `C:\Users\Hype\AppData\Local\Temp\ngecor-pr85-rereview-_2pi2ts9`.
- #78: `C:\Users\Hype\AppData\Local\Temp\ngecor-pr78-latest-j7ip6541`.

Only #78's `Assets/Game/Tests/Interaction/PlayerGrabTests.cs` was temporarily modified for an additional diagnostic; restored afterward. Review bodies, package-cache helper, result XML, logs, and readback metadata remain in the temporary projects. No serialized Unity asset was text-edited.

External changes:

- [PR #85 review 5407090914](https://github.com/GhazaGG/Ngecor/pull/85#pullrequestreview-5407090914), COMMENTED by GhazaGG at 23:10:04 Asia/Jakarta.
- [PR #78 review 5407108744](https://github.com/GhazaGG/Ngecor/pull/78#pullrequestreview-5407108744), COMMENTED by GhazaGG at 23:13:04 Asia/Jakarta.

## Technical decisions

#85's saturation defect is resolved. A full query doubles the cached buffer and retries before filtering. The committed regression detects a valid blocker among 12 ignored colliders and checks cache reuse. Existing collision-exclusion, Ignore Raycast physical-collision, static-step, contact-deduplication, force-cap and pure-calculation checks remain green. The prefab contains `_maxPushForce: 300` and retains local ownership default false.

#78's stale relock defect is resolved. Comparing the stored relock frame to `Time.frameCount` removes the dependency on Movement running first on the next frame. Both controlled Update-order tests and the before-Update expiry test pass. Throw continues to use the existing detach pipeline, camera direction, mass-dependent impulse and horizontal velocity inheritance. The earlier input-reference, lifecycle, test-object and unwanted-plan findings remain resolved.

No additional blocking implementation or security issue was identified within these offline diffs. No new dependency, framework, networking API or shared settings change was introduced. #85 allocates and repeats queries when growing its cache, then reuses that capacity; #78's frame comparison has constant cost. These observations are not measured low-end performance results. Heavy throw tuning remains #46 and carry collision policy remains #76.

Posted COMMENT reviews rather than approvals because `docs/WORKFLOW.md` requires reviewer gameplay verification. #85 still explicitly lacks current-head Game View acceptance evidence and walking/pushing profiling. #78's developer-reported manual results do not substitute for independent reviewer feel checks. Marked the old technical findings resolved without inventing new code changes to request. Comments do not dismiss the earlier Request Changes reviews.

The synthetic merge tree `ba403e0e9505b78ab7646b7f3419e81876661137` includes both pushing/probe changes and the frame-based cursor guard without textual conflicts. This does not establish combined gameplay or physics behavior.

## Verification performed

Unity 6000.3.25f1, batchmode/nographics, with complete NUnit XML and captured process exit codes:

| Run | Result | Exit |
| --- | --- | --- |
| #85 unchanged full suite | 73/73 passed; Interaction 33, Material 12, Player 28; no skips | 0 |
| #78 unchanged full suite | 73/73 passed; Interaction 42, Material 12, Player 19; no skips | 0 |
| #78 additional imported reference diagnostic | 1/1 passed | 0 |

Evidence: #85 `pr85-290efd7-full.xml/.log`; #78 `pr78-7a2015d-full.xml/.log` and `pr78-7a2015d-wiring.xml/.log`.

The additional diagnostic verifies that PlayerThrow resolves to Player/Attack with the left-mouse binding, uses the project's configured action asset, and is assigned on Player.prefab with impulse 10 and `_isLocalPlayer` false. Inspected the scene diff statically: only Box_Heavy (25 kg, 0.5 m, GrabbableObject) and its root entry were added. Scene gameplay was not independently exercised.

PR-wide #85 `git diff --check` passed. #78 C#/test/history diff-check passed. Git diff exit 0 confirmed tracked product/test/settings files were unchanged after review in both final clones. Heads were rechecked immediately before each submission. Both posted bodies matched their prepared files after newline normalization.

The previous #78 clone reported two modified C# files despite no textual diff and refused checkout. Preserved that clone and all its evidence rather than forcing the checkout; used a new clone. Reused 28,335 matching locked package-cache files through hard links after verifying both manifest and lockfile match, without installing packages. Ran Unity projects sequentially. Fresh import produced nonfatal shader dependency messages; no claim is made about a clean interactive Console.

## Final result

Both requested re-reviews are complete and posted. Previous code defects are resolved, both original current-head suites pass, and no new implementation blocker was found. Neither PR was approved or merged; reviewer gameplay remains outstanding.

## Known limitations

No physical keyboard/mouse or interactive Game View review, gameplay feel assessment, warmed-up movement/pushing Profiler capture, weakest-team-laptop benchmark, Windows build, multiplayer check, or combined-PR runtime test was performed. Synthetic inputs and controlled Update calls establish tested logic rather than subjective feel. Temporary evidence may be removed by OS cleanup.

## Unresolved issues or follow-up work

- Complete #85's current-head light-prop, 25 kg bag/release, five-bag stack, traversal, empty-cart ramp, three-cargo flat-ground and Console checks; obtain relevant walking/pushing profiling.
- Complete independent reviewer gameplay checks for #78's aim angles, light/heavy throw, moving throw and cursor relock before approval.
- Preserve both PlayerMovement changes during integration and verify the resulting combined behavior after integration.
- Describe synthetic-test and gameplay limits accurately in #78's history rather than stating no offline limitations.
