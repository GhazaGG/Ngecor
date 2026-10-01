# Team Lead review of PR #48 (PLAYER-001) and rescope of PLAYER-002

## Task summary
Team Lead session. Reviewed RyoFPS's PR #48 `[PLAYER-001] Add basic player movement` for approach, logic, code, tests, and performance. Posted a "Request changes" review, posted a structured AI handoff prompt as a PR comment, and rescoped issue #8 PLAYER-002 because PR #48 already includes mouse look.

## Relevant previous context
- `2026-10-02-01-17-milestones-priorities-new-tickets-assignments.md`: RyoFPS owns #7 and #8. Before he started, he was told only that the game is first-person.
- `docs/DECISIONS.md`: client-owned player movement, host-owned physics; Input System; first-person; NGO code only in `NET-*` tickets (AGENTS.md rule 11).
- Issue #7's AC says "Input System package dengan keyboard + mouse", which overlaps #8's look AC.

## Changes made
- **PR #48 review (CHANGES_REQUESTED):**
  - Must Fix:
    - Do a human Play Mode test in Playground and attach a clip.
    - Delete `docs/superpowers/`, which isn't covered by `docs/PROJECT_STRUCTURE.md`.
    - Make the test helper `SetPrivateField` assert that the field exists, and drop the reflection type lookup.
  - Should Fix:
    - Remove the Enable/Disable gating, which has no effect because `InputSystem_Actions` is the project-wide actions asset and Unity enables it at startup.
    - Use `Time.captureFramerate` in every distance-comparison test.
    - List the non-local Camera/AudioListener as a known issue handed off to NET-001.
  - Suggestions: ground-snap vs. slope limit 50°, and the gamepad right-stick Look binding without `deltaTime`.
- **PR #48 comment:** a structured AI handoff prompt for RyoFPS's tool (role, task, files to read, applicable decisions, limits, done criteria, when to stop, output).
- **Issue #8:** the body was rewritten. Basic look is now accepted in #7. #8 covers:
  - cursor lock and unlock
  - camera height
  - sensitivity tuning
  - jitter
  - a camera reference for INT-001 that doesn't rely on `Camera.main`

  A comment on #8 explains the rescope.

## Files affected
- This report. Everything else was a GitHub PR review, PR comment, or issue edit.

## Technical decisions
- `CharacterController` is accepted as the player body. A client-driven Rigidbody player would conflict with host-authoritative physics.
- Mouse look stays in PR #48 instead of being split out. The overlap came from the #7 ticket wording, not from the developer.
- The non-local Camera/AudioListener isn't fixed in #48. It belongs to NET-001, together with the ownership replacement.

## Verification performed
- Read the PR diff on `origin/feat/player-movement`:
  - `PlayerMovement.cs`, the tests, both asmdefs
  - prefab component list, scene diff
  - `DECISIONS.md` and `PROJECT_STRUCTURE.md` changes
- Confirmed the project-wide actions setting in `ProjectSettings/EditorBuildSettings.asset` and the Look bindings in `InputSystem_Actions.inputactions`.
- Computed the Playground ramp angle (20°) from its quaternion and checked it against the -2 m/s ground snap.
- Confirmed on GitHub that the review state is CHANGES_REQUESTED and that the comment and the #8 edit are visible.
- Unity wasn't run in this session. The review conclusions come from reading the code and asset files, not from Play Mode.

## Final result
PR #48 is blocked on Must Fix items 1–3. Its approach is approved. Issue #8 now has a non-overlapping scope.

## Known limitations
- The point that the Enable/Disable gating does nothing relies on Unity 6 enabling project-wide actions automatically. Nobody has observed this in this project's Play Mode yet.

## Unresolved issues or follow-up work
- Design question for interaction/physics: should walking players push physics props? `CharacterController` doesn't push Rigidbodies. If they should, the push has to happen on the host. No ticket exists yet.
- The owner still has to decide whether automated Play Mode tests are a team norm or stay optional. PR #48 adds the `Assets/Game/Tests/<System>/` convention.
- NET-001 must make Camera/AudioListener follow local ownership.
