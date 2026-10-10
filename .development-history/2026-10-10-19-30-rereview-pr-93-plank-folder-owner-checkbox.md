# Re-review PR #93 at 0631145 — Plank folder and owner feel checkbox

## Task summary
Re-reviewed PR #93 (`[BUILD-001] Tune scaffolding impact threshold`, DePo4l) at head `0631145`. Posted a small Request Changes review and an AI handoff prompt.

## Relevant previous context
The review at `c7dc12d` asked for two things:
- restore the side-contact filter, so that sacks landing on the deck count as load, not impact;
- run three manual cases: a running throw collapses the scaffold, a moving deck drop stays stable, and a ground shove into a post stays standing.

## Changes made
- Posted a CHANGES_REQUESTED review and a handoff prompt on PR #93.
- No source or asset change by the reviewer.

## Findings
- **Resolved:**
  - the side-contact filter (`|normal.y| >= 0.5`) is restored exactly as suggested;
  - the script default and the prefab both use 75 N·s;
  - held objects are kinematic and return early, so brushing a post while carrying does not trigger a collapse;
  - DePo4l ran the three manual cases on `ba69ad9` (same code as the head) with a clean Console.
- **Collapse readability:** a 1.5 m/s outward separation is applied once at break. The heaviest part carries 20 kg × 1.5 = 30 N·s, below the 75 N·s threshold, so falling parts do not automatically chain-collapse neighbors.
- **Must Fix 1:** `ba69ad9` added `Plank.prefab` in a new folder `Prefabs/Construction/Scafolding/`.
  - The folder name is misspelled and is not in `PROJECT_STRUCTURE.md`.
  - Asked DePo4l to move the prefab to `Prefabs/Construction/` through the Unity Project window (this keeps the GUID and the dev scene references) and to declare it a dev-scene test prop.
  - If Plank is meant as a gameplay object, it should get its own issue.
- **Must Fix 2:** the verification report called "D" the project owner, but D is DePo4l. The PR checkbox "Owner confirms gameplay feel" was ticked by the developer; asked to untick it and correct the attribution in a new report.
- **Suggestions:**
  - make the 1.5 separation speed a `[SerializeField]`;
  - add a paired PlayMode test for the impact filter.
- **Retracted before posting:** I first told the owner that the Plank material was missing from the repo. Its GUID is URP's built-in `Lit.mat` in the package cache, so the claim was dropped.

## Files affected
- `.development-history/2026-10-10-19-30-rereview-pr-93-plank-folder-owner-checkbox.md`

## Verification performed
- Read the full PR diff against merge base `8af0787` and the current `OnCollisionEnter` and `BreakIntoParts`.
- Checked `PlayerGrab` for the kinematic hold.
- Resolved the Plank material GUID against the URP package `Lit.mat.meta`.
- `git merge-tree` against `origin/main` is clean.
- Did not run Unity for this PR.

## Final result
PR #93: CHANGES_REQUESTED, with two small Must Fixes. After them, the owner's feel test, including the 75 N·s confirmation, decides approval.

## Unresolved issues or follow-up work
- Owner feel test of the three cases in `Dev_DePo4l` after DePo4l pushes.
