# BUILD-001 Issue #18: Modular Scaffolding Prototype

## Task summary

Implemented the current Issue #18 prototype plan for physics-only, stackable scaffolding modules. The superseded boards, nails, and hammer plan was not used.

## Relevant previous context

- Issue #18 and GhazaGG's latest comment define the current scope: one Rigidbody per complete module, primitive colliders, two-level stacking, a static access ramp, and cargo-only load tests.
- `.development-history/2026-10-01-23-55-check-issue-18-blockers.md` documented that a project CharacterController/player implementation was not available and that the pre-existing modular showcase must be preserved.
- The project already contained separate frame/platform prefabs, an existing `Scaffolding_Showcase`, a static `Ramp`, and a Rigidbody-based `TestPlayerDummy`. These were left in place.

## Changes made

- Added a separate Unity Editor menu action, `Ngecor/Build BUILD-001 Scaffolding Module`, that creates a combined module prefab and an Issue #18 playground showcase without calling the older destructive builder path.
- Added `ScaffoldingModule`, built from primitive frame and deck geometry under a single 65 kg root Rigidbody. Its colliders are primitive colliders; the mass, friction material, and transform size remain editable in the Inspector.
- Added a two-level module arrangement, a static 45-degree access ramp, and standard/heavy movable cargo fixtures to the Issue #18 showcase.
- The new showcase is placed separately from the existing one. The builder checks for its own showcase root before creating it, and leaves existing showcase objects untouched.

## Files affected

- `Assets/Game/Scripts/Editor/ScaffoldingPrototypeBuilder.cs`
- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab` and its Unity-generated `.meta`
- `Assets/Game/Scenes/Playground.unity` (Issue #18 showcase added through Unity Editor)
- This development-history report

## Technical decisions

- Each module is a single Rigidbody with primitive child colliders; no joints, mesh colliders, snap logic, grab controls, player movement, networking, or hammer mechanics were introduced.
- The module uses the existing project friction material. The ramp is static and has a 45-degree incline, within the issue's stated 50-degree slope limit.
- Existing frame/platform assets and the old `Scaffolding_Showcase` were retained.

## Verification performed

- Unity Editor compiled the updated builder and successfully executed the new menu item.
- Unity Editor lookup found both `/Scaffolding_Showcase` and `/Scaffolding_Issue18_Showcase` in the loaded Playground scene, confirming the prior showcase root remains present.
- A scene search found no `CharacterController` component. The required player traversal/no-jitter acceptance check therefore could not be performed with the project's actual player controller.
- No Play Mode physics or traversal tests were run. Issue #18's manual How To Test steps remain for the developer to run in Unity.
- Opening Unity also surfaced an untracked default `ProjectSettings/SceneTemplateSettings.json`; it was preserved and not treated as an implementation change.

## Final result

The combined scaffolding module prefab and isolated two-level test showcase are in the project. The Issue #18 gameplay acceptance criteria are not declared passed because Play Mode verification, especially with the actual CharacterController player, remains outstanding.

## Known limitations

- No project CharacterController/player prefab or movement component was found, so player traversal and collision feel remain unverified.
- Settling/sleep behavior, imperfect placement, load stability, tilting/tipping, and repeatability have not been observed in Play Mode.

## Unresolved issues or follow-up work

- The developer/lead should run Issue #18's five manual Play Mode checks after the intended player controller is available, including the center-load and heavy off-center-load comparisons.
- If the two-level assembly cannot settle without joints/snapping, or if the real player jitters, stop and ask the lead as required by GhazaGG's comment.
