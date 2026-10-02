# VEH-001 Wheelbarrow Physics Prototype

## Goal

Create a lightweight, playable wheelbarrow physics prototype for issue #13. The wheelbarrow can be pushed by the player through physical contact, traverse the existing simple slope, carry at least three independent test cubes, and spill them when tipped. Its mass and handling remain tunable in the Inspector.

## Scope

- Add a graybox wheelbarrow prefab with one root Rigidbody and compound primitive colliders.
- Keep the cargo cubes as independent Rigidbody objects so gravity and collisions can move them out of the tray.
- Let the existing CharacterController player physically push dynamic Rigidbody objects on contact. Keep this behavior generic; do not reference the wheelbarrow type from player movement.
- Use the existing Playground for the issue's manual test, adding only the wheelbarrow and nearby test cubes needed for that flow.
- Tune the wheelbarrow mass through its Rigidbody and handling through collider layout. Do not add a package or a general vehicle framework.

## Explicitly deferred

- Press-E hold/release, target detection, and player/vehicle control switching. Those belong to the later wheelbarrow interaction work after INT-001 and INT-002.
- Multiplayer, final art, damage, and durability.
- A motor, suspension, or separately simulated wheel. VEH-001 proves the lightweight rigidbody-and-cargo loop; add articulated wheel behavior only if the Unity playtest shows the prototype cannot satisfy the slope or handling criteria.

## Components and behavior

1. `Wheelbarrow.prefab` owns one dynamic Rigidbody. Primitive colliders on the same root approximate the tray, handles, supports, and wheel contact shape. Keep the moving colliders primitive to match the performance budget.
2. The Rigidbody mass and collider layout are the prototype's tuning controls. No wheelbarrow-specific runtime script is needed unless Play Mode tuning proves these Inspector settings insufficient.
3. Player contact transfers the player's horizontal movement into a push on a contacted dynamic Rigidbody. It does not change input maps, lock the player to the wheelbarrow, or alter where WASD is routed.
4. Cargo remains separate from the wheelbarrow Rigidbody and is not parented or constrained to the tray. Three cubes can rest inside the tray under gravity; tipping the wheelbarrow allows them to fall naturally.
5. The Playground test uses the existing ground and 20-degree ramp. The wheelbarrow is placed on the flat area, with three loose cubes available beside it for loading.

## Data flow

Player movement remains the source of the player's motion. On physical contact, the player movement component passes horizontal motion to the contacted dynamic Rigidbody. The wheelbarrow then moves and tips under normal physics; its cargo responds only to gravity and collisions. No interaction intent or control ownership state is introduced.

## Failure handling and limits

- A missing or non-dynamic Rigidbody on a contacted collider is ignored; player movement continues normally.
- No special cargo recovery or auto-loading is added. A cube that falls must be picked up and put back manually in the prototype scene.
- The fixed compound-collider wheel is an intentional first-pass limit. If slope traversal or tipping feels wrong in Play Mode, revise the wheel/collider arrangement within VEH-001 before adding a joint-based wheel system.

## Verification

- Run the existing Player movement tests and add a focused automated check that player contact moves a dynamic Rigidbody while normal wall collision remains intact.
- In Unity 6000.3.25f1, open Playground and verify: contact-push the empty wheelbarrow on flat ground; push it up and down the 20-degree ramp; load three cubes and push normally; tip the tray until cargo falls; adjust mass and handling values and confirm the result changes.
- Check the Console for new errors and inspect Rigidbody motion for runaway impulses, floor penetration, or persistent jitter.
- Report automated checks separately from Unity Play Mode results. Do not mark manual acceptance as passed until these steps are run in Unity.

## Files expected during implementation

- `Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab` and its `.meta`
- `Assets/Game/Scripts/Player/PlayerMovement.cs` and its `.meta` (contact push path)
- `Assets/Game/Tests/Player/PlayerMovementTests.cs` and any required assembly/folder metadata
- `Assets/Game/Scenes/Playground.unity` only for the explicitly requested test setup

All Unity serialized assets must be created or changed through the Unity Editor. Do not add networking code, packages, or ProjectSettings changes.
