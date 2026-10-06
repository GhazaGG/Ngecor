# PLAYER-003 Contact Push Review Fix

## Task Summary

Address the new review feedback on PR #85 for contact pushing, CementBag friction, and regression coverage.

## Relevant Previous Context

The reviewed implementation capped contact push at 300 N. Review feedback asked to restore the CementBag friction values, apply the push at the Rigidbody center-of-mass height, and test a higher force cap only if the 25 kg bag still stuck or was stepped onto. Gameplay feel and warmed-up Profiler review remain separate manual gates.

## Changes Made

- Restored CementBag dynamic friction to 0.6 and static friction to 0.75.
- Clamped the contact push point's Y coordinate to `body.worldCenterOfMass.y` before calculating contact velocity and applying the push.
- Set the Player prefab push cap to 350 N and reduced `PushResponseTime` from 0.2 s to 0.1 s. At 0.2 s the 25 kg bag's calculated response stayed below the configured cap, so increasing the cap alone had no effect.
- Expanded the CementBag PlayMode test to record all 15 orientation trials before reporting failures. Updated the procedural test player to use the cap configured on the Player prefab and added a no-torque assertion for an elevated contact point.

## Files Affected

- `Assets/Game/Materials/Material/CementBag.physicMaterial`
- `Assets/Game/Prefabs/Player/Player.prefab`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`

## Technical Decisions

- 350 N was the lowest tested cap that passed all 15 CementBag orientations. The 300 N cap failed the flat-bag trials; 350 N passed flat, side, and side-corner trials.
- Because the accepted value exceeds 300 N, the project owner must update DECISIONS #74 if this value is approved. That decision document was not changed in this task.
- No scene or wheelbarrow asset was modified.

## Verification Performed

- Unity 6000.3.25f1 PlayMode suite: 74 passed, 0 failed.
- CementBag regression: all 15 orientation trials passed at 350 N; the slow 1 kg body push regression passed.
- Inspected the empty Playground wheelbarrow in the Editor: its Rigidbody is dynamic and has mass 4 kg. A reliable manual push observation was not obtained because keyboard movement could not be verified in the visible GameView.
- `git diff --check` passed before commit.

## Final Result

The code and regression coverage address the review's friction, contact-height, and bag-push findings. PR #85 still needs independent gameplay acceptance and the warmed-up movement/push Profiler capture before approval.

## Known Limitations

- The empty wheelbarrow was identified but its response to player contact was not verified in Play Mode.
- No warmed-up Profiler capture or independent gameplay review was completed.
- DECISIONS #74 remains unchanged pending the project owner's decision about the 350 N cap.

## Unresolved Issues / Follow-up Work

- Ask the project owner to update DECISIONS #74 if 350 N is accepted.
- Obtain the empty-wheelbarrow gameplay observation, human gameplay review, and warmed-up movement/push Profiler evidence before approval.
