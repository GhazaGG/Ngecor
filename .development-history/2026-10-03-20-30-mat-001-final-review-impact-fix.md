# MAT-001 Final Pre-PR Review: Impact Fix and Test Proof

## Task summary
The user approved the cement bag feel and asked for a final review before opening a PR.

## Relevant previous context
- `2026-10-03-19-45-mat-001-thicker-bag-depenetration.md`: thicker prefab, depenetration cap; the new depenetration test was not proven to fail without the fix.

## Findings and changes
- Bug in `CementBag.OnCollisionEnter`: it removed the bag's separating velocity in absolute terms, so a bag hit by a moving body (cart, sliding bag, thrown object) was treated like a hit from a fixed wall. The momentum it received at impact was discarded. Fixed: the separating velocity is now measured relative to the other body's point velocity (`GetPointVelocity`). Drops onto static ground are unchanged.
- New test `MovingBodyCarriesBagOnImpact`: an equal-mass, gravity-free body hits the bag at 4 m/s. An inelastic result should be about 2 m/s; the bag must exceed 1.5 m/s.
- `OverlappingObjectDoesNotLaunchBag` was weak: it passed even without the depenetration cap, because moving a static collider does not wake a sleeping bag, and the 1 m/s threshold was too loose. It now wakes the bag, tracks max speed over 10 steps, and asserts < 0.3 m/s. Measured: about 0.11 m/s with the cap, about 0.63 m/s without it.

## Files affected
- `Assets/Game/Scripts/Material/CementBag.cs`
- `Assets/Game/Tests/Material/CementBagTests.cs`
- `.development-history/2026-10-03-20-30-mat-001-final-review-impact-fix.md`

## Verification performed
- Unity 6000.3.25f1 batchmode PlayMode: 38/38 passed (5 CementBag tests).
- Proof that the tests fail without the fixes (temporary source changes, restored, hash-verified, grep clean):
  - Impact fix reverted → `MovingBodyCarriesBagOnImpact` FAILED (0.80 m/s vs > 1.5).
  - Depenetration cap removed → `OverlappingObjectDoesNotLaunchBag` FAILED (0.63 m/s vs < 0.3).
- The user's exact "bags fly when the gizmo pushes the stack" scenario was not reproduced in a test (a static 0.1 m overlap only reaches about 0.63 m/s uncapped). The user reported the feel as acceptable after the cap.

## Known limitations / follow-up before PR
- Not covered by automated tests: the ramp case and the "heavier than test cube when pushed by player" feel (issue #15 ACs). These need a manual playtest and a written note in the PR.
- PR hygiene: exclude `debug.log`, `ProjectSettings/SceneTemplateSettings.json`, and unrelated history reports. Move `docs/MAT-001-Cement-Bag-Test-Cases.md` content into the PR/issue rather than adding a per-ticket doc. The Dev_Ghaza bag instances still override the old tilted rotation.
