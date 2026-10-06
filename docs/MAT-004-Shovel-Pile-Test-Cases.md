# MAT-004 Shovel and Recoverable Pile Checks

Use Unity **6000.3.25f1** and `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`. The `MAT-004 Trial` group contains a finite 80-unit sand source, an empty 2-unit shovel, and an empty bucket. Existing MAT-002 objects remain in the scene. Test one carried tool at a time.

`E` grabs/drops, left mouse throws, and one `R` press scoops with an empty shovel or dumps with a loaded shovel. Hold `R` to pour a bucket. Shovel targeting starts at `BladeTip`, searches up to 1 metre forward, and requires a clear path. Ground deposit searches down up to 3 metres and merges within 0.5 metre. Tune the prefab fields if needed; record changed values.

Automated evidence and GUI observations are recorded in the latest MAT-004 development report. Synthetic Input System events and scripted fixture placement do not establish human keyboard input or gameplay feel. The checklist below is for a gameplay reviewer and starts **NOT VERIFIED**.

| Case | Setup and action | Expected result | Reviewer status |
| --- | --- | --- | --- |
| Grab, scoop, hold | Focus Game View, aim at the shovel, press E. Move its blade toward the source and hold R for one second. | Exactly one scoop: source 80 → 78; shovel 0 → 2. Holding R does not dump or repeat. | NOT VERIFIED |
| Partial receiver | Give the bucket one unit of free capacity. Release and press R with a loaded shovel near it. | Bucket accepts 1; shovel retains 1. Total is unchanged. | NOT VERIFIED |
| Ground dump and merge | Scoop 2 units, aim at clear ground, press R. Scoop again and dump within 0.5 m on the same floor. | First dump creates a pile; second grows that pile to 4 units. Visual and mesh collider grow together. | NOT VERIFIED |
| Recover and exhaust | Scoop from the new pile until empty, then dump into the bucket. | Exactly 2 units move per scoop or the smaller remaining amount; empty runtime pile disappears. | NOT VERIFIED |
| Type changes | Scoop from an isolated Cement source, empty the shovel, then scoop Concrete. Use matching receivers. | Empty shovel follows each source type. Loaded shovel never changes type or mixes contents. | NOT VERIFIED |
| Rejection and ties | Put a full/rejecting receiver nearest the blade and a valid one farther away; repeat with two equidistant nearest targets. | Full/rejecting nearest accepts zero with no fallback. Exact nearest tie cancels, including ground fallback. | NOT VERIFIED |
| Reach and obstruction | Put stock behind the blade, beyond 1 m, or behind a wall. Dump over a void or a dynamic platform. | Ineligible scoop targets are ignored. Failed ground deposit keeps all units in the source. | NOT VERIFIED |
| Bucket ground pour | Fill and carry the bucket over clear ground. Hold R, release it, then test E drop just before the next physics tick. | Rate-limited ground pour produces recoverable units and existing tilt/particle feedback. Release/drop stops queued pouring. | NOT VERIFIED |
| Tilted drops and throw | Drop a loaded bucket and shovel at an angle exceeding 70°, and throw each with left mouse while the cursor is already locked. | Tilt spill creates recoverable piles. Source decreases only by accepted units. Grab/drop/throw retain their normal controls. | NOT VERIFIED |
| Multiple floors | Dump same-type material within 0.5 m horizontally on two different floors. | Separate piles; quantities never transfer between floors. | NOT VERIFIED |
| Profiling and Console | Keep several piles and tilted containers active; record CPU/GPU Profiler captures, FPS, hardware, resolution, and Console. | No new gameplay errors; quantify frame cost. Compare with the 60 FPS target on the team's reference laptop. | NOT VERIFIED |

Record **PASS**, **FAIL**, **BLOCKED**, or **NOT VERIFIED** for each case. For a result, include branch/head, tester, scene, tuned values, before/after units, Console errors, feel observations, and screenshot or video paths. Do not count an automated assertion as a human gameplay pass.

The focused automated suite covers all three material types, conservative scoop/dump, partial capacity, clear paths, exact ties, no fallback, local held state, failed deposits, same-type merging, separate floors/types, growth beyond 100 units, empty runtime pile deletion, passive spill conservation, ground pour cancellation, actual mound/bucket prefab transfer, and input-adapter use requests. It also retains the MAT-002 nearest/exact-tie and drop-race regressions. Press/hold/release timing remains a live Game View check.

The original finite `SandPile` source keeps its MAT-002 empty/refill behavior: its empty mound and collider are disabled. Runtime `GroundPile` objects are deleted when exhausted. Mixing, freshness, digging, networking, and final art remain outside MAT-004.

## Recorded local observations

- OS-synthesized keyboard events in the live Game View, with fixture placement through a temporary Editor helper, produced E grab, one scoop while R was held for 1 second (80 → 78 source, 0 → 2 shovel), and one bucket dump while R was held for 1 second (2 → 0 shovel, 0 → 2 bucket). The sum remained 80. This is automation evidence, not a human feel review.
- A separate scripted stress fixture tilted 12 bucket prefabs with 20 units each. It produced 12 recoverable piles containing all 240 units and no recorded gameplay errors. Warm Editor session averages observed around 91–111 FPS on AMD Ryzen 5 7430U/Radeon Graphics, approximately 16 GB RAM, with a 1920×1080 Game View. These observations do not establish standalone performance, the 8 GB reference budget, or four-player performance.
- [Game View stress capture](MAT-004-Piles-PlayMode.png) and [CPU Profiler capture](MAT-004-Profiler-CPU.png) show the local fixture. Raw profile data remains in ignored `Logs/Mat004Stress.data`. GPU timing was not established; the GPU highlight track did not provide usable timing evidence.
- Log review found the original non-convex mesh `ClosestPoint` warning despite passing quantity tests. Shovel targeting now intersects the actual mound mesh and the real-mound test also rejects physics warnings/errors during scoop.
