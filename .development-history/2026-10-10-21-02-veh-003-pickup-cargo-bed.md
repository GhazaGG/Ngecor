# VEH-003 Parked Pickup Cargo Bed Prototype

## Task summary

Implement issue #44 (VEH-003): a parked old pickup with a physical cargo bed, free-rolling wheels and no driving input. A handbrake holds it on a Playground ramp under normal load; an overload of real cargo mass makes it creep backwards; the player can recover by chocking the wheels or unloading; cargo can fall out when tilted; mass, handbrake force and rollback threshold are tunable in the Inspector. Work was coordinated with Orca workers (T1 script, T2 prefab/scene/probe, T3 read-only review, T4 review fixes) on branch `feat/pickup-cargo-bed`, created from `main` at `258c075` after `git pull`.

## Relevant previous context

- `2026-10-10-17-38-pr-92-leg-friction-and-downhill-brake.md` and the VEH-001/VEH-002 reports: the wheelbarrow is a single Rigidbody with a low-friction wheel collider and no `WheelCollider`; the same approach was reused.
- `docs/DECISIONS.md`: push cap 350 N (PLAYER-003), performance budget (primitive colliders, Rigidbody may sleep, no allocations in update loops), no networking code outside NET-* tickets.
- Issue #44 was blocked by SETUP-005, which is merged (`Ramp_Gentle` 10 degrees and `Ramp_20Degree` exist in `Playground.unity`).

## Changes made

- `PickupWheels` (new, `Ngecor.Vehicle`): four sphere wheels roll freely; each grounded wheel gets a lateral grip impulse; the rear wheels carry a handbrake whose force is a finite capacity (`_handbrakeHoldForce`), so overload emerges from the real mass pressing on the Rigidbody. No cargo counter. Brake cancels the gravity-predicted velocity. A held, still pickup is put to sleep unless its speed grows during the still window; `SetHandbrake` wakes the body.
- `Pickup.prefab` (new): one Rigidbody (600 kg, centre of mass (0, 0.5, 0.1)), 8 BoxColliders and 4 SphereColliders with `WheelLowFriction`, bed walls 0.15 m, tailgate lip 0.07 m, primitive visuals using existing materials, wheel pivots for spin.
- `Playground.unity`: new `PickupTest` root with one `Pickup` on `Ramp_Gentle` (brake engaged, empty), 6 `CementBag` instances and 3 new 10 kg chock cubes. No existing object was changed except the `SceneRoots` entry.
- `docs/DECISIONS.md`: entry "2026-10-10 — Rem tangan pickup berkapasitas gaya (VEH-003)".

## Files affected

- `Assets/Game/Scripts/Vehicle/PickupWheels.cs` and `.meta`
- `Assets/Game/Prefabs/Vehicle/Pickup.prefab` and `.meta`
- `Assets/Game/Scenes/Playground.unity`
- `docs/DECISIONS.md`
- `.development-history/2026-10-10-21-02-veh-003-pickup-cargo-bed.md`

## Technical decisions

- **Sphere wheels plus scripted handbrake instead of `WheelCollider`** (user choice): a physical chock box stops a sphere by ordinary contact; `WheelCollider` raycasts would not be stopped by a chock.
- **Prefab and scene built by throwaway Editor scripts in a scratch copy of the project** (user choice), run in Unity 6000.3.25f1 batchmode. The builder and probe scripts were never copied into the repo. Only Unity-produced assets (prefab, `.meta`, scene) were copied back; the prefab script GUID matches `PickupWheels.cs.meta`.
- **T1's first script could not satisfy the issue by tuning.** Measured: an empty pickup crept 0.141 m in 10 s and never slept even on flat ground; raising the hold force from 1265 N to 3000 N changed nothing (0.1414 m). Causes: brake cancelled velocity before gravity was integrated, and tiny impulses reset the PhysX wake counter every step. Fixed with the predicted-velocity brake, a mass-relative impulse noise floor and a sleep rule. After: 0.0000 m and asleep.
- **Review finding S4 (overloaded pickup could freeze)** was fixed differently from the review suggestion: a "wanted impulse > clamp" flag would block all sleeping because the 544 N cap is below the pickup's own slope load (about 1022 N on 10 degrees); a variant with a speed threshold made 4 bags creep at 1.4 mm/s (failed case a). The final rule blocks sleep when speed grows during the still window. The capacity edge is fuzzy by about 3 kg.
- **Hold force 544 N is not the issue formula result.** The issue formula `F/(g sin) - M` ignores wheel friction. The load sweep fits an effective friction of about 0.09 (nominal 0.05, cause not found). 1 N of hold equals about 1.25 kg of load.
- **Cargo spill (option A, chosen by the coordinator):** lower walls (0.15 m, lip 0.07 m), no new physics material and no change to `CementBag`. A flat bag tips only at about 82 degrees and `CementBag.OnCollisionEnter` damps impacts, so a slippery floor would be needed to spill at 25 degrees, which would also make bags slide out when parked on the ramp.

## Verification performed

All numbers come from batchmode scripted-physics simulation (`Physics.simulationMode = Script`, 50 Hz) in a scratch project, not from Play Mode, with files byte-identical to the repo (`cmp`). Final run: 34 PASS, 0 FAIL, 0 `error CS`, in both the project solver setting (6/1) and a 10/2 override.

- Ramp_Gentle, brake on: empty, 3 cubes and 4 bags move 0.0000 m and sleep; 5 bags hold; 6 bags creep 0.36 m at 5 s and 0.92 m at 10 s (peak 0.13 m/s).
- Chock (10 kg box): creeping pickup stops, final speed 0.0000; removing 2 bags stops the creep within 1.76 s.
- 350 N horizontal push on the braked pickup on flat ground: 0.0001 m or less in 3 s (force applied to the Rigidbody, not through `PlayerMovement`).
- Brake released: rolls freely (1.84 m/s after 1 s). A sleeping pickup rolls within 0.02 s after `SetHandbrake(false)`.
- Parked and settled: 0 cargo leaves the bed, no jitter.
- Read-only review (T3): 0 blockers, 5 should-fix, 5 nits; S3, S4, N1, N2 fixed, S2 handled as above, S1 and S5 handled in `DECISIONS.md`. I confirmed independently: `git status` lists only the expected paths, GUIDs resolve, all files are LF, the scene diff is additive (the 2 reported deleted lines are diff-alignment artefacts).

## Final result

Implemented and simulated; **not yet played in Unity**. Acceptance criteria: 1, 4 and 7 met on static inspection and simulation; 2, 3 and 5 only partially (see limitations); 6 weakly met.

## Known limitations

- **Not tested in Play Mode:** feel, real-time behaviour, interpolation, jitter on `Ramp_Gentle`, wheel spin direction, console warnings, FPS or Profiler. The player's `CharacterController` and the real `PlayerMovement` push path were not exercised (only a 350 N force on the Rigidbody).
- **AC 3 is true only on `Ramp_Gentle`.** On `Ramp_20Degree` the brake cannot hold even an empty pickup: it rolls about 5.6 m and stops on flat ground (peak 2.85 m/s). A parked pickup on that ramp will roll.
- **AC 6 weakly met:** cargo falls only at about 45 degrees of roll, and then depends on the solver (3 of 6 bags vs 0). No spill at 25 degrees or a 4 m/s stop. "How to Test" step 5 ("push the pickup until it tilts") is not reproducible: the braked pickup does not move under 350 N and its static tipping angle is about 63 degrees.
- **AC 5:** chocks are plain 10 kg cubes with no `GrabbableObject` (the review saw `Box_Heavy` has `GrabbableObject` plus `NetworkObject`; I did not check whether `GrabbableObject` alone is enough, and AGENTS rule 11 forbids adding NGO components here); the player can push them, not carry them. Unloading by hand needs grab and was tested only by removing bags in simulation.
- The threshold depends on the ground friction material and the wheel material; the overload margin is narrow (about 40 N of hold force between "4 bags hold" and "6 bags creep").
- Solver iterations cannot be set per Rigidbody in the prefab; the prefab uses the project default 6/1.
- Chocks sit 3.5 m past the ramp bottom; the 6 loose bags lie at z 10.5 to 11.3.

## Unresolved issues or follow-up work

- Owner to run the "How to Test" steps in `Playground`, record the feel, and decide on AC 6 (lower walls were applied; stronger spill would need a `CementBag` change or a different bed design) and on AC 3 for `Ramp_20Degree`.
- Decide whether the chocks should be grabbable once INT tickets allow it, and whether a hold force window of about 40 N is too sensitive for playtests.
- Nothing was pushed and no PR was opened. The PR must list the untested items above and must not tick the Play Mode checklist.

## Addendum: bags and chocks not grabbable in Play Mode

- **Developer finding (Play Mode, reported by the developer):** the empty pickup parked on `Ramp_Gentle` held still and could be pushed. The six loose bags could not be grabbed, so the bed could not be loaded. (Also: `Playground` has no camera until Start Host is pressed; that comes from NET-002 `NetworkPlayer.ApplyOwnership`, not from this branch.)
- **Cause (read from code):** `CementBag.prefab` has no `GrabbableObject`, and in an active host session `NetworkPlayerInteraction.HandleLocalGrabRequest` rejects objects without a spawned `NetworkObject`.
- **Change:** on the 6 `CementBag` and 3 `Chock` scene instances under `PickupTest` only, added `GrabbableObject`, `NetworkObject`, `NetworkTransform` and `NetworkRigidbody` with the same serialized settings as the existing `Box_Heavy` (36 components, 0 differences). `CementBag.prefab`, `Pickup.prefab` and all scripts were not changed. The pickup itself stays non-networked. No new C# networking code; components were added through Unity APIs in a scratch copy and only `Playground.unity` was copied back. The 12 `NetworkObject`s in the scene have 12 distinct non-zero `GlobalObjectIdHash` values.
- **Verification (batchmode Play Mode in a scratch copy, not the developer's Editor):** after `StartHost` all 9 objects were spawned; `HandleLocalGrabRequest` returned true for a bag and a chock, which were carried and then dropped and settled; no NGO warnings; the empty pickup moved 0.0071 m in 3.5 s and slept. Three "referenced script missing" warnings on `Player` and an editor SearchDatabase exception also appear in a baseline run without this change; their source was not investigated.
- **Not verified:** the developer's real Editor with real input (the grab was called directly), a second client or RPC replication, and carrying a bag onto the bed by hand.
- **Not committed:** `Assets/Game/Settings/DefaultNetworkPrefabs.asset` was modified automatically by the developer's Editor (adds `Player_Prototype.prefab`); it is unrelated and left out of the branch.

## Addendum: developer Play Mode playtest

Reported by the developer (not run by me), in `Playground` on `feat/pickup-cargo-bed` after Start Host, with the loose bags moved onto the bed by hand:

- Empty pickup on `Ramp_Gentle` stays still and does not jitter, jump or sink into the ramp. Walking into it pushes it.
- 1 to 5 bags on the bed: nothing happens. The 6th bag makes the pickup start rolling back down the ramp slowly. Taking the 6th bag off (back to 5) makes it stop again after a short slow slide.
- With a cube chocked behind the wheels and 6 bags loaded again, the pickup stays stopped.
- Cargo can fall out when the pickup is moved manually in the scene (not by pushing it in play).
- Console: only the SessionManager host and client connection log lines; the developer judged them unrelated to this issue.
- Profiler CPU: mostly stable 60 FPS with occasional spikes down to about 30 and 15 FPS. **The source of the spikes was not identified**; no Profiler frame or marker was inspected, so it is not attributed to the pickup. The pickup script was reviewed for allocations in `FixedUpdate`, `LateUpdate` and `OnCollisionStay` (none found), but it was not profiled.

Comparison with the batchmode simulation: the 5-bag hold and the 6-bag creep match (sim: 5 bags hold, 6 creep, removing bags stops the creep). One mismatch: the simulation applied a 350 N force at the centre of mass and the braked pickup did not move, while in play the pickup can be pushed by walking into it. The real push goes through `PlayerMovement` contact impulses at the contact point and was not simulated that way, and the size of the displacement was not measured.

Acceptance criteria after the playtest (developer-observed): 1, 2 (6 bags loaded), 3 on `Ramp_Gentle` (not tested on `Ramp_20Degree`), 4, 5 (chock and unloading both worked), 7 (not exercised in the Inspector during this test). 6 is only met by moving the pickup manually in the scene, not by a player action. Feel was not rated beyond the notes above.
