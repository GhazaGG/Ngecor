# PR #93 Deck Impact Filter Follow-up

## Task summary

Respond to Ghaza's latest PR #93 review, which found that horizontal sack momentum was being counted on every collision surface and could make a moving platform drop collapse the scaffold.

## Relevant previous context

The earlier follow-up changed `ScaffoldingLoadFailure.OnCollisionEnter` to count horizontal momentum for near-horizontal contacts as well as side contacts. The latest review on head `c7dc12d` identifies the deck-drop false positive and asks for the side-contact filter to be restored, followed by three Play Mode cases: running throw collapse, moving drop on the deck remains stable, and ordinary ground shove into a post remains safe.

## Changes made

- Restored the side-contact gate `Mathf.Abs(contact.normal.y) >= 0.5f` before calculating impact momentum.
- Kept the horizontal momentum calculation for accepted side contacts and the existing collision impulse threshold.
- Did not change `PlayerMovement`, prefabs, or scenes.

## Files affected

- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`
- `.development-history/2026-10-10-16-17-pr93-deck-impact-filter.md`

## Technical decisions

The 0.5 normal-Y cutoff follows the latest review's requested starting point: deck/top contacts are treated as load, while side contacts can still use horizontal momentum. No gameplay values or threshold were changed.

## Verification performed

- Reviewed the latest GitHub comment on PR #93 and inspected the collision callback and its call sites.
- `git diff --check` passed.
- Unity CLI reported no connected Editor instance, so compilation status could not be checked after this edit.
- No Play Mode test was run after this correction, following the developer's instruction to handle Play Mode tests themselves.

## Final result

The deck-drop path is filtered before the horizontal momentum threshold check. The requested runtime cases remain unverified on this corrected code.

## Known limitations

There is no compile or Play Mode result for this latest edit. The previous manual results were recorded before restoring the side-only filter and do not validate this version.

## Unresolved issues or follow-up work

In Unity 6000.3.25f1, `Dev_DePo4l`, rerun: (1) running sack throw into side post collapses the scaffold, (2) carry and drop the sack while moving on the deck without collapse, and (3) push the sack from the ground into a post without collapse. Record Console errors/warnings and owner feel after the clean manual session.