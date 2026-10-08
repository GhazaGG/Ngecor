# INT-006 Interaction Feedback (Highlight and Prompt) Investigation

## Task summary
Investigated GitHub issue #77 (`[INT-006] Interaction feedback: highlight and prompt`), studied the repository guidelines in `AGENTS.md`, and reviewed all project design and architectural documentation in `docs/` (`DECISIONS.md`, `PROJECT_STRUCTURE.md`, `WORKFLOW.md`, `GAME_DESIGN.md`, `PROJECT_MANAGEMENT.md`).

## Relevant previous context
- INT-001 (#9) established first-person interactable object detection via `InteractionDetector`, `IInteractable`, and `InteractableObject`, using raycasting from the local player camera with solid collider line-of-sight occlusion and trigger collider pass-through (`QueryTriggerInteraction.Ignore`). Debug feedback was rendered via immediate-mode `OnGUI` with a simple crosshair and text box. Final visual prompts and object highlights were explicitly marked out of scope for INT-001.
- INT-002 (#10), INT-003 (#11), INT-004 (#12), and INT-005 (#76) implemented physics grab, carry hold positioning, drop depenetration / collision separation rules, and throw mechanics for `GrabbableObject`.
- The game currently lacks distinct visual feedback in the 3D world: players cannot distinguish grabbable/interactable props from static environment geometry.

## Changes made
- Added this investigation report only. No gameplay assets, prefabs, scenes, or code files were modified in this turn.

## Files affected
- `.development-history/2026-10-08-13-08-investigate-int-006-interaction-feedback.md`

## Technical decisions and analysis
1. **Highlight Architecture via `MaterialPropertyBlock`:**
   - **Zero Material Leakage / No Asset Mutation:** Using `MaterialPropertyBlock` ensures `renderer.sharedMaterial` is never cloned (avoiding runtime `Material (Instance)` leaks) and project asset files on disk are completely untouched, satisfying Acceptance Criteria 1 and 2.
   - **URP Lit Tint Compatibility:** URP Lit materials in the project (such as `Bucket.mat`, `Sand.mat`, `Scaffolding_Wood.mat`) use `_BaseColor` and `_EmissionColor`. Because `_EMISSION` shader keyword is often compiled out on materials that do not enable emission in the Inspector, setting only `_EmissionColor` via `MaterialPropertyBlock` may have no effect on non-emissive materials. The highlight implementation should apply a clear `_BaseColor` tint (or brightness boost) and/or `_EmissionColor` tint without breaking base textures.
   - **Renderer Caching:** Cache `Renderer[]` targets only when `CurrentTarget` changes. Never call `GetComponentsInChildren<Renderer>()` every frame.
   - **Cleanup Lifecycle:** When the detected target changes, is cleared (looking away), becomes held (`IsHeld`), is disabled, or is destroyed, the property block on previous renderers must be cleared immediately via `renderer.SetPropertyBlock(null)` (or resetting the block).

2. **UI Architecture (uGUI):**
   - **Single Local Screen-Space Canvas:** A single Canvas overlay (not per-object world canvases) containing:
     - A permanent central crosshair indicator (e.g., small dot or cross mark).
     - A centered prompt text below the crosshair showing `CurrentTarget.InteractionPrompt` when an interactable is in focus.
   - **Zero Per-Frame Allocations:** UI text is only updated when the prompt string or target state changes (dirty tracking), avoiding per-frame string concatenation or allocations in `Update()`.
   - **Debug Legacy:** The existing `OnGUI` debug box from INT-001 can remain as a developer debug option controlled by `_showDebugFeedback` on `InteractionDetector`, defaulting to `false`.

3. **Target Lifecycle and Null Safety:**
   - Must handle cases where target GameObjects are destroyed, deactivated, or grabbed while highlighted without throwing `NullReferenceException` or `MissingReferenceException`.
   - Held objects (`IsHeld == true`) must immediately clear any active highlight.

4. **Convention Documentation:**
   - Must document the standard interactable prop convention in `docs/PROJECT_STRUCTURE.md` or `docs/DECISIONS.md`: `IInteractable` component belongs on the root GameObject having non-trigger colliders; trigger colliders are strictly ignored by interaction detection rays.

5. **Scope Boundaries:**
   - Strictly client-side / local player visual feedback; no networking code (`AGENTS.md` rule #11).
   - No outline shaders, post-processing passes, or third-party asset store packages (`AGENTS.md` rule #1).
   - No dynamic action rebinding text in this ticket.

## Verification performed
- Verified repository status: working tree is clean on `main` branch (`origin/main`).
- Verified Unity installation: Unity 6000.3.25f1 installed at `D:\Unityhub\Editor\6000.3.25f1\Editor\Unity.exe`.
- Inspected all relevant existing scripts: `IInteractable.cs`, `InteractableObject.cs`, `GrabbableObject.cs`, `InteractionDetector.cs`, `PlayerGrab.cs`.
- Verified `Packages/manifest.json`: `com.unity.ugui: 2.0.0` is already installed.
- Verified asmdef configurations: `Ngecor.Interaction.asmdef` can reference `UnityEngine.UI` if UI components reside in the interaction assembly.

## Final result
Comprehensive analysis of Issue #77 completed and aligned with project constraints, architectural guidelines, and technical rules.

## Known limitations
No implementation code or scene assets were changed; implementation awaits the developer's confirmation.

## Unresolved issues or follow-up work
- Upon user prompt, create task branch `feat/interaction-highlight-prompt` and implement INT-006 following the established workflow.
