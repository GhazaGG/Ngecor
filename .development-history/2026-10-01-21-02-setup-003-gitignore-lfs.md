# SETUP-003 gitignore and Git LFS

## Task summary
Add Unity `.gitignore`, `.gitattributes`, and Git LFS for issue #36 so teammates can clone and commit safely. Also record the owner's Play Mode verification for #34.

## Relevant previous context
`2026-10-01-20-53-setup-001-unity-baseline.md`: baseline in PR #38 with no `.gitignore` yet. The owner then opened the project in the Editor, pressed Play in `Playground`, and reported a clean Console.

## Changes made
- On `chore/unity-baseline` (PR #38):
  - committed the URP upgrade fields Unity wrote on first Editor open (`PC_RPAsset`, `Mobile_RPAsset`, `URPProjectSettings`);
  - checked the 4 verified acceptance criteria on #34 and commented on it. "Semua developer menggunakan versi yang sama" stays open.
- New branch `chore/gitignore-lfs`:
  - `.gitignore`: Unity generated folders, IDE/solution files including `.vscode/` and `*.slnx`, crash/build artifacts.
  - `.gitattributes`: `* text=auto`; Unity YAML, C#, and JSON as LF text; LFS for images, models, audio, video, fonts, `.dll`/`.so`, and `.zip`.
  - `README.md`: `git lfs install` step before cloning.
  - `docs/DECISIONS.md`: recorded the ignore/LFS patterns and the Visible Meta Files/Force Text state.
- Ran `git lfs install` on the owner's machine.

## Files affected
`.gitignore`, `.gitattributes`, `README.md`, `docs/DECISIONS.md`, this report. On PR #38: `Assets/Game/Settings/PC_RPAsset.asset`, `Assets/Game/Settings/Mobile_RPAsset.asset`, `ProjectSettings/URPProjectSettings.asset`. Remote: issue #34.

## Technical decisions
- `*.unitypackage` is ignored rather than stored in LFS. Third-party packages are imported into `Assets/ThirdParty/`, not committed as archives.
- No UnityYAMLMerge driver; it is out of scope for #35 and #36.
- `.vscode/` is ignored as a personal setting.

## Verification performed
- `git status` shows only intended files; Unity cache and IDE files are no longer listed.
- `git check-ignore -v` matched `Library`, `Logs`, `UserSettings`, `Temp/`, `obj/`, `Build/`, `Builds/`, `.vscode`, and `Ngecor.slnx`. It matched nothing under `Assets/`, `ProjectSettings/`, or `Packages/`.
- `git add --renormalize .` produced no changes.
- `git lfs track` lists the 24 patterns.
- On a local throwaway branch, a `.png` committed as an LFS pointer (`git lfs ls-files` listed it, `git show` printed the pointer). The branch was deleted and never pushed.
- Mistake during testing: a `git stash --keep-index` temporarily removed the new `.gitignore` and `.gitattributes` from the working tree when switching branches. Both were restored with `git stash pop`, and their contents were confirmed.

## Final result
The ignore and LFS setup is ready for review on `chore/gitignore-lfs`, stacked on PR #38.

## Known limitations
LFS patterns are a proposal; the team agrees on them in PR review as the issue requires. Nobody else has cloned yet.

## Unresolved issues or follow-up work
- Merge order: #37, then #38, then this PR.
- After the merges, one teammate clones, opens the project with 6000.3.25f1, and presses Play to close the last #34 criterion.
- #35 (SETUP-002) checks are satisfied by the baseline: Visible Meta Files, Force Text, `.meta` tracked, and YAML text diffs visible (e.g. the URP asset upgrade diff). Its PR or closure is still pending.
