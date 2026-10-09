# BUILD-001 Review Follow-up and Main Sync

## Task summary

Review the remaining BUILD-001 scaffolding findings, sync the task branch with the latest `main`, align the script's impact threshold default with the tuned prefab, and avoid Play Mode testing per developer instruction.

## Relevant previous context

- Earlier BUILD-001 reports record fixes for impact-triggered collapse, overlap-query saturation, Rigidbody deduplication, and the development scene test setup.
- The current task branch already contains those code changes. Its prefab uses a 75 N·s impact threshold, while the script field default still used 50 N·s.
- The working tree had local changes in `Dev_DePo4l`, Project Settings, and an untracked construction prefab. These were preserved.

## Changes made

- Fetched `origin/main` at `8af0787` and merged it into `fix/scaffolding-impact-threshold` as merge commit `f9341d5`.
- Changed the `ScaffoldingLoadFailure` script default impact threshold from 50 to 75 N·s to match the tuned prefab.
- Made no edits to Unity serialized assets or the existing local scene changes.

## Files affected

- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`
- `.development-history/2026-10-08-22-02-build-001-review-follow-up.md`
- 46 upstream files integrated from `main` by merge commit `f9341d5`.

## Technical decisions

- Kept the existing impact warning through the wobble interval; the current code breaks the module when that interval ends.
- Kept the existing retry-on-full overlap buffer and per-Rigidbody deduplication before parent-component lookup.
- Matched the script default to the prefab's 75 N·s tuning so new component instances do not silently use the older value.
- Preserved the developer's dirty scene and prefab work. The current scene contains the scaffolding module and four cement bags; a separate `ImpactRamp` object was not found in the working scene.

## Verification performed

- Confirmed `origin/main` is an ancestor of the task branch after the merge.
- Statically inspected the impact, load-query, deduplication, prefab, and scene references.
- `git diff --check` passed.
- No automated tests or Play Mode checks were run.
- Unity Editor CLI for this project did not expose a reachable Pipeline; no scene edit was made. A subsequent status check showed only the pre-existing editor for the mixer worktree.

## Final result

The latest `main` is merged, and the code-level review fixes remain present with the script threshold default aligned to the prefab. Existing local work is preserved.

## Known limitations

- The current development scene has no separate impact ramp, and its scene setup was not changed because Unity Editor access was unavailable and the developer then asked not to open Unity on this laptop.
- The gameplay behavior was not re-tested in Play Mode.

## Unresolved issues or follow-up work

- Add or restore a dedicated impact ramp through Unity Editor when the developer is ready to use it.
- Run the requested gameplay checks later; this follow-up makes no Play Mode verification claim.
