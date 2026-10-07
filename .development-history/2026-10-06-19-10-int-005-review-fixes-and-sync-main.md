# INT-005 Review Fixes and Main Synchronization

## Task summary
Addressed Team Lead code review on PR #89 ([INT-005] Carried object collision behavior, Issue #76), resolved merge conflicts following the merge of PR #85 ([PLAYER-003]) into `main`, and ensured the entire test suite passes cleanly.

## Relevant previous context
- PR #89 had review feedback from Team Lead:
  - Must Fix 1: Update Owner/source for INT-005 in `docs/DECISIONS.md` to state that it is a proposal in PR #89 awaiting project owner approval.
  - Must Fix 2: Reset `PlayerMovement.CarriedMass = 0f` in `PlayerGrab.LateUpdate` when `!IsCarrying` (external destruction) and in `PlayerGrab.OnDisable()`.
  - Should Fix 3: Add a configurable pinch drop delay (`_pinchDropDelay = 0.3f`) and timer so brief pinches do not drop carried objects instantly.
  - Should Fix 4: Record automated PlayMode test counts per assembly and manual playtest results per point.
- PR #85 ([PLAYER-003]) merged into `main`, introducing `_maxPushForce`, `ApplyContactPush`, and dynamic step-probe blocking in `PlayerMovement.cs`.

## Changes made
1. **Documentation Alignment:**
   - Updated `docs/DECISIONS.md` under `2026-10-06 — Aturan tabrakan objek yang dibawa player (INT-005)` to specify `Owner/source: Usulan meryzennn di PR #89; menunggu persetujuan pemilik proyek.`
2. **CarriedMass Reset on Destruction & Disable:**
   - In `PlayerGrab.LateUpdate`: When `!IsCarrying` and clearing `_carriedObject`, reset `_playerMovement.CarriedMass = 0f`.
   - In `PlayerGrab.OnDisable`: Reset `_playerMovement.CarriedMass = 0f` and clear `_pinchTimer = 0f`.
   - In `PlayerGrabTests.cs`: Updated `CarriedObject_HandlesExternalDestructionGracefully` to assert `CarriedMass == 0f` and added `OnDisable_WhileCarryingHeavyObject_ResetsCarriedMassToZero`.
3. **Pinch Auto-Drop Delay:**
   - In `PlayerGrab.cs`: Added `[SerializeField, Min(0f)] private float _pinchDropDelay = 0.3f;` with public property `PinchDropDelay` and private `_pinchTimer`.
   - In `LateUpdate`: When `isPinched` is true, increment `_pinchTimer += Time.deltaTime`; trigger `ExecuteDrop()` only when `_pinchTimer >= _pinchDropDelay`. Reset timer to 0 when not pinched, on grab, and on drop.
   - Refined sweep overlap handling in `ResolveHoldPosition`: When initial sweep hit has `distance <= 0.0001f`, verify whether it is an opposing vertical wall via raycast normal checks (`Mathf.Abs(normal.y) < 0.7f && Dot(normal, camera.forward) < -0.3f`), preventing false pinch drops on sloped ramps while ensuring true wall obstacles trigger pinch delays correctly.
   - In `PlayerGrabTests.cs`: Added `CarriedObject_PushedTooCloseAgainstPlayer_BrieflyPinchedThenReleased_DoesNotDrop` and updated `CarriedObject_PushedTooCloseAgainstPlayer_ExceedingPinchDelay_TriggersAutoDrop`.
4. **Merge with Main & Conflict Resolution:**
   - Merged `origin/main` (incorporating PR #85 and MAT-002) into `feat/int-005-carried-object-collision`.
   - Resolved conflicts in `PlayerMovement.cs`:
     - Retained `_maxPushForce`, `ApplyContactPush`, and `ShouldBlockStepOverDynamicBody` from #85.
     - Retained cursor relock guard (`CursorRelockedThisFrame`) from #78.
     - Preserved `CarriedMass`, `_massSpeedPenaltyFactor`, and `_minMassSpeedMultiplier` from INT-005.
     - Scaled horizontal movement speed by `massMultiplier`.
     - In `ShouldBlockStepOverDynamicBody` invocation, computed `horizontalDistance` using effective movement speed (`direction.magnitude * (_moveSpeed * massMultiplier) * deltaTime`).

## Files affected
- `docs/DECISIONS.md`
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Interaction/PlayerGrabTests.cs`
- `.development-history/2026-10-06-19-10-int-005-review-fixes-and-sync-main.md`

## Technical decisions
1. **Vertical Wall Differentiation for Initial Sweep Overlaps:** When `Physics.SphereCastNonAlloc` encounters an initial contact (`distance <= 0.0001f`), Unity PhysX returns an unpopulated hit normal (`Vector3.zero`). A directed raycast is used to inspect the surface normal; only steep vertical obstacles opposing camera forward (`Mathf.Abs(normal.y) < 0.7f && Dot(normal, camera.forward) < -0.3f`) constrain clearance to initiate a pinch, whereas ramps and ground geometry slide smoothly under the depenetration solver.
2. **Effective Speed in Step Probes:** Dynamically scaled the step probe distance in `PlayerMovement` with `massMultiplier` so heavy payloads accurately probe the distance traversed per frame rather than overshooting against dynamic obstacles.

## Verification performed
- Executed full batchmode PlayMode test suite across all assemblies:
  - `Ngecor.Interaction.Tests.dll`: 51/51 passed (0 failed, 0 skipped)
  - `Ngecor.Material.Tests.dll`: 33/33 passed (0 failed, 0 skipped)
  - `Ngecor.Player.Tests.dll`: 33/33 passed (0 failed, 0 skipped)
  - Total: 117/117 passed (100% green).
- Verified zero console errors or exceptions.

## Final result
All Must Fix and Should Fix review comments from PR #89 are completed and verified. The branch is cleanly merged with `origin/main` without breaking any PLAYER-003 or MAT-002 functionality.

## Known limitations
- Physical subjective feel (e.g. slowness perception when holding Box_Heavy vs Box_1) requires manual verification in the Unity Editor Play Mode.

## Unresolved issues or follow-up work
- Manual playtest in `Dev_Ghaza` / `Playground` scene to observe visual feel and verify box placement in wheelbarrow.
- Update PR #89 description with test numbers and notify reviewers.
