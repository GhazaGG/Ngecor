# Implement and stage MIX-001 in Dev_DePo4l

## Task summary
Completed the MIX-001 concrete mixer prototype and prepared the requested `Dev_DePo4l` scene for a hands-on Play Mode trial.

## Relevant previous context
- The task branch is `DePo4l/mix-001-concrete-mixer-prototype` and includes the fetched `origin/main` commit `8af0787`, which added the shared MIX-003 recipe calculator.
- Earlier playtest diagnosis found the player action map was not enabled by `PlayerMovement`; a locally owned player also needs a camera for the interaction crosshair.
- The separate untracked `Dev_DePo4l_MIXER` scene was only used as a reference. It was not saved or changed on disk during this task.

## Changes made
- Added `Mixer` with `Idle`, `Mixing`, and `Ready` states, a one-cement/one-sand default batch, a five-second tunable duration, and an Inspector context-menu action to start a batch during Play Mode.
- Reused `ConcreteRecipeCalculator` and the shared bulk container. A one-plus-one batch removes one cement and one sand unit and outputs two concrete units, conserving the input unit count.
- Added `BulkMaterialContainer.ConfigureMultipleTypesWhenEmpty` so the mixer can accept cement, sand, and concrete without changing a filled container.
- Enabled the local player's referenced input action map on startup and when local ownership is assigned. Added a movement test that verifies the map is enabled before checking movement.
- Saved the requested `Dev_DePo4l` scene with a visible mixer body and two existing bucket prefab instances in front of it: five sand units and five cement units. The cement bucket's pour action is configured for cement.

## Files affected
- `Assets/Game/Scripts/Construction/Mixer.cs`
- `Assets/Game/Scripts/Construction/Mixer.cs.meta` (existing GUID retained)
- `Assets/Game/Scripts/Material/BulkMaterialContainer.cs`
- `Assets/Game/Scripts/Player/PlayerMovement.cs`
- `Assets/Game/Tests/Player/PlayerMovementTests.cs`
- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity`
- This development-history report.

## Technical decisions
- MIX-003 defines output quantity as the sum of the recipe's cement and sand units; the prototype defaults to one unit of each.
- The prototype reuses the existing bucket pour receiver and keeps starting the mixer as a dev-only Inspector action. No player-facing mixer interaction or extra framework was added.
- The requested dev scene was saved through Unity Editor. The reference scene and unrelated user changes were left untouched.

## Verification performed
- Unity Editor `6000.3.25f1` recompiled the scripts successfully.
- Play Mode test `Ngecor.Player.Tests.PlayerMovementTests.LocalOwnerCanMoveAfterInstantiation` passed (1/1).
- In `Dev_DePo4l` Play Mode, the Player's local camera and input action map were active. The mixer accepted cement, sand, and concrete; the two buckets each held five units.
- Runtime smoke test: added one cement and one sand unit through the shared container API, started mixing, observed `Ready` with two concrete units and no remaining inputs, then removed the output and observed `Idle` with an empty mixer.
- Final runtime state is Play Mode active, mixer `Idle` and empty, and both five-unit buckets staged for the developer.
- `git diff --check` passed for the changed C# files. Unity's saved scene has whitespace on a few empty serialized YAML values; the scene was not edited as text.

## Final result
The MIX-001 logic is implemented and the requested dev scene is saved and running in Play Mode with materials ready for the developer's grab-and-pour trial.

## Known limitations
- Grab and pour were not manually exercised with keyboard or mouse input. The Game View screenshot also did not establish whether Unity's IMGUI crosshair is visible, although the local camera and action map were confirmed active.
- Start a batch from the Mixer component's `Start Mixing` context menu while in Play Mode. The recipe ratio remains a prototype default.

## Unresolved issues or follow-up work
- Manually grab both buckets with `E`, pour with `R` over the mixer, and start a batch from the Mixer component menu to validate physical transfer and visually confirm the crosshair in the Game View.
