# Close SETUP-002 and restrict merges to squash

## Task summary
The owner asked to close issue #35 (SETUP-002) and allow only squash merges in the repo.

## Relevant previous context
`2026-10-01-21-02-setup-003-gitignore-lfs.md` noted that the baseline in PR #38 already satisfies #35. Codex's untracked `2026-10-01-19-47-inspect-setup-002-unity-git-settings.md` inspected the now-deleted `My project/`; it was left untouched.

## Changes made
- Issue #35: checked all four acceptance criteria, commented with the evidence, and closed it as completed.
- Repo settings: `allow_squash_merge=true`, `allow_merge_commit=false`, `allow_rebase_merge=false`. `delete_branch_on_merge` stays `false`; the owner did not ask for it.

## Files affected
This report. Remote: issue #35 and repo merge settings.

## Technical decisions
Closed #35 before PR #38 merges, at the owner's request. The closing comment says the evidence lives on `chore/unity-baseline` and that #37 and #38 must merge before it reaches `main`.

## Verification performed
Evidence came from files on `chore/gitignore-lfs`:
- `VersionControlSettings.asset` has `m_Mode: Visible Meta Files`.
- `EditorSettings.asset` has `m_SerializationMode: 2`.
- All 9 tracked assets and 3 folders under `Assets/` have tracked `.meta` files.
- Commit 1f0a50a shows readable YAML diffs.

Read back after the changes:
- `gh issue view 35` shows CLOSED with all boxes checked.
- The repo API returns the three merge flags above.

## Final result
#35 is closed and the repo accepts only squash merges.

## Known limitations
No prefab exists yet, so the text-diff check used asset and settings files.

## Unresolved issues or follow-up work
- Merge #37 → #38 → #39, deleting each head branch after merge so the next PR retargets to `main`.
- One teammate clones and presses Play to close the last #34 criterion.
