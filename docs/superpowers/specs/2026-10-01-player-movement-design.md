# PLAYER-001 Basic Player Movement

## Outcome

Give the prototype a controllable player that moves forward, backward, left, and right at a consistent, tunable speed and stays outside normal floor and collider geometry. The feature is testable in the existing Playground scene.

## Agreed decisions

- Target the prototype at PC with keyboard and gamepad controls.
- Keep the installed Unity Input System package and existing `Player/Move` Vector2 action and bindings. Do not add packages or edit the input asset.
- Use a Unity `CharacterController` for locomotion.
- Interpret movement on world-space XZ axes, independent of camera orientation. Game perspective and camera-relative controls remain undecided.
- Start at 5 meters per second; expose speed in the Inspector for tuning.

## Design

Create one `PlayerMovement` MonoBehaviour in `Assets/Game/Scripts/Player/`. It reads the existing `Player/Move` action through an Inspector reference, bounds input magnitude so diagonals do not move faster, and sends horizontal and vertical displacement through `CharacterController.Move` each frame. The controller owns collision response against floors, walls, ramps, and other ordinary colliders. Apply project gravity in code because `CharacterController` does not apply gravity itself.

Create a simple capsule player prefab in `Assets/Game/Prefabs/Player/` with the controller and movement component. Add one instance to `Assets/Game/Scenes/Playground.unity` so the issue can be tested in the scene it names. Make all serialized Unity changes in the Editor; do not hand-edit Unity YAML. Use the current Playground floor, ramp, and wall colliders.

The script only handles basic movement. It does not add jump, sprint, camera control, animation, stamina, interaction, pushing Rigidbody objects, or multiplayer behavior. `CharacterController` does not respond to forces or push Rigidbodies by itself.

## Acceptance and verification

In Unity 6000.3.25f1 Play Mode in Playground:

1. WASD and arrow keys move in all four directions; the existing gamepad left stick does the same when a gamepad is available.
2. Diagonal movement has the same maximum speed as straight movement.
3. Movement speed is adjustable in the Inspector and remains consistent across frame rates.
4. The player stays on the flat floor and moves on the ramp without passing through normal colliders.
5. The Console has no new errors during the test.

Record input devices actually tested and any unavailable hardware. A clean compile or static inspection alone does not satisfy the Play Mode acceptance criteria.

## Risks and limits

- A `CharacterController` is not a physics body; later interaction work must explicitly define how player actions affect physical objects.
- The 5 m/s initial value and default slope behavior need a gameplay feel check in Play Mode.
- World-relative controls are independent of camera orientation; revisit them only when the camera/perspective issue chooses its control model.
