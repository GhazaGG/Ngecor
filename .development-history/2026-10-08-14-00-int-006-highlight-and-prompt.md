# INT-006 — Highlight and prompt (consolidated)

## Task summary
Completed the INT-006 line of work: reference/UI setup, `InteractionDetector` debug toggle, `InteractionHighlighter`, `InteractionPromptUI`, Player prefab wiring, documentation of the interactable convention, and a full PlayMode test run across all assemblies.

## Relevant previous context
The INT-006 work was split into six dispatched tasks on branch `feat/interaction-highlight-prompt`:
1. Add `UnityEngine.UI` to `Ngecor.Interaction.asmdef` and default `InteractionDetector.ShowDebugFeedback` to `false`.
2. Implement `InteractionHighlighter` (MaterialPropertyBlock, zero allocations) and its tests.
3. Implement `InteractionPromptUI` (crosshair + dynamic prompt with dirty tracking) and its tests.
4. Set up the Interaction HUD prefab and wire it into the Player prefab via a temporary batchmode editor script (no text editing of `.prefab` files).
5. Document the interactable convention in `docs/DECISIONS.md` and `docs/PROJECT_STRUCTURE.md`.

## Changes made
- `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`: added `UnityEngine.UI` reference.
- `Assets/Game/Scripts/Interaction/InteractionDetector.cs`: `_showDebugFeedback` now defaults to `false`; exposes `public bool ShowDebugFeedback`.
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (new): `[RequireComponent(typeof(InteractionDetector))]`, `MaterialPropertyBlock`-based highlight, reusable `List<Renderer>` cache, clears highlight on target change/null/held/destroyed/deactivated, restores with `SetPropertyBlock(null)`, never mutates `renderer.material`.
- `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs` (new): `[RequireComponent(typeof(InteractionDetector))]`, always-on uGUI crosshair, dynamic prompt `Text` shown only when `HasTarget` and not held, dirty tracking (`_lastPromptVisible`, `_lastDisplayedPrompt`) for zero per-frame GC, programmatic fallback canvas/crosshair/text in `Awake`.
- Tests: `InteractionHighlighterTests.cs`, `InteractionPromptUITests.cs`, added `InteractionDetectorTests.ShowDebugFeedback_DefaultsToFalse`.
- `Assets/Game/Prefabs/UI/InteractionHUD.prefab` (new) and `Assets/Game/Prefabs/Player/Player.prefab` (wired with highlighter + prompt UI) — created/edited through Unity, not by text editing.
- `docs/DECISIONS.md` and `docs/PROJECT_STRUCTURE.md`: recorded the INT-006 conventions.
- Temporary `Assets/Game/Scripts/Editor/INT006Setup.cs` was deleted after use.

## Files affected
- `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`
- `Assets/Game/Scripts/Interaction/InteractionDetector.cs`
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs`
- `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs`
- `Assets/Game/Tests/Interaction/InteractionDetectorTests.cs`
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs`
- `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs`
- `Assets/Game/Prefabs/UI/InteractionHUD.prefab`
- `Assets/Game/Prefabs/Player/Player.prefab`
- `docs/DECISIONS.md`, `docs/PROJECT_STRUCTURE.md`

## Technical decisions
- Highlight uses `MaterialPropertyBlock` + `Renderer.SetPropertyBlock` so shared materials are never instanced; renderers are cached in a reusable list to avoid per-frame GC.
- Prompt UI reuses one crosshair and one text element, updating only on state change (dirty tracking) to keep `Update` allocation-free, matching the project performance budget.
- `IInteractable` lives on the root GameObject; detection raycasts with `QueryTriggerInteraction.Ignore` and resolves via `GetComponentInParent<IInteractable>()`.
- `OnGUI` debug feedback is off by default and only enabled manually in the editor.

## Verification performed
Full batchmode PlayMode suite (Unity 6000.3.25f1):
`-runTests -testPlatform PlayMode -batchmode -nographics`
Result: **133 total, 132 passed, 0 failed, 1 skipped**, exit code 0.
Per assembly:
- **Ngecor.Interaction.Tests**: 67 total, 67 passed, 0 failed, 0 skipped (includes InteractionHighlighterTests 6/6, InteractionPromptUITests 6/6, InteractionDetectorTests 9/9, PlayerGrabTests 39/39).
- **Ngecor.Player.Tests**: 33 total, 33 passed, 0 failed, 0 skipped.
- **Ngecor.Material.Tests**: 33 total, 32 passed, 0 failed, 1 skipped (`SandPileAndBucketPrefabPlayModeTests.PourPreviewCreatesMissingDirectoryAndCanCaptureAgain`, pre-existing and unrelated to INT-006).
The earlier per-task runs for `InteractionDetectorTests`, `InteractionHighlighterTests`, `InteractionPromptUITests`, and `PlayerGrabTests` also passed.

## Final result
All INT-006 acceptance criteria implemented and green in PlayMode; docs updated; full PlayMode suite passes with no failures.

## Known limitations
- One pre-existing Material test is skipped (unrelated to INT-006); not investigated here.
- Performance was validated by design (MaterialPropertyBlock, cached lists, dirty tracking) rather than a captured Profiler trace.

## Unresolved issues or follow-up work
- Optional: capture a Profiler trace for the highlight/prompt path to confirm the zero-allocation claim with data.
- Investigate the pre-existing skipped `SandPileAndBucketPrefabPlayModeTests` case separately.