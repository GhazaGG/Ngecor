# MIX-003 Generic Concrete Recipe Calculator

## Task summary
Implemented reusable pure recipe calculation for cement and sand inputs, with deterministic concrete output and retained ingredient leftovers.

## Relevant previous context
- `.development-history/2026-10-02-14-45-bulk-material-contract-and-manual-mixing.md` records that sack units and mixing values remained undecided.
- `.development-history/2026-10-08-19-25-mix003-recipe-values-blocker.md` records the initial pause because no production ratio was specified.
- The developer then asked to continue building. The implementation accepts recipe quantities as input and does not choose a production ratio or sack conversion.

## Changes made
- Added `ConcreteRecipe`, `ConcreteMixResult`, and `ConcreteRecipeCalculator` alongside the existing material types. The recipe is cement and sand units per batch; concrete output per batch equals their sum, preserving total units.
- Added tests for unit conservation, leftover ingredients, and empty cement input.

## Files affected
- `Assets/Game/Scripts/Material/BulkMaterialContainer.cs`
- `Assets/Game/Tests/Material/BulkMaterialContainerTests.cs`
- `.development-history/2026-10-08-19-38-mix003-recipe-calculator.md`

## Technical decisions
- Keep recipe quantities as caller-supplied data because project decisions do not specify a production ratio.
- Derive concrete output from the sum of recipe inputs so the calculator cannot create or destroy units.
- Leave `BulkMaterialContainer` behavior unchanged; it is not modified for any material type.
- Work is on `feat/mix-recipe`, created from the current `main`.

## Verification performed
- Unity 6000.3.25f1 PlayMode test runner: `Ngecor.Material.Tests`, 55 passed, 0 failed. The three new recipe tests passed.
- `git diff --check` passed.
- No C# compiler warnings or gameplay test errors were found in the run log. Unity logged that its licensing access token was unavailable; the test runner completed successfully.
- No manual gameplay or feel test was relevant to this pure logic change.

## Final result
PR A's generic calculation and requested automated coverage are implemented on `feat/mix-recipe` and published as PR #97: https://github.com/GhazaGG/Ngecor/pull/97. The PR is open and awaiting review.

## Known limitations
- The current project has no selected production ratio or explicit sack-to-unit conversion. Consumers must supply a recipe; this change does not configure the production recipe.
- The existing test suite was run as PlayMode tests; no scene gameplay was exercised.

## Unresolved issues or follow-up work
- Agree and record the production cement:sand ratio and sack conversion before configuring manual mixing in PR B.
- PR #97 has no configured CI checks.
