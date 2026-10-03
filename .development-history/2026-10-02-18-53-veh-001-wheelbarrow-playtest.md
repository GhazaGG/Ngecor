# VEH-001 Wheelbarrow Playtest Stability Investigation

## Task summary
Reviewed the reported wheelbarrow tipping and cargo-spill behavior. No gameplay assets or code were changed.

## Relevant previous context
- Issue #13 requires stable Rigidbody/Collider behavior, pushing on flat ground and a slope, carrying three test cubes, spill when tipped, and tunable mass/handling.
- Earlier contact-push testing confirmed the player can push a large dynamic box and kick a ball at the current push strength.

## Changes made
- No product code or Unity assets changed.
- Added this investigation report.

## Files affected
- This report only.

## Technical decisions
- The saved WheelBarrow.prefab is a compound primitive prototype, not a single collider cube: its root has one Rigidbody and its children include tray bottom/sides/back, two handles, two legs, and a wheel with a CapsuleCollider.
- The tray has no front wall. A cube can slide out toward the wheel with little tilt, so add a low front lip before treating minor cargo loss as a mass/friction issue.
- The root Rigidbody uses mass 4, angular damping 0.05, and implicit/automatic center of mass. The center of mass is therefore not manually lowered despite the earlier intended tuning.
- PlayerMovement.OnControllerColliderHit applies AddForceAtPosition at the actual contact point. An off-center bump creates torque, which can tip a light, top-heavy cart.
- Cargo should stay as separate dynamic objects under PhysicsObjects; it should not be parented to the wheelbarrow. Keep their centers low and inside the tray walls.
- Keep player push behavior unchanged for this diagnosis; tune the wheelbarrow's geometry and Rigidbody first.

## Verification performed
- Read the current issue #13 acceptance criteria.
- Inspected WheelBarrow.prefab, Playground.unity, PlayerMovement.cs, and the prior contact-push playtest investigation.
- Observed Cargo_1, Cargo_2, and Cargo_3 under PhysicsObjects in the open Unity Editor. The scene was dirty, and those cargo objects were not present in the saved scene file at inspection time.
- No Play Mode test was run during this investigation; the reported tipping and spill behavior is developer-reported.

## Final result
The most supported causes are the automatic center of mass over the full compound shape, low angular damping, real torque from off-center contact pushes, and the missing front tray wall.

## Known limitations
- The cargo objects' exact transforms, masses, and scales were not captured.
- The exact contact location and the cart's full runtime trajectory during the reported bump were not observed.
- Manual Unity reproduction is still required.

## Unresolved issues or follow-up work
- In Prefab Mode, add a low front lip to the tray.
- Set a lower manual center of mass and test a higher angular damping value, changing one Rigidbody setting at a time.
- Re-test normal pushes with three cubes on flat ground and the ramp; then intentionally tip the cart to confirm cargo can still spill.
