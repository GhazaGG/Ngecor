# Playground Ramp Setup Investigation

## Task summary

Inspect the user's saved Playground scene change after reporting that the wheelbarrow remains supported and stationary when released on a slope.

## Relevant previous context

- VEH-002 issue #14 asks for a manual release-on-slope check and requires the wheelbarrow to remain physics-driven.
- The prior ramp investigation found that the wheelbarrow has a dynamic Rigidbody and high-friction support feet; a supported cart may remain at rest on an incline.
- The user reported changing `Box_Heavy` into a simple speed-bump-like test object.

## Changes made

- Added this investigation report only. No scene, prefab, gameplay code, physics material, or ProjectSettings file was changed.

## Files affected

- `.development-history/2026-10-07-18-19-investigate-playground-ramp.md`

## Technical decisions

- The saved `Box_Heavy` transform is unrotated, at `(−6.02, 0.56, 7.12)`, with scale `(2, 0.1, 0.5)` and a unit BoxCollider. Its lower face initially sits around `y=0.51`, while the Ground plane is at `y=0`. It has a dynamic Rigidbody with gravity enabled, so it should fall until it contacts the floor; it remains horizontal after settling and is not an inclined ramp.
- The scene already contains `Ramp_Gentle` rotated 10 degrees and `Ramp_20Degree` rotated 20 degrees. These are the suitable existing objects for an incline release check.
- The wheelbarrow's support feet use `FeetHighFriction` (static/dynamic friction 0.8), while its wheel uses `WheelLowFriction` (0.05). Remaining stationary while still supported can be a normal physics equilibrium.
- Do not change the user's scene setup without confirmation. If the test was performed on `Box_Heavy`, repeat it on an existing ramp before treating it as slope evidence.

## Verification performed

- Read the current saved scene diff and serialized transforms/colliders for `Box_Heavy`, `Ground`, `Ramp_Gentle`, and `Ramp_20Degree`.
- Read the wheelbarrow Rigidbody and physics material values.
- Confirmed the worktree already contains a user scene edit plus Unity project-setting changes; these were left untouched.
- No Play Mode run or runtime contact inspection was performed.

## Final result

The likely test-setup issue is that the edited `Box_Heavy` is not sloped and does not touch the ground. Use `Ramp_Gentle` or `Ramp_20Degree` for the release-on-slope check. If the cart stays still there while its wheel or support feet remain in contact, that alone does not show that physics is disabled.

## Known limitations

- The saved scene file was inspected; the exact ramp and wheelbarrow contact state during the user's Play Mode test is unknown. Live unsaved Inspector values were not observed.
- No new gameplay behavior or scene configuration was validated in Unity.

## Unresolved issues or follow-up work

- Re-run the release check on `Ramp_Gentle` (and optionally `Ramp_20Degree`) and note which parts remain supported after release.
- If the cart remains stationary after all support contacts clear the ramp edge, inspect runtime Rigidbody state and collider contacts before changing friction or support-leg geometry.
