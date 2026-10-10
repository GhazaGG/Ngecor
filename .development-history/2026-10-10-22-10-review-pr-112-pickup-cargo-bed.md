# Review PR #112 at d1e58e2 — pickup cargo bed (VEH-003), first review

## Task summary
Gave PR #112 (`[VEH-003] Parked pickup cargo bed prototype`, RyoFPS) its first, in-depth review at head `d1e58e2`. Recorded four owner decisions, posted a CHANGES_REQUESTED review and an AI handoff prompt, created follow-up issue #113, and left a note on #46.

## Relevant previous context
- Issue #44 (VEH-003) acceptance criteria:
  - free-rolling wheels;
  - a bed that holds 6 bags;
  - a handbrake that holds normal load;
  - creep from real cargo mass;
  - recovery by chocking or unloading;
  - cargo that can fall out;
  - Inspector tuning.
- `docs/DECISIONS.md`: the player push cap is 350 N.
- The author's report is `2026-10-10-21-02-veh-003-pickup-cargo-bed.md`.
- PR #108, which held this session's earlier reports, was merged into `main` (`3f03002`), so this report is on a new branch.

## Changes made
- Posted a CHANGES_REQUESTED review and a handoff prompt on PR #112.
- Created #113, `[VEH-007] Cargo spill from the pickup bed by player action` (P3, M2).
- Commented on #46 (MAT-003): when the `CementBag` prefab gets `GrabbableObject`, remove the instance overrides under `PickupTest`.
- No source or asset change by the reviewer.

## Findings
- **Gate 0 approved:** one Rigidbody, primitive colliders, a fixed-capacity handbrake so overload comes from real mass, chocks that work by contact, and a sleep rule. The reasons for not using `WheelCollider` hold.
- **Must Fix 1:** the sphere wheels are children of one Rigidbody, so they slide on the ground instead of rolling, and PhysX friction from `WheelLowFriction` acts on all four wheels.
  - From the PR's own numbers, at the 5/6-bag boundary on 10° (about 737 kg), the slope load is about 1,256 N.
  - The 544 N brake covers only part of it. The rest, about 712 N (effective μ ≈ 0.10), comes from wheel friction.
  - As a result, a released pickup on flat ground needs about 590 N to start moving, above the 350 N player cap (AC1). The Inspector values also do not describe the real threshold (AC7).
  - Fix: a new frictionless wheel material, then retune the hold force (about 1,240 N) so the issue formula holds.
- **Must Fix 2:** `_rollingResistanceForce` never applies.
  - The per-wheel impulse is 40 N × 0.02 s / 4 = 0.2 N·s, below the noise floor of 600 kg × 5e-4 = 0.3 N·s.
  - Fix: compare the floor against the wanted impulse before clamping.
- **Must Fix 3:** no automated tests are committed. The handoff lists six PlayMode cases in `Assets/Game/Tests/Vehicle/`.
- **Should Fix:**
  - `OnValidate` should wake the body, so Inspector tuning in Play Mode takes effect while the pickup sleeps;
  - trim the DECISIONS entry to the decision, reason, and limits, and move measurements to history.
- **Notes:**
  - after the retune, the empty pickup can still be pushed downhill by walking into it (margin about 240 N, below 350 N). This is physically consistent and recoverable;
  - copying a scene from a scratch project risks overwriting others' scene work, but it was safe this time;
  - the source of the FPS spikes is still unattributed.

## Owner decisions (2026-10-10)
1. AC6 (cargo spill) is a known issue; follow-up in #113.
2. Parking is valid only on slopes of 10° or less. `Ramp_20Degree` not holding is accepted as a design limit, and LEVEL-001 must park the pickup on a gentle slope.
3. The NGO components on the 6 bags and 3 chocks in `Playground` are accepted as a test setup: Playground is host-only since NET-002, and AC5 needs grab. Recorded on #46.
4. #44 closes when the PR merges, since it is already linked as a closing issue. The author should replace "not closed by this PR" with `Closes #44`.

## Files affected
- `.development-history/2026-10-10-22-10-review-pr-112-pickup-cargo-bed.md`

## Technical decisions
- Made the wheel friction a Must Fix rather than a documented limit: it breaks AC1 free rolling, and it leaves an unexplained number in DECISIONS. Removing it is a one-material change that makes the Inspector values exact.
- Kept the fixed-capacity brake model. It is physically sound and is what makes overload come from real mass.

## Verification performed
- Ran batchmode PlayMode on `d1e58e2` (includes `main` `3f03002`) in the review worktree: 242/242 passed, 0 `error CS`.
- Compared `Playground.unity` with `origin/main` `3f03002` block by block:
  - 0 blocks removed;
  - 1 block changed (`SceneRoots`, one new root);
  - 76 blocks added.
- Read the prefab:
  - mass 600, interpolation on;
  - the wheels use `WheelLowFriction` (static and dynamic 0.05, combine Minimum);
  - `_wheels` order is FL, FR, RL, RR, and `_brakedWheels` brakes indices 2 and 3 (the rear wheels).
- `git merge-tree` shows no conflict with `origin/main`.
- Confirmed through GraphQL that #44 is connected to the PR as a closing issue.
- The friction split and the noise-floor result are calculated from code and the PR's numbers. I did not run Play Mode.

## Final result
PR #112: CHANGES_REQUESTED with three Must Fixes and two Should Fixes. After the fixes, the PR needs a code re-check and the owner's feel test.

## Known limitations
- The 0.05 → 0.10 gap in effective wheel friction is not explained (possibly PhysX patch friction). Making the wheels frictionless removes it rather than explaining it.

## Unresolved issues or follow-up work
- Re-review after Ryo pushes.
- Owner feel test in `Playground` (Start Host).
- #113 needs a design choice (for example, an openable tailgate) before work starts.
