# Water in the shared recipe, material economy, and MAT-006

## Task summary
The project owner approved the lead's proposal to: (A) make the concrete recipe calculation aware of water and remove the contradiction in DECISIONS, (B) open a separate water logistics ticket, and (C) record the material economy for the Rumah Pak Ujang slice.

## Relevant previous context
- `2026-10-09-20-10-mix003-refactor-dry-wet-mixing.md`: water, `WaterDrum` (200 units), and the dry/water/wet mixing order were added to PR #98.
- PR #97 merged `ConcreteRecipeCalculator` with cement + sand only. It was used only by tests.
- `ManualMixingSpot` kept its own per-batch fields and summed them itself.
- DECISIONS line "Resep MVP: cement + sand. Kerikil dan air ditambah ... setelah playtest logistik" contradicted the 2026-10-08 entry that already includes water.

## Changes made
- `ConcreteRecipe` takes `waterUnits`; `ConcreteMixResult` reports leftover water; `ConcreteRecipeCalculator.Mix` takes water and limits batches by it.
- `ManualMixingSpot` builds a `ConcreteRecipe` from its existing serialized fields and uses it in `ConvertBatch`. Serialized field names are unchanged, so prefab values are kept.
- Tests in `BulkMaterialContainerTests` updated for water, plus one test that water limits batch count.
- DECISIONS: bulk contract recipe line now reads cement + sand + water (gravel still deferred). New entry "2026-10-09 — Ekonomi bahan cor Rumah Pak Ujang": target 160 concrete, 3 cement sacks, sand pile 100, water drum starting at 20, unlimited tap (MAT-006).
- GitHub: created #100 MAT-006 (water source, drum, container mass from contents; M2, P1, assigned DePo4l) and added it to EPIC-003 #31. Commented the economy numbers on #19 and #26.

## Files affected
- `Assets/Game/Scripts/Material/BulkMaterialContainer.cs`
- `Assets/Game/Scripts/Material/ManualMixingSpot.cs`
- `Assets/Game/Tests/Material/BulkMaterialContainerTests.cs`
- `docs/DECISIONS.md`

## Technical decisions
- Kept the spot's serialized int fields instead of a shared ScriptableObject recipe asset. A shared asset is only needed when the mixer (MIX-001 #17) exists; it can construct the same `ConcreteRecipe` until then.
- Container mass following its contents was put in MAT-006 as a generic `BulkMaterialContainer` change, because `PlayerGrab` samples `rb.mass` only at grab time and containers never change mass today.

## Verification performed
- Unity 6000.3.25f1 batch PlayMode run (`-runTests -testPlatform PlayMode`): 175 passed, 0 failed, 0 skipped, including the four `ConcreteRecipe*` tests.
- No manual Play Mode check was done for this change.

## Final result
Recipe math knows water in one struct, DECISIONS is consistent, and water logistics has its own ticket.

## Known limitations
- Level supply numbers are recorded only; the `SandPile` (80) and `WaterDrum` (200) prefabs keep their dev values until LEVEL-001 places level instances.

## Unresolved issues or follow-up work
- MAT-006 #100 after PR #98 merges.
- Revisit cement value per sack after the MVP playtest (one sack can make up to 100 concrete).
