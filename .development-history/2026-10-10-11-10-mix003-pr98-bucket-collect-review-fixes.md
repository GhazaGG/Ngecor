# MIX-003 PR B (#98): Bucket Collect Review Fixes

## Task summary
Addressed DePo4l's changes-requested review of PR #98 at head `b0fbc3d`: three bucket collect issues in `BucketPourAction`. The fourth comment (TextMesh label facing) was assessed but not changed.

## Relevant previous context
- `.development-history/2026-10-09-22-36-mix003-pr98-ready-for-review.md`: PR marked ready for review.
- An earlier static review comment from the author's account at `dd6ffa1` already flagged the type-lock and nearest-receiver issues.

## Changes made
- P1: an empty bucket that starts collecting (water or concrete) now gets its previous single type back if the request is cancelled before the first unit moves. Once a unit moves, the new type stays.
- P2: collecting uses the same unique-nearest, no-fallback rule as pouring. The nearest registered container is chosen first; if it is not a `CollectOnlyType` source, nothing is collected. An exact tie also cancels.
- P2: when the active collect source unregisters, the whole request is cancelled, so a held R can select the next source.
- Added `BulkMaterialContainer.TryGetSingleType`.
- Added PlayMode tests: tap-and-release at a drum, nearer ordinary container blocks a farther drum, exact tie cancels, held bucket moves to the next source. Test buckets now start at the origin.

## Files affected
- `Assets/Game/Scripts/Material/BucketPourAction.cs`
- `Assets/Game/Scripts/Material/BulkMaterialContainer.cs`
- `Assets/Game/Tests/Material/ManualMixingSpotTests.cs`
- `.development-history/2026-10-10-11-10-mix003-pr98-bucket-collect-review-fixes.md`

## Technical decisions
- Restore the previous type on cancel instead of deferring configuration, because the transfer needs the bucket to accept the type before credit accumulates.
- Label comment not changed: `TextMesh` text is readable from the side its forward axis points away from, and `LookRotation(label - camera)` is the standard billboard for that; the developer saw the label during the Play Mode test.

## Verification performed
- Unity 6000.3.25f1 batchmode PlayMode suite: 179 passed, 0 failed.
- Did not run the new tests against the old code to confirm they fail before the fix.
- No manual Play Mode check of these bucket cases.

## Final result
The three bucket issues are fixed and covered by tests; the commit is local until the developer approves the push.

## Known limitations
- If a bucket started as a multi-type container, its mode is not restored (buckets are single type).

## Unresolved issues or follow-up work
- Push, reply to DePo4l's four threads, and request re-review.
