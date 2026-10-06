# BUILD-001 PR #84 review fixes (findings 1, 3, 4)

Date: 2026-10-06 10:31 WIB
Branch: fix/build-001-pr84-review (from origin/feat/build-001-scaffolding, PR #84)
Commit: 16d16a8

## Task summary

Implemented code fixes for review findings #1 (P1), #3 (P2), and #4 (P3) from
GhazaGG's "changes requested" review of PR #84 "[BUILD-001] Scaffolding
prototype", all in `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`.
Finding #2 (assembling the tested prefab into the dev scene via the Unity
Editor) cannot be done without Unity and was explicitly left to Deva. The
changes are small and preserve the existing detection mechanics and gameplay
feel where the review did not ask for a change.

## Relevant previous context

- Review (2026-10-05, 18:49 UTC) by GhazaGG requested changes on PR #84.
  Findings 1/3/4 target ScaffoldingLoadFailure.cs; finding 2 targets the
  Dev_DePo4l.unity dev scene.
- Issue #18 acceptance criteria (relevant): tunable payload limit that makes
  the scaffold wobble then collapse on real mass; hard impacts (heavy object
  thrown/pushed fast) can collapse the scaffold; a player walking normally
  and bumping a pole must NOT collapse it; load limit and stiffness tunable
  in the Inspector; collapsed parts remain physical objects.
- Verified physics calibration from the codebase: PlayerGrab._throwForce
  default 10f, applied as impulseVelocity = throwDir * (_throwForce / rb.mass).
  So a thrown 20 kg cement bag carries ~10 N·s. A 20 kg object sliding down a
  ~2 m ramp reaches ~6 m/s → ~120 N·s of impact impulse.

## Changes made

Finding #1 (P1) — hard impacts can now collapse the module:
- Added serialized field `_impactCollapseImpulse` (Min(0f), default 60f) with
  a tooltip explaining the calibration: thrown 20 kg cement bag ≈ 10 N·s
  (wobble path), heavy 20 kg object sliding down a ramp ≈ 100+ N·s (collapse).
- OnCollisionEnter: impulse >= _impactCollapseImpulse → BreakIntoParts()
  immediately; else if impulse >= _maximumImpactImpulse (30) → _impactWarning
  = true (existing wobble path). Existing detection mechanics untouched.
- Threshold ladder: walking bump (<30) → nothing; hard throw (30–60) →
  wobble; ramp slide (100+) → collapse. All Inspector-tunable.

Finding #3 (P2) — full load-probe buffer is now detected:
- Buffer enlarged 16 → 32 colliders for `_overlaps` (and the `_seenBodies` /
  `_seenPlayers` dedup arrays, to match capacity).
- When OverlapBoxNonAlloc fills the whole buffer, the measured mass is
  treated as a lower bound: a throttled warning is logged (once per 5 s) and
  `_payloadUncertain` makes the module wobble as the safe side instead of
  silently undercounting. The warning message suggests splitting the probe
  or excluding irrelevant colliders.

Finding #4 (P3) — component lookup reordered:
- In MeasurePayloadMass, the cheap checks (null / self / kinematic / dedup)
  now run first; GetComponentInParent<ScaffoldingLoadFailure>() only executes
  for unique, load-bearing Rigidbody candidates. No behavior change — the
  final set of counted bodies is identical.

## Files affected

- Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs (only file
  changed; +41/−6). No .unity/.prefab/.asset/.meta files touched.

## Technical decisions

- Default `_impactCollapseImpulse = 60f`: reachable by real game actions
  (ramp-slide scenario ≈ 120 N·s) but not by a standard throw (≈ 10 N·s),
  keeping ordinary gameplay safe. Tunable in the Inspector per issue #18 AC.
- Uncertainty → wobble (not immediate collapse): mirrors the safe-side
  response requested in the review without adding a new destruction path.
- Kept the change surface minimal: no new systems, no refactoring of the
  wobble/collapse state machine.

## Verification performed

- `git status --short` was clean before starting; branch
  fix/build-001-pr84-review created from the PR #84 HEAD (not from main).
- Re-read the modified file in full after editing; `git diff --check`
  passed with no whitespace errors.
- Committed on the task branch. No push performed.

## Final result

Local branch fix/build-001-pr84-review contains one commit (16d16a8) with
the three review fixes. Findings #1, #3, #4 are addressed at the code level.

## Known limitations

- No Unity/Play Mode testing was possible (Unity is not available on this
  machine); syntax and logic were verified only by careful re-read and
  `git diff --check`. A C# compiler was not installed (installing packages
  is prohibited by repo rules).
- The exact wobble→collapse feel and the ramp-slide impulse numbers need
  real Play Mode verification.

## Unresolved issues / follow-up work

1. Finding #2 (P2): assemble the tested prefab (ScaffoldingModule +
   ScaffoldingLoadFailure) plus cement bags and ramp in Dev_DePo4l.unity via
   the Unity Editor — Deva must do this; serialized Unity files must not be
   edited as text.
2. Run the issue #18 Play Mode checklist (overload wobble/collapse, ramp
   impact collapse, walking-bump must-not-collapse, payload query with a
   crowded probe) and tune `_impactCollapseImpulse` /
   `_maximumImpactImpulse` in the Inspector against real impulses.
3. Push the branch and update PR #84 (no GitHub authentication was available
   here; push intentionally not attempted).
