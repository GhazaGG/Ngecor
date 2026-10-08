# Prepare a Static Scaffolding Ladder Plan

## Task summary

Reviewed GitHub issue #18 and its comments after the request to make the custom scaffolding ladder follow the repository instructions. Prepared a static-step plan for lead review and removed the climb-only changes from the previous local attempt. No stair geometry was implemented because the GitHub prompt requires lead review of the plan before coding.

## Relevant previous context

The working tree contains the developer's custom `Scaffolding_` prefab in `Dev_DePo4l.unity`. A local `LadderClimbable` script and `PlayerMovement` climb changes had been added immediately before this review. The current issue body was updated on 2026-10-03. GhazaGG's issue comment specifies static access with the Player's 0.45 m step offset and 50-degree slope limit, with no jump or climb behavior, and requires the implementation plan to be reviewed before coding. A later comment reassigned issue #18 to GhazaGG and cautions against duplicate work.

## Changes made

- Removed the `LadderClimbable` component from both ladder roots in the custom scaffold using the Unity Editor.
- Disabled the two root BoxColliders whose bounds had been repurposed as climb trigger zones; their child rail and rung colliders remain present.
- Applied the prefab overrides and saved `Dev_DePo4l.unity` through the Unity Editor.
- Restored `PlayerMovement.cs` to the branch version and deleted the climb-only script and its metadata through the Unity Editor.
- Removed the temporary scene screenshot from `Assets/Temp` through the Unity Editor.
- Left unrelated existing changes in ProjectSettings and `verify_physics.log` untouched.

## Files affected

- `Assets/Game/Prefabs/Construction/Scaffolding_.prefab`
- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity`
- Removed `Assets/Game/Scripts/Player/LadderClimbable.cs` and its `.meta` file.
- Removed the temporary `Assets/Temp` screenshot and folder metadata.

## Technical decisions

- Proposed editing only the custom scaffold prefab after review: keep the left and right vertical rails, replace the four directly stacked rungs with shallow primitive steps, each rising about 0.30 m (below the 0.45 m CharacterController step offset) and extending outward enough to form a walkable flight. Measure the actual platform height before choosing the final tread count and make the top tread meet the deck.
- Add only primitive BoxColliders to the static steps. Add no climb script, input action, Rigidbody, joint, or PlayerMovement change. Keep the scaffold's existing mass and friction settings unchanged; no new body means no new mass value to tune.
- The custom prefab currently contains multiple Rigidbodies and FixedJoints, while the GitHub prompt calls for one Rigidbody and no joints. This plan does not silently restructure that prefab; lead review must decide whether the existing prefab is the intended #18 target and how that mismatch is handled.

## Verification performed

- Unity 6000.3.25f1 Editor reported Play Mode stopped, recompile completed, and zero compiler errors or warnings after removing the climb script.
- Confirmed the climb component and script are absent from the scene/project.
- No Play Mode ladder traversal was performed; static steps do not exist yet.

## Final result

The climb-only behavior has been removed. A static ladder plan is ready for lead review; implementation is pending that review as instructed in GhazaGG's issue comment.

## Known limitations

- The ladder remains the developer-authored vertical-rail/four-rung geometry and is not yet walkable from the ground using normal movement.
- The root ladder colliders remain disabled because their saved dimensions came from the temporary climb-zone calculation; the child primitive colliders remain enabled.
- The existing prefab's multiple Rigidbody/FixedJoint structure has not been reconciled with the issue comment's one-Rigidbody/no-joint decision.

## Unresolved issues or follow-up work

- Obtain lead review of the static-step plan and confirm this custom prefab is the intended issue #18 target.
- After approval, implement the steps through Unity Editor and manually verify ascent, walking across the upper platform, and descent in Play Mode.
- Resolve the scaffold Rigidbody/FixedJoint scope mismatch with the lead before claiming issue #18 is complete.
