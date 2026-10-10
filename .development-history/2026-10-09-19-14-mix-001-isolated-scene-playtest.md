# Isolated MIX-001 scene playtest

## Task summary
Prepared the dedicated mixer test scene so it runs separately from the scaffolding scene, then exercised the MIX-001 batch output in Play Mode.

## Relevant previous context
- MIX-001 logic and the earlier playtest setup are documented in the 2026-10-09 MIX-001 report.
- The workspace already contained an untracked `Dev_DePo4l_MIXER` scene with a player, floor, mixer, and two test buckets. It did not contain scaffolding, so this pass used that isolated scene instead of making a redundant duplicate.
- The scaffolded `Dev_DePo4l` scene was not opened for editing or changed in this pass.

## Changes made
- Set the dedicated scene's `PlayerMovement._isLocalPlayer` to true so its player enables local input and interaction feedback.
- Positioned the player behind the staged buckets and angled the camera down toward the mixer.
- Saved the scene through the Unity Editor.
- No gameplay scripts, packages, project settings, or build scene lists were changed.

## Files affected
- `Assets/Game/Scenes/Dev/Dev_DePo4l_MIXER.unity`
- This development history report.

## Technical decisions
- Reused the existing mixer-only scene because it already had no scaffolding objects.
- Kept the MIX-001 prototype batch at one cement unit plus one sand unit, producing two concrete units through the existing recipe calculator.
- Temporarily enabled `Application.runInBackground` in Play Mode so the Editor advanced the five-second timer while running the smoke check. This was runtime-only and is not saved to project settings.

## Verification performed
- Opened only `Dev_DePo4l_MIXER`; the active scene contains the player, floor, mixer, and test buckets, with no scaffolding.
- In Play Mode, transferred one cement unit and one sand unit from the staged buckets into the mixer, started a batch, and asserted after the mixing duration that the mixer was Ready with two concrete units and zero cement or sand remaining.
- The Game View capture showed the center crosshair and carrying feedback. The temporary capture was deleted after review.
- Unity Editor 6000.3.25f1 reported no compilation failure and zero current Unity Console errors. One Visual Studio integration warning reported that its UDP port was unavailable. The Pipeline CLI buffer also recorded transient sharing-violation entries while querying its heartbeat.
- Temporary smoke-check scripts and screenshots were removed after verification.

## Final result
The dedicated scene is active in Play Mode with the mixer Ready and two concrete units in its material container. The scaffolded scene remains separate and unchanged.

## Known limitations
- The smoke check moved materials through the container API; keyboard movement and the complete grab-and-pour flow were not manually tested.
- The mixer stores the two concrete units in its container. This scene does not show a concrete mesh or fill visual.

## Unresolved issues or follow-up work
- MIX-003 may revise the provisional recipe ratio; update this batch if that decision changes.
- A developer should manually exercise movement, grabbing, pouring, and the mixer start action in the dedicated scene.


