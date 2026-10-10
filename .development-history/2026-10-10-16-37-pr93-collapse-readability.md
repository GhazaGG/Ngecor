# PR #93 Collapse Readability Follow-up

## Task summary

Diagnose why the scaffold appeared to wobble without visibly collapsing during the owner's manual test, then make the smallest gameplay change that makes a triggered break visibly separate the scaffold parts.

## Relevant previous context

The latest PR #93 review requested a player-driven hard impact while preserving safe ordinary shoves and top-deck drops. The existing code uses a 75 N·s side-impact threshold and restores stability by filtering top contacts. The owner asked to perform Play Mode testing personally.

## Changes made

- Added a 1.5 m/s horizontal separation velocity to each break part in `BreakIntoParts`, directed outward from the scaffold center.
- Kept the 75 N·s threshold and side-contact filter unchanged.
- Did not modify the scene, prefab, player movement, or throw force.

## Files affected

- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`
- `.development-history/2026-10-10-16-37-pr93-collapse-readability.md`

## Technical decisions

The live `Dev_DePo4l` hierarchy showed the scaffold root gone and `Frame_Front`, `Frame_Back`, `Platform_Left`, and `Platform_Right` as separate Rigidbody objects. These match the parts detached by `BreakIntoParts`, so the break path had run. The existing code preserved only the scaffold's small wobble velocity; the pieces could remain touching and visually upright. A small outward velocity separates them after the already-qualified hard impact, while safe hits still do not enter the break path.

## Verification performed

- Inspected the loaded `Dev_DePo4l` hierarchy and confirmed its detached scaffold parts were separate Rigidbody objects.
- `git diff --check` passed.
- Connected Unity Editor reported `compiling=false`, `compilationFailed=false`, and 0 Console errors, warnings, and logs at the time checked.
- Did not start or run Play Mode, following the owner's instruction to do the gameplay test themselves.

## Final result

When the existing hard-impact condition breaks the scaffold, its four parts now receive outward separation motion so the collapse is visibly readable. The safe-shove and deck-contact thresholds remain unchanged.

## Known limitations

The new separation speed and resulting collapse feel have not been observed in Play Mode.

## Unresolved issues or follow-up work

Have the owner verify a running sack throw into a vertical side post, a moving sack drop on the deck, and an ordinary ground shove into a post, then report collapse readability and feel.
