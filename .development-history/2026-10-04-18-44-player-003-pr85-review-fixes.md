# PLAYER-003 PR #85 Review Fixes

## Task summary

Addressed the implementation and test findings on GhazaGG's change request for PR #85, then prepared the branch for re-review. The PR remains open and unmerged.

## Relevant previous context

- Review targeted commit `b2ea12969591bb566699c4318c924789c338c53c` and identified stale-velocity responses from repeated collider callbacks, step-probe false positives for collision-excluded objects, and two failing 25 kg test thresholds.
- The developer had previously reported manual Play Mode observations in `Playground`: a 1 kg prop stayed bounded by walking speed; a 25 kg CementBag moved slightly under W and slowed to a stop after release; occasional stepping onto the bag was considered reasonable; five stacked bags resisted initially and toppled after sustained pushing; an empty wheelbarrow climbed the ramp; and a wheelbarrow containing `cargo_1`, `cargo_2`, and `cargo_3`, each 1 kg, could be pushed. The surface for the loaded-cart check was not confirmed. These observations predate the fixes in this report.

## Changes made

- Deduplicated contact-push impulses per `Rigidbody` during each `CharacterController.Move`, while keeping the existing shared per-move impulse budget.
- Changed the step capsule query to include all layers, then filtered candidates using the player/object layer collision matrix and explicit ignored collider pairs.
- Adjusted the 25 kg displacement assertions to allow small, measurable movement and extended the low-body test from 30 to 120 captured frames.
- Added regression coverage for repeated same-body callbacks, ignored collider pairs, ignored layer pairs, physically collidable Ignore Raycast objects, and static step traversal.
- Changed pure push-calculation tests to call their public static helpers directly rather than use reflection.

## Files affected

- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- This development-history report.

## Technical decisions

- A per-player `HashSet<Rigidbody>` is cleared before each `CharacterController.Move`. It prevents callbacks for compound colliders on one body from queuing multiple responses calculated from the same stale velocity, without changing the total force budget or suppressing pushes to distinct bodies.
- The probe queries `Physics.AllLayers` because raycast defaults exclude Ignore Raycast; it then applies physical collision exclusions explicitly.
- The 25 kg tests assert gradual, observable movement over a useful observation window rather than requiring an arbitrary displacement that exceeds the measured heavy-body behavior. The force cap was not raised.
- A 25 kg CementBag used as cart cargo remains a separate MAT-003/VEH-002 investigation; it is not treated as proof that PLAYER-003's force cap failed.

## Verification performed

- Test-first baseline: focused `PlayerMovementTests` ran 27 tests; 23 passed and the four new regression tests failed on the reviewed implementation as expected.
- After the fixes, focused `Ngecor.Player.Tests.PlayerMovementTests`: **27 passed, 0 failed**.
- Full Unity PlayMode suite on Unity `6000.3.25f1`: **72 passed, 0 failed**.
- `git diff --check`: passed.
- A read-only review of the current diff found no additional code findings.
- No visible Game View retest or Unity Editor Console inspection was performed after these code changes. The batchmode log contained a Unity licensing token warning; it is not treated as evidence about gameplay Console errors.

## Final result

The three implementation/test findings are addressed and automated PlayMode verification is green. PR #85 is ready for GhazaGG to review the new commit; it was not merged.

## Known limitations

- The developer's manual gameplay observations above were reported on the pre-fix head and have not been repeated after the narrow callback-deduplication and probe-filter changes.
- Current-head visual feel, Unity Editor Console state, and profiling of the per-frame capsule sweep remain unverified.
- The `Dev_RyoFPS` scene and other local work were preserved and are not part of this PR update.

## Unresolved issues or follow-up work

- GhazaGG should repeat the visible Play Mode checks for the actual CementBag, release/deceleration, five-bag stack, light prop, static traversal, empty-cart ramp, and the three 1 kg cargo setup on flat ground.
- Investigate the 25 kg CementBag cart-cargo behavior separately under MAT-003/VEH-002.
