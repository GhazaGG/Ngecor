# MIX-003 PR B: Grabbable Cement Sacks in Dev_Ghaza

## Task summary
Made the cement sacks in the developer's own dev scene grabbable so the MIX-003 PR B sack intake can be feel-tested with E.

## Relevant previous context
- `.development-history/2026-10-09-19-20-mix003-cement-sack-not-grabbable.md`: no sack in `Dev_Ghaza` had `GrabbableObject`; a component added during the earlier test session was never saved.

## Changes made
- Added menu item `Ngecor/Make Dev_Ghaza Cement Sacks Grabbable` to `ManualMixingSpotSetup`. It opens `Dev_Ghaza`, adds `GrabbableObject` to every `CementBag` instance that lacks one, and saves the scene.
- Ran it through Unity 6000.3.25f1 batchmode `-executeMethod`: 7 sacks updated.
- Committed the previously uncommitted "Mixing Test Cement Sack" instance near the mixing spot together with this scene change.

## Files affected
- `Assets/Game/Scripts/Editor/ManualMixingSpotSetup.cs`
- `Assets/Game/Scenes/Dev/Dev_Ghaza.unity`
- `.development-history/2026-10-09-19-40-mix003-dev-ghaza-grabbable-sacks.md`

## Technical decisions
- Did not change `CementBag.prefab`. Making the prefab grabbable is MAT-003 (#46, assigned to DePo4l); the components are scene-instance additions in a personal dev scene only.
- Used a Unity editor method instead of editing the `.unity` file as text.

## Verification performed
- Batchmode log: "Added GrabbableObject to 7 cement sacks in Dev_Ghaza.", exit code 0.
- `Dev_Ghaza.unity` now has 8 `GrabbableObject` references (7 sacks plus the existing one on the Player instance object); `git diff` shows no prefab changes.
- E pickup in Play Mode with the saved scene has not been tested yet.

## Final result
Cement sacks in `Dev_Ghaza` can be targeted by the generic grab interaction.

## Known limitations
- When MAT-003 adds `GrabbableObject` to the prefab, these scene-instance additions become duplicates under `[DisallowMultipleComponent]` and should be removed in Unity.
- The existing `GrabbableObject` plus extra Rigidbody on an object inside the Player prefab instance in `Dev_Ghaza` was left untouched.

## Unresolved issues or follow-up work
- Developer verifies E pickup and the remaining PR #98 manual steps.
