# VEH-001 Physics Material Settings Confirmed

## Task Summary
Record the developer's confirmation of the wheel and support-leg Physics Material settings for the issue #13 review.

## Relevant Previous Context
The issue #13 prefab now has one dynamic Rigidbody at the root, with a low-friction Physics Material on the wheel and a high-friction Physics Material on both support legs. The previous follow-up asked to confirm each material's Friction Combine setting in the Inspector.

## Changes Made
Updated the Draft PR #67 acceptance checklist and known issues to record the confirmed friction values and combine modes.

## Files Affected
- PR #67 description
- .development-history/2026-10-02-20-54-veh-001-material-combine-confirmed.md

## Technical Decisions
- Record the wheel material as Dynamic/Static Friction 0.05 with Friction Combine Minimum.
- Record the support-leg material as Dynamic/Static Friction 0.8 with Friction Combine Maximum.
- Keep PR #67 in Draft until final gameplay acceptance results after these asset changes are reported.

## Verification Performed
- Inspected the developer-provided Unity Inspector screenshots for both Physics Material assets.
- Confirmed the displayed friction values, zero bounciness, Wheel Minimum combine, Feet Maximum combine, and Average bounce combine.
- No Unity session or automated tests were run by the assistant.

## Final Result
The Physics Material settings are confirmed and documented in PR #67. The PR remains Draft pending final manual gameplay checks.

## Known Limitations
- The developer has reported smoother, more stable control, but full ramp, cargo, tipping/spill, and Console results after the final material changes are not yet recorded.

## Unresolved Issues or Follow-up Work
- Record final Play Mode results in Playground after the final prefab and material updates.
- Update PR #67 readiness after those checks.
