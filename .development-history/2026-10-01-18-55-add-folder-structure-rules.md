# Add folder structure and naming rules

## Task summary
Define where files go and how they are named in the Unity project, so four developers and their AI assistants keep the project organized.

## Relevant previous context
`docs/WORKFLOW.md` already proposed `Assets/Game/{Art,Audio,Materials,Prefabs,Scenes,Scripts,Settings}`, scripts in `Scripts/<System>/` with namespace `Ngecor.<System>`, and dev scenes at `Scenes/Dev/Dev_<Nama>.unity` (`2026-10-01-18-44`, `2026-10-01-18-51`). `docs/DECISIONS.md` lists the real project location as open until SETUP-001.

## Changes made
- Added `docs/PROJECT_STRUCTURE.md`: repo root layout, `Assets/` tree, `<System>` subfolders taken from the board's System field, a `Shared/` rule, `ThirdParty/` isolation, no `Resources/`, the `Editor` folder rule, dev scene rule, the process for proposing new folders, a naming table per asset type, banned names, and a review rejection rule.
- `docs/WORKFLOW.md`: replaced the inline folder proposal with a link to the new doc.
- `AGENTS.md`: added the new doc to the reading list, and added hard rule 5 (no files or folders outside the structure), renumbering the rules to 10.
- `README.md`: added the new doc to the start list.

## Files affected
`docs/PROJECT_STRUCTURE.md`, `docs/WORKFLOW.md`, `AGENTS.md`, `README.md`, this report.

## Technical decisions
- Kept the existing type-first layout (`Prefabs/<System>/`, `Scripts/<System>/`) instead of a feature-first layout, to stay consistent with prior docs.
- A Unity project at the repo root is marked as a proposal to confirm in SETUP-001, not a decision.
- `Resources/` is banned in favor of direct Inspector references.

## Verification performed
`git diff --check` is clean. All relative links in `README.md`, `docs/WORKFLOW.md`, and `docs/PROJECT_STRUCTURE.md` resolve.

## Final result
Folder and naming rules are documented and referenced from every entry point.

## Known limitations
No Unity project exists yet, so the structure is untested against real imports. Some Asset Store packages use hard-coded paths and may break if moved into `ThirdParty/`.

## Unresolved issues or follow-up work
Confirm the repo-root project location in SETUP-001 and record it in `docs/DECISIONS.md`.
