# BUILD-001: Thicken Timber Scaffolding Supports

## Task summary

Adjusted the wooden scaffolding supports after the user noted that they looked too thin.

## Relevant previous context

- The current prototypes use timber for both frames and decks.
- The 1.8 m tall posts had been set to 8 cm square, while several support bars were only 6 cm thick.
- The Playground contains both the newer combined module and an older modular frame showcase; both were kept.

## Changes made

- Increased the new module posts from 8 cm to 14 cm square.
- Increased its foot plates, lower/middle crossbars, and top supports so the base and joints read as substantial timber members.
- Increased the older frame prefab's posts, feet, rungs, and top support, keeping its upper support aligned with the existing platform height.
- Regenerated both prefabs through the Unity Editor builder menu.

## Files affected

- `Assets/Game/Scripts/Editor/ScaffoldingPrototypeBuilder.cs`
- `Assets/Game/Prefabs/Construction/ScaffoldingFrame.prefab`
- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab`
- This development-history report

## Technical decisions

- Kept the material timber and retained the existing module masses and assembly behavior.
- Increased primitive collider dimensions along with the visible support geometry so the collider shape continues to match the timber members.

## Verification performed

- Unity Editor reported no script compilation errors.
- Unity Editor successfully executed `Ngecor/Build BUILD-001 Scaffolding Module` and regenerated the prefabs.
- No Play Mode physics or traversal tests were run.

## Final result

Both scaffold prefabs now have visibly thicker wooden posts and supports. Their mass settings and overall module layout remain unchanged.

## Known limitations

- The prefabs still use simple cube primitives and do not have detailed timber joints or diagonal bracing.
- Collision feel and stacking stability after the thicker colliders require Play Mode verification.

## Unresolved issues or follow-up work

- Run the Issue #18 manual stacking and cargo checks in Unity Play Mode when the intended player controller is available.
