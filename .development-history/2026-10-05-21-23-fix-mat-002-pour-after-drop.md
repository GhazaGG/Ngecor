# Fix Pending Pour After Bucket Drop

## Task summary

Address DePo4l's P2 review on PR #86: prevent a queued bucket pour from transferring material after the bucket is dropped and before the next input update.

## Relevant previous context

`2026-10-05-21-07-assess-depo4l-pour-drop-review.md` traced the race to `BucketPourAction.FixedUpdate` transferring to a cached receiver without checking carried state. The receiver-overlap GUI run did not cover this drop-order case.

## Changes made

- Cache the bucket's `GrabbableObject` in `BucketPourAction` and cancel pending pour before transfer when it is no longer held.
- Add `DroppingHeldBucketBeforeNextPhysicsTickCancelsPendingPour`, which primes fractional transfer credit, drops through `PlayerGrab.ExecuteDrop`, and checks the next physics tick.
- Commit `5f76836` (`fix(mat-002): stop pour on bucket release`) and push it to `feat/sand-bucket`.
- Post test evidence to PR #86 and request DePo4l review.

## Files affected

- `Assets/Game/Scripts/Material/BucketPourAction.cs`
- `Assets/Game/Tests/Material/BucketPourPlayModeTests.cs`
- This development report.
- PR #86 comment and reviewer request on GitHub.

## Technical decisions

- Enforce the held-state invariant at the shared transfer tick, immediately before `TransferForSeconds`.
- Preserve the low-level action's existing use in isolated tests and tools without a `GrabbableObject`; apply the new guard when the component is present, as it is on the production bucket prefab.
- Stage only the two code/test files for the implementation commit. The pre-existing dirty `ProjectSettings/ProjectSettings.asset` and unrelated untracked files were not staged.

## Verification performed

- Ran Unity 6000.3.25f1 Play Mode for assembly `Ngecor.Material.Tests`, without `-nographics`; Unity reported Direct3D 12.
- Actual XML `Logs/Mat002DropFixResults.xml`: **33 passed, 0 failed, 0 skipped, 0 inconclusive**, including the new regression test.
- Unity Test Runner log `Logs/Mat002DropFixTests.log` reports `Test run completed. Exiting with code 0 (Ok)`.
- `git diff --check` passed before commit.
- Confirmed the pushed commit is `5f76836` and the PR contains the follow-up test comment. DePo4l was added as a requested reviewer.

## Final result

The P2 transfer race is guarded and covered by a regression test. PR #86 has the pushed fix and verification result, with DePo4l review requested.

## Known limitations

Real E/R keyboard input, carry/drop feel, and manual case TC-MAT002-15 remain NOT VERIFIED. The PR remains open pending DePo4l's review and the outstanding manual gameplay checks.

## Unresolved issues or follow-up work

- Complete the real keyboard and gameplay-feel checks before merge.
- Review and disposition DePo4l's response to commit `5f76836`.
