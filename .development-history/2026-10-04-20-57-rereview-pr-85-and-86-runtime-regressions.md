# Re-review PR #85 and PR #86 runtime regressions

## Task summary

Re-reviewed PR #85 and PR #86 as requested, covering previous fixes, logic, approach, code, integration, security, performance, tests, and acceptance evidence. Reproduced one new P2 defect in each PR. Posted a Request Changes review on #85 and a technical follow-up comment on #86.

| PR | Author | Reviewed head | Base |
| --- | --- | --- | --- |
| #85 PLAYER-003 | RyoFPS | `7c16b3bb8806a888960d029596d05a238475cd3b` | `d271570a63d75bf7b2bb431a0283e3b92c411e6a` |
| #86 MAT-002 | GhazaGG | `c900196a4f36f664cbcf26b1b6bb420927d202f5` | `d271570a63d75bf7b2bb431a0283e3b92c411e6a` |

Confirmed that #86 is authored by GhazaGG and had been reviewed by Ryo, rather than being authored by Ryo. Both PRs remained open and unmerged; no CI checks were configured. Their heads were rechecked before posting.

## Relevant previous context

- Read the project instructions, RTK guidance, Ponytail skill, README, workflow, project structure, relevant game-design and decision sections, and live issues #73/#16.
- Reviewed local `2026-10-04-17-43-review-pr-85-human-strength-pushing.md` and the existing GitHub reviews.
- Read #85's review-fix report, partial QA update, and supplied Profiler/Console evidence. Read #86's review-fix and animation reports and relevant material implementation memory. Historical source and verification claims were checked against the current heads.
- No `.codegraph` directory exists; no index was created. Kept the active `docs/history-review-pr-69` branch and all pre-existing untracked reports intact.

## Changes made

- Fetched current main and both PR refs; created detached temporary clones at the reviewed heads.
- Ran original Unity suites, added temporary review diagnostics, and tested small candidate corrections only in those clones.
- Inspected supplied #85 screenshots and independently generated #86 runtime pour/cancel images.
- Restored all modified runtime/test C# files in both clones. Preserved diagnostic fragments, candidate patches, XML, and logs as temporary evidence.
- Published the two review outcomes and created this report. No product fixes, commits, pushes, approvals, or merges were performed.

## Files affected

Active workspace: this report only.

Temporary #85 clone: `C:\Users\Hype\AppData\Local\Temp\ngecor-pr85-rereview-_2pi2ts9`. Temporarily changed `PlayerMovement.cs` and `PlayerMovementTests.cs`; both restored.

Temporary #86 clone: `C:\Users\Hype\AppData\Local\Temp\ngecor-pr86-rereview-vwe6elsn`. Temporarily changed `BucketPourAction.cs`, `BucketPourPlayModeTests.cs`, and `SandPileAndBucketPrefabPlayModeTests.cs`; all restored.

External changes:

- [PR #85 review 5406534579](https://github.com/GhazaGG/Ngecor/pull/85#pullrequestreview-5406534579), `CHANGES_REQUESTED`, submitted as GhazaGG at 20:52:27 Asia/Jakarta.
- [PR #86 comment 5980765267](https://github.com/GhazaGG/Ngecor/pull/86#issuecomment-5980765267), submitted as GhazaGG at 20:56:36 Asia/Jakarta. This is a technical follow-up comment on that account's own PR, not an approval or a formal Request Changes review state.

## Technical decisions and findings

### PR #85: P2 probe-buffer saturation hides a real blocker

At `PlayerMovement.cs:121`, the capsule query uses an eight-hit buffer and all layers. Collider ownership, ignored-layer, and ignored-pair filters are applied afterward. Excluded hits can fill every slot, preventing a valid low dynamic body from reaching the filter. The method returns false without handling a full result buffer; its caller retains normal step offset.

Controlled Play Mode reproduction:

1. A grounded player and a 0.18 m high dynamic box are detected by a forward probe over 1 m in the control case.
2. Add 12 low colliders in the query path and ignore each collider pair against the CharacterController.
3. The same probe returns false. All eight returned hits are `ReviewIgnored0` through `ReviewIgnored7`, and the valid dynamic box is absent.

A temporary candidate grows the cached buffer and repeats a full query before filtering. It returns true, with the valid box included in the results, and passes the full suite plus the diagnostic. Ordinary queries retain buffer reuse; growth allocates only when a larger capacity is first needed. This was a helper-level reproduction, not a recorded keyboard-driven step-over. Its 1 m query range was controlled explicitly and is not presented as a normal 60 FPS walking interval.

The suggestion is to handle truncation, optionally reduce irrelevant layers before querying, and retain physical collision filtering and static stepping. Merely increasing a fixed buffer moves the threshold. The [Unity CapsuleCastNonAlloc API](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.CapsuleCastNonAlloc.html) describes storing results in the supplied buffer and returning the count stored.

Previous #85 implementation findings are resolved: same-body impulses are deduplicated per movement step, collision exclusions and Ignore Raycast physical collisions are considered, pure helpers are called directly, and heavy-body tests now check gradual measurable movement over appropriate windows. The cap remains 300 N. The committed prefab uses the new force field. No scene, package, networking, or global physics setting changes were introduced.

The supplied Console screenshot shows zero logs/warnings/errors at the captured point. The Profiler screenshot is idle-only: median 7.908 ms, maximum 78.432 ms, with the peak dominated by EditorLoop. It does not establish capsule-sweep or PlayerMovement cost while moving/pushing. Current-head gameplay checks remain pending and are now marked honestly in the PR.

### PR #86: P2 a pending pour can transfer after ExecuteDrop

At `BucketPourAction.cs:95`, FixedUpdate transfers to the cached receiver without rechecking whether the bucket is still carried. `BucketPourInput.Update` cancels only when it next reads the changed holder state. If drop happens after that input update, a transfer tick can execute first.

Controlled Play Mode reproduction uses the real grab/drop APIs and production Update/FixedUpdate methods. Components are created before Awake, matching prefab initialization. After a successful grab/request, five normal fixed transfer ticks accumulate credit at 10 units/s. The bucket remains at 10 units and the receiver at 0. ExecuteDrop sets both IsHeld and IsCarrying false. A transfer tick before input cancellation changes the quantities to 9/1; the input-before-transfer control remains at 10/0. Total quantity is conserved, but the transfer happens after release.

A temporary candidate caches GrabbableObject during Awake and cancels pending transfer before executing a tick when an actual bucket is no longer held. It leaves MAT-005 quantity/transfer code untouched and passes both order cases. This diagnoses the action's release boundary; it does not establish physical keyboard input or full gameplay feel.

Earlier receiver-selection and PNG-directory findings are resolved. Imported binding verification confirms Player/Pour points to keyboard R on the configured project action asset. Existing input maps, action IDs, bindings, and control schemes are unchanged apart from Pour and Editor serialization fields. Runtime tests verify conservation, capacity/rejection, target selection, cancellation, mound geometry/collider, cavity, tilt spill, and visual feedback. Inspected new runtime images showing a tipped bucket emitting grains and an upright stopped bucket without grains.

A non-blocking consistency suggestion was posted: let project/test lifecycle own action activation instead of BucketPourInput re-enabling a deliberately disabled Pour action in OnEnable/Update. This matches the PlayerGrab cleanup in #78.

### Approach, security, and performance

- Both changes remain scoped and use existing systems. No new package, global manager, networking implementation, unsafe code, external trust boundary, or additional blocking security issue was identified.
- #85 uses pure calculations, a shared impulse budget, cached references, and contact deduplication. #86 composes with MAT-005 rather than duplicating quantity authority; receiver scanning is allocation-free over the retained set in ordinary operation.
- Imported bucket budget checks passed: 32 maximum live particles, particle collision disabled, shadows off, and five primitive BoxColliders. Particles remain feedback rather than material state.
- No low-end benchmark or movement/pour Profiler session was performed. Loaded 25 kg cart response remains a MAT-003/VEH-002 investigation; it does not prove a 300 N cap violation. Ground piles remain #54, and throw integration remains #78.
- #85 diff-check passed. #86 PR-wide diff-check reports only Unity-generated serialized whitespace; C#/documentation checks were not blocked by those asset lines. Serialized files were not text-edited.

## Verification performed

Used Unity 6000.3.25f1 with captured process exits and completed NUnit XML. #85 ran batchmode/nographics; #86 ran graphics-enabled batchmode.

| Run | Result | Process exit |
| --- | --- | --- |
| #85 unchanged full suite | 72/72 pass: Interaction 33, Material 12, Player 27 | 0 |
| #85 saturation diagnostic, unchanged runtime | 0/1 pass; expected blocker missing | 1 |
| #85 candidate plus full suite | 73/73 pass | 0 |
| #86 unchanged full suite | 82/82 pass: Interaction 33, Material 32, Player 17 | 0 |
| #86 drop order and imported wiring diagnostics, unchanged runtime | 2/3 pass; physics-before-cancellation transfers one unit | 1 |
| #86 candidate plus full suite | 85/85 pass | 0 |
| #86 strengthened normal-fixed-tick diagnostic, unchanged runtime | 2/3 pass; same one-unit transfer after drop | 1 |
| #86 strengthened normal-fixed-tick diagnostic, candidate | 3/3 pass; both orders remain 10/0 | 0 |

Evidence files include #85 `pr85-current-baseline.xml`, `pr85-probe-diagnostic.xml`, `pr85-candidate-buffer-fix.xml`; #86 `pr86-current-baseline.xml`, `pr86-final-review-diagnostics.xml`, `pr86-final-candidate-drop-fix.xml`, `pr86-fixed-tick-original.xml`, and `pr86-fixed-tick-candidate.xml`, with corresponding logs. Candidate outcomes are not claimed as outcomes of the current PR code.

An initial candidate run returned 84/85 because the review fixture added GrabbableObject after the action's Awake. The fixture was corrected to initialize all bucket components before Awake; unchanged production code still reproduced the defect. The final diagnostic also replaced a manually chosen fractional transfer duration with normal production fixed ticks. These corrected runs, with a passing control, support the posted finding.

Initial fresh-clone startup failed with ENOSPC before tests. Removed only the newly created clones' generated Library directories after verifying their absolute paths. Reused matching locked package-cache files from the earlier review clone through hard links; package manifests were identical. Parallel fresh imports then hit Mono allocation failures before results. Sequential runs completed successfully. These environment failures are not PR test failures. Shared project settings and packages were not changed.

Confirmed restoration with git diff exit 0 for the affected source/test files in both clones. Rechecked GitHub heads before publishing. Read back #85's review metadata and verified author, commit, state, and URL. Read back #86's comment and matched its entire body to the prepared file. Preserved all earlier active-workspace reports.

## Final result

Both reviews are complete and posted. Recommend changes before approval: #85 needs complete probe results; #86 needs release validation at the execution tick. Previous requested code fixes are acknowledged as resolved. Neither PR was approved or merged.

## Known limitations

No interactive Game View/physical keyboard gameplay review, weakest-team-laptop profiling, Windows build, multiplayer test, or combined-PR integration run was performed. Runtime fixtures, synthetic/control invocation, and rendered feedback do not establish subjective feel or every physical configuration. Complete manual acceptance remains pending in both PRs. Temporary evidence may eventually be removed by OS cleanup.

## Unresolved issues or follow-up work

- Fix #85 query saturation while retaining collision exclusions, static traversal, force cap, and cached ordinary-path behavior.
- Fix #86 pending transfer after release and retain a real ExecuteDrop-before-tick regression.
- Repeat the current-head gameplay and relevant performance checks. #86 still needs approval from another team member.
- Keep heavy loaded-cart investigation, recoverable ground piles, and throw integration in their existing tickets.
