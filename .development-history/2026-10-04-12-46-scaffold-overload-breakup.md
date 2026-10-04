# Scaffold Overload Breakup

## Task summary

Added mass-based overload breakup for the scaffolding module. The intact module keeps one Rigidbody and primitive colliders. When the physical payload exceeds its Inspector limit, it separates into four persistent Rigidbody parts. Added rungs on the support's left and right sides per the developer's correction.

## Relevant previous context

- Issue #18 calls for real cargo mass to cause sway/collapse, no counters, and no more than six Rigidbodies per scaffolding. Its earlier comment excluded CharacterController load and reassembly.
- The developer's latest request explicitly includes one person in the supported load and asks for four reusable pieces. This report records that scope change.
- The previous user correction moved the requested ladder access to both sides of the supports.

## Changes made

- Added `ScaffoldingLoadFailure` to measure cargo Rigidbody mass contacting the deck and CharacterController presence over the platform.
- Set Inspector defaults to 150 kg maximum payload (75 kg player plus three 25 kg bags) and 400 N·s impact impulse. Both are tunable.
- On failure, detaches the front frame, back frame, left deck, and right deck as four primitive-collider Rigidbody objects, preserving the assembly's velocity. Their masses total 65 kg.
- Updated the Editor prefab builder to create two deck boards and ladder rungs at left and right supports.
- Added a targeted Editor menu action that regenerates only the BUILD-001 module prefab.
- Regenerated the prefab and saved the Playground scene through Unity Editor.

## Files affected

- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs` and its Unity-generated `.meta`
- `Assets/Game/Scripts/Editor/ScaffoldingPrototypeBuilder.cs`
- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab`
- `Assets/Game/Scenes/Playground.unity`
- This development history report.

## Technical decisions

- Kept one Rigidbody (65 kg) while intact and added no joints. The four child assemblies receive Rigidbodies only when the module breaks.
- Cargo is measured from actual Rigidbody masses and top-deck contacts. CharacterController weight is represented by a tunable 75 kg downward force because CharacterController has no Rigidbody mass.
- The player plus three standard 25 kg bags equals the default 150 kg payload. Heavy cargo or further load can exceed it. A real collision impulse can also break the module.
- No grab or runtime reassembly system was added. The separated parts remain physical and reusable for a later rebuild interaction; current repositioning/reassembly is an Editor task.

## Verification performed

- Unity 6000.3.25f1 completed script compilation successfully after removing an unnecessary cross-assembly ladder reference.
- Unity Editor inspection confirmed the prefab root has one Rigidbody at 65 kg, no FixedJoint, and serialized references to all four break parts.
- Confirmed the prefab and both Playground module instances contain left and right ladder rung geometry. The ladder climb trigger remains present.
- Saved `Playground.unity` through Unity Editor.
- Did not run Play Mode or Unity Test Runner. The 150 kg stable-load case, overload breakup, and impact threshold remain unverified at runtime.

## Final result

The prefab and Editor builder now describe a one-body scaffold that can separate into four physical parts under measured payload or impact thresholds. The earlier request to place ladder rungs on both sides is also reflected.

## Known limitations

- Runtime threshold feel and whether three bags remain stable have not been tested in Play Mode.
- The parts persist after collapse, but gameplay pickup and reassembly are not implemented; rebuilding requires Editor manipulation until that system is in scope.
- The load monitor samples at most 16 simultaneous cargo rigidbodies and 16 overlap colliders.

## Unresolved issues or follow-up work

- In Play Mode, verify one player plus three 25 kg bags remains stable, a fourth bag or heavy cargo breaks into exactly four pieces, and an impact above the Inspector threshold causes breakup.
- Confirm the four parts land without overlap jitter and can be arranged again in the Editor.

