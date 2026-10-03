# Implement MAT-005 bulk material container

## Task summary
Implement issue #62's reusable bulk material container on `feat/mat-005-bulk-container`.

## Relevant previous context
Reviewed `2026-10-02-14-45-bulk-material-contract-and-manual-mixing.md`, `2026-10-02-17-13-scaffolding-rescope-bulk-container-split.md`, and `2026-10-02-18-07-plan-mat-005-bulk-container.md`. The current `docs/DECISIONS.md` requires integer quantities in world containers, conserved transfer, and a shared component. Issue #62 permits spills to disappear until MAT-004 implements piles.

## Changes made
- Added one `BulkMaterialContainer` MonoBehaviour with Inspector-configurable single- or multi-type acceptance, total capacity, integer quantities per type, rate-limited transfer, Rigidbody tilt spill, and a referenced fill visual.
- Added a Material runtime assembly and a test assembly with six focused checks.
- Added an Editor-only demo tool and created `Dev_Ghaza.unity` through Unity Editor. The menu can trigger transfers in Play Mode and runs checks in batchmode.
- Let Unity generate all new `.meta` files. Removed the unrelated `ProjectSettings/SceneTemplateSettings.json` generated during Editor startup.

## Files affected
`Assets/Game/Scripts/Material/BulkMaterialContainer.cs`, `Ngecor.Material.asmdef`, their `.meta` files and folder `.meta`; `Assets/Game/Tests/Material/BulkMaterialContainerTests.cs`, `Ngecor.Material.Tests.asmdef`, their `.meta` files and folder `.meta`; `Assets/Game/Scripts/Editor/BulkMaterialDemo.cs` and its `.meta` and folder `.meta`; `Assets/Game/Scenes/Dev/Dev_Ghaza.unity` and its `.meta`; this report.

## Technical decisions
- Sand, cement, and concrete are enum values; no ScriptableObject database or package was added.
- Transfers clamp to source stock, receiver free capacity, and accepted type. Fractional rate credit is retained between calls, while actual amounts stay integer.
- Spillage is passive only when a Rigidbody is present and tilted beyond its configured angle. The current issue allows lost units; pile creation remains MAT-004.
- The container has no player input, global manager, recipe, freshness, or networking code. The editor demo is separate from the runtime component.

## Verification performed
- Unity 6000.3.25f1 imported the new assets, generated `.meta` pairs, and compiled the runtime, test, and Editor assemblies without C# errors.
- Six NUnit test methods were invoked directly in Unity Editor batchmode and passed: capacity/conservation, empty/full/rejected cases, Inspector content normalization, rate carry and stopping, fill visual scaling, and Rigidbody tilt spill.
- A separate Unity Editor batchmode check reopened the serialized dev scene and passed its state, transfer conservation, rejection, and tilt checks without saving the test mutations.
- `git diff --check` found no tracked-file whitespace errors; the task files were still untracked at report creation.

## Final result
MAT-005 implementation and a dev scene are ready for team review. No PR merge or issue closure was performed.

## Known limitations
- Unity's ordinary headless Test Runner invocation failed to start because the licensing client reported no valid Editor license. The passing checks used an Editor method to invoke the same NUnit methods directly.
- Interactive Play Mode, visual feel, and Editor Console after manual scene interaction were not verified. The dev scene's menu actions are prepared for that review.
- Spill units disappear until MAT-004 adds piles, as issue #62 explicitly permits.

## Unresolved issues or follow-up work
Run the dev scene in interactive Play Mode with an activated Unity Editor, have a teammate review gameplay, and complete the usual PR review before treating the issue as done. MAT-002, MAT-004, MIX-003, MIX-002, and networking retain their separate scope.
