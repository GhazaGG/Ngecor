# PLAYER-003 PR #85 QA Evidence Clarification

## Task summary

Recorded the developer's clarification about the three-cargo wheelbarrow test and Unity Console result in PR #85, and prepared a repeatable manual Game View and Profiler check for the current head.

## Relevant previous context

- PR #85 contains the PLAYER-003 review fixes in commit `6bfeaadaf4bf929b3c22b80cc26bf505625c15a0`; GhazaGG has been requested to review it again.
- The preceding PR description said the loaded-cart test surface was unknown and that no Console result had been reported.
- The developer clarified that exactly `cargo_1`, `cargo_2`, and `cargo_3` (1 kg each) were tested in the wheelbarrow on flat ground, and that the Unity Console was clean with no errors.
- Those visible Play Mode observations were not explicitly reported as repeated after commit `6bfeaada`; they remain labeled as earlier developer evidence rather than current-head verification.

## Changes made

- Updated PR #85's How to Test section with the exact three 1 kg cargos on flat ground and the developer-reported clean Unity Console.
- Marked the no-new-console-errors item checked as developer-reported, while leaving current-head manual feel acceptance unchecked.
- Kept the current-head Game View retest and Profiler capture listed as pending.
- Added a practical profiling scenario: profile a warmed-up local player walking on flat ground and pushing a low dynamic object, record CPU Usage and GPU Usage on the weakest team laptop, and inspect `PlayerMovement.Update`, frame time, and GC Alloc. If the built-in Profiler does not expose the capsule query as a separate sample, report the containing method's cost rather than using Deep Profile for timing.

## Files affected

- PR #85 description on GitHub.
- This development-history report.

## Technical decisions

- The reported manual observations remain labeled as preceding the review-fix commit because the developer did not say they had repeated all Game View cases on the new head.
- The wheelbarrow test is now explicitly recorded as flat ground with exactly three 1 kg cargo bodies.
- No code instrumentation or profiling package was added. Unity's Profiler documentation notes that Deep Profile adds substantial overhead; normal CPU Usage timing should be preferred, and a missing per-call sample should be reported as a limitation rather than treated as an exact capsule-cast measurement.
- Unity Console cleanliness is recorded as developer-reported evidence, not as an independent inspection by the agent.

## Verification performed

- Verified issue #73 asks for a flat-ground three-cargo wheelbarrow test and includes 60 FPS performance guidance with CPU/GPU Profiler captures on the team's weakest laptop.
- Confirmed PR #85 still points at commit `6bfeaadaf4bf929b3c22b80cc26bf505625c15a0` and GhazaGG remains requested for review.
- Updated PR #85 successfully. No Unity tests were rerun because this follow-up only changed PR documentation and evidence wording; the current code's earlier full PlayMode result remains 72/72.

## Final result

PR #85 now records the exact loaded-cart setup and clean Console report without presenting the earlier manual observations as a current-head retest.

## Known limitations

- The current head has not yet received visible Game View verification for the sack, five-bag stack, light prop, traversal, and wheelbarrow cases.
- The capsule sweep has not yet been profiled, and no current-head CPU/GPU captures are attached.

## Unresolved issues or follow-up work

- Repeat the manual Game View cases on commit `6bfeaada` and record the feel/observations.
- Capture CPU and GPU Profiler evidence on the weakest available team laptop and note the frame time and GC allocation results.
