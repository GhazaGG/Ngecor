# INT-006 InteractionPromptUI crosshair and prompt dirty tracking

## Task summary
Implemented `InteractionPromptUI`, which renders an always-visible crosshair plus a prompt label reflecting the `InteractionDetector`'s current target, with dirty tracking over `_lastPromptVisible`/`_lastDisplayedPrompt` to avoid per-frame string work. Added `InteractionPromptUITests` covering the six required scenarios; all passed on the first Unity run. Committed both scripts and their `.meta` files (plus the required test-asmdef reference) on branch `feat/interaction-highlight-prompt`.

## Relevant previous context
- `.development-history/2026-10-08-13-41-int-006-implement-interaction-highlighter.md` implemented `InteractionHighlighter` on the same branch, reusing the detector's `CurrentTarget` and guarding against destroyed/inactive/held targets.
- `.development-history/2026-10-08-13-36-...` added the `UnityEngine.UI` reference to `Ngecor.Interaction.asmdef` and defaulted debug feedback off.
- `InteractionDetector.CurrentTarget` is a read-only `IInteractable` set by `Detect()`; `InteractionDetector.HasTarget` already accounts for Unity fake-null.
- `GrabbableObject.IsHeld` becomes true on `OnGrab`. `InteractableObject`/`GrabbableObject` store their prompt in a private `_prompt` field with no public setter.
- uGUI (`com.unity.ugui` 2.0.0) is present; the runtime assembly name is `UnityEngine.UI`.

## Changes made
1. `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs` (new): `[RequireComponent(typeof(InteractionDetector))]`; serialized `Canvas`/`Image`/`Text` fields with a programmatic fallback in `Awake` that builds a ScreenSpaceOverlay `Canvas` (CanvasScaler + GraphicRaycaster), a centered crosshair `Image`, and a prompt `Text`. `LateUpdate` reads the detector target, computes `shouldShow = has-target && active && !held`, and only touches `Text.enabled`/`Text.text` when state actually changes. Public surface for tests: `Canvas`, `Crosshair`, `PromptText`, `IsPromptVisible`, `DisplayedPrompt`. Crosshair stays enabled always.
2. `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs` (new): drives detection via real raycasts and covers `CrosshairIsAlwaysVisible`, `PromptTextDisplaysPrompt_WhenTargetAcquired`, `PromptTextHides_WhenTargetLost`, `PromptTextUpdates_WhenTargetChanges`, `PromptTextHidesSafely_WhenTargetDestroyed`, `PromptTextHides_WhenTargetIsGrabbed`.
3. `Assets/Game/Tests/Interaction/Ngecor.Interaction.Tests.asmdef`: added the `UnityEngine.UI` reference so the new test file can reference `Image`/`Text`/`Canvas` (this assembly uses explicit references, so auto-referencing does not apply).
4. Generated `.meta` files for both new scripts.

## Files affected
- `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs` (+ `.meta`)
- `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs` (+ `.meta`)
- `Assets/Game/Tests/Interaction/Ngecor.Interaction.Tests.asmdef`
No `.unity`, `.prefab`, or `.asset` files were edited, and no packages were added.

## Technical decisions
- Dirty tracking uses a `bool` plus a `string` reference comparison. `InteractionPrompt` returns a cached field, and `prompt` derives from it without concatenation, so `ReferenceEquals` works and no new string is built per frame; `Text.text` is assigned only on change. On hide, `enabled` is set false and text cleared once.
- `IsPromptable()` mirrors `InteractionHighlighter.IsHighlightable()`: rejects null/fake-null (destroyed) targets, inactive behaviours/GameObjects, and held `GrabbableObject`s.
- Programmatic fallback was added as required; it also makes the component usable in tests without scene wiring. Font lookup tries `LegacyRuntime.ttf` (Unity 6) and falls back to `Arial.ttf`.
- The prompt string was set on test targets reflectively because the serialized `_prompt` field has no public setter; this keeps production code untouched while allowing distinct prompts per target.

## Verification performed
- Ran: `Unity.exe -projectPath D:\coding\Ngecor -runTests -testPlatform PlayMode -testFilter InteractionPromptUITests -batchmode -nographics` (Unity 6000.3.25f1).
- Result: `TestResults-639270639653005688.xml` -> total 6, passed 6, failed 0; all six named tests show `Passed`. The generated results file was deleted after inspection.
- Committed as `1ba4cbf` "feat: implement InteractionPromptUI with crosshair and prompt dirty tracking" (5 files changed, 400 insertions, 1 deletion).

## Final result
The interaction UI shows a persistent crosshair and a prompt that appears, updates, and hides based on the detector's current target, clears safely when the target is destroyed or grabbed, and performs no per-frame string allocation; all six dedicated PlayMode tests pass.

## Known limitations
- Only the `InteractionPromptUITests` filter was executed; the full PlayMode suite was not rerun.
- The component is not yet wired into the player prefab/scene; the programmatic fallback creates UI at runtime, but scene wiring remains a later INT-006 step.
- Legacy uGUI `Text` is used (not TextMeshPro) to stay within the existing asmdef references.

## Unresolved issues or follow-up work
- Remaining INT-006 scope: adding `InteractionPromptUI` (and `InteractionHighlighter`) to the player prefab so they run in `Playground.unity`.