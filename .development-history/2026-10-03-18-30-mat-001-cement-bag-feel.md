# MAT-001 Cement Bag Feel (Filled-Sack Physics)

## Task summary
User playtest: the cement bag felt like an empty cardboard box. It bounced when dropped, a 5-bag stack toppled easily, the shape was too boxy, and a bag stood upright did not look or behave bottom-heavy. Goal: make it feel like a filled sack, physics first.

## Relevant previous context
- `2026-10-03-17-01-mat-001-physics-test-attempt.md`, `2026-10-03-17-52-create-mat-001-manual-test-cases.md`: prefab exists (BoxCollider, Rigidbody mass 35, damping 0.15, friction 0.5/0.45, bounciness 0). Stack/ramp checks not verified.
- Issue #15 acceptance: primitive collider, 5-bag stack settles, bag does not bounce like a ball, Rigidbody sleeps.

## Root causes found (static inspection)
- Prefab root Transform carries a rotation of (-4.07, 76.6, 0.04) and position (0.5, 0.15, 0.5), so every instance spawns tilted about 4 degrees, which makes stacks slide.
- Scale (0.75, 0.3, 0.45): 0.3 m thick is about twice a real 40–50 kg sack (~0.65 x 0.45 x 0.13 m).
- Center of mass fixed at the geometric center: no powder settling, so it behaves like a hollow box.
- A rigid box landing on an edge converts rotation into a hop. Nothing absorbs impact energy.

## Changes made
- Added `Assets/Game/Scripts/Material/CementBag.cs` (namespace `Ngecor.Material`):
  - Fill shift: each awake FixedUpdate, the fill vector moves toward local gravity (`MoveTowards`, flow speed) and drives `Rigidbody.centerOfMass`. It only applies when the change is over a threshold and skips sleeping bodies, so the bag can still sleep.
  - Impact absorption: on `OnCollisionEnter` above `_impactSpeed`, it removes the separating velocity along the contact normal and damps angular velocity (`_impactAbsorb`).
  - Visual: a runtime subdivided-box mesh deformed into a pillow (thin sewn ends and sides, pinched outline, per-instance Perlin unevenness). The end where the fill gathers stays full and the opposite end thins. When the bag lies flat, the top sags. The mesh only rebuilds when the fill changes. The visual stays inside the BoxCollider.
- Collider stays a primitive BoxCollider (performance budget, issue AC).

## Files affected
- `Assets/Game/Scripts/Material/CementBag.cs` (new)
- `.development-history/2026-10-03-18-30-mat-001-cement-bag-feel.md` (new)

## Technical decisions
- Prefab, physic material, and scene were NOT edited by text (AGENTS.md rules 3/4). The required Inspector changes are listed for the developer below.
- No networking code. Visual noise is cosmetic and seeded with `Random`. It may differ per client later, which is acceptable for visuals.

## Required Unity Editor steps (developer)
1. CementBag prefab root: Transform position (0,0,0), rotation (0,0,0), scale (0.7, 0.14, 0.45).
2. Add Component `CementBag`.
3. Rigidbody: Mass 40, Linear Damping 0.2, Angular Damping 1.5, Interpolate = Interpolate, Collision Detection = Continuous Speculative (thinner body).
4. `CementBag.physicMaterial`: Static 0.9, Dynamic 0.75, Friction Combine Maximum, Bounce Combine Minimum.

## Verification performed
- Compiled `CementBag.cs` with Unity 6000.3.25f1's bundled Roslyn against UnityEngine Core/Physics modules: 0 errors.
- NOT run in Unity Play Mode. Stacking, ramp, bounce, sleep, and visual shape are unverified.

## Final result
Code ready. It needs the Inspector steps above, then a playtest.

## Known limitations
- The mesh only changes in Play Mode. In Edit Mode, the bag still looks like a cube.
- Hard normals at cube seams (reads as stitched seams).
- Whether `Rigidbody.centerOfMass` ignores transform scale is assumed (unscaled). If the shift looks too strong or weak, tune `_maxFillShift`.

## Unresolved issues or follow-up work
- Playtest TC-MAT001 cases after the Inspector steps. Tune `_flowSpeed`, `_impactAbsorb`, and the shape sliders by feel.
