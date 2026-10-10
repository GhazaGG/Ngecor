# MIX-003 Recipe Values Blocker

## Task summary
Reviewed the MIX-003 handoff, its issue and downstream consumers, project decisions, relevant history, and current material scripts. Implementation is paused because the recipe and cement sack conversion values are not specified.

## Relevant previous context
- `.development-history/2026-10-02-14-45-bulk-material-contract-and-manual-mixing.md` records that bag unit size and manual mixing values were still unresolved.
- `docs/DECISIONS.md` specifies a 25 kg cement sack and 1 kg per material unit, but does not say how many cement units a sack yields or define the cement:sand:concrete ratio.
- MIX-003 is intended to provide reusable recipe logic for BUILD-002, MIX-002, and MIX-001.

## Changes made
No gameplay code or Unity assets were changed. Added this investigation report to record the missing decisions and pause point.

## Files affected
- `.development-history/2026-10-08-19-25-mix003-recipe-values-blocker.md`

## Technical decisions
- Do not implement PR A until the developer confirms the sack conversion and recipe quantities. Choosing either would violate the MIX-003 handoff gate.
- Read the existing `BulkMaterialContainer`, `ShovelAction`, `ShovelInput`, `GroundMaterialPile`, and `CementBag`; no existing recipe or sack-unit conversion is implemented in these components.
- No branch was created; the checkout remains on `main` with the pre-existing untracked files preserved.

## Verification performed
- Read the relevant project instructions, design/workflow/structure/decision documents, MIX-003 and downstream issues, and related material history.
- Inspected the current material scripts listed above.
- Confirmed the working tree is on `main...origin/main`; no tests or Unity Play Mode were run because no implementation was made.

## Final result
MIX-003 is blocked on two product values: cement units produced by one full 25 kg sack, and the recipe quantities for cement, sand, and concrete output.

## Known limitations
- Recipe conservation behavior cannot be specified or tested until the material unit quantities are agreed.
- Unity gameplay and feel remain unverified.

## Unresolved issues or follow-up work
- Confirm the sack conversion and recipe equation in material units. Then implement and validate PR A on `feat/mix-recipe` before beginning PR B.
