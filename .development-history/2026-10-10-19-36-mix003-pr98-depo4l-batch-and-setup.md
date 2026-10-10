# MIX-003 PR B (#98): DePo4l Review on Batch Storage and Setup Tuning

## Task summary
Addressed DePo4l's changes-requested review of PR #98 at `540ca1d`: (1) concrete batches merge in the spot container, (2) setup reruns overwrite prefab Inspector tuning.

## Relevant previous context
- `.development-history/2026-10-10-18-51-mix003-pr98-meryzennn-review-fixes.md`: previous round; meryzennn also raised the tuning overwrite as a P3.
- Issue #55 technical note: "concrete harus disimpan sebagai batch di container agar freshness bisa ditempel".
- `docs/DECISIONS.md` bulk material contract: freshness belongs to the concrete batch or its container; mixed batches use the oldest age.
- The first PR B version had a per-batch `ConcreteBatch.cs`; it was removed in the dry/wet redesign (`86e2082`).

## Changes made
- (1) No code change. The project owner decided the contract already covers it: concrete is stored per container, and MIX-002 attaches freshness to the container with the oldest-age rule. Recorded in the MIX-003 entry of `docs/DECISIONS.md`.
- (2) `Ngecor/Setup Manual Mixing Spot` only builds the spot and drum prefabs when they are missing. The new menu `Ngecor/Rebuild Manual Mixing Prefabs` overwrites them with defaults on purpose.

## Files affected
- `Assets/Game/Scripts/Editor/ManualMixingSpotSetup.cs`
- `docs/DECISIONS.md`

## Technical decisions
- No per-batch record: nothing reads one until MIX-002, and the agreed contract merges batches with the oldest age anyway.

## Verification performed
- Ran `Ngecor/Setup Manual Mixing Spot` in Unity 6000.3.25f1 batchmode: compiles, completes, and leaves both prefabs unchanged. The scene was re-saved with new instance IDs only; that was reverted to the committed version.
- PlayMode suite not rerun (editor-only and docs change); last run at `540ca1d`: 184 passed.
- `Rebuild Manual Mixing Prefabs` was not run.

## Final result
Both review points are addressed; the commit is local until the developer approves the push.

## Known limitations
- Rerunning setup still replaces the spot instance in Dev_Ghaza, so scene-level overrides on that instance are lost.

## Unresolved issues or follow-up work
- Push, reply to DePo4l, request re-review. meryzennn's re-review is still pending.
