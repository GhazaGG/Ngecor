# MAT-001 Dev_Ghaza Player Collision Test Setup

## Task summary
The user wanted to ram the cement bag stack with the Player for the manual issue #15 checks. They chose Dev_Ghaza over Playground: open PRs #67 and #69 both modify Playground, so changing it would risk scene merge conflicts.

## Changes made (via a temporary batchmode editor script, which deleted itself)
- `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`:
  - Re-laid the 5 stacked CementBag instances at (2, y, 2): rotation set to identity (removes the old ~4 degree tilt override), y = 0.09, 0.28, 0.47, 0.66, 0.85 (thickness 0.18 + 1 cm gaps).
  - Added a `Player.prefab` instance at (2, 0, 6), yaw 180 (facing the stack), `_isLocalPlayer` = true (same override as the Playground instance).
  - Disabled `Main Camera` so the Player camera renders.
- The ramp bag `CementBag (5)` was not touched.

## Verification performed
- Batchmode exit 0 with the log line "Dev_Ghaza ready"; the temporary script was removed.
- Read-only check of the scene YAML: the bag positions/rotations, the `_isLocalPlayer: 1` override, and `Main Camera` `m_IsActive: 0` are as intended.
- Not run in Play Mode: whether the Player moves, looks around, and pushes the stack is for the user to verify.

## Follow-up
- The user runs the manual player-push and ramp checks and records the results in the PR.
