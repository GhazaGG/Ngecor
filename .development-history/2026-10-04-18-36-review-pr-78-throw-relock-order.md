# PR #78 throw review: cursor relock update order

## Task summary

Reviewed Ryzen's PR [#78](https://github.com/GhazaGG/Ngecor/pull/78), `[INT-004] Throw carried object`, covering logic, approach, code, input and asset integration, security, performance, tests, and project scope. Recommended Request Changes for one reproduced input bug.

- Reviewed head: `f235b228f968b6fc3b4a7a6ee2bb0d92c93fe49c`.
- Base: `eb4a0dac14182e324650f5e493872d8736f148f3`.
- Rechecked GitHub immediately before recording results: the head and base were unchanged; the PR remained open, with no CI checks configured.

## Relevant previous context

Read the relevant repository instructions and project documents, issue #12, the existing GitHub review, and these local reports:

- `2026-10-04-12-06-review-pr-78-int-004.md`.
- `2026-10-03-22-29-review-pr-75-int-003.md`.
- `2026-10-03-12-15-review-pr-69-int-002.md`.

Also read the PR's latest `2026-10-04-18-13-int-004-review-fixes-verification.md`. Historical claims were checked against the current diff and imported assets. The previous review's cursor-order statement correctly describes the relocking click itself; it does not cover a fresh click in the immediately following frame.

## Changes made

Created this investigation report. No product changes were made in the active workspace. Review diagnostics and a candidate fix were tested only in an isolated temporary clone. Both source files were restored afterward. No GitHub review was submitted for this task; the previously submitted Request Changes review remains on the PR.

## Files affected

- Active workspace: this report only. Existing untracked PR #85 reports were preserved.
- Temporary clone only: `Assets/Game/Tests/Interaction/PlayerGrabTests.cs` received three review diagnostics; `Assets/Game/Scripts/Player/PlayerMovement.cs` received a candidate frame-based guard. Both were restored to the reviewed source after testing.
- Evidence directory: `C:\Users\Hype\AppData\Local\Temp\ngecor-pr78-review-yf5fncit`.

## Technical decisions and findings

### P2: a stale relock flag can discard the next throw click

Affected locations: `Assets/Game/Scripts/Player/PlayerMovement.cs:61` and `Assets/Game/Scripts/Interaction/PlayerGrab.cs:91` at the reviewed head.

`CursorRelockedThisFrame` is a persistent Boolean, cleared only by `PlayerMovement.Update`. After a relocking click sets it to true in frame N, `PlayerGrab.Update` can still read true before Movement clears it in frame N+1. If a new Attack press arrives in that next frame, Grab discards it despite the cursor already being locked. The press does not remain pending for a later frame. Neither component establishes a relative script execution order.

Reproduction in Unity Play Mode with controlled invocation of the real private Update methods:

1. Carry an object; unlock with Escape.
2. Process a mouse click that relocks the cursor. Both Update orders correctly preserve the object.
3. Queue release and press events for the next sampled render frame, representing a fast subsequent click at a low frame rate.
4. Verify that Attack reports `WasPressedThisFrame()` and that the cursor is locked.
5. Invoke Grab before Movement: the object remains carried. Invoke Movement before Grab in the control case: the object is thrown.

On the unchanged PR, the failing case logged relock frame 16, current frame 17, flag true, and carrying true after the second click. The control logged frames 22/23 and carrying false. This establishes the order-dependent logic under synthetic input; it is not a recording of physical mouse use.

Suggested minimal correction: record the frame number of the relock, initialized to -1, and expose `CursorRelockedThisFrame` as equality with `Time.frameCount`. Set the frame number when relocking and remove the Update-based Boolean reset. This preserves first-click protection in either order without adding global script execution settings. The candidate was verified in the temporary clone.

Unity's [script execution order documentation](https://docs.unity3d.com/6000.3/Documentation/Manual/script-execution-order.html) confirms that relative order at the same/default execution order is not guaranteed.

### Previous requested fixes are resolved

- The `docs/superpowers` plan is absent from the current PR/tree.
- A dedicated `Box_Heavy` is committed in Playground: 0.5 m cube, 25 kg dynamic Rigidbody with gravity, and `GrabbableObject`. The scene diff adds that fixture without modifying CementBag or Box_8.
- After Unity import, `PlayerThrow` resolves to `Player/Attack`, includes the mouse binding, uses the configured project action asset, and is wired to `Player.prefab`. The local-player prefab flag remains false and ThrowForce remains 10.
- PlayerGrab's hardcoded mouse fallback and runtime action Enable calls were removed. The test fixture owns activation of its mock action map.

### Other review conclusions

- The throw path remains small and reuses `DetachObject`, including collision restoration, saved Rigidbody state, holder cleanup, and inherited horizontal player velocity.
- Camera direction, inverse-mass impulse, empty-hand behavior, and absent-action behavior have passing existing tests. The 10 N-s impulse gives 10 m/s at 1 kg and 0.4 m/s at 25 kg; tuning remains the previously agreed follow-up for #46.
- No new package, global manager, networking implementation, external input boundary, or blocking security finding was identified in this offline scope.
- The added runtime work is constant-time input checks and release-time velocity arithmetic. No new per-frame allocations or physics queries were introduced by the throw change. No performance benchmark was performed.
- Existing carry/depenetration concerns remain under #76; they were not expanded into new throw requirements.
- Diff whitespace warnings were limited to Unity-generated serialized asset lines; these were not treated as code defects or text-edited.

## Verification performed

Used Unity `6000.3.25f1`, batchmode with `-nographics`, in the detached temporary clone at the exact reviewed head. Captured process exits and NUnit result XML.

- **Unchanged full suite: PASS, 70/70**, exit 0, no failures or skips. Interaction 40, Material 12, Player 18. Evidence: `pr78-baseline-results.xml` and `pr78-baseline.log`.
- **Additional diagnostics on unchanged production code: 2/3 passed, 1 failed**, process exit 1. Imported action wiring passed; Movement-before-Grab control passed; Grab-before-Movement next-click regression failed. Evidence: `pr78-diagnostics-results.xml` and `pr78-diagnostics.log`.
- An initial diagnostic fixture incorrectly inspected queued UnityTest input before the player loop consumed it. Both order cases failed at the setup assertion. The fixture was corrected to wait for input processing; only the resulting valid control/regression run supports the finding.
- **Candidate frame-based correction plus full suite: PASS, 73/73**, exit 0, no failures or skips, including both next-click orders and imported asset wiring. Evidence: `pr78-frame-fix-results.xml` and `pr78-frame-fix.log`. This is candidate-fix evidence, not a claim that the PR itself passes 73 tests.
- Restored review-only source modifications and checked repository state. Active workspace product files remained unchanged.

## Final result

Recommend **Request Changes** for the P2 relock-flag defect. All previous Must Fix items and the action-lifecycle Should Fix are resolved. No additional blocking finding was identified in the reviewed scope.

## Known limitations

The reviewer did not perform interactive gameplay/feel checks or physical keyboard/mouse input. Author-reported interactive Playground checks were read but are not independent reviewer evidence. Batchmode verifies runtime assertions and imported references, not subjective feel. Multiplayer remains outside INT-004 and was not tested.

## Unresolved issues or follow-up work

- Author should make relock state frame-specific and cover the next click with both valid Update orders.
- Repeat reviewer gameplay checks before declaring gameplay Definition of Done complete.
- Keep heavy-throw tuning in #46 and carry collision decisions in #76.
- This review outcome has not been newly posted to GitHub.
