# Development History: [INT-006] Edge-Only Outline and Plus Crosshair Interaction Feedback

## Task Summary
Implement clean, non-intrusive interaction feedback for issue #77 ([INT-006]):
1. A permanent, sharp `+` (plus sign) crosshair centered at `(0, 0)` in uGUI, replacing the old debug GUI and eliminating dual-crosshair artifacts.
2. An edge-only (silhouette/outline) white highlight on focused interactable objects using a lightweight inverted-hull shader (`OutlineEdge.shader`), leaving the object's interior textures, materials, and albedo 100% visible and untouched (eliminating previous solid yellow tinting).
3. A centered interaction prompt text below the crosshair at `(0, -35)` displaying `CurrentTarget.InteractionPrompt` with zero per-frame garbage collection.
4. Immediate dismissal of the outline and prompt text when the object is grabbed (`IsHeld == true`), destroyed, deactivated, or lost from line of sight.

## Relevant Previous Context
- Previously, an initial implementation was reverted because:
  - Two overlapping crosshairs appeared (a debug `+` from `InteractionDetector.OnGUI` and an 8x8 dot Image from `InteractionPromptUI`).
  - Highlighting painted the target object in a flat, solid yellow block via `MaterialPropertyBlock` setting `_BaseColor`, which obliterated the object's natural albedo, textures, and material look.
  - The user requested a complete revert and reimplementation adhering strictly to:
    - Single `+` crosshair only.
    - Highlight on border/edge only (`tepian/edge saja`) in white, NOT filling the object.
    - Orca orchestration workflow for task management.

## Changes Made
1. **Inverted-Hull Outline Shader (`OutlineEdge.shader`) & Material (`M_OutlineEdge.mat`):**
   - Implemented an unlit Universal Render Pipeline (URP) shader in `Assets/Game/Materials/Interaction/OutlineEdge.shader`.
   - Uses `Cull Front`, `ZTest LEqual`, and extrudes backfaces outward along normal (`normalOS * _OutlineWidth`).
   - Created native material `M_OutlineEdge.mat` configured with `_OutlineColor = Color(1, 1, 1, 0.9)` and `_OutlineWidth = 0.012`.
2. **Edge-Only Interaction Highlighter (`InteractionHighlighter.cs`):**
   - Added `InteractionHighlighter` to `Assets/Game/Scripts/Interaction/`.
   - Caches `MeshFilter` components of focused `IInteractable` target without per-frame allocations.
   - During `LateUpdate`, draws the silhouette outline via `Graphics.DrawMesh(mesh, matrix, _outlineMaterial, layer, null, submesh)`.
   - Leaves `Renderer.sharedMaterial` and `Renderer.material` 100% untouched; zero runtime material cloning and zero property block overrides on base color.
   - Automatically drops highlight if `IsHeld == true`, target is null, deactivated, or destroyed.
3. **Dual-Bar Plus Crosshair & Dirty-Tracked Prompt (`InteractionPromptUI.cs`):**
   - Added `InteractionPromptUI` to `Assets/Game/Scripts/Interaction/`.
   - Replaced dot crosshair with two intersecting white Image bars: horizontal `(12, 2)` and vertical `(2, 12)`, forming a crisp, symmetrical `+` crosshair at `(0, 0)` that is resolution-independent and requires no font glyphs.
   - Centered prompt text at `(0, -35)` with dirty tracking (`ReferenceEquals` check) so no strings are built or updated on frames where target prompt does not change.
   - Automatically hides prompt text when object is held, lost, or destroyed.
4. **Debug Feedback Flag & Prefabs (`Player.prefab`, `InteractionHUD.prefab`):**
   - Disabled legacy debug GUI in `InteractionDetector.cs` and `PlayerGrab.cs` (`_showDebugFeedback = false`).
   - Updated `Player.prefab` via Unity Editor API: wired `InteractionHighlighter` with `M_OutlineEdge.mat`, wired `InteractionPromptUI`, and disabled `_showDebugFeedback`.
   - Created `InteractionHUD.prefab` in `Assets/Game/Prefabs/UI/`.
   - Added reference to `UnityEngine.UI` in `Ngecor.Interaction.asmdef` and `Ngecor.Interaction.Tests.asmdef`.
5. **Comprehensive Test Suite (`InteractionHighlighterTests.cs`, `InteractionPromptUITests.cs`):**
   - Added 6 PlayMode unit tests for `InteractionHighlighter`:
     - `HighlightsTarget_WhenInteractionDetectorAcquiresTarget`
     - `ClearsHighlight_WhenTargetBecomesNull`
     - `ClearsHighlight_WhenTargetIsGrabbed`
     - `ClearsHighlightSafely_WhenTargetDestroyed`
     - `ClearsHighlightSafely_WhenTargetDeactivated`
     - `DoesNotMutateSharedMaterial`
   - Added 6 PlayMode unit tests for `InteractionPromptUI`:
     - `CrosshairIsAlwaysVisible`
     - `PromptTextDisplaysPrompt_WhenTargetAcquired`
     - `PromptTextHides_WhenTargetLost`
     - `PromptTextUpdates_WhenTargetChanges`
     - `PromptTextHidesSafely_WhenTargetDestroyed`
     - `PromptTextHides_WhenTargetIsGrabbed`

## Files Affected
- `Assets/Game/Materials/Interaction/OutlineEdge.shader` (New)
- `Assets/Game/Materials/Interaction/OutlineEdge.shader.meta` (New)
- `Assets/Game/Materials/Interaction/M_OutlineEdge.mat` (New)
- `Assets/Game/Materials/Interaction/M_OutlineEdge.mat.meta` (New)
- `Assets/Game/Prefabs/UI/InteractionHUD.prefab` (New)
- `Assets/Game/Prefabs/UI/InteractionHUD.prefab.meta` (New)
- `Assets/Game/Prefabs/Player/Player.prefab` (Modified)
- `Assets/Game/Scripts/Editor/INT006Setup.cs` (New)
- `Assets/Game/Scripts/Editor/INT006Setup.cs.meta` (New)
- `Assets/Game/Scripts/Interaction/InteractionDetector.cs` (Modified)
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (New)
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs.meta` (New)
- `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs` (New)
- `Assets/Game/Scripts/Interaction/InteractionPromptUI.cs.meta` (New)
- `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef` (Modified)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (New)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs.meta` (New)
- `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs` (New)
- `Assets/Game/Tests/Interaction/InteractionPromptUITests.cs.meta` (New)
- `Assets/Game/Tests/Interaction/Ngecor.Interaction.Tests.asmdef` (Modified)
- `docs/DECISIONS.md` (Modified)

## Technical Decisions
1. **Inverted-Hull Silhouette Outline over MaterialPropertyBlock Color Replacement:**
   Overwriting `_BaseColor` turned the entire object into an untextured solid flat block. Inverted hull renders only back-faces extruded along normals with `Cull Front` and `ZTest LEqual`, which causes the object's original surface and textures to remain completely visible while creating a sharp, glowing white silhouette at the edges.
2. **Dual-Bar uGUI Crosshair over Font or Sprite Textures:**
   Relying on font glyphs (like `+` character in Text) suffers from font baseline offsets, font fallback mismatches, and antialiasing softness across different resolutions. Two simple rectangular white `Image` bars (12x2 and 2x12) produce a guaranteed pixel-perfect, crisp, symmetrical crosshair at zero asset overhead.
3. **Orca Task-Based Coordination:**
   Tasks were defined, tracked, and updated via Orca CLI (`orca orchestration task-create` and `task-update`) across all 5 milestones.

## Verification Performed
- Ran the full automated Unity PlayMode test suite via batchmode:
  `Unity.exe -projectPath . -runTests -testPlatform PlayMode -batchmode -nographics`
  - Total tests run: 132
  - **Passed: 131**
  - **Failed: 0**
  - **Skipped: 1** (known pre-existing scaffolding test)
  - All 6/6 `InteractionHighlighterTests` passed.
  - All 6/6 `InteractionPromptUITests` passed.
  - All 53/53 other interaction & player tests passed.

## Final Result
All acceptance criteria for INT-006 and the user's specific feedback have been achieved:
- Permanent single `+` crosshair (no double crosshairs).
- Edge-only white silhouette outline (no solid yellow color fill).
- Clean interaction prompt text below crosshair.
- Instant dismissal when holding objects.
- 100% green test suite.

## Known Limitations
- Inverted-hull outline width is in object-space (0.012 m), which scales with object distance in perspective projection (slightly thinner when far away, thicker when close). This matches natural depth perspective and satisfies the lightweight performance budget without costly multi-pass screen-space Sobel edge detection.

## Unresolved Issues or Follow-Up Work
- None on this ticket. Ready for PR and gameplay review.
