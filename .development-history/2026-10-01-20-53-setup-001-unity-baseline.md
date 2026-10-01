# SETUP-001 Unity baseline

## Task summary
Create the Unity project baseline for issue #34 at the repo root. Also clean up the stray `My project/` folder created when the owner first opened Unity.

## Relevant previous context
- `2026-10-01-19-08-root-location-ruleset-codeowners.md` recorded the repo-root decision and flagged `My project/` (Unity `6000.6.3f1`).
- `2026-10-01-19-00-lightweight-performance-and-urp.md` set URP and the performance budget.
- `2026-10-01-19-47-inspect-setup-002-unity-git-settings.md` (Codex, untracked) inspected `My project/` for SETUP-002. That folder no longer exists.

## Changes made
- The owner chose Unity **6000.3.25f1** (6.3 LTS) over 6000.6.3f1. They created `C:\NGECORRR\NgecorBaseline` from the Universal 3D template in Unity Hub.
- Deleted `My project/`, an unused 6.6 template project of 1.3 GB.
- Using a temporary editor script run in batchmode (`AssetDatabase` API, so GUIDs and Build Settings stay correct):
  - moved `Scenes/`, `Settings/`, and `InputSystem_Actions.inputactions` into `Assets/Game/`;
  - renamed `SampleScene` to `Playground` and `SampleSceneProfile` to `PlaygroundProfile`;
  - deleted the template's `TutorialInfo/` and `Readme.asset`.
- Applied the performance budget through the API: disabled additional light shadows on `PC_RPAsset`, the SSAO renderer feature on `PC_Renderer`, and Bloom, Vignette, and Motion Blur on `PlaygroundProfile`. Tonemapping stays on; MSAA was already off.
- Removed the leftover `HubForceResolve.cs` that Hub injects.
- Moved `Assets/`, `Packages/`, and `ProjectSettings/` into the repo root on branch `chore/unity-baseline`.
- Set `PlayerSettings.productName` to `Ngecor` through a temporary batchmode script, then deleted that script.
- Deleted the temporary `NgecorBaseline` folder.
- `docs/DECISIONS.md`: recorded the exact version, the real project structure, and the input system shipped by the template.
- `README.md`: added local setup steps, including how to detect a broken Hub install.

## Files affected
`Assets/`, `Packages/`, `ProjectSettings/`, `docs/DECISIONS.md`, `README.md`, this report.

## Technical decisions
- Kept `Mobile_RPAsset` and `Mobile_Renderer` because the quality levels reference them. Removing them means editing QualitySettings, which is outside SETUP-001.
- Left `templateDefaultScene` in `ProjectSettings.asset` pointing at the old template path. It is template metadata, and serialized files are not edited by hand.
- Left template packages (Visual Scripting, Collab Proxy, Timeline, AI Navigation, Test Framework) untouched. The issue puts package changes out of scope.
- No `.gitignore` in this PR (SETUP-003). Only `Assets/`, `Packages/`, and `ProjectSettings/` were staged, by explicit path.

## Verification performed
- Unity Hub's silent installer failed three times with NSIS exit code 2. Hub logs showed it still marked the editor installed because `Unity.exe` was present, leaving it without `Data/UnityReferenceAssemblies` (batchmode failed with "Scripts have compiler errors").
- The owner reinstalled with the official installer, which I downloaded to `D:` and verified (4,201,959,888 bytes, Authenticode signature valid, Unity Technologies SF). After that, `UnityReferenceAssemblies/unity-4.8-api/Facades` holds 108 files.
- The batchmode reorganization script ran with exit 0 and logged its completion line.
- Batchmode imports at the repo root ran with exit 0 and no `error CS`, "Compilation failed", or "compiler errors" lines, including a final import after the temporary script was removed.
- Every file and folder under `Assets/` has a `.meta`, and there are no orphan `.meta` files.
- `EditorBuildSettings` lists `Assets/Game/Scenes/Playground.unity` with the original scene GUID.
- `ProjectVersion.txt` = `6000.3.25f1`. `VersionControlSettings` = Visible Meta Files. `EditorSettings` `m_SerializationMode: 2` (Force Text).
- Unity Cloud is disabled (empty `cloudProjectId`/`organizationId`).

## Final result
The repo-root Unity 6000.3.25f1 URP baseline is ready for review on `chore/unity-baseline`.

## Known limitations
- Play Mode was not run; batchmode cannot press Play. "Default scene can be run" still needs a manual check in the Editor.
- No second developer has cloned and opened the project yet.
- Until SETUP-003 lands, `Library/`, `Logs/`, `UserSettings/`, `.vscode/`, `*.csproj`, and `*.slnx` show up as untracked files.

## Unresolved issues or follow-up work
- Owner: open the project in the Editor, press Play in `Playground`, and confirm the Console is clean.
- A teammate: clone, open with 6000.3.25f1, and run the same check.
- SETUP-003 `.gitignore`/LFS should follow immediately.
- Consider removing unused template packages and the Mobile URP assets in a separate issue.
- Delete `D:\UnitySetup64-6000.3.25f1.exe` once all is well.
