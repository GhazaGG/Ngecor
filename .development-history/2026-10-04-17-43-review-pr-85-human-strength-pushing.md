# Team Lead Review of PR #85: Human-Strength Pushing

## Task summary

Reviewed RyoFPS's PR #85 against issue #73 for correctness, physics approach, integration, security, performance, maintainability, scope, and verification quality. Recommendation: **request changes before merge**. This is a local review; no GitHub review, comment, approval, or merge was submitted.

- Reviewed head: `b2ea12969591bb566699c4318c924789c338c53c`.
- Compared main/base: `d271570a63d75bf7b2bb431a0283e3b92c411e6a`.
- Both SHAs and the open PR state were refreshed from GitHub during review.

## Relevant previous context

- Read the current project instructions, README, workflow, structure, game design, issue #73, PR description, and relevant decisions. The active documentation branch was older than main; inspected the current decision changes explicitly.
- Reviewed `2026-10-02-16-17-veh-001-contact-push.md`, `2026-10-02-17-04-veh-001-contact-push-playtest-investigation.md`, `2026-10-02-21-43-review-pr-58-67-veh-001.md`, and `2026-10-02-22-15-pr58-framerate-review-fix.md`. These establish the contact-push filters, render-frame force regression, low-prop stepping concern, and pending loaded-wheelbarrow checks.
- Read the four PLAYER-003 history reports included in PR #85. Their original 65/65 result predates the final mass-based target speed and step probe.
- Current decisions specify kilograms, approximately 250-300 N maximum player force, a 25 kg cement bag, and offline implementation. The October 3 automated-test policy requires critical pure-logic tests; physics feel and interactions require manual Play Mode evidence. This review does not impose a new permanent physics-test framework.
- No `.codegraph/` directory exists in the active worktree; no index was created.

## Changes made

- Fetched the PR and main into remote refs for read-only source inspection. Kept the active `docs/history-review-pr-69` branch and its product files unchanged.
- Created an isolated temporary clone at `C:\Users\Hype\AppData\Local\Temp\ngecor-pr85-review-dxn86xei`, detached at the reviewed head, and ran Unity 6000.3.25f1 tests there.
- Added temporary review-only diagnostic tests and a hit recorder in that clone. Restored the original PR test source afterward. Diagnostic fragments, the recorder, XML results, and logs remain in the temporary clone for inspection.
- Created this report. No fixes were applied to the PR.

## Files affected

Active project: this development report only.

Reviewed PR changes: `PlayerMovement.cs`, `PlayerMovementTests.cs`, `Player.prefab`, and four development-history reports. The PR does not change scenes, packages, global physics settings, material mass/friction, or networking code.

Temporary verification artifacts include `pr85-baseline-results.xml`, `pr85-focused-original-results.xml`, `pr85-focused-diagnostics-results.xml`, `pr85-trace-results.xml`, corresponding logs, `review-diagnostics.fragment.txt`, `review-trace.fragment.txt`, and the review-only `ReviewPushTrace.cs` under the clone's existing player test folder. Unity generated its metadata and an untracked `ProjectSettings/SceneTemplateSettings.json` in the clone.

## Technical decisions and findings

### P1: The committed suite is not green; reconcile the two heavy-body fixtures

`PlayerMovementTests.cs:279` and `:301` fail on the reviewed head. The full unchanged suite returned 65 passed / 2 failed; the unchanged focused player suite later returned 20 passed / 2 failed.

- High 25 kg cube: expected more than 0.10 m movement in 120 rendered frames at 60 FPS; measured approximately 0.07083 m.
- Low 25 kg cube: expected more than 0.05 m movement in 30 rendered frames; measured approximately 0.03501 m in the full run and 0.03960 m in the unchanged focused run.
- A separate three-second trace showed sustained high-cube movement of approximately 0.03598 m/s, reaching about 0.10792 m. All 180 watched contacts were horizontal; none were rejected by the downward-contact filter. This supports continuous slow movement, rather than an assertion that pushing stops entirely.

These fixtures create cubes, not the actual CementBag prefab with its material and behaviour. The failures establish inconsistent committed verification, not a definitive manual gameplay failure. Reconcile the fixtures, observation duration, and expectations with issue #73 and the current test policy. Do not simply increase the force cap or weaken assertions to manufacture a green result.

### P2: Repeated contacts can bypass the target-speed response

`PlayerMovement.cs:155-168` reads point velocity for every callback but only tracks the remaining global force budget. Unity accumulates AddForce effects until the next physics simulation; previously queued impulses are absent from the velocity used by subsequent callbacks.

Controlled PlayMode reproduction: a 1 kg body initially moving at 4 m/s, 30 FPS, nine same-body callback invocations at its centre of mass, followed by a physics step, reached **5.381664 m/s** despite the 5 m/s target. One callback reached 4.153518 m/s. The remaining 300 N budget was still positive in the nine-callback case.

The total force budget itself worked: 100 controlled callbacks consumed exactly 10 N s at a 1/30 s render interval, with zero remaining budget. The defect is target-speed handling across repeated body contacts, not multiplication beyond the global 300 N cap. Deduplicate contacts per body/movement step or account for queued impulses when calculating the remaining target-speed response.

Evidence boundary: callback data in this reproduction was synthesized and invoked through reflection in Unity PlayMode. A separate real nine-collider fixture at 30 FPS passed, peaking at 4.989509 m/s. This review does not claim that the existing wheelbarrow reproduces the nine-callback overshoot. An actual multi-contact gameplay reproduction remains useful.

### P2: The step probe ignores player collision exclusions

`PlayerMovement.cs:118-137` sweeps default raycast layers and accepts low dynamic bodies without checking whether they can collide with this CharacterController. A PlayMode probe test set `Physics.IgnoreCollision(controller, bodyCollider, true)`, verified the ignored pair, and still obtained `ShouldBlockStepOverDynamicBody == true`.

The Update caller then suppresses step offset for a body the player is allowed to pass through. Honour layer collision rules and ignored collider pairs when selecting a blocker. The current raycast mask also excludes Ignore Raycast objects independently of whether they physically collide with the player.

The confirmed failure is the helper's false positive. Its effect on a complete static-step traversal with an ignored prop was not separately reproduced. An existing-contact low-body probe passed; an overlap-miss defect is not asserted.

### Verification and PR-description corrections

- Complete the exact flat-ground wheelbarrow check with three cargo bodies. Reporting `cargo_1` and `cargo_3` does not establish this acceptance case.
- Record independent reviewer gameplay evidence for the 1 kg prop, actual 25 kg bag, stop/release, five-bag stack, static stepping, empty-cart ramp, and three-cargo flat ground. The existing developer observations remain useful but are not independent reviewer QA.
- The PR marks "No new Console errors" checked while explicitly saying no Console result was reported. Correct that checkbox or supply actual evidence.
- Current GitHub checks are not configured. Do not cite the historical 65/65 as validation of this head.

### Approach, performance, security, and maintainability assessment

- The main approach is appropriately scoped: new force field with Newton semantics, serialized prefab value 300, mass-scaled target speed, pure calculations, shared impulse budget, and preserved kinematic/top-contact exclusions. No new manager, framework, dependency, multiplayer API, or global physics change is introduced.
- Existing light-body, 5 kg 30/120 FPS, horizontal-push, kinematic, stood-on-body, movement, and ramp tests passed in the reviewed runs. This is evidence for those fixtures, not every physical configuration.
- Runtime references are cached and the new capsule query uses a reusable eight-hit buffer. No new managed per-frame allocation was found in the changed runtime code. The query adds physics work on each grounded movement render frame; no Profiler or low-end FPS measurements were performed. Buffer saturation is not handled; crowd/compound-collider probe behaviour is a follow-up risk, not a reproduced defect here.
- No new external input, I/O, secret handling, RPC, or networking trust boundary appears in the diff. No new security vulnerability was identified in this offline change. Host/client behaviour was not tested and remains the NET tickets' responsibility.
- Pure static helpers are tested through reflection despite being public. Direct calls would remove unnecessary string lookup and boxing from the tests; this is a small maintainability suggestion, not a runtime performance defect.
- Loaded-cart interpretation needs care: the model uses only the contacted body's `mass`. The current Wheelbarrow Rigidbody is 4 kg. At 60 FPS and zero speed, its calculated force is approximately **95.95 N**, even though the cap is 300 N; separate cargo bodies do not increase that calculation's mass. The reported inability to push a cart carrying a 25 kg bag is therefore not proof that a 300 N human-strength limit has been exceeded. Investigate this alongside MAT-003/VEH-002. The 25 kg loaded-cart scenario is not an additional acceptance requirement invented for issue #73.

## Verification performed

| Run | Source state | Result |
| --- | --- | --- |
| Full Unity PlayMode suite | Original reviewed PR | 67 total; 65 passed, 2 failed |
| Focused player suite | Original test source restored | 22 total; 20 passed, 2 failed |
| Focused player plus diagnostic suite | Original runtime, six review-only diagnostics | 28 total; 24 passed, 4 failed; two original failures plus target-speed and ignored-collision diagnostics |
| Sustained heavy-contact trace | Original runtime, review-only observer | Completed; recorded gradual movement and contact directions; no gameplay acceptance assertion |

For failing runs, the captured RTK process exit was 1 and the Unity log explicitly reported test-run exit code 2. The trace run returned process/log exit code 0. XML files were inspected only after process completion. There were no C# compilation failures. Editor startup licensing/debugger messages were not treated as proof of a clean gameplay Console.

Also performed PR-wide `git diff --check`, current SHA/state checks, changed-file inventory, prefab force-value inspection, interaction/carry caller inspection, and restoration checks confirming no tracked runtime, prefab, or original player-test differences in the clone after diagnostics.

Unity API contracts checked against official Unity 6.3 documentation:
- [Rigidbody.AddForce](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForce.html): deferred application of accumulated impulses.
- [Rigidbody.AddForceAtPosition](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Rigidbody.AddForceAtPosition.html): linear force and torque.
- [Physics.CapsuleCastNonAlloc](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Physics.CapsuleCastNonAlloc.html): non-allocating buffered query.

## Final result

Review completed with a **request changes** recommendation. The current suite failures, repeated-contact speed handling, collision filtering, and remaining acceptance evidence prevent approval at this point. No production changes or external review submission were made.

## Known limitations

- Unity runs were batchmode/nographics PlayMode with synthetic Input System input. No visible Game View session, real keyboard playtest, gameplay feel review, Windows build, host/client session, or Profiler capture was performed.
- The target-speed counterexample uses controlled callbacks; the real compound fixture passed. The ignored-collision counterexample isolates the probe helper.
- The heavy-body diagnostic measures a cube fixture; actual CementBag acceptance relies on developer feedback until a reviewer tests it.
- Temporary artifacts may be removed by normal OS temporary-directory cleanup. Numeric evidence and reproduction conditions are preserved in this report.

## Unresolved issues or follow-up work

1. Reconcile the two failed committed tests with approved acceptance and test policy.
2. Correct repeated-body target-speed accounting while preserving the total force cap.
3. Correct the step probe's collision filtering and validate ordinary static traversal.
4. Complete and record the outstanding manual acceptance cases, especially exactly three cargo bodies on flat ground, and correct the Console checkbox.
5. Recheck loaded-wheelbarrow force response in the relevant material/vehicle work; do not classify the 25 kg cargo observation as physically expected solely from the 300 N configured cap.
