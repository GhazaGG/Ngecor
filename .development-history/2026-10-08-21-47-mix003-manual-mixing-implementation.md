# MIX-003 Manual Mixing Implementation

## Task summary
Implemented the manual concrete mixing gameplay stage on `feat/manual-mixing`. The scene now has a multi-type mixing spot, sack intake, shovel work progress, batch concrete output, and bucket pickup.

## Relevant previous context
- PR #97 (MIX-003 PR A) was merged into `main` as `8af0787` before this branch was created.
- `.development-history/2026-10-08-21-07-mix003-pr-b-design-review.md` records the approved separate output object per concrete batch design.
- MIX-003 values were approved for the prototype: 25 cement units per full sack, a 1:2 cement-to-sand recipe, and five shovel actions per batch. These tunings are also recorded in `docs/DECISIONS.md`.
- Issue #55 currently shows closed after PR A. PR B still implements the remaining gameplay acceptance criteria.

## Changes made
- Added a multi-material ground spot that accepts sand and cement, converts a full cement sack atomically, shows ingredient volume and shovel work, and keeps a failed sack intact if the spot lacks capacity.
- Each completed recipe produces a distinct concrete pile with a concrete-specific batch component. Ingredient leftovers remain in the spot.
- Routed empty shovel `Player/Pour` requests to one mixing action when the spot is the nearest valid target. Loaded shovels retain the existing dump path.
- Extended bucket transfer to use its actual stored material type and to collect nearby concrete batches through a registered pickup trigger. Collection remains latched until the pour action is released.
- Added Unity Editor setup tooling to build the spot prefab and place it in `Dev_Ghaza`.
- Added focused PlayMode tests for sack conversion/rejection, recipe work and leftovers, shovel input routing, cement bucket intake, and concrete bucket pickup.

## Files affected
- `docs/DECISIONS.md`
- `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`
- `Assets/Game/Prefabs/Material/ManualMixingSpot.prefab` and its `.meta`
- `Assets/Game/Scripts/Editor/ManualMixingSpotSetup.cs` and its `.meta`
- `Assets/Game/Scripts/Material/BucketPourAction.cs`
- `Assets/Game/Scripts/Material/ConcreteBatch.cs` and its `.meta`
- `Assets/Game/Scripts/Material/ManualMixingSpot.cs` and its `.meta`
- `Assets/Game/Scripts/Material/ShovelAction.cs`
- `Assets/Game/Scripts/Material/ShovelInput.cs`
- `Assets/Game/Tests/Material/ManualMixingSpotTests.cs` and its `.meta`

## Technical decisions
- Kept ingredient stock in the existing `BulkMaterialContainer` with `MultipleTypes`; generic container behavior is unchanged.
- Kept concrete batch identity on separate output pile objects so freshness can be added to concrete-specific behavior later.
- Used the existing `Player/Pour` action and the existing bucket receiver registration. No new controls, networking code, manager, or event bus were added.
- The recipe calculator from PR A determines whether ingredients can make a batch; one batch is consumed per completed shovel-work threshold.

## Verification performed
- Unity 6000.3.25f1 PlayMode suite: 148 passed, 0 failed, 0 skipped.
- An earlier focused `ManualMixingSpotTests` run passed 5/5; the final full suite also passed after adding the shovel-input routing regression.
- `git -c core.whitespace=-blank-at-eol diff --cached --check` passed. Unity generated standard empty YAML values in the new prefab and `.meta` files.
- Unity batch logs showed successful test completion with no C# compiler warnings/errors or test-run console warnings/errors. Unity licensing reported an unavailable access token during startup; the editor completed compilation and tests successfully.
- The Editor setup method created the prefab and updated `Dev_Ghaza` through Unity APIs.

## Final result
Implementation and automated validation are complete on `feat/manual-mixing`. The Unity Editor is open on `Dev_Ghaza` for the developer's feel test. A human Play Mode review has not yet been performed.

## Known limitations
- Freshness data and multiplayer behavior remain out of scope for MIX-003.
- The default recipe, sack payload, and work threshold are prototype tunings intended for feel review.
- Issue #55 is already closed on GitHub after PR A; this PR will link the gameplay follow-up to the same issue.

## Unresolved issues or follow-up work
- Developer Play Mode feel review remains pending; record observations in PR B before requesting gameplay approval.
- MIX-002 must carry concrete batch freshness through later transfers.
