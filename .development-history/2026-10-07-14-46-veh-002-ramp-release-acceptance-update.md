# VEH-002 Ramp Release Acceptance Update

## Task summary

Reconcile the owner's empty-wheelbarrow ramp result and the BoxCollider warning attribution with VEH-002 acceptance.

## Relevant previous context

- VEH-002 requires the wheelbarrow to remain subject to physics and asks the tester to release it on a slope; it does not require the cart to fall to flat ground on every release.
- The prefab has gravity enabled, a dynamic Rigidbody, and no Rigidbody constraints.
- The owner reported stable pushing both with and without cargo, and that slope traversal depends on ramp scale.

## Changes made

- Added this acceptance update only. No gameplay code, prefab, or scene was changed.

## Files affected

- `.development-history/2026-10-07-14-46-veh-002-ramp-release-acceptance-update.md`

## Technical decisions

- A stationary cart can still be subject to physics when its wheel and support legs remain in contact with the ramp and friction balances the downhill force.
- Do not treat falling to flat ground as a required outcome unless the issue's acceptance criteria are explicitly expanded to require rolling off a ramp edge.
- The owner clarified that the negative-scale BoxCollider warning comes from `Box_Heavy`, not the wheelbarrow.

## Verification performed

- Recorded the owner's Play Mode result: the empty wheelbarrow was released near/over the ramp lip and remained on the incline.
- The screenshot shows the wheel and support legs still appearing to contact the ramp; exact runtime contacts and center-of-mass position were not independently measured.
- Recorded the owner's confirmation that the Console warning is associated with `Box_Heavy`.
- No fresh Play Mode run or automated tests were performed.

## Final result

The owner's reported push tests and the saved dynamic Rigidbody configuration support the written VEH-002 acceptance. The cart remaining supported on the incline does not by itself show that gravity is disabled. No physics tuning change was justified.

## Known limitations

- Exact ramp angle and runtime contact points were not captured.
- Guaranteed downhill roll-off is not currently an explicit acceptance requirement.

## Unresolved issues or follow-up work

- If the intended behavior is to roll down to flat ground after crossing the ramp edge, add that as an explicit gameplay requirement and tune the wheel/support friction against that target.
