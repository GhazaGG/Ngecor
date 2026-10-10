# PR #92 Main Merge Conflict Resolution

## Task summary

Merge the updated `main` branch into the VEH-002 PR branch and resolve the conflicts reported by GitHub.

## Relevant previous context

- PR #92 contains the wheelbarrow interaction changes and follow-up for review findings.
- `origin/main` advanced with BUILD-001 scaffolding work and INT-005 carried-object collision and mass handling.
- The PR branch had local, uncommitted changes to `Playground.unity` and `ProjectSettings/VersionControlSettings.asset`; these were preserved and excluded from the merge resolution.

## Changes made

- Merged `origin/main` into `RyoFPS/veh-002-wheelbarrow-interaction`.
- Resolved the `PlayerGrab.cs` conflict by retaining both wheelbarrow hold lifecycle/feedback and main's carried-mass, pinch-drop, and separation-collision cleanup.
- Resolved the `PlayerMovement.cs` conflict by applying carried-mass speed scaling to the interaction-controlled movement direction and step probe.
- Kept the remaining incoming main changes intact, including the scaffolding implementation and its project documentation.

## Files affected

- `Assets/Game/Scripts/Interaction/PlayerGrab.cs`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- The merge also brings the files from `origin/main` commits `1b54847` and `622e49e`.
- `.development-history/2026-10-08-18-39-pr-92-main-merge-conflict.md`

## Technical decisions

- Preserve the main branch carried-object speed and collision behavior while keeping wheelbarrow movement disabled for the player during active pushing.
- Do not stage the local Playground scene, Version Control settings, or untracked local history/configuration files.

## Verification performed

- `git diff --cached --check` passed, and no conflict markers or unmerged paths remain.
- Unity 6000.3.25f1 Play Mode batch suite: 131 test cases, 130 passed, 0 failed, 1 skipped. The skipped screenshot regression requires an active graphics device.
- `WheelbarrowInteraction_StaysHeldAtNormalSeparationAndSecondPressReleases` passed.
- `CementBagSlidesSlowlyWithoutBeingSteppedOver` passed in this run; the previously reported `statePtr` exception did not reproduce in the log.
- No manual gameplay playtest was performed after the merge.

## Final result

The merge conflicts are resolved, and the merged Play Mode suite reported no failing tests. The branch is ready for the merge commit and PR update.

## Known limitations

- Unity batch mode does not verify gameplay feel, ramp handling, or wheel contact. Earlier manual results were reported by the user before this merge.
- The `statePtr` exception may still be intermittent because it appeared in the user's earlier run but did not reproduce in this batch run.

## Unresolved issues or follow-up work

- Recheck PR #92 mergeability after pushing the merge commit.
- Repeat the manual wheelbarrow acceptance in Playground if the merged code changes gameplay feel.
