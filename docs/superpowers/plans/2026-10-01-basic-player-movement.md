# PLAYER-001 Basic Player Movement Plan

**Goal:** Meet issue #7 acceptance criteria with a first-person Windows player that moves with keyboard input and looks with the mouse in Playground.

**Architecture:** `PlayerMovement` reads the existing `Player/Move` and `Player/Look` Input System actions, turns a camera child, and moves a `CharacterController` relative to player yaw while applying gravity. A capsule prefab instance is placed in Playground.

**Constraints:** Unity 6000.3.25f1; Input System 1.20.0 already installed; do not change packages or the input action asset; do all serialized Unity edits through Unity Editor; no gamepad, jump, sprint, animation, interaction, throw behavior, or multiplayer code in this issue.

## Tasks

1. [x] **Document the binding choice.** In `docs/DECISIONS.md`, record Move = WASD/arrows, Look = mouse delta, Interact = E, and Throw = existing left-mouse `Attack` binding. The latter two bindings are documented only; behavior remains out of scope.

2. [x] **Add Play Mode tests.** Tests cover Look yaw/pitch, forward movement relative to yaw, four cardinal directions, diagonal speed, non-local input gating, runtime local-owner assignment, wall collision, and ramp traversal. Fixtures use a separate coordinate area from Playground objects.

3. [x] **Implement movement/look.** `PlayerMovement` uses `CharacterController`, defaults to non-local and enables/reads Move/Look only when `_isLocalPlayer` is explicitly true, clamps Move magnitude, rotates yaw and camera pitch from Look (mouse delta is not multiplied by delta time), moves relative to yaw at serialized 5 m/s using delta time, applies gravity, exposes look sensitivity, and disables actions it enabled with component lifecycle. `SetLocalPlayer` lets a runtime owner assign local control; the Playground instance is explicitly local.

4. [x] **Run automated tests and compile.** Unity 6000.3.25f1 command-line Play Mode suite reports 8 passed, 0 failed, 0 skipped. This does not establish a clean interactive Console: Unity's Package Manager Window previously logged `Error fetching package list. Operation cancelled` during editor validation.

5. [x] **Create the player prefab in Unity Editor.** Created `Assets/Game/Prefabs/Player/Player.prefab` with a capsule visual, CharacterController, PlayerMovement, camera pivot/camera, and existing Move/Look references. Defaults are 5 m/s and a 0.1 look sensitivity.

6. [x] **Place the prefab in Playground through Unity Editor.** Added the player instance to the existing Playground scene and removed the old free camera. The saved spawn is near the ramp's low end. Existing ground, ramp, walls, and physics props remain.

7. [ ] **Manual acceptance test in Play Mode.** Still required: directly verify WASD and arrows, mouse look, releasing input, diagonal speed, adjustable 5/2.5 m/s at normal/reduced frame rate, flat ground, ramp and wall collision, and Console cleanliness. Automated tests passed, and the scene entered Play Mode, but direct keyboard/mouse delivery and gameplay feel were not confirmed. Gamepad is backlog.

8. [ ] **Review and hand off.** `git diff --check` passed and an English development-history report was added. Team technical/gameplay review, manual acceptance, and the required PR review/merge are outstanding; do not mark issue Done before those gates.
