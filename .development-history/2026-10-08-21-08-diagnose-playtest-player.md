# Diagnose Player controls during the MIX-001 playtest

## Task summary
Investigated why the developer could not move or see the interaction crosshair while playtesting MIX-001, and verified whether the task branch contains the latest fetched `main` commit.

## Relevant previous context
- MIX-001 (#17) adds the concrete mixer prototype. Earlier work prepared temporary sand and cement buckets for a Play Mode trial.
- The task branch is `DePo4l/mix-001-concrete-mixer-prototype`; it has existing uncommitted mixer changes and a dirty in-progress scene.

## Changes made
- Fetched `origin/main` and compared the fetched commit with the current branch and local `main`.
- Started Unity Play Mode and confirmed that both test buckets remain in the loaded scene.
- Inspected the live Player component values and related player/input code. No gameplay source or serialized Unity asset was changed.
- A Unity runtime-eval attempt timed out before changing the Player.

## Files affected
- `.development-history/2026-10-08-21-08-diagnose-playtest-player.md`

## Technical decisions
- The loaded Player has `_isLocalPlayer = false`. `PlayerMovement.Update` exits for non-local players, `LocalCamera` returns null, and `InteractionDetector.OnGUI` skips the crosshair when that camera is null.
- The player action references are assigned, but the loaded scene has no `PlayerInput` component. The player scripts do not enable Move/Look actions; the movement tests enable their test action map explicitly. Unity Input System actions are disabled by default until an action or its map is enabled.
- Kept the fix diagnostic and did not change the dirty scene or shared Player prefab because issue #17 is scoped to the mixer and the current scene contains developer work.

## Verification performed
- `git fetch origin main` completed. `HEAD`, `main`, `origin/main`, and `FETCH_HEAD` all resolve to `72b2ff0d79b2cfef328d216e7852356e9d20b398` (`[MAT-004] Shovel scoop/dump and recoverable ground piles (#91)`).
- Unity Editor 6000.3.25f1 reports Play Mode active and no compilation or domain reload in progress.
- Live component inspection confirmed `_isLocalPlayer = false` and valid Move/Look/Interact action references.
- The loaded scene hierarchy contains `TestBucket_Sand` and `TestBucket_Cement`.
- No automated tests or manual movement/grab input were run.

## Final result
The branch is current with the fetched `main`. The confirmed cause of the missing crosshair and inactive movement is that the scene Player is not assigned as local. Input action activation is also missing from the inspected scene setup and player scripts, so enabling local ownership alone may not restore controls. The Player was not changed; Play Mode remains active with the buckets present.

## Known limitations
- Runtime activation did not complete because the Unity Pipeline C# eval timed out; the player still needs its local-control and action-map setup.
- The Unity Console may show the Pipeline timeout alongside earlier bridge errors. No script compilation error was reported.

## Unresolved issues or follow-up work
- Add or configure the single-player/dev-scene Player initialization so it assigns local ownership and enables the Player action map before testing movement and interaction.
