# BUILD-001: Wood-Only Scaffolding Materials

## Task summary

Updated the visible scaffolding prototypes to use wood as their sole construction material, matching the user's direction that one material should be enough to make scaffolding.

## Relevant previous context

- Issue #18 calls for physics-only scaffolding modules and keeps crafting, grabbing, and inventory systems outside the prototype scope.
- The preceding implementation used a steel-colored frame and wood-colored deck for the new module, while the older frame prefab also used the steel material.
- The old Playground showcase was to be preserved rather than deleted or rebuilt destructively.

## Changes made

- Updated the BUILD-001 Editor menu action to regenerate the legacy frame prefab using the existing wood material.
- Updated the new combined module so its frame geometry and deck both use the same wood material.
- Kept the existing Playground instances and showcase roots; saving the frame prefab updates its linked instances without rebuilding the scene.
- Kept the existing friction PhysicsMaterial, which controls collision behavior and is not a construction ingredient.

## Files affected

- `Assets/Game/Scripts/Editor/ScaffoldingPrototypeBuilder.cs`
- `Assets/Game/Prefabs/Construction/ScaffoldingFrame.prefab` (regenerated through Unity Editor)
- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab` (regenerated through Unity Editor)
- This development-history report

## Technical decisions

- Timber is now the only visual/build material assigned to scaffolding geometry in the Playground.
- No nails, hammer, grab mechanic, inventory, or resource-counting/crafting code was added. The current task establishes the one-material prototype direction; a gameplay recipe remains outside Issue #18.

## Verification performed

- Unity Editor recompiled the builder with no reported errors.
- Unity Editor successfully executed `Ngecor/Build BUILD-001 Scaffolding Module` after regeneration.
- No Play Mode tests were run.

## Final result

Both the new physics module and the legacy frame shown in the Playground use the same wood material. Their physics and assembly behavior are otherwise unchanged.

## Known limitations

- The scaffold geometry still uses primitive shapes, so the wood reads as simple timber blocks rather than detailed lumber.
- One-unit resource consumption and a crafting interaction are not implemented.

## Unresolved issues or follow-up work

- If Issue #18 later expands to include material collection or crafting, define what one unit of wood means and how it becomes a module in a separate scoped task.
