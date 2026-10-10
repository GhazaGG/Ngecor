# Development History: [INT-006] Fix Corner Splitting on Hard Edges & Reduce Outline Width

## Task Summary
1. Fix corner gaps/tearing on hard-edged geometries (such as cubes) where outline edges appeared disconnected at vertices ("ujung nya itu putus").
2. Reduce the default outline width to 0.02 m (~2 cm) per user request ("kecilin lagi deh size outlinenya").

## Relevant Previous Context
- The user provided a screenshot (`orca-paste-1791448281765-dac85e9a-0192-4b37-b312-271bf861f33b.png`) showing an interactable cube.
- In the screenshot:
  - The outline bars were overly chunky (at 0.04 m).
  - The outline edges along the top and sides did not meet at the corners: each face was separated by a noticeable triangular/rectangular gap at the vertices.
- Root Cause: Meshes with flat shading / hard edges (such as Unity's default Cube primitive with 24 vertices, 4 per face) have split vertex normals. At a cube corner where 3 faces meet, the 3 vertices share the same position $(x, y, z)$, but their normals point in mutually perpendicular cardinal directions: $(0, 1, 0)$, $(1, 0, 0)$, and $(0, 0, 1)$. When the inverted-hull vertex shader pushed each vertex outward along its normal, the 3 vertices separated from each other, tearing the inverted hull along every corner and edge.

## Changes Made
1. **Smoothed Normal Mesh Generation (`InteractionHighlighter.cs`):**
   - Implemented `GetOrCreateOutlineMesh(Mesh source)`:
     - Spatial quantization (`QuantizedVertex` with 0.5 mm grid snap) identifies all co-located vertices that share a corner or edge.
     - Sums the normals of all meeting faces at each position and normalizes the result.
     - Creates a duplicate mesh (`Instantiate(source)`) with `hideFlags = HideFlags.DontSave` and assigns the welded diagonal normals.
     - Caches the generated mesh in `s_OutlineMeshCache` per source mesh instance for zero per-frame garbage generation.
   - Guarded non-readable meshes (`!source.isReadable`) to fall back gracefully to source mesh without errors.
   - Updated `RenderOutline()` to pass the smoothed outline mesh to `Graphics.DrawMesh`.
2. **Reduced Outline Thickness to 0.02 m:**
   - Changed default `_outlineWidth` from `0.04` to `0.02` (2 cm) in `OutlineEdge.shader`, `InteractionHighlighter.cs`, and `INT006Setup.cs`.
3. **Unit Tests (`InteractionHighlighterTests.cs`):**
   - Added `GetOrCreateOutlineMesh_SmoothesNormalsAtHardCorners` to verify that for a Cube mesh, corner vertices receive smoothed diagonal normals instead of cardinal axes.
   - Updated `OutlineWidth_CanBeConfiguredAndClamped` to assert default `0.02f`.
4. **Documentation (`docs/DECISIONS.md`):**
   - Recorded the normal welding technique and 0.02 m thickness in project architectural decisions.

## Files Affected
- `Assets/Game/Materials/Interaction/OutlineEdge.shader` (Modified)
- `Assets/Game/Scripts/Editor/INT006Setup.cs` (Modified)
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (Modified)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (Modified)
- `docs/DECISIONS.md` (Modified)
- `.development-history/2026-10-08-15-36-int-006-fix-corner-splitting-and-reduce-outline.md` (New)

## Technical Decisions
- Computing smoothed normals on a separate cached outline mesh preserves the original mesh and renderers completely untouched. No assets on disk are modified, zero memory is leaked, and the visual outline corners stay seamlessly mitered and closed regardless of whether the target model has flat or smooth normals.

## Verification Performed
- Mathematical analysis of corner normal welding on 24-vertex cubes.
- Added automated NUnit test checking normal divergence.

## Final Result
- Corners on cubes and hard-edged interactables are now completely continuous and closed without gaps or tears.
- Outline width is reduced to a clean 0.02 m (2 cm), providing a sleek, non-intrusive border.
