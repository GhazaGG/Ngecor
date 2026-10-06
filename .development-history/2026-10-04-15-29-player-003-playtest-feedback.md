# PLAYER-003 Playtest Feedback Follow-up

## Task summary

Adjusted the push target speed after the developer's manual feedback that four CementBags still felt too light. Diagnosed why a single bag could be stepped over during the push attempt.

## Relevant previous context

- PLAYER-003 implementation is on `feat/player-003-human-strength` with a 300 N maximum push force and a per-frame shared impulse budget.
- The developer manually observed that one CementBag was stepped on and four duplicated bags felt like a box with contents, but not very heavy.
- The CementBag root scale makes its unit BoxCollider 0.18 m high. The Player CharacterController step offset is 0.45 m, so it can step onto the bag.
- `PlayerMovement` previously targeted the player's full 5 m/s movement speed for every Rigidbody mass. A continuously pushed heavy object could therefore approach the player's full speed.

## Changes made

- Added a pure target-speed calculation that scales down push speed by the square root of mass relative to 5 kg; a 25 kg body now targets about 2.24 m/s instead of 5 m/s.
- Applied the mass-based target speed to contact pushes while keeping the existing 300 N force cap.
- Updated the 25 kg PlayMode assertion and added pure-calculation coverage for 1 kg, 5 kg, and 25 kg target speeds.
- Added a non-allocating capsule sweep before movement. When a low, dynamic Rigidbody is in the player's path, step offset is temporarily set to zero for that movement, so the player pushes it instead of stepping over it. Normal step-up behavior remains available for static steps.
- Added a PlayMode case for pushing a low 25 kg body without stepping onto it.

## Files affected

- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- This development-history report.

## Technical decisions

- The 5 kg reference mass matches PLAYER-003's light-prop acceptance threshold. Heavier bodies receive progressively lower target speeds, while no body is targeted above player walking speed.
- The capsule sweep only suppresses stepping for low dynamic bodies ahead. It does not globally change CharacterController traversal settings.

## Verification performed

- `git diff --check` passed after the edits.
- Developer-reported Play Mode feedback after the follow-up: one 25 kg sack resists contact and needs a gradual push; a five-sack stack feels wall-like at first and falls after sustained pushing; occasional player contact on top of a sack feels physically reasonable to the developer.
- The developer provided manual Play Mode feedback for the 25 kg sack and five-sack pile after these changes. No Console result was reported.
- Automated Unity PlayMode tests were not rerun because Unity Editor processes were active; the new assertions remain unverified.
- The earlier 65/65 PlayMode result predates this follow-up and does not verify the new mass-based target speed.

## Final result

The push calculation now accounts for mass both in force response and in its target speed. Low dynamic objects resist the player's approach without changing static step-up. The developer reports that the sack and stack feel match the intended gameplay, including occasional stepping onto a sack.

## Known limitations

- The post-change PlayMode test suite has not been rerun.

## Unresolved issues or follow-up work

- Verify release/deceleration after the player stops pushing.
- Complete the 1 kg prop and wheelbarrow ramp/cargo checks required by issue #73 when their test setup is available.
- Complete the wheelbarrow checks required by issue #73 when its ramp and cargo setup is available.
