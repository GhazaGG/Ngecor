# PR #92 Roll Gate, Feel Values and Reverse (S) Investigation

## Task summary

Address the follow-up reviews GhazaGG posted on PR #92 against head `ee4db26`:

1. Code review: the 15 degree steering tilt limit measured total tilt (including pitch), so A/D stopped working on `Ramp_20Degree`. Must measure roll only. Also make the three steering feel values tunable (later downgraded to optional).
2. Owner feel test: A/D feel approved; pulling the barrow backward (S) reportedly lifts the front wheel. Review Must Fix 1 asked to apply W/S force at centre-of-mass height and add tests.

## Relevant previous context

- `2026-10-10-14-15-pr-92-steering-rereview-fixes.md` (head `ee4db26`) introduced player-led steering applying steering force at centre-of-mass height.
- `PlayerMovement.ApplyContactPush` already does `point.y = body.worldCenterOfMass.y`, so W/S force from `WheelbarrowInteraction.LateUpdate` was already applied at centre-of-mass height before this task. `PlayerMovement` must not be changed in this PR.

## Changes made

`Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs` (commit `594d9f0`)
- Steering is gated on roll only: `Mathf.Abs(90f - Vector3.Angle(transform.right, Vector3.up))`. Pitching up or down a ramp keeps the right axis horizontal, so slopes no longer disable A/D.
- `_maxSteeringSpeed`, `_steeringLeadSpeed` and `_maxSteeringLead` are now `[SerializeField]` fields with the same defaults (2, 1.5, 0.5). Stability constants stay `const`. No steering behaviour or default value changed.

`Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `594d9f0`: prefab tests for A and D on a 20 degree slope (wheel grounded), and a test that steering stays off when the barrow is rolled 25 degrees.
- This commit: W and S prefab tests (empty and three 1 kg cargo, 1 second, grounded) that assert travel direction, pitch change under 5 degrees and front wheel rise under 3 cm, replacing the earlier single W cargo test. The prefab/slope helpers take a slope angle and re-aim the camera before detection.

No `.unity`, `.prefab`, `.asset` or `.meta` file was edited, and `PlayerMovement` was not changed.

## Files affected

- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-10-17-30-pr-92-roll-gate-and-reverse-investigation.md`

## Technical decisions

- Roll gate: verified the new tests are meaningful by temporarily installing the old total-tilt gate in the scratch copy only; both 20 degree slope tests then fail (handle shift 0.0) and pass with the new gate.
- Reverse (S) fix was **not** applied: the proposed change (force at centre-of-mass height) is already how the force is applied, so it would not change behaviour, and the reported front wheel lift could not be reproduced (see below). No speculative physics change was made.

## Verification performed

Unity 6000.3.25f1 batchmode PlayMode on a scratch copy of the project (the project's Editor was open):
- Full suite on the final code: 145 tests, 144 passed, 0 failed, 1 skipped (`PourPreviewCreatesMissingDirectoryAndCanCaptureAgain`, needs a graphics device). 24 wheelbarrow tests pass.
- Reverse investigation, prefab on flat ground (empty and three 1 kg cargo): S for 1 second moves the barrow backward, pitch change under 5 degrees, front wheel rise under 3 cm.
- Reverse investigation in a temporary Play Mode test that loaded `Playground.unity` (the scratch copy contained the local uncommitted Playground edits) with the real Player and Wheelbarrow, movement input injected before `LateUpdate`, at 30, 60 and 144 fps: no front wheel rise and no pitch change (about 0.1 degree at most) for S from rest and for W followed by S. That diagnostic file was not committed.
- `git diff --check` on the changed C# files is clean.

## Final result

Roll gate and feel-value changes are done and tested. The owner-reported front wheel lift during S is **not reproduced** by any automated check; no change was made for it.

## Known limitations

- Observed but not explained: in the Playground simulation S from rest settles at about 0.6 m/s while W reaches about 3.4 m/s, and speed is uneven when reversing after W (roughly -0.5 to -1.2 m/s). Ignoring player-barrow collision did not change this. This may relate to the owner's "jengat" impression but was not investigated further.
- The owner's repro conditions (starting situation, frame rate, local scene edits, loaded or empty) are unknown; the lift may depend on something the simulation lacks.
- Manual feel in `Playground` was not done by me. Profiler not run.

## Unresolved issues or follow-up work

- Ask the owner/reviewer for exact S repro conditions or a short capture, and tell the reviewer that W/S force is already at centre-of-mass height.
- If needed, investigate why reverse is slow and uneven compared with forward push.
- Reply on the PR with commit hash and test numbers once confirmed.
