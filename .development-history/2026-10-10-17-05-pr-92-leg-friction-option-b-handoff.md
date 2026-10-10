# PR #92 — owner chooses leg-friction option B; handoff posted

## Task summary
The owner approved option B for the wheelbarrow leg friction in PR #92 (`[VEH-002]`, RyoFPS): low leg friction only while held and while there is W/S/A/D input, with the original friction restored otherwise. Posted the decision and a replacement AI handoff prompt on PR #92.

## Relevant previous context
`2026-10-10-16-44-rereview-pr-92-fb45d48-leg-friction.md`:
- the TL retracted the push-height finding;
- leg friction (`FeetHighFriction` 0.8, combine Maximum) is the likely shared cause of the reverse front-wheel lift, the "dragging a table" feel, and unpushable 25 kg sacks;
- options A, B, and C were presented, and the TL recommended B with the idle-restore rule.

## Changes made
- Posted a PR #92 comment with the owner decision and the handoff prompt. The handoff covers:
  - **Should Fix:** route the ground-plane push through `PlayerMovement.ApplyMovementPush` with an optional surface normal, and remove the duplicated 350 N / response-time constants;
  - **Option B:** serialized leg colliders and a moving-leg material assigned in the prefab via Inspector, restored on idle, release, and disable;
  - a downhill brake capped by the same player push limit;
  - tests a–f: reverse on flat, three 25 kg loads, held idle on 20°, parked on 20°, downhill W, material restore;
  - a short DECISIONS entry recording the decision;
  - full test-run evidence with a matching hash, plus a manual Playground check.
- No source or asset change by the reviewer.

## Files affected
- `.development-history/2026-10-10-17-05-pr-92-leg-friction-option-b-handoff.md`

## Technical decisions
- B stays inside #92, because the reverse lift fails the VEH-002 "stable push, no heavy jitter" acceptance.
- Option C (handles lifted while held) is deferred to a separate issue, if a logistics playtest still needs it.
- Lifted the earlier "do not change PlayerMovement" restriction, because RyoFPS owns that system and the change removes duplication without altering other callers.

## Verification performed
- Confirmed the DECISIONS heading for the 350 N push limit (`2026-10-03 — Tenaga dorong player dan massa sak semen`) and the `CementBag.prefab` mass (25) cited in the prompt.
- Confirmed PR #92 head is still `fb45d48` when posting.

## Final result
The owner decision is recorded on PR #92, and Ryo has a complete prompt for option B. The previous CHANGES_REQUESTED stands until the owner re-checks feel.

## Known limitations
- The leg-friction root cause is still an inference from code, prefab values, and Ryo's measurements, pending the implementation and the owner's re-check.

## Unresolved issues or follow-up work
- Owner re-check of reverse, ramps, and loaded pushing after Ryo pushes.
- Possible separate issue for option C after the logistics playtest.
