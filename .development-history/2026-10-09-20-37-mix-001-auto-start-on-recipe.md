# MIX-001 automatic start after pouring the recipe

## Task summary
Fix the mixer staying on the `SIAP` status after the player had poured both required ingredients.

## Relevant previous context
- MIX-001 is being tested in the isolated `Dev_DePo4l_MIXER` scene.
- The current prototype recipe is one cement unit plus one sand unit, producing two concrete units.
- The earlier status panel displayed `SIAP` because the mixer status was initialized on `Awake`, while mixing could only be started through an Inspector context menu. Pouring a complete recipe did not start the mixer or refresh that message.
- The preceding report verified the success panel visually and noted that physical keyboard-controlled pouring was not manually exercised.

## Changes made
- In `Mixer.Update`, an idle mixer now starts automatically when its container has a complete recipe.
- While the recipe is incomplete, the status now shows the current cement and sand counts.
- When all concrete has been removed from a ready mixer, it returns to idle and refreshes the status.

## Files affected
- `Assets/Game/Scripts/Construction/Mixer.cs`
- This report.

## Technical decisions
- Automatic start is the smallest interaction that makes pouring a complete recipe run the mixer; the previous Inspector-only start action was not reachable through normal gameplay.
- Kept the existing recipe and five-second mixing duration. No scene, package, or serialized Unity asset was changed in this pass.

## Verification performed
- Unity Editor 6000.3.25f1 remained connected and in Play Mode in `Dev_DePo4l_MIXER`.
- Transferred one cement unit and one sand unit into the mixer through the container API and waited through the five-second duration.
- Runtime inspection confirmed `MixerState.Ready`, two concrete units, and the message `BERHASIL: 2 CONCRETE`.
- Confirmed `OnGUI` had initialized its status style. The CLI Game View screenshot captured the 3D scene without the IMGUI panel, so this pass did not re-confirm the displayed text visually.
- Unity reported `compilationFailed=false`, `compiling=false`, and zero current Console errors.

## Final result
The complete recipe now starts mixing automatically, and the runtime reaches Ready with two concrete units instead of remaining idle with a stale `SIAP` message.

## Known limitations
- This pass verified recipe transfer through the container API; it did not manually exercise bucket grabbing and keyboard-controlled pouring.
- The latest CLI screenshot omitted the IMGUI panel even though runtime inspection confirmed the success message and that `OnGUI` had initialized. The panel should be rechecked directly in the Unity Game View.
- Concrete remains stored as material units without a visible output mesh. The recipe remains provisional and may change with MIX-003.

## Unresolved issues or follow-up work
- Manually verify the text panel, bucket pour, movement, and grabbing in `Dev_DePo4l_MIXER`.
- Revisit the recipe if MIX-003 changes the material ratio.
