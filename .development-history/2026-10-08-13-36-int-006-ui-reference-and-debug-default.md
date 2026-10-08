# INT-006 add UI reference and default debug feedback off

## Task summary
Prepared the interaction assembly for the INT-006 highlight/prompt work by adding the `UnityEngine.UI` assembly reference to `Ngecor.Interaction.asmdef`, defaulting the legacy `OnGUI` debug feedback off in `InteractionDetector`, exposing a `ShowDebugFeedback` property, and adding a PlayMode test that asserts the default is `false`. Verified with a Unity batchmode PlayMode test run and committed on branch `feat/interaction-highlight-prompt`.

## Relevant previous context
- `.development-history/2026-10-08-13-08-investigate-int-006-interaction-feedback.md` analysed issue #77 and recommended keeping the INT-001 `OnGUI` debug box behind `_showDebugFeedback` defaulting to `false`, and confirmed `com.unity.ugui: 2.0.0` is already in `Packages/manifest.json`, so a `UnityEngine.UI` asmdef reference is valid without adding packages.
- INT-001 introduced `InteractionDetector`, `IInteractable`, and `InteractableObject`. `_showDebugFeedback` previously defaulted to `true`.

## Changes made
1. `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`: added `"UnityEngine.UI"` to the `references` array (after `Unity.InputSystem`).
2. `Assets/Game/Scripts/Interaction/InteractionDetector.cs`: changed `_showDebugFeedback` default from `true` to `false`; added a public `bool ShowDebugFeedback { get; set; }` property that reads/writes `_showDebugFeedback`.
3. `Assets/Game/Tests/Interaction/InteractionDetectorTests.cs`: added PlayMode-agnostic `[Test] ShowDebugFeedback_DefaultsToFalse` asserting `_detector.ShowDebugFeedback` is `false`.

## Files affected
- `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`
- `Assets/Game/Scripts/Interaction/InteractionDetector.cs`
- `Assets/Game/Tests/Interaction/InteractionDetectorTests.cs`
No `.unity`, `.prefab`, or `.meta` files were edited.

## Technical decisions
- Kept the existing private `[SerializeField] bool _showDebugFeedback` field pattern and added a public property rather than replacing the field, matching the existing `MaxDistance` property style and preserving Inspector serialization.
- Made `ShowDebugFeedback_DefaultsToFalse` a plain `[Test]` (no yield) since it needs no frame step; it runs within the same PlayMode fixture.
- Used the coordinators exact commit message and did not alter any package manifest.

## Verification performed
- Ran: `Unity.exe -projectPath D:\coding\Ngecor -runTests -testPlatform PlayMode -testFilter InteractionDetectorTests -batchmode -nographics` (Unity 6000.3.25f1).
- Result: `TestResults-639270632781863194.xml` -> test-run result `Passed`, total 9, passed 9, failed 0, including `ShowDebugFeedback_DefaultsToFalse -> Passed`. The generated results file was deleted after inspection and was not committed.
- Committed as `4b0ba59` "feat: add UnityEngine.UI reference and toggle debug OnGUI off by default" (3 files changed, 15 insertions, 2 deletions).

## Final result
Interaction assembly references `UnityEngine.UI`, debug `OnGUI` feedback is off by default and togglable via `ShowDebugFeedback`, and all 9 `InteractionDetectorTests` pass in PlayMode.

## Known limitations
- Only the `InteractionDetectorTests` filter was executed; the wider PlayMode suite was not run.
- Removing a stale `.git/index.lock` (0 bytes, created before this task) was required to commit; no other git processes were active.

## Unresolved issues or follow-up work
None for this task. Next INT-006 steps (renderer highlight via MaterialPropertyBlock, uGUI crosshair/prompt canvas) are out of scope here.