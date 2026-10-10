# MIX-003 PR B (#98): RyoFPS Review, Stale Dry Label and Water Cap

## Task summary
Addressed two P3 findings from RyoFPS's static review of PR #98 at `6e5f20a`. The review found no blocking code bug; its must-fix item is the PR checklist (see follow-up).

## Relevant previous context
- `.development-history/2026-10-10-19-36-mix003-pr98-depo4l-batch-and-setup.md`: previous round.
- `.development-history/2026-10-10-18-51-mix003-pr98-meryzennn-review-fixes.md`: introduced `AcceptLimit` and the water cap.

## Changes made
- Stale dry label: after a finished dry mix, newly added sand or cement made `IsDryMixed` false, but the label still read "Aduk kering 5/5" with a full bar until the next stir. `DryWork` is now derived (0 once new dry material arrives), and phase, label, progress bar, and bed color use it. The bed shows "Siap aduk kering (tahan R)".
- Water cap: the cap used the rounded-up batch count, so 1 cement + 3 sand accepted 2 water. Water is now capped by the batches the dry material can make right now (`min(cement / 1, sand / 2)`). The room reservation for missing ingredients is unchanged. Notice text changed to "Air cukup untuk bahan kering yang ada".
- Tests: stale-label asserts in `DryIngredientsAddedAfterDryMixingNeedMixingAgain`; `ExcessWaterIsRefusedSoTheNextSackStillFits` uses 1 cement + 3 sand; `DryMixThenWaterThenWetMix...` now expects 2 water accepted; `HeldPourThatEmptiesTheBucketDoesNotStartCollecting` dry-mixes added sand first so the bed takes water.

## Files affected
- `Assets/Game/Scripts/Material/ManualMixingSpot.cs`
- `Assets/Game/Tests/Material/ManualMixingSpotTests.cs`

## Technical decisions
- Derive the displayed dry work instead of resetting state outside a stir, so no hook on container changes is needed.
- Not changed: the notice side effect inside `AcceptLimit` (nit, deferred earlier).

## Verification performed
- Unity 6000.3.25f1 batchmode PlayMode suite: 184 passed, 0 failed.
- Not played in Play Mode.

## Final result
Both P3 findings are fixed; the commit is local until the developer approves the push.

## Known limitations
- None new.

## Unresolved issues or follow-up work
- RyoFPS must-fix: the checklist item "Acceptance criteria issue terpenuhi di manual Play Mode" stays checked while tap/hold, bed limits and notices, and bucket collect after a water trip have not been played. Play them in `Dev_Ghaza` and record the result, or uncheck it.
- DePo4l and meryzennn re-reviews pending.
