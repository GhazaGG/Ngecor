# MAT-004 Shovel and Recoverable Ground Piles

## Task summary

Implement the approved issue #54 plan on `feat/mat-004-shovel-piles`, based on `main` commit `4fdadde`: a locally carried shovel, discrete scoop/dump requests, recoverable ground deposits, bucket ground pouring, and conservative passive spills.

## Relevant previous context

- `2026-10-06-20-15-prepare-mat004-shovel-task.md` established the branch, closed dependencies, existing container ownership, and the formerly undecided input binding. The approved implementation plan supplies the single-press R decision.
- `2026-10-06-18-57-sync-main-after-mat002-merge.md` records the preserved local scenes and stash.
- `2026-10-04-04-05-correct-sand-pile-volume-and-verify-prefab-physics.md` established the reusable mound mesh, static matching collider, and hollow bucket.
- `2026-10-05-21-23-fix-mat-002-pour-after-drop.md` established the immediate held-state check before a pending pour tick.
- `2026-10-06-21-17-commit-mat004-implementation.md` records a concurrent commit handoff. Its commits `e4838db`, `bbb48f1`, `8938207`, and `26df65c` were preserved and reviewed; they were not replaced.

## Changes made

- Exposed conserved integer `TransferUnitsTo`; added single-type configuration guarded by emptiness, material-type inspection, unlimited receiving capacity for runtime piles, and a shared rate-limited ground-transfer path.
- Added `ShovelAction` and `ShovelInput`. R requests scoop when empty and dump when loaded, once per press. Execution requires a locally held shovel. Forward targeting ignores self and occluded/behind targets, cancels exact nearest ties, and retains the nearest full/rejecting receiver.
- Added local `GroundMaterialDeposit` and `GroundMaterialPile`. Valid static surfaces receive material through the same container transfer; nearby same-type piles on the same support/plane merge. Failed deposits leave source units intact. Exhausted runtime piles are destroyed.
- Wired passive bucket/shovel spills to the same deposit path. Buckets can pour to ground while R is held; nearest/tie/drop rules and pour feedback remain supported.
- Created shovel and ground-pile prefabs, simple Cement/Concrete materials, and a MAT-004 trial group in `Dev_Ghaza` through Unity Editor APIs. The existing MAT-002 source/prefabs were reused.
- Added focused tests, updated former disappearance-based spill checks to retention without a sink, documented the controls, and added a gameplay-review checklist with local media.
- Log review found a non-convex `ClosestPoint` warning that quantity assertions did not reject. Replaced that query with actual mesh intersection and added warning/error capture around real-mound scoop assertions.
- Removed both temporary Editor verification/setup helpers and their metadata through `AssetDatabase.DeleteAsset`. No helper or fixture controller remains in shipped assets.

## Files affected

- `Assets/Game/Scripts/Material/`: `BulkMaterialContainer.cs`, `SandPileVisual.cs`, `BucketPourAction.cs`, `BucketPourVisual.cs`, and new `GroundMaterialDeposit.cs`, `GroundMaterialPile.cs`, `ShovelAction.cs`, `ShovelInput.cs` with Editor-generated metadata.
- `Assets/Game/Scripts/Editor/BulkMaterialDemo.cs`: retained-unit expectation for a source without a ground deposit adapter.
- `Assets/Game/Prefabs/Material/`: `Bucket.prefab`, new `GroundPile.prefab` and `Shovel.prefab` with metadata.
- `Assets/Game/Materials/Material/`: new `Cement.mat`, `Concrete.mat` with metadata.
- `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`: additive trial group and Editor serialization of the new default capacity field.
- `Assets/Game/Tests/Material/`: container, bucket, prefab regressions and new `ShovelPilePlayModeTests.cs` with metadata.
- `docs/DECISIONS.md`, `docs/MAT-004-Shovel-Pile-Test-Cases.md`, and the two linked PNG captures.
- This report. Raw logs, native input probes, and profiler data remain under ignored `Logs/`.

## Technical decisions

- `BulkMaterialContainer` remains the sole quantity authority; no global manager, inventory, new package, networking code, or player/interaction specialization was added.
- Defaults: shovel capacity 2 units, forward reach 1 m, merge radius 0.5 m, downward surface search 3 m. Existing spill/transfer rates remain Inspector tunables.
- Runtime piles use `int.MaxValue` receiving capacity and a 100-unit visual reference; growth follows cube-root volume rather than the capacity ratio.
- Merge requires the same static support collider, a plane-height difference within 0.05 m, and matching normals. No cross-floor merge is allowed.
- Original finite `SandPile` retains its tested empty/refill behavior: an empty mound/collider is disabled. The new runtime `GroundPile` is deleted when exhausted.
- Saturated physics query buffers cancel/retain units rather than choosing from incomplete results.
- The original mesh warning was corrected at shared target discovery, covering both scoop and dump callers.

## Verification performed

- Reviewed relevant history, project documents, live issue #54, current branch/stash, and the container, interaction, visual, prefab, and test flows. No `.codegraph/` directory was present.
- Unity 6000.3.25f1 asset build/reload verification completed successfully. All serialized asset creation/changes and helper deletion used Unity Editor APIs.
- Initial material suite: 48/48 passed. Additional real-mound and adapter checks increased the suite. Initial full runs passed 122/122 and 123/123 but log inspection exposed the mesh warning.
- Final graphics-enabled full Play Mode run, after helper cleanup and the mesh fix: **123 passed, 0 failed, 0 skipped**, Unity process exit **0**. Evidence: `Logs/Mat004FullResults.xml` and `Logs/Mat004FullTests.log`. The final log contains no `Physics.ClosestPoint` warning. The real-mound checks assert no warnings/errors during scoop.
- Coverage includes all three material types, partial capacity, rejection/ties/no fallback, local held state, clear paths, failed deposits, immediate merge, distinct floors/types, growth beyond 100 units, exhaustion, passive spill conservation, ground-pour release, actual mound/bucket transfers, adapter use requests, and MAT-002 receiver/drop regressions.
- OS-synthesized E/R in live Game View with temporary fixture placement confirmed E grab and one action during each 1-second R hold: source 80 to 78/shovel 0 to 2, then shovel 2 to 0/bucket 0 to 2. Sum remained 80. This is automated keyboard-path evidence, not a human feel review.
- A separate scripted fixture tilted 12 buckets with 20 units each and produced 12 piles containing all 240 units, with zero recorded gameplay errors. Observed warm Editor session averages were approximately 91–111 FPS on AMD Ryzen 5 7430U/Radeon, approximately 16 GB RAM, 1920x1080 Game View. CPU capture and raw `Logs/Mat004Stress.data` were recorded.
- Headless synthetic keyboard experiments failed to establish delivered input. Those experiments were not retained as tests or claimed as keyboard evidence; the shipped adapter check executes use requests.
- Reviewed source/diffs and confirmed no tracked package, shared project-settings, player prefab, interaction script, or main-scene change. Source/documentation diff checks pass; Unity-generated asset/meta blank fields retain serialization trailing spaces.
- Preserved the original stash and four untracked MAT-002 scene files. The automatically generated, untracked `ProjectSettings/SceneTemplateSettings.json` is excluded from delivery.

## Final result

MAT-004 implementation and automated verification are complete and reviewable on the feature branch. Delivery is a draft PR with `Closes #54`; technical/gameplay review and approval are required before merge. This report does not claim the workflow Definition of Done or team gameplay approval.

## Known limitations

- Human keyboard playthrough, carry/drop/throw feel, and the complete reviewer checklist are not verified. Shared focus/pose changes prevented completion of the remaining live keyboard cases.
- GPU timing, standalone build performance, the 8 GB reference-device budget, and four-player performance are not established. Editor FPS is a local observation, not acceptance proof for those targets.
- Profiling includes Editor/helper/Profiler overhead; recorded total Editor memory is not gameplay memory usage.
- Adjacent coplanar floor colliders are treated as separate supports for merging. Mixed positive material types are not arbitrarily selected for a scoop.
- The original source pile has the preserved MAT-002 empty/refill behavior described above.
- Freshness, mixing, terrain digging, final VFX/art, mass coupling, and multiplayer integration remain outside this task.
- The optional no-mistakes gate is not initialized in this repository; no new gate configuration was added outside the approved project structure. Validation used Unity Test Runner and direct diff review.

## Unresolved issues or follow-up work

- A team gameplay reviewer must execute `docs/MAT-004-Shovel-Pile-Test-Cases.md`, record feel/Console evidence, and assess the initial downward view and tool alignment before the PR is marked ready.
- Obtain GPU and reference-device/four-player performance evidence as required by the workflow.
- Resolve remaining review findings through the same branch; do not merge without team approval.
- Decide separately whether the untracked SceneTemplateSettings file should be retained; it was neither staged nor removed.
