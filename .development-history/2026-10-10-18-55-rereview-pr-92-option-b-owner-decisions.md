# Re-review PR #92 at 80dd531 — option B code ready, owner decisions, follow-up issues

## Task summary
Re-reviewed PR #92 (`[VEH-002] Wheelbarrow player interaction`, RyoFPS) after the option B push. Recorded the owner's decisions and created two follow-up issues. Moved the review Unity to the new head for the owner's feel test.

## Relevant previous context
`2026-10-10-17-05-pr-92-leg-friction-option-b-handoff.md` requested:
- low leg friction while held with W/S/A/D input;
- a shared push function with a surface normal;
- a downhill brake;
- tests a–f;
- a DECISIONS entry.

## Changes made
- Posted a COMMENT review on PR #92: code ready, owner decisions, and an attribution correction.
- Created issue #109 `[VEH-005] Push a heavily loaded wheelbarrow` (P2, M2).
- Created issue #110 `[VEH-006] Wheelbarrow grip from the side on ramps` (P3, M2).
- Moved the isolated review worktree `C:\Users\Hype\AppData\Local\Temp\ngecor-pr-92-review` from `fb45d48` to `80dd531` (detached, clean), and relaunched Unity: 0 `error CS`.
- No source or asset change by the reviewer.

## Findings
- **The code has no Must Fix:**
  - `ApplyMovementPush` now takes a `surfaceNormal`. With `Vector3.up` it is identical to the old `point.y = COM.y`.
  - The duplicated 350 N and response-time constants are gone, and the barrow push now uses the player's per-frame push budget.
  - Contact pushes are already disabled while an interactable controls movement, so there is no double push.
- **The downhill brake** mirrors the push law with the same cap. Push and brake are never both non-zero.
- **The leg material** swaps only on state changes, and every `LateUpdate` exit path restores it through `StopInteraction`.
- **Tests:** 197 (196 passed, 1 skipped) on `2673bcd`. The merge with `main` kept main's DECISIONS entries.
- **Minor, non-blocking:** the brake uses `deltaTime` as its response time, while the velocity only updates per physics step. Above 50 FPS the brake slightly over-corrects, by about 0.06 m/s on a 20° ramp.
- **Deviation:** low friction applies to W/S only. With A/D included, two existing 20° steering tests failed.
- **Attribution:** Ryo's comment labelled the Playground hand test as the owner's and stated owner decisions. The owner confirmed in session that they had not tested this head. Corrected on the PR.

## Technical decisions (owner, 2026-10-10)
- The W/S-only low friction is accepted.
- The occasional front-wheel lift when reversing on a ramp is accepted as a known issue and tracked in #109.
- Follow-up issues #109 and #110 were created.

## Verification performed
- Read the diff `fb45d48..38719f5`, `LateUpdate`, `PlayerMovement.Update` (budget reset), `OnControllerColliderHit`, and `CalculatePushForce` at `2673bcd`.
- Compared DECISIONS headings between `origin/main` and `2673bcd`.
- `git merge-tree` against `origin/main` is clean.
- Unity Editor log: 0 compile errors after the relaunch.
- Did not run the tests; the counts are Ryo's.

## Final result
PR #92: COMMENTED. Code is ready, and approval waits for the owner's feel test on `80dd531`.

## Known limitations
- The brake over-correction size is derived from the code, not measured.

## Unresolved issues or follow-up work
- Owner feel test on `80dd531`, then approve.
- #109 and #110 in the backlog.
