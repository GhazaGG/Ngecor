# PLAYER-001 Basic Player Movement

## Outcome

Give the prototype a controllable player that moves forward, backward, left, and right at a consistent, tunable speed and stays outside normal floor and collider geometry. The feature is testable in the existing Playground scene.

## Agreed decisions

- Target Windows PC, first-person, with keyboard and mouse. Gamepad is backlog.
- Keep the installed Unity Input System package and existing `Player/Move` and `Player/Look` actions. Use WASD/arrows for movement and mouse delta for look; do not add packages or edit the input asset.
- Use a Unity `CharacterController` for locomotion.
- Move relative to the player's yaw so forward follows the first-person view direction.
- Use the existing `Player/Interact` E binding and existing `Player/Attack` left mouse binding as the minimum M1 bindings for interact and throw; this task implements movement and look only.
- Start at 5 meters per second; expose speed in the Inspector for tuning.

## Design

Create one `PlayerMovement` MonoBehaviour in `Assets/Game/Scripts/Player/`. It reads the existing `Player/Move` and `Player/Look` actions through Inspector references only when `_isLocalPlayer` is enabled. The field defaults to false so newly created replicas do not consume local input by accident; the owner assigns local control with `SetLocalPlayer` when it spawns the local player. The Playground instance is explicitly local. It bounds movement input magnitude, turns the player by mouse X, pitches a child camera by mouse Y, and moves relative to player yaw through `CharacterController.Move`. The controller owns collision response against floors, walls, ramps, and other ordinary colliders. Apply project gravity in code because `CharacterController` does not apply gravity itself.

Create a simple capsule player prefab in `Assets/Game/Prefabs/Player/` with the controller and movement component. Add one instance to `Assets/Game/Scenes/Playground.unity` so the issue can be tested in the scene it names. Make all serialized Unity changes in the Editor; do not hand-edit Unity YAML. Use the current Playground floor, ramp, and wall colliders.

The script only handles basic movement and first-person look. It does not add jump, sprint, animation, stamina, interaction, throwing, pushing Rigidbody objects, or multiplayer behavior. `CharacterController` does not respond to forces or push Rigidbodies by itself.

## Acceptance and verification

In Unity 6000.3.25f1 Play Mode in Playground:

1. WASD and arrow keys move in all four directions relative to the player's facing; mouse movement turns the view.
2. Diagonal movement has the same maximum speed as straight movement.
3. Movement speed is adjustable in the Inspector and remains consistent across frame rates.
4. The player stays on the flat floor and moves on the ramp without passing through normal colliders.
5. The Console has no new errors during the test.

Record input devices actually tested and any unavailable hardware. A clean compile or static inspection alone does not satisfy the Play Mode acceptance criteria.

## Risks and limits

- A `CharacterController` is not a physics body; later interaction work must explicitly define how player actions affect physical objects.
- Mouse sensitivity, 5 m/s default speed, and slope behavior need a gameplay feel check in Play Mode.
