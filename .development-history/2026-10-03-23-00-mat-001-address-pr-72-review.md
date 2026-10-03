# MAT-001 Address PR #72 Review (meryzennn)

## Task summary
Evaluate meryzennn's CHANGES_REQUESTED review on PR #72 and fix the valid points.

## Relevant previous context
- Issue #15 was edited by the owner at 2026-10-03 13:55Z, after the PR work began. The new ACs are mass 25 kg, effective floor friction ~0.6–0.75, high angular drag, a drop from 1.5 m bouncing ≤ 0.1 m and resting in ~1 s, and ramp rest. Player-push feel moved out of scope to PLAYER-003 (#73).

## Assessment and changes
- MF1 mass 25 kg: valid (new AC). Prefab mass 40 → 25 via the Unity API.
- MF2 friction: valid. Physic material static 0.75, dynamic 0.6, combine Average (effective ~0.6–0.68 against Unity's default 0.6 floor). Via the Unity API.
- MF3 ramp: valid. Added `ThreeBagsDroppedOnRampComeToRest` (20° static ramp, 3 bags, 6 s, no upward velocity ≥ 0.5, all at rest). Replaced the drop test with `DroppedFromOneAndHalfMetersBarelyBouncesAndSettles` (AC numbers; observes a full 3 s and measures settle from the last movement, not the first momentary stop).
- MF4 "centerOfMass scaled twice": rejected after measurement. A temporary probe in Unity 6000.3 showed `centerOfMass` (1,0,0) on a 0.5-scaled body gives a world offset of 1.0, so it is not scaled. `UprightBagIsBottomHeavy` now asserts a 0.10–0.16 m drop (expected 0.13). With the reviewer's suggested change (no lossyScale), it fails at 0.20 m. Applied to thickness, that suggestion would push the COM to 0.175 m, outside the 0.09 m half-thickness.
- SF1 performance: partly valid. Deform runs only when the fill moves past a threshold and the body is awake, with no allocations. Per-vertex Perlin noise is now precomputed once in `BuildMesh`. The pillow mesh is kept (an owner request on shape, not final art).
- SF2 UVs: declined (YAGNI; untextured material, final art out of scope).
- Nit 1 `contactCount == 0` guard: added.
- Nit 2 euler hint leftover: cleared via `SerializedObject` (`m_LocalEulerAnglesHint` → 0).
- Nit 3 `DestroyImmediate` in Edit Mode: declined. `Awake` (which creates the mesh) does not run in Edit Mode, and `OnDestroy` already null-guards.

## Files affected
- `Assets/Game/Prefabs/Material/CementBag.prefab`, `Assets/Game/Materials/Material/CementBag.physicMaterial` (Unity API, temporary editor scripts deleted themselves)
- `Assets/Game/Scripts/Material/CementBag.cs`
- `Assets/Game/Tests/Material/CementBagTests.cs`

## Verification performed
- Full PlayMode suite (before the final drop-test tightening): 58 total, 55 passed. The 3 failures are `PlayerGrabTests` (#69), already confirmed failing identically on pure main 798cd6c.
- CementBagTests after the final edit: 7/7 passed. Drop: bounce 0.000 m, settle 0.06 s (stays at rest across the full 3 s). Ramp: all 3 bags rest in place, max upward 0.00.
- MF4 mutant: `UprightBagIsBottomHeavy` FAILED at 0.200 (expected range 0.10–0.16). Source restored, hash-verified, no MUTANT left.

## Known limitations
- The ramp check is automated. A manual Dev_Ghaza ramp run with 3 bags was not done (Dev_Ghaza has 1 ramp bag).
- Pre-existing `PlayerGrabTests` failures on main need a separate follow-up with the INT-002 owner.
