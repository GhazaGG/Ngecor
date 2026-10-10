# PR #92 — owner feel test: steering approved, reverse lifts the front wheel

## Task summary
Opened Unity for the owner's feel test of PR #92 (`[VEH-002] Wheelbarrow player interaction`, head `ee4db26`). Recorded the result as a Request Changes review with an updated AI handoff prompt that supersedes the earlier one.

## Relevant previous context
`2026-10-10-14-45-rereview-pr-92-steering-tilt-gate.md`: the code review requested a roll-only steering tilt gate and optional Inspector-tunable steering values; the owner's feel test was pending.

## Changes made
- Moved the isolated review worktree `C:\Users\Hype\AppData\Local\Temp\ngecor-pr-92-review` from `31ce142` to exact head `ee4db26` (detached, clean).
- Its `Library/PackageCache` was corrupt: `com.unity.inputsystem` held only `package.json`, NGO was missing, and the first launch produced CS0234/CS0246 compile errors. Deleting only `PackageCache` was not enough, because Unity restored the resolved-package state from `Library`. Stopped the review Unity processes, deleted that worktree's whole ignored `Library`, and relaunched; the fresh import finished with 0 compile errors.
- Relaunched Unity once more at the owner's request after the Editor had closed (0 errors).
- Posted the owner feel-test review (CHANGES_REQUESTED) and a replacement handoff prompt on PR #92.
- No source, scene, prefab, or settings change.

## Findings
- **Owner feel test:** steering A/D is approved. Reversing with S makes the front wheel lift (screenshot shared by the owner in session).
- **Cause, from code and prefab geometry:**
  - `LateUpdate` applies the W/S push through `ApplyMovementPush` at the handle centre. The handles sit at local y 0.9, about 0.7 m above the explicit COM (local `(0, 0.2, 0.45)`, `m_ImplicitCom: 0`) and 1.4 m behind it.
  - A backward force there creates a nose-up pitch moment of about F × 0.7 m. The 4 kg barrow (~39 N, ~0.7 m arm about the legs) cannot resist the capped push force, so the front wheel lifts.
  - Forward W pitches the nose down, so only reverse shows it.
- **Requested fix:** apply the W/S push at COM height at the handle's horizontal position, the same pattern as the steering roll fix. Add prefab tests that hold S with the wheel grounded every step and pitch < 5°, for empty and 3-cargo loads.
- Kept the roll-only tilt gate as Must Fix 2.
- Downgraded the Inspector-tunable steering values to optional, because the owner approved the steering feel.

## Files affected
- `.development-history/2026-10-10-15-22-pr-92-owner-feel-reverse-lift.md`

## Verification performed
- Unity Editor log showed 0 `error CS` after the fresh import and after the relaunch.
- Feel result is the owner's direct Play Mode test. The pitch-moment explanation comes from code and prefab values; no force or angle trace was recorded.

## Final result
PR #92: CHANGES_REQUESTED, with two small Must Fixes (reverse push height and the roll-only tilt gate). The steering feel gate is passed.

## Known limitations
- The reverse-lift cause was not measured in Play Mode.
- The tilt-gate behavior on `Ramp_20Degree` is still code-derived; the owner did not report testing that ramp.

## Unresolved issues or follow-up work
- Owner re-check of S and ramps after Ryo pushes.
- Follow-up issue for pushing heavy cement-sack loads (leg friction while held).
- Unity remains open on the review worktree for the owner.
