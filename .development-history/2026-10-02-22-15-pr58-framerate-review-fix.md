# PR #58 frame rate review fix

## Task summary

Address the team lead's PR #58 review: make contact pushing independent of render frame rate, prove it with a PlayMode regression, and refresh the PR's test evidence. PR #67 remains stacked until #58 merges.

## Relevant previous context

PR #58 introduced generic CharacterController contact pushing with `ForceMode.Force` from an `Update`-driven collision callback. PR #67 adds the wheelbarrow and has a Playground override of `Push Strength` at 1.3. Earlier manual feedback favored that value for the cart with the old force behavior. The team lead requested a 30/120 FPS comparison and a fresh full PlayMode run.

## Changes made

- Apply `ForceMode.Impulse` scaled by `Time.deltaTime` at the existing contact point.
- Compare one second of pushing at 30 and 120 FPS in a new PlayMode test.
- Allow the existing frame helper to set the capture frame rate.
- Synchronize physics transforms after the existing top-contact test teleports the player; that test failed in some full-suite runs but passed in isolation before this setup correction.
- Tune the source default `Push Strength` to 5 after RyoFPS's Editor playtest. The previous default 2 moved the test cube but felt too weak; a trial at 1.3 failed the existing dynamic-prop displacement check.
- Update the PR #58 description with actual automated and reported manual results.

## Files affected

- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `.development-history/2026-10-02-22-15-pr58-framerate-review-fix.md`

## Technical decisions

The contact filters and generic push direction remain unchanged. The regression uses a 5 kg test body with vertical motion and rotation frozen so the frame-rate comparison focuses on horizontal impulse. No Unity-serialized assets, packages, networking code, or gameplay scene were committed in this round. RyoFPS's local Playground test edits remain uncommitted.

## Verification performed

- Baseline before edits: Unity 6000.3.25f1 PlayMode, 16/16 passed.
- New regression on the old force code: failed as intended, with 0.588 m at 30 FPS and 2.130 m at 120 FPS.
- New regression on the impulse code: passed.
- Full PlayMode suite at default 2: 17/17 passed on two consecutive runs after the top-contact test setup correction.
- Full PlayMode suite at the final default 5: 17/17 passed, 0 failed.
- RyoFPS reported in Unity Editor that a dynamic cube and `Ball_8` moved at strength 5, without launching; after setting those test bodies to Rigidbody `Interpolate`, visible jitter disappeared. The cube sometimes veered slightly under off-center physical contact. RyoFPS reported no Console errors. The assistant did not perform the visual Editor playtest.
- `git diff --check`: passed.
- An initial batch command that included `-quit` produced no test results; it was corrected before any test result was reported.

## Final result

The frame-rate defect, regression, and ordinary-prop tuning are addressed. PR #58 is ready for the team lead's second review.

## Known limitations

The wheelbarrow and ramp have not been retested with the new impulse code. No claim is made that the cart has been retuned. Small props below the CharacterController step height can still be stepped onto. Rigidbody interpolation was applied only to local test props; PR #58 does not include those scene changes.

## Unresolved issues or follow-up work

- Mark PR #58 Ready for review and request the team lead's re-review.
- After #58 merges to `main`, continue PR #67, retarget it to `main`, merge `origin/main`, and complete its Editor-only prefab/scene changes and playtest.
