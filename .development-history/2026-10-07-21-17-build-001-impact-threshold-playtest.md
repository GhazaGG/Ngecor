# BUILD-001 Impact Threshold Playtest

## Task Summary

Tune the scaffolding side-impact threshold after the latest review of merged PR #84, then verify the threshold with low- and high-momentum impacts in Unity before preparing a follow-up PR.

## Relevant Previous Context

The review noted that a 25 kg sack pushed by the player could collapse the scaffolding at the existing 50 N·s threshold. The owner chose to exclude player mass from scaffold load calculations and suggested testing a higher impact threshold. A previous local follow-up had changed the prefab value to 75 N·s, but its player-driven check was inconclusive.

## Changes Made

- Kept `_maximumImpactImpulse` at 75 N·s on `ScaffoldingModule.prefab`.
- Made no gameplay code or scene changes.

## Files Affected

- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab`
- `.development-history/2026-10-07-21-17-build-001-impact-threshold-playtest.md`

## Technical Decisions

The 75 N·s value gives margin above the measured ordinary sack impact (about 51 N·s) while preserving a higher-speed impact path. This tunes the existing Inspector field only; load thresholds and collision filtering remain unchanged.

## Verification Performed

- Unity Editor 6000.3.25f1, `Dev_DePo4l`, Play Mode.
- Three 25 kg sacks on the platform (75 kg total) remained stable; the scaffold Rigidbody stayed kinematic.
- A 25 kg sack struck a support at 2.05 m/s (about 51.25 N·s). It stopped at the support and the scaffold stayed kinematic.
- A 25 kg sack struck a support at 3.2 m/s (about 80 N·s). The scaffold began its wobble and broke; the module root disappeared and the four frame/platform groups remained as Rigidbody objects alongside the four sacks.
- Exited Play Mode without saving. The active scene was not dirty afterward.
- Unity reported no compilation failure and zero warnings. The Console still reported three errors: Input System device-discovery assertions and a test lookup for the module root after it had already been destroyed. These were not gameplay-code compilation errors.
- `git diff --check` passed.

## Final Result

The 75 N·s threshold passed the low-momentum stability check and still allowed a higher-momentum side impact to break the module into four physical groups.

## Known Limitations

- Automated W input did not move the player in this Editor session, so the player-driven push path was not independently repeated at 75 N·s. The low-momentum check used a Rigidbody impact with the corresponding 2.05 m/s speed.
- The PR #84 review already recorded a successful player-driven sack push at the earlier 50 N·s threshold; this follow-up verifies the physical threshold margin, not a new manual-input playtest.

## Unresolved Issues or Follow-up Work

- Confirm the feel of the 75 N·s threshold with a manual player push during the next owner playtest.
