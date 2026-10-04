# MAT-002 Milestone Commits

## Task summary

Organized the existing MAT-002 work into local milestone commits, as requested by the user. The active branch is `feat/sand-bucket`; no push or merge was requested.

## Relevant previous context

- Reviewed `2026-10-04-00-46-mat-002-sand-bucket.md` and its `00-50` finalization report for runtime, input, and dev-scene ownership.
- Reviewed the `03-38` mound/collider repair, `04-05` volume/physics verification, `14-24` manual checklist, `15-15` scope assessment, and `15-37` animation implementation reports.
- Confirmed current files and tracked diffs before staging. Earlier reports describe historical states; the final current prefab includes mound repairs and the pouring animation.
- The initial staging area was empty. Unrelated local history, MAT-001 documentation, generated settings/logs, and the user's separate test scene were already present.

## Changes made

- Created `c38feaa` (`feat: add sand bucket transfer input and mound scaling`): runtime transfer/input/receiver/pile adapters, runtime assembly references, and the Pour action/reference.
- Created `ea8499c` (`feat: assemble sand mound and bucket pour animation`): mound mesh, Unity folder metadata, materials, pile/bucket prefabs, visual animation adapter, and the existing MAT-002 dev-scene setup.
- Grouped the existing Play Mode tests, test assembly references, manual checklist, decisions, seven related history reports, and this report for the documentation/test milestone: `docs: record sand bucket tests scope and development history`.
- Used explicit file paths for staging. No runtime code or serialized Unity asset content was edited during this task.

## Files affected

- Runtime/input milestone: 12 files under `Assets/Game/Scripts/Material/` and `Assets/Game/Settings/`.
- Prefab/animation milestone: 18 files under `Assets/Game/Art/`, `Materials/Material/`, `Prefabs/Material/`, `Scripts/Material/`, and `Scenes/Dev/Dev_Ghaza.unity`.
- Documentation/test milestone: two test scripts and their metadata, `Ngecor.Material.Tests.asmdef`, `docs/DECISIONS.md`, `docs/MAT-002-Sand-Pile-Bucket-Test-Cases.md`, seven MAT-002 history reports, and this report.

## Technical decisions

- Milestones group the current implementation by dependency: runtime and input, assembled playable prefabs/animation, then tests and documentation. They do not reconstruct historical intermediate prefab versions.
- Kept animation code and serialized prefab/emitter wiring together so the asset milestone includes its script dependencies.
- Preserved asset/metadata pairs and the existing MAT-005 implementation.
- Left `Dev_Ghaza_MAT002.unity` and its metadata outside these commits because the prior scope report identifies it as the user's separate temporary test scene.
- Left `ProjectSettings/SceneTemplateSettings.json`, `debug.log`, the MAT-001 checklist, and unrelated untracked history outside the MAT-002 commits.

## Verification performed

- Checked the active branch, initial empty index, tracked diffs, untracked inventory, and explicit staged file lists.
- Confirmed the first two commits were created successfully and remained on `feat/sand-bucket`.
- Confirmed local `main` remains `d271570a63d75bf7b2bb431a0283e3b92c411e6a`, matching its value at the start of this task.
- Read the existing `Logs/BucketPourAnimationResults.xml`: 25 tests passed, zero failed, skipped, or inconclusive, from the earlier 2026-10-04 15:32 WIB run. This was a read of prior evidence, not a new Unity run.
- Confirmed the manual checklist still marks its cases NOT VERIFIED.
- No tests, Unity session, keyboard input, push, or merge were run in this commit-organization task.

## Final result

The existing MAT-002 work is organized into scoped local milestones on the user's feature branch. This report accompanies the documentation/test milestone. `main` and unrelated working files are preserved.

## Known limitations

- These commits do not establish new gameplay acceptance or recreate earlier historical implementation checkpoints.
- Interactive R input, carry/drop feel, and hardware performance remain subject to the prior reports' verification limits.
- Serialized Unity content was preserved, including the whitespace previously documented in `Dev_Ghaza.unity`.

## Unresolved issues or follow-up work

- Run the existing manual checklist and complete technical/gameplay review separately.
- Free ground pouring remains assigned to MAT-004; no additional feature implementation was performed here.
