# MIX-003 PR B: Bounded Output Slots and Held-Sack Guard

## Task summary
Fixed two issues found while checking PR #98 review readiness and merged the latest `main` into `feat/manual-mixing`.

## Relevant previous context
- `.development-history/2026-10-09-18-50-check-mix003-pr98-review-readiness.md` listed the findings.
- `.development-history/2026-10-08-21-47-mix003-manual-mixing-implementation.md` describes the original PR B implementation.

## Changes made
- `ManualMixingSpot` now places concrete batches in a fixed number of output slots (`_outputSlots`, default 3, 1.4 m apart from the output point). Previously each batch moved 1.4 m further without limit, so after enough batches the raycast left the floor and mixing stopped permanently with no output.
- When every slot still holds a batch, stirring keeps progress at full and the recipe is not consumed. The next stir after a batch is fully collected (the empty pile destroys itself) creates the batch in the freed slot.
- `TryReceiveBag` rejects a sack whose `GrabbableObject` is held. Intake moved from `OnCollisionEnter` to `OnCollisionStay`, so a sack released while already touching the spot is still taken in.
- Added PlayMode tests `HeldSackIsNotConsumedUntilReleased` and `FullOutputSlotsPauseMixingUntilABatchIsCollected`.
- Merged `origin/main` (`f3fe648`, INT-006 #95) into the branch; no conflicts.

## Files affected
- `Assets/Game/Scripts/Material/ManualMixingSpot.cs`
- `Assets/Game/Tests/Material/ManualMixingSpotTests.cs`
- `.development-history/2026-10-09-18-56-mix003-fix-output-slots-and-held-sack.md`

## Technical decisions
- Limited output space instead of an unbounded row: it matches the physical-capacity principle and turns the stuck state into a recoverable one (collect concrete to continue).
- The new serialized field uses its code default, so the prefab and scene were not edited.

## Verification performed
- Unity 6000.3.25f1 batchmode PlayMode run after the merge: 168 passed, 0 failed, 0 skipped, including all 8 `ManualMixingSpotTests`.

## Final result
The blocker from the readiness check is fixed and covered by tests; the branch is up to date with `main`.

## Known limitations
- Manual Play Mode feel test in `Dev_Ghaza` is still not done. The three output slots at about 2.2, 3.6, and 5.0 m to the right of the spot center have not been checked for free floor in the scene.
- When slots are full, the only feedback is the full progress bar plus the visible piles.
- The uncommitted extra "Mixing Test Cement Sack" in `Dev_Ghaza.unity` was left untouched and not committed; that instance has no `GrabbableObject` added.

## Unresolved issues or follow-up work
- Run the manual steps, including shovel collection of concrete and filling all three output slots.
- Update the PR "How to Test" section and mark the PR ready for review.
