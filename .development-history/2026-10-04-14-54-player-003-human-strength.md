# PLAYER-003 Human-Strength Push

## Task summary

Implemented a bounded, mass-aware push response for the local player under issue #73. The player now applies a capped total push impulse per rendered movement step and reduces force as a body approaches the player's walking speed.

## Relevant previous context

- Existing `PlayerMovement` used a fixed `_pushStrength` and could add force repeatedly when a controller contacted multiple colliders.
- Existing `CementBag` is a 25 kg dynamic body with a low profile and nonzero friction.
- The player prefab's `CharacterController.stepOffset` is 0.45 m; it remains unchanged.
- Work was performed on `feat/player-003-human-strength` in the existing checkout. No worktree, commit, push, or PR was created.

## Changes made

- Replaced `_pushStrength` with the Inspector-tunable `_maxPushForce`, serialized as 300 N on the Player prefab.
- Added a mass-aware exponential response toward the player's 5 m/s walking speed, capped at `_maxPushForce`.
- Added one shared impulse budget per `CharacterController.Move` call so repeat collider callbacks cannot multiply the maximum force.
- Preserved the kinematic-body and downward-contact exclusions.
- Added light- and heavy-body movement tests and pure force-calculation coverage. The integration tests assert that 1 kg and 25 kg bodies move without exceeding walking speed.
- Created `Assets/Game/Scenes/Dev/Dev_RyoFPS.unity` through Unity Editor with a ground plane, a Player instance configured as local, and a 25 kg CementBag at `(0, 0.09, 1.5)` for focused manual testing.

## Technical decisions

- Set the prefab cap to 300 N, within issue #73's 250–300 N range. The 275 N trial moved the heavy test body too little in the diagnostic setup; 300 N produced measurable movement while the automated speed assertion remained within the 5 m/s target.
- Kept the existing step offset and the change scoped to push behavior; changing traversal over low objects would alter general player movement and needs a separate, reproducible gameplay check.

## Verification performed

- Focused `PlayerMovementTests`: 20/20 passed.
- Full Unity PlayMode suite: 65/65 passed (`C:\Temp\player003-all.xml`; Unity 6000.3.25f1).
- `git diff --check`: passed.
- Opened `Dev_RyoFPS` in Unity and attempted a manual Play Mode check. After configuring the scene Player as local, runtime showed the Player at approximately Z=1.776 and the CementBag at X=-6.670, Z=1.500. This was not a controlled push observation; the cause is unresolved, so it is not counted as gameplay acceptance evidence.

## Final result

The code and automated PlayMode coverage are implemented and passing. The dedicated developer scene is saved for follow-up validation.

## Known limitations

- Manual acceptance for an actual 25 kg CementBag push, release/deceleration, collider-multiplication behavior in the live scene, and the empty/three-bag wheelbarrow ramp scenarios remains unverified.
- The runtime transform result in `Dev_RyoFPS` needs investigation before treating the manual scene as valid evidence.
- The generated `ProjectSettings/SceneTemplateSettings.json` appeared during Unity Editor scene creation and was left intact.

## Unresolved issues or follow-up work

- Reproduce and explain the `Dev_RyoFPS` runtime transform change with a focused Unity Game View session.
- Complete the issue's manual wheelbarrow checks in Play Mode and record the observed feel before marking the issue gameplay-verified.
- Existing local changes to `ProjectSettings/VersionControlSettings.asset` and the untracked VEH-002 readiness report were preserved and not included in this task's changes.
