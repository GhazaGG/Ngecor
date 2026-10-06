# Prepare Unity Editor for the PR #78 owner playtest

## Task summary
The owner asked to run the game to try PR #78 (INT-004 throw). I opened Unity 6000.3.25f1 on an isolated clone at the PR head.

## Relevant previous context
- Another review session prepared a temporary clone, `C:\Users\Hype\AppData\Local\Temp\ngecor-pr78-latest-j7ip6541`, detached at `ff0c696`. Unity opened it in Safe Mode because of missing package assemblies.
- The owner's main checkout `C:\NGECORRR\Ngecor` is on `feat/sand-bucket` with uncommitted changes, so I didn't use it.

## Changes made
- Ran a batchmode import on the clone. It failed with 123 compile errors: Input System, Netcode, and NUnit types weren't found, even though the packages were registered.
- Diagnosed the cause: the clone's `Library/PackageCache` was corrupt. The folders were registered but empty; `test-framework` had 1 file and the netcode folder was missing.
- Deleting only `PackageCache` wasn't enough, because Unity restored the stale resolution state from `Library/PackageManager`.
- Deleted the clone's whole `Library` folder (generated, temporary clone only) and ran a clean batchmode import: exit 0, 0 compile errors, `PackageCache` present.
- Launched the Unity Editor on the clone with `Playground.unity`. Editor.log shows the PR #78 project loading with no Safe Mode and no `error CS`.
- No tracked files in the clone or the repo were changed.

## Files affected
- This report. Generated folders and import logs exist only in the temporary clone.

## Technical decisions
- Used the isolated clone instead of the owner's main checkout, which has uncommitted work on another branch.

## Verification performed
- `git log -1` in the clone shows `ff0c696` (the PR #78 head). The clean import log reports exit 0 and 0 compile errors.
- Editor.log after launch contained no Safe Mode and no compile errors at the time of checking. Gameplay itself was not tested by me.

## Final result
The Unity Editor is open on PR #78 for the owner's playtest.

## Known limitations
- The playtest is the owner's. I didn't play or observe Play Mode.

## Unresolved issues or follow-up work
- Record the owner's playtest feedback on PR #78 before merging.
