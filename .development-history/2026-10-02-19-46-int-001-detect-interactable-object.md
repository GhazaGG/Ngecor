# INT-001 Detect Interactable Object

## Task summary
Implemented first-person raycast-based interactable object detection for issue #9 on `feat/interaction-detect`, along with generic interactable contracts, automated Play Mode regression tests, and debug feedback.

## Relevant previous context
Interaction detection serves as the fundamental layer for grabbing, carrying, dropping, tools, vehicles, and construction objects across M1 and M2. PLAYER-001 and PLAYER-002 established player locomotion, first-person camera pivot rotation, cursor lock lifecycle, and exposed `PlayerMovement.LocalCamera`. Netcode baseline was established in NET-001. Per `AGENTS.md` and `docs/DECISIONS.md`, interaction detection remains strictly offline, avoiding global singletons, event buses, or over-engineered frameworks.

## Changes made
- Created `IInteractable` interface in `Ngecor.Interaction` exposing `InteractionPrompt` and `CanInteract(GameObject interactor)`.
- Implemented `InteractableObject` component as a default ready-to-use interactable component that can be placed on any GameObject without custom code.
- Created `InteractionDetector` component for the player that casts a forward raycast from the local camera (or assigned origin).
  - Configurable `_maxDistance` (tunable, default 3m).
  - Line-of-sight solid collider occlusion check: stops at the nearest solid collider and does not detect interactables hidden behind walls.
  - Resolves interactables from child/compound colliders via `GetComponentInParent<IInteractable>()`.
  - Ignores triggers (`QueryTriggerInteraction.Ignore`) so volume triggers do not block interaction rays.
  - Validates `CanInteract` before assigning `CurrentTarget`.
  - Null-safe: gracefully sets `CurrentTarget` to null without throwing exceptions when looking at empty space or non-interactables.
  - Multi-instance guard: only executes detection and debug feedback for the local player instance (`_isLocalPlayer`).
  - Simple dev debug feedback: `OnGUI` overlay displaying target name, prompt, and distance; `OnDrawGizmosSelected` drawing raycast in Scene view.
- Added assembly definitions `Ngecor.Interaction.asmdef` and `Ngecor.Interaction.Tests.asmdef`.
- Added Play Mode test suites covering:
  - `InteractableObjectTests` (default prompt, `CanInteract` toggle, prompt updates).
  - `InteractionDetectorTests` (direct detection, max distance limit, solid wall blocking, null safety, parent resolution from child collider, trigger collider pass-through, disabled interaction check, non-local player ignore).
- Documented implementation plan in `docs/superpowers/plans/2026-10-02-int-001-detect-interactable-object.md`.

## Files affected
- `Assets/Game/Scripts/Interaction/IInteractable.cs`
- `Assets/Game/Scripts/Interaction/InteractableObject.cs`
- `Assets/Game/Scripts/Interaction/InteractionDetector.cs`
- `Assets/Game/Scripts/Interaction/Ngecor.Interaction.asmdef`
- `Assets/Game/Tests/Interaction/InteractableObjectTests.cs`
- `Assets/Game/Tests/Interaction/InteractionDetectorTests.cs`
- `Assets/Game/Tests/Interaction/Ngecor.Interaction.Tests.asmdef`
- `docs/superpowers/plans/2026-10-02-int-001-detect-interactable-object.md`
- `.development-history/2026-10-02-19-46-int-001-detect-interactable-object.md`

## Technical decisions
- Raycasting originates from `PlayerMovement.LocalCamera` by default with fallback to `_originTransform` or `transform`, ensuring seamless operation with Player prefab as well as standalone test fixtures.
- Used `GetComponentInParent<IInteractable>()` to ensure compound physics shapes (nested colliders) correctly resolve to the parent interactable object.
- Kept `InteractionDetector` lightweight and self-contained; no global manager, event bus, or complex reactive pipelines were introduced.
- Handled physics transform synchronization in unit/playmode test fixtures using `Physics.SyncTransforms()` after dynamic runtime instantiation and repositioning.

## Verification performed
- Executed Unity 6000.3.25f1 Play Mode automated test suite in batch mode:
  - `Ngecor.Interaction.Tests`: 11 passed, 0 failed, 0 skipped.
  - `Ngecor.Player.Tests`: 12 passed, 0 failed, 0 skipped.
  - Total: 23 passed, 0 failed, 0 skipped.
- Verified line-of-sight occlusion: rays do not penetrate solid obstacles.
- Verified trigger colliders do not obstruct detection.
- Verified clean git working tree without accidental `.meta` desync or serialized asset edits.

## Final result
All acceptance criteria for issue #9 are satisfied and proven by automated Play Mode tests. Code is committed and ready for pull request creation.

## Known limitations
- Manual feel in Unity Editor interactive Play Mode with physical player movement and mouse look in `Playground.unity` remains for developer playtesting pass.
- Crosshair UI and final visual HUD prompts are intentionally out of scope for INT-001 (handled via lightweight `OnGUI` debug feedback).

## Unresolved issues or follow-up work
- Proceed to INT-002 (Grab and carry physics object, issue #10) on a fresh branch `feat/grab-carry` after opening the PR for INT-001 and submitting implementation plan comment on issue #10.
