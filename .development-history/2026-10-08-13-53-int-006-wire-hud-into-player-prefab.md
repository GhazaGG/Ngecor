# INT-006 wire HUD prefab into Player prefab

## Task summary
Created the `InteractionHUD` prefab (ScreenSpaceOverlay Canvas, CanvasScaler, centered crosshair Image, offset prompt Text) and attached `InteractionHighlighter` and `InteractionPromptUI` to the `Player` prefab with the HUD nested as a prefab instance and its Canvas/Image/Text wired into the component, all through a temporary Unity editor script run in batch mode (no text editing of `.prefab` files). The temporary script was deleted before committing, and `PlayerGrabTests` was verified green. Committed only the prefabs and their `.meta` files on branch `feat/interaction-highlight-prompt`.

## Relevant previous context
- `.development-history/2026-10-08-13-41-...` implemented `InteractionHighlighter` (guid `c236d428bf72c634080de032ad32a965`).
- `.development-history/2026-10-08-13-47-...` implemented `InteractionPromptUI` (guid `7b15ea2a6bec06047ae1b441fb5d1ad9`) with serialized `_canvas`/`_crosshair`/`_promptText` fields and a programmatic fallback.
- `Assets/Game/Prefabs/Player/Player.prefab` root already carried `PlayerMovement`, `InteractionDetector`, and `PlayerGrab`; `Assets/Game/Prefabs/UI` did not exist.
- An existing editor script lives at `Assets/Game/Scripts/Editor/BulkMaterialDemo.cs` in namespace `Ngecor.Editor` (no editor asmdef; compiled into `Assembly-CSharp-Editor`, which auto-references the `Ngecor.Interaction` asmdef).

## Changes made
1. Wrote a temporary `Assets/Game/Scripts/Editor/INT006Setup.cs` (`Ngecor.Editor.INT006Setup.Execute`) that:
   - created `Assets/Game/Prefabs/UI` via `AssetDatabase.CreateFolder`;
   - built `InteractionHUD.prefab` (Canvas set to ScreenSpaceOverlay, sortingOrder 100; CanvasScaler ScaleWithScreenSize 1920x1080; GraphicRaycaster; `Crosshair` Image 6x6 white at center; `PromptText` Text 480x40 at y=-35, MiddleCenter, Disabled) and saved it with `PrefabUtility.SaveAsPrefabAsset`;
   - loaded the Player prefab with `PrefabUtility.LoadPrefabContents`, added `InteractionHighlighter` and `InteractionPromptUI` to the root, instantiated the HUD prefab as a child, and set `_canvas`/`_crosshair`/`_promptText` via `SerializedObject`, then saved with `PrefabUtility.SaveAsPrefabAsset` and unloaded.
2. Ran it in batch mode; deleted the script and its `.meta` afterwards.

## Files affected
- `Assets/Game/Prefabs/Player/Player.prefab` (modified)
- `Assets/Game/Prefabs/UI.meta` (new folder meta)
- `Assets/Game/Prefabs/UI/InteractionHUD.prefab` (+ `.meta`) (new)
- Temporary: `Assets/Game/Scripts/Editor/INT006Setup.cs` (+ `.meta`) — deleted before commit.

## Technical decisions
- Everything was done through Unity Editor APIs (`PrefabUtility`, `SerializedObject`, `AssetDatabase`); no `.prefab`/`.meta` file was text-edited, per project rules.
- The HUD is a nested prefab instance under the Player root so it can be edited once and reused; `InteractionPromptUI._canvas/_crosshair/_promptText` reference the nested instance's stripped components.
- Included `Assets/Game/Prefabs/UI.meta` in the commit even though the task's `git add` line only named the folder and the Player prefab; without the folder `.meta` the new assets would get a fresh GUID on another machine.
- First invocation used `-executeMethod INT006Setup.Execute` and failed with "class could not be found"; reran with the namespace-qualified `Ngecor.Editor.INT006Setup.Execute`, which succeeded. No code change was needed.

## Verification performed
- Batch run: `Unity.exe -projectPath D:\coding\Ngecor -executeMethod Ngecor.Editor.INT006Setup.Execute -batchmode -quit -nographics` -> exit 0, log line "INT006Setup: InteractionHUD prefab created and Player prefab updated."
- Inspected `InteractionHUD.prefab`: Canvas `m_RenderMode: 0`, CanvasScaler `1920x1080`, Crosshair `x6 y6` at `(0,0)`, PromptText `x480 y40` at `y=-35`.
- Inspected `Player.prefab`: root MonoBehaviour guids now include `InteractionHighlighter` (`c236d4...`) and `InteractionPromptUI` (`7b15ea...`); `_canvas` -> stripped Canvas, `_crosshair` -> stripped `UnityEngine.UI.Image`, `_promptText` -> stripped `UnityEngine.UI.Text` (`m_Enabled: 0`); `InteractionHUD` present as a nested `PrefabInstance` child.
- Ran `Unity.exe -projectPath D:\coding\Ngecor -runTests -testPlatform PlayMode -testFilter PlayerGrabTests -batchmode -nographics` -> `TestResults-639270643186769282.xml` total 39, passed 39, failed 0. The results file was deleted after inspection.
- Committed as `70b6d73` "feat: attach InteractionHighlighter and InteractionPromptUI to Player prefab" (4 files, 440 insertions).

## Final result
The Player prefab now carries the interaction highlighter and prompt UI, with a reusable InteractionHUD prefab wired into the prompt component; all 39 PlayerGrab PlayMode tests still pass and the working tree contains only the commit plus pre-existing history reports.

## Known limitations
- Only `PlayerGrabTests` was run; the newly created HUD was not exercised in a real scene, and the highlight/prompt visuals were not observed in Play Mode (batch mode, no rendering).
- The HUD is not yet placed in `Playground.unity`; it rides along with the Player prefab instance.
- The temporary editor script is intentionally not committed, so re-running the setup would require recreating it.

## Unresolved issues or follow-up work
- Verify the crosshair/prompt appear correctly in Play Mode on the local player and confirm the highlight tint reads clearly against URP Lit materials (visual/gameplay review for INT-006).