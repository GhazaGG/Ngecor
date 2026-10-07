# BUILD-001 Ramp Scaffolding Physics Verification

## Task Summary

Address the BUILD-001 review feedback for hard impacts, then run the remaining Unity checks that can be performed safely in the `Dev_DePo4l` scene. The active access design is the ramp requested by the developer.

## Relevant Previous Context

- PR #84 remains an open draft against `main` and has a `CHANGES_REQUESTED` review decision.
- The current branch contains one `ScaffoldingModule` prefab and the development scene with its module, ramp, and cement bags. Carry, ghost placement, and player assembly remain outside this ticket.
- Review feedback requested a side-contact impact threshold, stable normal loading, overload collapse, preservation of broken parts, and a low-mass impact that does not collapse the module.

## Changes Made

- `OnCollisionEnter` now uses the larger of PhysX's reported impulse and the incoming rigidbody's normal momentum (`mass × normal approach speed`). This covers the kinematic scaffold case where the reported impulse is zero.
- Impact detection remains restricted to dynamic rigidbodies and side contacts, and ignores contacts while the module is already wobbling.
- `CalmDown` restores the saved pose and clears velocities before making the body kinematic.
- Removed the uniquely named temporary test cargo through Unity Editor after Play Mode. Restored the original scene objects and player position. The scene was not saved.

## Files Affected

- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`
- `.development-history/2026-10-06-18-37-build-001-physics-verification.md`

## Technical Decisions

- Kept the configured defaults at 90 kg load threshold, 2 seconds of wobble, and 50 N·s impact threshold.
- Used the incoming momentum estimate only as a fallback alongside PhysX impulse; no counters, randomness, or extra gameplay systems were added.
- Preserved the existing four physical break groups and configured masses: 20, 20, 12.5, and 12.5 kg.

## Verification Performed

- Unity 6000.3.25f1, `Dev_DePo4l`: compilation completed with no failure; final Editor state is stopped.
- Normal load: the runtime probe measured 75 kg from three 25 kg cement bags. With the player moved up the ramp and across the upper platform using `CharacterController.Move`, the intact module remained kinematic.
- Ramp traversal: the direct controller sweep reached the upper platform at approximately `(3, 2.14, 0.2)` without passing through the ramp or platform. This did not exercise keyboard input or gameplay feel.
- Overload: three bags plus a temporary 25 kg rigidbody measured 100 kg. The module entered its dynamic wobble phase and the root was destroyed after the configured interval.
- Hard impact: a 25 kg sphere moving at 10 m/s contacted a side support. The intact body became dynamic, then broke into four surviving rigidbodies with the configured masses.
- Low-mass impact: a 1 kg sphere at 10 m/s contacted the same post; the root remained kinematic and intact after 2.5 seconds.
- Final Unity Console status was cleared of the temporary CLI lookup/stop diagnostics; error and warning counts were zero. No compilation errors were present.
- `git diff --check` passed.

## Final Result

The impact threshold now distinguishes a high-momentum object from a 1 kg, 10 m/s object in the tested side-contact setup. Normal 75 kg loading, direct ramp traversal, 100 kg overload collapse, and four-part persistence were observed. PR #84 remains a draft and is not ready for acceptance sign-off.

## Known Limitations

- Reducing the load during the wobble phase was not verified; the overload collapsed before the temporary cargo could be removed.
- The player was not driven through the ramp using normal keyboard input, and jitter/gameplay feel were not assessed.
- A separate ground-level player-to-support bump and an actual `PlayerGrab` throw or bag sliding down the ramp were not verified. The hard-impact test used a directly configured rigidbody.
- The scene was left unsaved; runtime test changes are not part of the PR.

## Unresolved Issues or Follow-up Work

- Re-test load-removal recovery and a ground-level support bump in a controlled Play Mode session.
- Verify keyboard movement, ramp feel, and a real material throw/slide in Game View before checking the remaining PR acceptance boxes.
- Request a fresh review after pushing the impact fix and verification notes.
