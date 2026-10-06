# MAT-002 Pile Volume and Prefab Physics Verification

## Task summary

Continued the requested sand-pile and physics repair, and clarified whether MAT-002 includes cement textures. Work remains on `feat/sand-bucket`.

## Relevant previous context

- `2026-10-04-03-38-fix-sand-pile-shape-and-colliders.md` records the mound mesh, matching static collider, and hollow compound bucket colliders. Its test attempt was inconclusive.
- `2026-10-04-00-50-mat-002-sand-bucket-finalization.md` records the pour adapter and the original six MAT-002 tests.
- Current issue #16 covers a finite sand pile, carried bucket, explicit conserved pouring, receiver rejection, and tilt loss using MAT-005. Final art and granular simulation are excluded.
- Current issue #62 explicitly permits spilled units to disappear until MAT-004 (#54) implements ground piles. This is an accepted interim behavior, not a newly discovered regression.
- Cement-bag issue #15 also excludes final art. Cement textures require separate art scope.

## Changes made

- Added `SandPileVisual`, which reads the existing container quantity and scales the mound in all three axes by the cube root of its fill ratio. The slope remains consistent and the base stays fixed as volume decreases.
- Deactivated the mound child, including its collider, when the pile is empty. Refilling restores it without a zero-scale collider.
- Wired the pile prefab through Unity Editor APIs and removed its generic vertical-only fill reference. MAT-005 quantity and transfer code was not changed.
- Added four Play Mode checks against the actual pile and bucket prefabs.
- Removed the temporary prefab configuration script and its metadata through `AssetDatabase.DeleteAsset`.

## Files affected

- `Assets/Game/Scripts/Material/SandPileVisual.cs` and its Editor-generated `.meta`
- `Assets/Game/Prefabs/Material/SandPile.prefab`
- `Assets/Game/Tests/Material/SandPileAndBucketPrefabPlayModeTests.cs` and its Editor-generated `.meta`
- This report

## Technical decisions

- A dedicated pile visual adapter preserves mound shape without changing the shared container or introducing another quantity system.
- The existing static mesh collider follows the same transform as the visual. The bucket retains the five primitive colliders from the previous repair.
- Kept this repair within MAT-002 while the optional question about mass coupling and recoverable spills remains unanswered. No per-unit kilogram mapping was selected.
- No packages, shared project settings, networking, or scene assets were changed in this continuation.

## Verification performed

- Identified the previous test command error using the installed Unity Test Framework source: specifying `-quit` prevents command-line tests from running. Removed that flag for test runs; retained it for synchronous prefab configuration methods.
- Initial material Play Mode run: Unity process exit code 0 and XML result **18/18 passed** (`Logs/Mat002PlayModeResults.xml`).
- Unity Editor prefab configuration and cleanup both exited with code 0; the saved prefab passed reload assertions.
- Final Unity 6000.3.25f1 material Play Mode run: process exit code 0 and XML result **22/22 passed**, zero failed, skipped, or inconclusive (`Logs/Mat002PrefabPlayModeResults.xml`).
- New prefab checks verified proportional volume reduction with a fixed base, mesh collider bounds and an upward surface hit, empty/refilled collision state, a small falling Rigidbody entering the bucket cavity and settling on its bottom, and upright retention followed by tipped loss.
- The existing quantity/transfer and six pour-adapter tests also passed in that final run.
- Confirmed the temporary Editor helper and Test Runner initialization scene were removed. Tracked diff statistics remained unchanged from the pre-task state.
- Full `git diff --check` reports eight inherited trailing-whitespace lines in the existing serialized `Dev_Ghaza.unity` diff. They were preserved under the Editor-only asset rule. This supersedes any implication that the entire dirty checkout currently passes that check. The new C# files have no trailing whitespace.

## Final result

The pile now shrinks as a mound instead of flattening, and an empty pile has no active collision. Existing bucket collision and tilt behavior have runtime evidence from tests using the actual prefabs. Changes remain local and uncommitted.

## Known limitations

- Automated checks do not establish player-perceived carry/drop feel or an interactive `R` input path. These still need manual gameplay review.
- Bucket contents still do not affect Rigidbody mass. Bulk material units have no agreed kilogram mapping.
- Spills still disappear under the accepted MAT-005 interim rule; ground pile spawning and merging belong to MAT-004.
- The new prefab tests use Editor asset loading and run as Editor Play Mode tests, not standalone player tests.

## Unresolved issues or follow-up work

- Complete manual carry/pour/drop review in `Dev_Ghaza` before declaring all gameplay acceptance criteria complete.
- Decide whether to extend scope for mass coupling or bring forward MAT-004 spill recovery.
- Schedule cement texture/final-art work separately if requested.
