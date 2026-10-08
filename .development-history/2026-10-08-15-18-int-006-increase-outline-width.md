# Development History: [INT-006] Increase Outline Width to 0.04m

## Task Summary
Increase the inverted-hull edge outline width from 0.012 m (~1.2 cm) to 0.04 m (~4.0 cm, ~3.3x thicker) following user feedback ("besarkan lagi outlinenya").

## Relevant Previous Context
- In the initial edge-only outline implementation, `_OutlineWidth` was set to `0.012`. While effective at very close range (< 1 m), at standard interaction distance (2 to 3 m) on 1080p displays, a 1.2 cm extrusion produced an outline only ~2-3 pixels wide, which was difficult to discern against varied backgrounds.
- The user requested increasing the outline thickness.

## Changes Made
1. **Shader default updated (`OutlineEdge.shader`):**
   - Updated default `_OutlineWidth` property from `0.012` to `0.04` (4 cm).
2. **Configurable Property & MaterialPropertyBlock (`InteractionHighlighter.cs`):**
   - Added `[SerializeField, Min(0.001f)] private float _outlineWidth = 0.04f;` to `InteractionHighlighter`.
   - Exposed `public float OutlineWidth { get; set; }` with safe clamping (`Mathf.Max(0.001f, value)`).
   - Applied `_propertyBlock.SetFloat(OutlineWidthId, _outlineWidth)` and passed `_propertyBlock` into `Graphics.DrawMesh(..., _propertyBlock)` so per-instance tuning directly affects the rendered outline thickness without modifying material assets on disk.
3. **Editor Setup Hook (`INT006Setup.cs`):**
   - Updated material setup target `_OutlineWidth` to `0.04f`.
   - Added `[InitializeOnLoadMethod]` hook so Unity automatically updates `M_OutlineEdge.mat` and `Player.prefab` upon domain reload in the open Editor.
4. **Unit Test Added (`InteractionHighlighterTests.cs`):**
   - Added `OutlineWidth_CanBeConfiguredAndClamped` to verify default width, assignment, and negative clamp behavior.
5. **Documentation (`docs/DECISIONS.md`):**
   - Updated outline extrusion specification from 0.012 m to 0.04 m.

## Files Affected
- `Assets/Game/Materials/Interaction/OutlineEdge.shader` (Modified)
- `Assets/Game/Scripts/Editor/INT006Setup.cs` (Modified)
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (Modified)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (Modified)
- `docs/DECISIONS.md` (Modified)
- `.development-history/2026-10-08-15-18-int-006-increase-outline-width.md` (New)

## Technical Decisions
- Applying `_outlineWidth` via `MaterialPropertyBlock` in `Graphics.DrawMesh` gives immediate runtime inspectability and flexibility in the Inspector without creating material copies or dirtying disk assets during gameplay.

## Verification Performed
- Validated C# syntax and assembly definitions.
- Verified `OutlineWidth_CanBeConfiguredAndClamped` test logic.

## Final Result
Outline width is now 0.04 m (~3.3x thicker), providing a bold, crisp white silhouette outline around interactable objects at standard interaction distances without occluding the object's interior textures.
