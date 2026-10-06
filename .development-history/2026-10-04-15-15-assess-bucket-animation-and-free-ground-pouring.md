# Bucket Animation and Free Ground Pouring Assessment

## Task summary

Assessed the user's request for a visible bucket pouring animation and the ability to pour sand outside receiver areas. Proposed a concrete behavior for scope clarification; no gameplay implementation was changed during this assessment.

## Relevant previous context

- `2026-10-04-04-05-correct-sand-pile-volume-and-verify-prefab-physics.md` records mound volume scaling, empty collision handling, and actual-prefab physics checks.
- `2026-10-04-14-24-create-mat-002-manual-test-cases.md` records the current manual checklist and its receiver-only pour scope.
- The user prefers cartoon party-game feel and whole-mound shrinking rather than local digging depressions.
- Current issue #54 assigns ground pile creation, nearby same-type merging, and material conservation to MAT-004.

## Changes made

- Investigated the current container, pour action/input, receiver trigger, pile visual, and existing test coverage.
- Proposed holding R to tilt the bucket with a simple visible sand stream, transferring to an appropriate receiver or creating/adding to a ground pile when no receiver is used.
- Asked an asynchronous scope question distinguishing the full proposed behavior from animation with the existing receiver-only route. No answer had arrived when this report was written.
- Created this investigation report.

## Files affected

- This report only.

## Technical decisions

- Existing `BulkMaterialContainer` remains the quantity and conserved-transfer authority.
- Free pouring needs a real ground destination with collision; material must not be removed unless a valid destination can accept it.
- Proposed reusing the existing static mound prefab and merging nearby compatible ground piles, following issue #54 rather than introducing a second quantity system.
- Animation and free pouring extend the currently documented MAT-002 behavior. Shovel/scoop implementation was not inferred from the request.

## Verification performed

- Read current issue #54 through `gh-axi` and reviewed relevant local history.
- Confirmed `BucketPourAction.RequestPour()` currently requires a registered nearby receiver and `FixedUpdate()` only invokes container-to-container transfer. No free ground destination or pouring animation exists in that path.
- Confirmed pile visual changes are driven by container quantity and whole-mound scaling.
- Checked current branch and dirty tree. An open Unity session has the user's `Dev_Ghaza_MAT002` test scene with unsaved state; it was preserved.
- No Unity tests or GUI input were performed. Tracked diff statistics remain the same as before this assessment.

## Final result

The requested behavior is feasible using the existing material system. A proposed input and destination flow is ready for the user's clarification; animation and unrestricted ground pouring have not been implemented.

## Known limitations

- Current gameplay remains receiver-only pouring with changing fill levels.
- Proposed animation, ground placement, merging, capacity behavior, and no-surface handling have no new runtime evidence.

## Unresolved issues or follow-up work

- Resolve the pending scope clarification and implement the selected behavior.
- Define handling for rejecting/full receivers and placement where no valid ground surface is reachable.
- Update the manual checklist and verify conserved quantities, animation feedback, pile merging, and cancel/drop behavior after implementation.
