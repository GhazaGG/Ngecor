# VEH-001 ramp stall investigation

## Task summary
Investigated the latest report that the loaded wheelbarrow still stalls on the ramp after the wider-wheel and raised-PushBar test. The user reported that `Box_8` climbs the same ramp at Push Strength 8, and then compared the wheelbarrow empty and with cargo.

## Relevant previous context
- `.development-history/2026-10-02-21-43-review-pr-58-67-veh-001.md` records the PR review and the need to verify the loaded-cart ramp test after the contact-push change.
- The user has already tried several combinations of push strength, CharacterController step offset, wheelbarrow mass, center of mass, PushBar height, and wheel support width. The latest reported result remains a stall.
- Issue #13 requires physics pushing on flat ground, an empty cart climbing a simple slope, carrying at least three test cubes on flat ground, and cargo spilling when tipped. The latest PR #67 review clarifies that a loaded-cart ramp ascent is bonus behavior.

## Changes made
- Added this investigation report only. No gameplay assets or code were changed in this turn.

## Files affected
- `.development-history/2026-10-03-17-15-veh-001-ramp-stall-investigation.md`

## Technical decisions
- Do not make another speculative physics-value adjustment yet. The current report confirms that the wider wheel and raised PushBar did not resolve the stall, but a still image cannot identify whether the ramp, shared push behavior, or wheelbarrow-specific contacts are responsible.
- The successful `Box_8` ramp run rules out a generally impassable ramp or a complete failure in shared contact pushing.
- User-reported wheelbarrow comparison: empty reaches the top at Strength 8; one cargo goes farther but stalls at Strength 8; one cargo reaches the top at Strength 20. This strongly indicates the current push force is insufficient for the added load at Strength 8, though compound collider contacts and support-foot friction may add resistance.
- User-reported follow-up: all three cargo cubes reached the ramp top at Strength 20, but pushing `Box_8` at the same strength felt too brutal. This rules out Strength 20 as a safe global default without changing the shared push behavior.
- The PR review flags load scaling as a follow-up design issue for VEH-002/#46 and says not to solve it in PR #67. A mass-aware/target-speed push therefore needs an explicit scope decision before code changes.
- User-reported isolation test: with three cargo and Strength 8, setting both foot-material friction values to 0 left the cart stuck at the ramp lip and made it less stable; friction 0.8 had felt firmer and less prone to tipping.
- User then reported COM Z=0.45 and foot friction Static=0.6/Dynamic=0.3. At Strength 8, the empty cart stayed stable and reached the ramp top; with cargo (count not specified), it reached only about halfway up the ramp.
- An initial local state check found COM Z=0.45 only as a scene instance override. The user then confirmed changing the prefab; a follow-up local check confirmed the prefab now stores COM Z=0.45 and the scene has no COM Z override.
- After setting COM Z=0.45 in the prefab, the user reported that pushing the three-cargo cart on flat ground at Strength 8 made the whole cart lose balance, tip over, and spill its cargo. This fails the three-cargo flat-ground handling check. The test changed COM and foot friction relative to the prior setup, so the cause is not isolated.
- Follow-up user-reported flat-ground trials: with foot friction 0.3/0.6, the cart tipped at Strength 8 and still tipped at Strength 5; with friction 0.8/0.8, Strength 8 was less stable than 0.3/0.6 but required careful steering; Strength 5 with friction 0.8/0.8 felt stable and consistent. The surrounding test context was the three-cargo cart.
- Clarification: at Strength 8, the three-cargo cart tips sideways to the right or left (not a full 180-degree flip) and spills the cargo. At Strength 5 with foot friction 0.8/0.8, it remains stable. It is not yet established whether a centered straight push at Strength 8 also tips it or whether steering/off-center contact triggers the side fall.
- Correction from the user: the centered straight-push test that briefly wobbled and retained all cargo was at Strength 5, not 8. At Strength 8, the cart wobbles and spills cargo. Do not use the earlier assistant inference that centered Strength 8 was stable.
- Latest correction/test detail: with three cargo on flat ground at Strength 6, the cart only wobbles briefly at the start and then remains upright without spilling cargo. The empty cart still wobbles/falls on the ramp at Strength 6 (and had the same issue at Strength 5). Earlier, the empty cart reached the top at Strength 8. Strength 8 with cargo was reported to tip and spill, but exact steering/contact details were not recorded.
- The current local prefab wheel center is at z=1.422, while the TL COM load-split calculation used wheel z=0.914. With feet at z=-0.25 and COM z=0.45, a simple two-support estimate gives roughly 42% of cart load at the wheel rather than the review's approximately 60%. This is an approximate model and needs TL confirmation before further COM changes.
- Local state check confirms the final candidate values currently saved are foot friction 0.8/0.8, COM Z=0.45, and `PlayerMovement` code default Strength 5. `Playground.unity` still has a saved Strength 8 instance override that must be reverted after final validation.
- Preserve the user's current Unity scene and prefab edits while the cause is isolated.

## Verification performed
- Read the current `PlayerMovement.OnControllerColliderHit` implementation. It applies the contact impulse at `hit.point`, with impulse magnitude scaled by `_moveSpeed * _pushStrength * Time.deltaTime`.
- Read the current GitHub issue #13 acceptance criteria and PR #67's review/test notes.
- User-reported Play Mode comparison: `Box_8` reached the ramp top and fell from it at Push Strength 8, but felt too brutally pushed at Strength 20; empty wheelbarrow reached the top at Strength 8; one-cargo cart stalled midway at Strength 8 and reached the top at Strength 20; three-cargo wheelbarrow reached the top at Strength 20.
- User-reported foot-friction isolation: zero foot friction did not get the loaded cart past the ramp lip and reduced stability.
- After the requested COM/friction tuning, the user reported the empty cart passes the ramp at Strength 8, while a loaded cart stalls around halfway (cargo count not specified).
- Local asset inspection confirms the foot material currently has Static=0.6, Dynamic=0.3, Friction Combine=Maximum. The prefab stores COM Z=0.45 and the scene has no COM Z override.
- User-reported current regression: with three cargo on flat ground, pushing at Strength 8 caused the whole cart to tip and spill its contents after COM Z=0.45 and foot-friction tuning.
- User-reported follow-up: with the three-cargo flat-ground setup, Strength 5 and foot friction 0.8/0.8 was stable and consistent; the higher-strength combinations were less stable.
- User clarified that the Strength 8 failure is a sideways tip to the right or left with cargo spilling. A straight, centered push has not yet been isolated.
- User corrected the centered straight-push result: Strength 5 caused a brief wobble then stable movement with cargo retained; Strength 8 caused wobble and cargo spill.
- At Strength 6, three cargo on flat ground stayed in the cart after an initial wobble; the empty cart fell on the ramp at Strength 6, as it had at Strength 5. The empty cart had reached the top at Strength 8 in an earlier test.
- Local state check: feet friction is 0.8/0.8 (Maximum), prefab COM Z=0.45, code default Strength=5, and Playground still overrides Strength to 8.
- No Play Mode test was run by the assistant; these results are user-reported.
- Confirmed the working tree already contains user/task changes to the wheelbarrow prefab and Playground scene, plus unrelated Unity ProjectSettings changes. These were left untouched.

## Final result
With COM Z=0.45 and feet restored to Static/Dynamic Friction 0.8/0.8, the user reports three-cargo flat movement is stable at Strength 5 and remains stable at Strength 6 after an initial wobble. The empty cart tips/falls on the ramp at Strength 5 and 6, while an earlier Strength 8 run reached the top. Strength 8 with cargo was reported to tip and spill, but steering/contact details were not captured. Strength 20 gets three cargo up the ramp but makes `Box_8` feel too brutal. Loaded ramp ascent is a bonus rather than an AC for #13 per the latest TL review. The code default is 5; the saved Playground override remains 8. The local wheel center z=1.422 differs from the z=0.914 used in the TL's COM estimate, so the load split should be recalculated/confirmed before more tuning.

## Known limitations
- Strength 5 and 6 are reported stable for three cargo on flat ground, but the empty cart falls on the ramp at both; Strength 8 reached the top in an earlier test but loaded-cart spill was also reported. Resolve test conditions and the wheel-position/COM assumption with the TL before selecting a default.
- Console state under the latest tuning was not reported.
- Whether VEH-001 should expand to include mass-aware contact pushing or defer that behavior remains undecided; the PR review places it outside PR #67.
- No fresh Unity Play Mode reproduction was run by the assistant.

## Unresolved issues or follow-up work
- Decide scope with the project lead before changing the shared push behavior. Keep the global default low enough for ordinary objects; if loaded-vehicle pushing must work at the same feel, address mass-aware/target-speed handling in its approved ticket.
- Ask the TL to confirm whether the local wheel position z=1.422 should remain or be restored to the reviewed z=0.914, since this changes the support-load estimate. After alignment, rerun a consistent empty-ramp/three-cargo-flat comparison and choose a default. Then finish intentional spill and Console checks.
- Complete the core checks: empty flat ground, three cargo flat ground, deliberate tipping/spill, and Console. Record loaded-ramp stall as a bonus limitation if it persists.
- Confirm cargo containment and Console state from the user's Strength 20, three-cargo run before updating the PR's acceptance notes.
- After the cause is isolated, repeat the loaded three-cube ramp test and update PR #67's reported results before claiming VEH-001 complete.
