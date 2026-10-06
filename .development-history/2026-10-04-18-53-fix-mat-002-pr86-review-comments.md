# Fix MAT-002 PR #86 Review Comments

## Task summary

Implemented the approved plan for RyoFPS's two PR #86 comments: create the screenshot output directory and choose the nearest receiver when triggers overlap. Published the fixes to `feat/sand-bucket` and updated the PR verification details.

## Relevant previous context

- Reviewed `2026-10-04-18-09-inspect-ryo-mat-002-review-comments.md`, `2026-10-04-15-37-implement-simple-bucket-pour-animation.md`, `2026-10-04-16-44-publish-mat-002-pull-request.md`, and `2026-10-04-14-24-create-mat-002-manual-test-cases.md`.
- Read current project rules, workflow, structure, design, decisions, issue #16, and the live PR body and reviews. Confirmed both findings against the current source and traced the input, trigger, transfer, and feedback paths.
- The existing graphics-enabled animation suite had 25 tests; it did not cover either reported issue. MAT-005 remains the quantity and transfer authority.
- Started on `feat/sand-bucket` at `9c1171e`, matching the remote branch, with a clean tracked tree. Preserved all pre-existing unrelated untracked files. No `.codegraph/` directory exists.

## Changes made

- Replaced first-entry receiver selection with a minimum squared-distance scan between bucket and receiver container root positions. Every `RequestPour` recalculates the target.
- Exact ties at the minimum distance return null and use existing cancellation to stop transfer and clear feedback. Ties at greater distances do not prevent a uniquely nearest target.
- Selection ignores null, destroyed, and source-container references. It does not filter capacity or material acceptance, so a full or rejecting closest receiver never triggers fallback.
- The screenshot helper derives project-root `Logs` from `Application.dataPath` using `Path.Combine`, creates it before allocating render objects, and accepts a private optional output-directory parameter. Existing screenshot names and null-graphics capture skipping are retained.
- Added six receiver regression tests and one missing-directory/repeated-capture regression test to the existing Material suite.
- Added manual case TC-MAT002-15 with overlap setup, expected target changes, exact-tie cancellation, no fallback, stop conditions, and evidence fields. Its status remains NOT VERIFIED.
- Committed the four changed source/test/checklist files as `de555af1acd3ce872bb34c8ba194186b095eb982` (`fix: select nearest bucket receiver and create screenshot directory`) and pushed normally to `origin/feat/sand-bucket`.
- Updated PR #86 using the existing template with current test evidence and pending manual/reviewer checks. Read back the body and confirmed it matches the prepared update.

## Files affected

- `Assets/Game/Scripts/Material/BucketPourAction.cs`
- `Assets/Game/Tests/Material/BucketPourPlayModeTests.cs`
- `Assets/Game/Tests/Material/SandPileAndBucketPrefabPlayModeTests.cs`
- `docs/MAT-002-Sand-Pile-Bucket-Test-Cases.md`
- This report
- Ignored local evidence: `Logs/Mat002ReviewFixResults.xml`, `Logs/Mat002ReviewFixTests.log`, `Logs/Mat002ReviewFixFinalResults.xml`, `Logs/Mat002ReviewFixFinalTests.log`, `Logs/Mat002ReviewFixFinalExitCode.txt`, regenerated `Logs/BucketPourAnimation-Pouring.png` and `Logs/BucketPourAnimation-Stopped.png`, and the temporary PR body file.

## Technical decisions

- Used the user's approved nearest-root rule, with exact float equality rather than an approximate-distance tolerance. Very close unequal squared distances still select a target.
- Kept the existing request/cancel/feedback reset and MAT-005 transfer-credit behavior. No public API, Inspector fields, prefab, scene, shared container, settings, package, or networking changes were needed.
- Regression tests reuse the current fixture and include real overlapping trigger registration for the movement case. Other cases explicitly control registration order.
- The screenshot regression uses a GUID-named child directory under project `Logs`, verifies two PNG outputs, and deletes only that unique directory in `finally`. It never deletes shared Logs or existing evidence.
- Captured the final Windows GUI process exit status through a retained process handle and `WaitForExit`, rather than relying on an empty synchronous `$LASTEXITCODE` from direct Unity invocation.

## Verification performed

- First graphics-enabled Material run passed 32/32; the attached Unity process exited 0. Then strengthened coverage to retain an actually destroyed container reference and distinguish nearly equal distances from exact ties.
- Final full `Ngecor.Material.Tests` Play Mode run on Unity **6000.3.25f1**, Windows, **Direct3D 12 / AMD Radeon Graphics**, ran **2026-10-04 18:49:13-18:49:50 WIB**. Captured process exit code: **0**, also written to `Logs/Mat002ReviewFixFinalExitCode.txt`.
- Actual final XML reports **total 32, passed 32, failed 0, skipped 0, inconclusive 0**. All seven added regression tests passed.
- Verified registration-order independence, farther-distance ties, nearly equal unequal distances, exact minimum ties with two and three receivers, clearing active transfer feedback, no fallback or source loss for full/rejecting nearest receivers, movement retargeting, unregister cancellation, and null/destroyed/self handling.
- Verified capture creates a previously absent directory, both first and subsequent PNG outputs succeed, and the test's unique directory is removed afterward. No `PourPreview-*` directory remained.
- Inspected the regenerated 960x720 runtime PNGs: the pouring bucket is tipped and visibly emits gold grains; the stopped bucket is upright without grains.
- `git diff --check` and the staged diff check passed. The implementation commit contains exactly the four intended files; existing `.meta` files remain tracked and unchanged. No serialized asset or settings modifications were introduced.
- The implementation push exited 0; live remote `feat/sand-bucket` matched the implementation commit afterward.
- PR edit exited 0 and readback matched. PR #86 remains open and unmerged; Ryo's two reviews remain COMMENTED, with no approval. GitHub reports no configured CI checks.
- The Unity log contains the same non-fatal licensing refresh message seen in the previous animation run; entitlement resolution succeeded. No C# compilation errors or test exceptions were found in the final log.

## Final result

Both review fixes are implemented, verified through the graphics-enabled Unity Material suite, and published to PR #86. The PR now documents 32 passing tests and the receiver-selection contract. This report records the completed implementation and verification; technical and gameplay review remain open.

## Known limitations

- Tests drive pour requests through APIs. Real keyboard R input, overlapping-trigger gameplay feel, camera alignment, and carry/drop interaction were not manually tested in this session. TC-MAT002-15 remains NOT VERIFIED.
- PNGs and logs are local ignored evidence, not uploaded PR media. Controlled runtime renders do not establish a full player-input session.
- No target low-end hardware performance, multiplayer, or Windows player-build verification was performed.

## Unresolved issues or follow-up work

- Run TC-MAT002-15 and the other pending manual cases, record positions, quantities, and visual evidence, and obtain Ryo's technical/gameplay review before merge.
- Free ground pouring and recoverable ground piles remain MAT-004 (#54). No merge or issue closure was performed.
