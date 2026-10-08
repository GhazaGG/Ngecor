# INT-006 — Document interactable object conventions

## Task summary
Record the interactable object convention from the INT-006 line of work in the project docs: add a decision entry to `docs/DECISIONS.md` and document the `Prefabs/UI/` folder plus `IInteractable` placement convention in `docs/PROJECT_STRUCTURE.md`, then commit both docs.

## Relevant previous context
INT-006 spans several prior tasks already merged on branch `feat/interaction-highlight-prompt`: `InteractionDetector` (raycast with `QueryTriggerInteraction.Ignore`, `GetComponentInParent<IInteractable>()`), `InteractionHighlighter` (`MaterialPropertyBlock`, no `renderer.material` mutation, zero per-frame GC), `InteractionPromptUI` (always-on crosshair + dynamic prompt with dirty tracking), `InteractionDetector.ShowDebugFeedback` defaulted to `false`, and the HUD wired into `Player.prefab`. This task only records the resulting conventions; it does not change code.

## Changes made
- `docs/DECISIONS.md`: appended `### 2026-10-08 — Konvensi Objek Interaktif (INT-006)` at the end of the "Catatan keputusan" section, matching the existing `Decision` / `Reason` / `Applies from` / `Owner/source` format. It records: `IInteractable` on the root GameObject with non-trigger colliders; trigger colliders ignored by interaction detection; visual feedback via `MaterialPropertyBlock` (never mutating shared material); uGUI central crosshair plus dynamic prompt text with zero per-frame GC; legacy `OnGUI` debug disabled by default.
- `docs/PROJECT_STRUCTURE.md`: added a `### Prefab UI` subsection documenting `Assets/Game/Prefabs/UI/` (system `UI`, wired via Inspector references, not `Resources/`) and a `### Konvensi objek interaktif (INT-006)` subsection summarising the placement and feedback rules.

## Files affected
- `docs/DECISIONS.md`
- `docs/PROJECT_STRUCTURE.md`

## Technical decisions
- Docs are written in Indonesian, so the new prose follows the existing language and heading style (`### YYYY-MM-DD — Topik` with an em-dash).
- Preserved the files' existing UTF-8 (no BOM) encoding and CRLF line endings; edits were made with `[System.IO.File]::WriteAllText` to avoid encoding drift.
- No code, scene, prefab, or `.meta` files were touched.

## Verification performed
- Confirmed the new entry is placed after the INT-005 entry and before `## Anggaran performa`, with correct blank-line spacing, via a line dump and a CRLF/byte check (`CR == LF`, first bytes `35,32,75`).
- Confirmed `PROJECT_STRUCTURE.md` blank-line structure and encoding the same way.
- `git status` checked to ensure only the two intended docs are staged for this commit.

## Final result
Both documents updated and committed with message `docs: record interactable object conventions for INT-006`.

## Known limitations
- Documentation only; no runtime verification was performed or needed for this task.

## Unresolved issues or follow-up work
- None for this task.