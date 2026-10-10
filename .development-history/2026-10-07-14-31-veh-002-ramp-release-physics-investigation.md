# VEH-002 Ramp Release Physics Investigation

## Task summary

Investigate the report that the wheelbarrow remains still near the end of a slope after release, rather than falling onto the flat ground.

## Relevant previous context

- VEH-002 asks that the wheelbarrow remain subject to physics and includes releasing it on a slope in its manual test steps.
- VEH-001 tuning widened the support legs to make the cart less likely to fall.
- The owner reported stable pushing with and without cargo and slope traversal depending on slope scale.

## Changes made

- Added this investigation report only. No gameplay code, prefab, or scene was changed.

## Files affected

- `.development-history/2026-10-07-14-31-veh-002-ramp-release-physics-investigation.md`

## Technical decisions

- A dynamic Rigidbody can remain stationary on a slope when contact support and static friction balance gravity. The acceptance step does not require the cart to fall off every slope.
- Do not lower support friction or narrow the legs without confirming the cart is unsupported and agreeing that it should roll on the tested slope; those changes could undo VEH-001 stability tuning.

## Verification performed

- Read the wheelbarrow prefab values: mass `4`, gravity enabled, not kinematic, and no Rigidbody constraints.
- Confirmed the wheel collider references `WheelLowFriction` (`0.05` static and dynamic friction) and the two support legs reference `FeetHighFriction` (`0.8` static and dynamic friction).
- Reviewed the prior VEH-001 tuning report, which records support-leg positions at `-0.35` and `+0.35` and the intent to reduce tipping.
- Recorded the owner's Play Mode report that the cart stays near the slope edge after release. The screenshot does not establish whether the wheel and support legs have fully left the ramp or whether the center of mass is unsupported.
- No fresh Play Mode manipulation, contact inspection, or Console warning trace was performed.

## Final result

The saved Rigidbody configuration does not disable gravity or freeze rotation. The reported stationary result is consistent with a supported, high-friction cart, so no code or physics tuning change is justified from the available evidence.

## Known limitations

- The exact slope angle, wheelbarrow pose, contact points, and center-of-mass position at release are unknown.
- The `BoxCollider does not support negative scale or size` warning in the screenshot was not traced to a specific object.

## Unresolved issues or follow-up work

- If the wheelbarrow is still stationary after its wheel and support feet have fully cleared the ramp edge, inspect the runtime collider contacts and the Console warning.
- If the intended design requires the wheelbarrow to roll down gentle slopes while supported, clarify that as a tuning requirement before changing the existing support friction or leg geometry.
