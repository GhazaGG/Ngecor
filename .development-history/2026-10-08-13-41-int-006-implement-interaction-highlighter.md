# INT-006 InteractionHighlighter via MaterialPropertyBlock

## Task summary
Implemented `InteractionHighlighter`, a component that highlights the `InteractionDetector`'s current target using a reusable `MaterialPropertyBlock` (never touching `renderer.material`), and added `InteractionHighlighterTests` covering acquisition, clearing on target-null/grab/destroy/deactivate, and shared-material immutability. All 6 PlayMode tests pass with Unity 6000.3.25f1, and the two new scripts plus their generated `.meta` files were committed on branch `feat/interaction-highlight-prompt`.

## Relevant previous context
- `.development-history/2026-10-08-13-08-investigate-int-006-interaction-feedback.md` recommended highlighting via `MaterialPropertyBlock` to avoid material leaks, caching renderers only on target change, clearing on target change/null/held/destroyed, and keeping the legacy `OnGUI` debug box behind a default-off flag.
- `.development-history/2026-10-08-13-36-int-006-ui-reference-and-debug-default.md` (this same branch) added the `UnityEngine.UI` asmdef reference and defaulted `InteractionDetector.ShowDebugFeedback` to false.
- `InteractionDetector.CurrentTarget` is a read-only `IInteractable` set during `Detect()`; `GrabbableObject.IsHeld` turns true on `OnGrab`.
- The project is URP and the performance budget forbids per-frame `GetComponent`/allocations, so caching and a single `MaterialPropertyBlock` were used.

## Changes made
1. `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (new): `[RequireComponent(typeof(InteractionDetector))]`, couples to the detector in `Awake`, evaluates in `LateUpdate`. Guarded `IsHighlightable()` rejects null/fake-null (destroyed) targets, inactive behaviours/GameObjects, and held `GrabbableObject`s. Renderers are gathered into a reusable `List<Renderer>` only when the target object changes. Highlight is applied by setting `_BaseColor` (and optionally `_Color`/`_EmissionColor`) on a cached `MaterialPropertyBlock` and pushing it via `Renderer.SetPropertyBlock`; clearing uses `SetPropertyBlock(null)` and resets cached state. Public surface: `HasHighlight`, `HighlightedRendererCount`, `HighlightColor`, `ClearHighlight()`.
2. `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (new): builds a real local player (CharacterController + PlayerMovement + InteractionDetector + InteractionHighlighter) and drives detection through actual raycasts against primitives. Tests: `HighlightsTarget_WhenInteractionDetectorAcquiresTarget`, `ClearsHighlight_WhenTargetBecomesNull`, `ClearsHighlight_WhenTargetIsGrabbed`, `ClearsHighlightSafely_WhenTargetDestroyed`, `ClearsHighlightSafely_WhenTargetDeactivated`, `DoesNotMutateSharedMaterial`.
3. Generated `.meta` files for both new scripts.

## Files affected
- `Assets/Game/Scripts/Interaction/InteractionHighlighter.cs` (+ `.meta`)
- `Assets/Game/Tests/Interaction/InteractionHighlighterTests.cs` (+ `.meta`)
No `.unity`, `.prefab`, or `.asset` files were edited, and no packages were added.

## Technical decisions
- Used `MaterialPropertyBlock` exclusively; `renderer.material` is never referenced, so no `Material (Instance)` leaks and no on-disk asset mutation. Verified by `DoesNotMutateSharedMaterial`, which asserts `renderer.sharedMaterial` stays reference-equal and its `_BaseColor` is unchanged after highlight.
- Per-frame work in `LateUpdate` is limited to null/behaviour comparisons; renderer discovery and property-block writes happen only on target change, keeping steady-state allocations at zero.
- `IsHighlightable` accepts the target via `IInteractable`, then treats it as `UnityEngine.Object`/`Behaviour`/`Component` to honor Unity's fake-null semantics for destroyed and deactivated targets.
- Made the `GrabbableObject` kinematic in the held-target test so gravity cannot move it out of the detection ray before the assertion.
- The `DoesNotMutateSharedMaterial` assertion compares captured before/after color channels with `Within(0.0001f)` rather than exact `Color` equality, because Unity's shader round-trip can perturb low-order bits.

## Verification performed
- Ran: `Unity.exe -projectPath D:\coding\Ngecor -runTests -testPlatform PlayMode -testFilter InteractionHighlighterTests -batchmode -nographics` (Unity 6000.3.25f1).
- First run: 5/6 passed; `DoesNotMutateSharedMaterial` failed on an exact `Color` comparison while displaying identical rounded values (floating-point noise). Updated the assertion to per-channel tolerance and reran.
- Final run: `TestResults-639270636361592148.xml` -> total 6, passed 6, failed 0; all six named tests show `Passed`. Generated `TestResults-*.xml` files were deleted after inspection.
- Committed as `bfe5196` "feat: implement InteractionHighlighter with MaterialPropertyBlock and zero allocations" (4 files, 358 insertions).

## Final result
`InteractionHighlighter` highlights the detector's target with a `MaterialPropertyBlock`, clears cleanly on target change/loss/grab/destroy/deactivate, and never mutates shared materials; all six dedicated PlayMode tests pass.

## Known limitations
- Only the `InteractionHighlighterTests` filter was executed; the full PlayMode suite was not rerun.
- Highlight tint targets the URP Lit `_BaseColor` property; emission is opt-in via `_useEmission`. Non-URP-Lit shaders may only respond to `_Color`.
- No scene or prefab was wired up (out of scope); the component must be added to the player in a later INT-006 step.

## Unresolved issues or follow-up work
- Remaining INT-006 scope: a uGUI crosshair/prompt canvas and adding the highlighter to the player prefab/scene (will require Unity Editor edits and coordination).