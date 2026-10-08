# VEH-002 Loaded Reverse Playtest Result

## Task summary

Recorded the user's manual acceptance after the reverse push strength adjustment to 0.65.

## Relevant previous context

- The change is recorded in `2026-10-08-17-30-veh-002-loaded-reverse-followup.md`.
- Before the 0.65 adjustment, the user had confirmed side alignment, improved steering, ramp traversal, a clean Console, and no front-wheel lift at the lower reverse strength.

## Changes made

- No gameplay code changed in this turn. Added this report to capture the new manual result.

## Files affected

- `.development-history/2026-10-08-17-36-veh-002-loaded-reverse-playtest.md`

## Technical decisions

- Keep the 0.65 reverse strength. The user reports both loaded and empty carts now reverse acceptably, without the severe stutter previously seen, and the front wheel remains on the ground.
- Leave the fixed-leg/model issue outside scope; no prefab or scene asset was edited.

## Verification performed

- User manually tested the current reverse adjustment in Unity Play Mode and reported acceptable reverse behavior with and without cargo and no front-wheel lift.
- Earlier user-reported observations remain: turning is improved, gentle and 20-degree ramps are passable with cargo, and the Console is clean.
- Automated PlayMode tests were not run after the 0.65 adjustment because Unity had previously failed to create CoreCLR due to insufficient memory.

## Final result

The user reports the loaded-reverse issue is resolved for the current scope. No commit, push, scene edit, or prefab edit was made.

## Known limitations

- Automated PlayMode test results do not cover the 0.65 adjustment.
- Some mild stutter from the model's fixed legs may remain; the user identified this as outside the current model scope.

## Unresolved issues or follow-up work

- None for the requested interaction behavior. Revisit the leg/contact model only if that is brought into scope later.
