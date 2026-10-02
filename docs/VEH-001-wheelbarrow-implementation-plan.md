# VEH-001 Wheelbarrow Physics Prototype Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the VEH-001 graybox wheelbarrow that the player can push through physical contact, load with three loose cubes, and tip on the existing Playground slope.

**Architecture:** Keep the wheelbarrow as one dynamic Rigidbody with compound primitive colliders and keep cargo as independent Rigidbody cubes. Extend the existing CharacterController contact path to push contacted dynamic Rigidbodies generically; leave E-based hold/release for the later interaction tickets.

**Tech Stack:** Unity 6000.3.25f1, C#, Unity Physics, Unity Test Framework already present in the project.

**Spec:** [VEH-001 wheelbarrow physics design](VEH-001-wheelbarrow-physics-design.md)

## Global Constraints

- Keep the moving colliders primitive to match the performance budget.
- Do not add a package or a general vehicle framework.
- Press-E hold/release, target detection, and player/vehicle control switching belong to the later wheelbarrow interaction work after INT-001 and INT-002.
- All Unity serialized assets must be created or changed through the Unity Editor.
- Do not add networking code, packages, or ProjectSettings changes.

## Review Focus

- Static colliders still block the player and never receive a push; preserve `CharacterControllerStopsAtWall` and add an assertion for the static target.
- Kinematic Rigidbody targets are ignored; add `ContactDoesNotMoveKinematicRigidbody`.
- Contact does not add vertical player velocity to a target's horizontal motion; add `ContactPushesHorizontally`.
- Repeated contact does not produce runaway target velocity; assert a bounded velocity in `ContactMovesDynamicRigidbody` after a fixed number of frames.
- Cargo remains independent and falls after tipping; verify with the three-cube Unity Play Mode test in Task 2.

---

### Task 1: Push dynamic Rigidbody targets from player contact

**Files:**
- Modify: `Assets/Game/Scripts/Player/PlayerMovement.cs`
- Test: `Assets/Game/Tests/Player/PlayerMovementTests.cs`

**Interfaces:**
- Consumes: Existing `CharacterController` movement and collider contacts.
- Produces: A generic physical push on a contacted non-kinematic Rigidbody; no wheelbarrow-specific reference or input mode.

- [ ] **Step 1: Add failing contact-push tests**

Add `ContactMovesDynamicRigidbody`, `ContactDoesNotMoveKinematicRigidbody`, and `ContactPushesHorizontally` to `PlayerMovementTests`. For the dynamic case, put a 1 kg cube at `TestOrigin + new Vector3(0f, 0.5f, 1.2f)`, hold W for 30 frames, then assert its z position increased by more than 0.1 m and its speed is below 3 m/s. For the kinematic case, use the same target position and assert its position is unchanged after 30 frames. For the horizontal case, assert the grounded target's vertical velocity remains within 0.1 m/s of zero. Also assert the existing static wall remains unmoved in `CharacterControllerStopsAtWall`.

- [ ] **Step 2: Run the focused tests and confirm the new assertions fail**

Run the tests in Unity Test Runner, EditMode, filtered to `Ngecor.Player.Tests.PlayerMovementTests`. Expected: the new contact-push assertions fail because contact currently does not transfer motion; existing player movement tests remain green.

- [ ] **Step 3: Implement generic contact pushing in `PlayerMovement`**

Add `[SerializeField, Min(0f)] private float _pushStrength = 1f` and `OnControllerColliderHit(ControllerColliderHit hit)`. Ignore hits without a dynamic Rigidbody. Project `_characterController.velocity` onto the horizontal plane and call `AddForceAtPosition(horizontalVelocity * _pushStrength * Time.deltaTime, hit.point, ForceMode.Impulse)` so the target can translate or tip while impulse scales with frame time. Do not add a manager, interface, or wheelbarrow-specific branch.

- [ ] **Step 4: Run the full Player movement test fixture**

Run `Ngecor.Player.Tests.PlayerMovementTests` in Unity Test Runner. Expected: all existing movement/look/wall/ramp tests and the new push tests pass; static and kinematic targets remain unmoved.

- [ ] **Step 5: Commit the player contact behavior**

```bash
git add Assets/Game/Scripts/Player/PlayerMovement.cs Assets/Game/Tests/Player/PlayerMovementTests.cs
git commit -m "feat: push rigidbodies on player contact"
```

### Task 2: Build and playtest the wheelbarrow prototype

**Files:**
- Create: `Assets/Game/Prefabs/Vehicle/Wheelbarrow.prefab` and its `.meta`
- Modify: `Assets/Game/Scenes/Playground.unity` through Unity Editor only

**Interfaces:**
- Consumes: The generic player-to-Rigidbody push from Task 1.
- Produces: A tunable wheelbarrow Rigidbody with primitive compound colliders and a tray that holds three independent test cubes.

- [ ] **Step 1: Create the graybox prefab in Unity Editor**

Create a root Rigidbody with compound primitive colliders approximating the tray, handles, supports, and fixed wheel contact shape. Tune Rigidbody mass and collider layout in the Inspector. Do not add a wheel joint or wheel-specific runtime script in this first pass.

- [ ] **Step 2: Set up only the VEH-001 test objects in Playground**

Use the existing ground and 20-degree ramp. Place the wheelbarrow on the flat area and make three separate Rigidbody cubes available beside the tray. Keep cargo unparented and unconstrained so it can fall naturally.

- [ ] **Step 3: Run the issue's Play Mode acceptance flow**

In Unity 6000.3.25f1, contact-push the empty wheelbarrow on flat ground; push it up and down the ramp; load three cubes and push normally; tip the tray until cargo falls; adjust mass and handling settings and confirm the behavior changes. Observe the Console and watch for runaway impulses, floor penetration, or persistent jitter.

- [ ] **Step 4: Review the serialized asset diff and commit the prototype**

Confirm the prefab and its `.meta` file are present, only the explicitly requested Playground setup changed, and no package or ProjectSettings files changed.

```bash
git add Assets/Game/Prefabs/Vehicle Assets/Game/Scenes/Playground.unity
git commit -m "feat: add wheelbarrow physics prototype"
```
