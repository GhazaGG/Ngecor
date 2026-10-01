# Inspect SETUP-002 Unity Git settings

## Task summary
Begin issue #35 while another developer's AI handles SETUP-001. Inspect Unity Git settings and determine which acceptance checks can currently run without interfering with the baseline work.

## Relevant previous context
`2026-10-01-18-51-git-workflow-unity-6-and-setup-issues.md` defines issue #35 and its verification steps. `2026-10-01-19-08-root-location-ruleset-codeowners.md` records the required repo-root Unity location and an existing untracked `My project/` directory owned by SETUP-001.

## Changes made
Created this investigation report. No Unity assets, settings, scenes, Git ignore rules, or LFS configuration were changed. Removed the temporary crashpad `debug.log` produced by this task's sandbox-denied Orca inspection commands, after checking its contents and timestamps.

## Files affected
Added this report. Read existing project settings and assets under `My project/` without changing them.

## Technical decisions
Do not configure or relocate another task's baseline. Issue #35 explicitly depends on #34 and excludes `.gitignore`, LFS, and UnityYAMLMerge. Verify against the final repo-root baseline before reporting completion.

## Verification performed
- Read the full current GitHub issue #35 and relevant repository instructions/history.
- The temporary project reports Unity `6000.6.3f1`.
- Its `VersionControlSettings.asset` contains `m_Mode: Visible Meta Files`; `EditorSettings.asset` contains `m_SerializationMode: 2`.
- All 23 asset files/directories currently under its `Assets/` have paired `.meta` files.
- `Assets/Scenes/SampleScene.unity` begins with a readable Unity YAML header.
- Orca window inspection found Unity Hub, but no visible Unity Editor.
- No repo-root `ProjectSettings/ProjectVersion.txt` currently exists. The temporary project remains untracked, so these checks do not establish tracked asset metadata.

## Final result
Initial settings are compatible with the issue requirements. SETUP-002 is not complete: the final baseline location/branch and Editor access are still needed.

## Known limitations
No prefab was modified through Unity Editor, no actual prefab Git diff was demonstrated, and `.meta` tracking against the final baseline was not verified. SETUP-001's checked issue boxes do not prove those conditions in the current checkout.

## Unresolved issues or follow-up work
Identify the final baseline folder/branch from SETUP-001, verify settings in Editor, perform and undo a text-diff test, audit tracked `.meta` pairs, and record the final verified configuration in `docs/DECISIONS.md` on a dedicated issue branch.
