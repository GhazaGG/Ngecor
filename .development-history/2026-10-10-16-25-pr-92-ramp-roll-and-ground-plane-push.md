# PR #92 Ramp Fixes: Steering Roll and Reverse Hop

## Task summary

The owner played head `c62b3a6` in `Playground` and reported:
- On flat ground A/D is fine; on `Ramp_Gentle` and `Ramp_20Degree` A/D lifts the barrow's legs.
- Pushing forward (W) for 2-5 seconds and then pulling back (S) lifts the front wheel immediately, on ramps only (not on flat ground).
- On flat ground S works but is slightly jerky; the owner suspects the wheelbarrow asset.
- Console clean.

## Relevant previous context

- `2026-10-10-17-30-pr-92-roll-gate-and-reverse-investigation.md` could not reproduce the S lift because every test ran on flat ground; the review premise (push applied at handle height) was also incorrect because `PlayerMovement.ApplyContactPush` already forces the push point to centre-of-mass height.
- Steering forces were applied at world centre-of-mass height since `ee4db26`.

## Changes made

`Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- **Steering roll on ramps.** Isolation runs (grip, steering force and the player's sideways step switched off one at a time) showed the roll came from the wheel grip force and the handle steering force, not from the player. Both are horizontal at centre-of-mass height, so their couple acts about the world vertical axis. On a pitched barrow (rest pose about 8 degrees nose up plus the ramp angle) part of that couple falls onto the barrow's forward axis as roll, growing with sin(pitch). Both forces now lie in the plane through the centre of mass perpendicular to the barrow's own up axis (`InYawPlane`, `forceRight`), and the yaw speed is measured about that axis.
- **Reverse hop on ramps.** `PlayerMovement.ApplyMovementPush` pushes horizontally. Pulling back with the nose uphill adds a component away from the ground of about F x sin(slope) (about 78 N at 20 degrees for the 227 N reverse push, twice the 39 N barrow weight), so the whole barrow hopped off the ground. W/S force is now applied along the ground surface (ground normal from the wheel raycast, horizontal fallback) from `WheelbarrowInteraction`, using the public `PlayerMovement.CalculatePushTargetSpeed` and `CalculatePushForce` with the same reverse strength (0.65), the same 350 N limit and the same 0.1 s response. `PlayerMovement` was not changed.
- `TryGetWheelGroundContact` now also returns the surface normal.

`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- Steering prefab tests now also assert relative roll under 5 degrees and both legs staying within 3 cm of the ground (the earlier tests measured total tilt only and let the ramp roll through).
- Added `WThenS` prefab tests on 10 and 20 degree slopes: W up the slope, then S, wheel and both legs must stay within 3 cm of the ground and the barrow must come back down.
- Ground in the prefab fixture is 40 m so the barrow does not leave it during long pushes.
- Test-input finding: with the Input System test fixture, `Press`/`Release` must be called from the Update phase (a coroutine resumed after `WaitForFixedUpdate` has no keyboard state buffer and throws "does not have an associated state"), and releasing one key and pressing another in the same frame cancelled out.

## Files affected

- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-10-16-25-pr-92-ramp-roll-and-ground-plane-push.md`

## Technical decisions

- Forces in the barrow's own yaw plane instead of at world centre-of-mass height: works on any pose, not just flat ground.
- Reimplemented the W/S push inside `WheelbarrowInteraction` rather than changing `PlayerMovement` (an explicit constraint of the review). The 350 N limit is now duplicated as a constant (`MaxPushForce`, same value as `_maxPushForce` on `Player.prefab`); if that inspector value is retuned, this constant must follow.
- Verified the new tests catch the bugs: on the code before each fix the 20 degree steering tests failed with roll about 19 degrees, and the 20 degree W-then-S test failed with a 7.4 cm hop (the 10 degree one passed, as the push component there is about equal to the weight).

## Verification performed

Unity 6000.3.25f1 batchmode PlayMode on a scratch copy of the project (the project's Editor was open):
- Full suite on the final code: 147 tests, 146 passed, 0 failed, 1 skipped (`PourPreviewCreatesMissingDirectoryAndCanCaptureAgain`, needs a graphics device). 26 wheelbarrow tests pass.
- Roll cause isolation (A on a 10 degree slope): all forces on gave about 21 degrees roll and a leg off the ground; grip off about 4.5 degrees; steering force off or player step off 0 degrees.
- W-then-S diagnostic: flat and 10 degrees no lift; 20 degrees the whole barrow hopped about 7 cm at the reversal, no pitch change.
- `git diff --check` on the changed C# files is clean.

## Final result

Both ramp symptoms the owner reported are reproduced in simulation and fixed with tests. The flat-ground jerkiness on S is not addressed.

## Known limitations

- Owner has not re-tested these changes in `Playground`.
- S from rest on flat ground still settles at about 0.6 m/s while W reaches about 3.4 m/s and reverse speed is uneven; ignoring player-barrow collision did not change it. The owner suspects the wheelbarrow asset; not investigated further.
- Downhill-facing ramps (negative slope in the fixture) were not tested: the grip setup in the fixture failed there. The same fix applies in principle (W pushes the barrow away from the ground when facing downhill).
- Profiler not run.

## Unresolved issues or follow-up work

- Owner re-test: A/D on `Ramp_Gentle` and `Ramp_20Degree`; W then S on both ramps; flat ground unchanged.
- Reply on the PR with commit hash and test numbers, including the correction that W/S force was already at centre-of-mass height.
- Optional follow-up: investigate slow, uneven reverse on flat ground and the downhill case.
