# MIX-003 PR B (#98): meryzennn Review Fixes

## Task summary
Addressed meryzennn's changes-requested review of PR #98 at `1e4388f`: one P1 and two P2 findings, plus three of the P3 nits.

## Relevant previous context
- `.development-history/2026-10-10-16-50-mix003-pr98-tap-collect-and-setup-rerun.md`: DePo4l's re-review fixes (tap collects, hold stirs, one drum).
- `.development-history/2026-10-10-11-10-mix003-pr98-bucket-collect-review-fixes.md`: introduced the restore-on-cancel bucket type, replaced here.

## Changes made
- P1: an empty bucket returns to its own type (`_materialType`, Sand) whenever it is empty and not collecting. Before, a bucket that had collected water or concrete kept that type after being emptied, so a shovel could not load sand into it. This replaces the restore-on-cancel fields.
- P2: a collect only starts on a fresh press. A hold that poured something (`_pourStarted`, cleared in `CancelPour`) no longer starts collecting when the bucket runs empty, so pouring the last water into a bed with concrete does not pull concrete back, and pouring into the drum does not refill at once.
- P2: the mixing bed can no longer get stuck. `BulkMaterialContainer.AcceptFilter` (bool) became `AcceptLimit` (units of a type the owner can take now), applied in `AddUnits`, `TransferUnitsTo`, and timed transfers. The spot reserves room for the missing ingredients of every batch its raw material implies, and caps water at what the dry mix can use. A sack that does not fit stays whole outside the spot. New label notices: "Penuh: olah atau ambil beton dulu", "Air cukup: aduk basah dulu".
- P3: wet progress resets when a re-dry-mix starts; the setup no longer deletes the long-gone `ConcreteBatch.cs`; both setup menus call `SaveCurrentModifiedScenesIfUserWantsTo` before opening Dev_Ghaza.
- `docs/DECISIONS.md`: records the bed reservation rule.
- Tests: `BedOnlyTakesWhatItCanStillTurnIntoConcrete`, `ExcessWaterIsRefusedSoTheNextSackStillFits`, `EmptiedBucketGoesBackToItsOwnTypeAfterAWaterTrip`, `HeldPourThatEmptiesTheBucketDoesNotStartCollecting`; wet-reset asserts added to `DryIngredientsAddedAfterDryMixingNeedMixingAgain`. `HeldSackIsNotConsumedUntilReleased` uses capacity 100 and `MakeConcrete` loads 10 cement, both because of the reservation.

## Files affected
- `Assets/Game/Scripts/Material/BulkMaterialContainer.cs`
- `Assets/Game/Scripts/Material/ManualMixingSpot.cs`
- `Assets/Game/Scripts/Material/BucketPourAction.cs`
- `Assets/Game/Scripts/Editor/ManualMixingSpotSetup.cs`
- `Assets/Game/Tests/Material/ManualMixingSpotTests.cs`
- `docs/DECISIONS.md`

## Technical decisions
- Prevent the stuck state instead of adding a bed-clearing action: no new input or mechanic, and every state the bed can reach can still be finished by bringing the missing ingredients. With the default 200-unit bed, at most two sacks (50 cement) fit at once.
- The reset-to-own-type rule replaces the restore-on-cancel logic, so the earlier cancel case is covered by the same code.

## Verification performed
- Unity 6000.3.25f1 batchmode PlayMode suite: 184 passed, 0 failed.
- Not played in Play Mode: tap/hold timing, the new limits, and the new notices.

## Final result
All three blocking findings are fixed and covered by tests; the commit is local until the developer approves the push.

## Known limitations
- Not fixed (P3, non-blocking): a tipped water bucket does not spill; `AcceptLimit` still shows notices from inside a query; the setup still regenerates both prefabs and overwrites Inspector tuning; the spot does not use `ConcreteRecipeCalculator`.

## Unresolved issues or follow-up work
- Push, reply to meryzennn's threads, uncheck "Gameplay feel reviewed" until tap/hold is played, request re-review.
