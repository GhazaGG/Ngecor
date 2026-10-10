# VEH-002 Wheelbarrow Player Interaction

## Task summary

Implement the player interaction for starting and stopping a wheelbarrow push, using the existing mass-aware player push behavior and keeping the wheelbarrow under normal Rigidbody physics.

## Relevant previous context

- VEH-001 provides the dynamic wheelbarrow Rigidbody and physical handle setup.
- PLAYER-003 provides mass-aware contact pushing, a shared per-step impulse budget, and Rigidbody deduplication.
- The project workflow requires testing in Playground and keeping the main scene unchanged.

## Changes made

- Added a hold-interactable contract for interactions that can begin and end.
- Updated `PlayerGrab` so Interact starts or ends a hold interaction while preserving the existing carry/drop path.
- Exposed the current normalized movement direction through the existing push path in `PlayerMovement`.
- Added `WheelbarrowInteraction`, which applies the interacting player's movement push at the Rigidbody center of mass while the interaction is active.
- Attached the component to the wheelbarrow prefab. Its Rigidbody remains dynamic.

## Files affected

- `Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab`
- `Assets/Game/Scripts/Interaction/IInteractable.cs`
- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Scripts/Vehicle.meta`
- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs`
- `Assets/Game/Scripts/Vehicle/WheelbarrowInteraction.cs.meta`
- `.development-history/2026-10-07-13-15-veh-002-wheelbarrow-interaction.md`

## Technical decisions

- Reused PLAYER-003's existing mass-aware contact push, force cap, deduplication, and impulse budget instead of adding a separate wheelbarrow force model.
- The interaction does not make the Rigidbody kinematic, move its transform, or depend on a scene reference.
- The existing Interact action toggles begin/release; movement input supplies the push direction.

## Verification performed

- Unity 6000.3.25f1 imported the scripts and opened the Playground scene in the issue worktree.
- Confirmed the wheelbarrow prefab contains `WheelbarrowInteraction` and its Rigidbody has `Is Kinematic` disabled.
- In Playground, observed the interaction detector show `Target: Wheelbarrow` and `Prompt: Push` at 2.1 m.
- Stopped Play Mode without saving scene changes. `git diff --check` passed; no automated tests were added or run.
- The Interact action is bound to E with a Hold interaction. The available Orca key input sends a tap, so beginning/releasing the interaction, movement force, cargo behavior, slope behavior, and jitter were not verified in Play Mode.
- The original `D:\Projects\Ngecor` checkout remains clean on `main`.

## Final result

The interaction and prefab wiring are implemented. The prompt was confirmed in Playground; gameplay acceptance remains pending a manual run with reliable held-key input.

## Known limitations

- Start/release, stable pushing, pushing with cargo, and release on a slope still need manual Play Mode verification.
- No Profiler capture or automated gameplay test was performed.

## Unresolved issues or follow-up work

- Run the issue's full Playground acceptance checks with reliable keyboard input, recording movement stability, physics after release, cargo behavior, and slope behavior.
