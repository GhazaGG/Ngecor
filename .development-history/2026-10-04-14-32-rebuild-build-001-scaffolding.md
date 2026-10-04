# Rebuild BUILD-001 Scaffolding on the Latest Main

## Task summary

Recreated the scaffolding work on a new branch from the latest `main`, addressed the requested technical review items for PR #83, and prepared a dedicated development scene for BUILD-001 (#18).

## Relevant previous context

PR #83 was based on an older `main`, included unrelated Player and Playground changes, and had a changes-requested review from the team lead. The review retained the single-body-to-four-parts concept and requested primitive steps, load probing, a recoverable wobble phase, and testing in a developer-owned scene.

## Changes made

- Pulled `origin/main` to `d271570` in the main worktree and created `feat/build-001-scaffolding` from that baseline. Preserved the former scaffolding branch.
- Brought over only the approved scaffolding module, load-failure script, wood material, friction material, and their metadata.
- Replaced the old ladder rungs with six primitive steps, each 0.3 m high, under `Frame_Front`.
- Updated load detection to use one non-allocating overlap probe, counting unique dynamic rigidbodies and each CharacterController once.
- Kept the intact module kinematic, added tunable wobble/collapse thresholds and duration, and changed impact handling to start a warning wobble rather than directly break the module.
- Created `Dev_DePo4l.unity` with a floor, player prefab, scaffolding module, four cement bags, and an impact ramp. The dev scene is not in Build Settings.
- Left `PlayerMovement.cs`, `Playground.unity`, and shared Project Settings out of the task changes.

## Files affected

- `Assets/Game/Prefabs/Construction/ScaffoldingModule.prefab`
- `Assets/Game/Scripts/Construction/ScaffoldingLoadFailure.cs`
- `Assets/Game/Scenes/Dev/Dev_DePo4l.unity` and its generated `.meta`
- One task history report

The initial branch commit contains only the selected scaffolding assets and their `.meta` files from the approved copy list.

## Technical decisions

- The wobble threshold is 160 kg, above the expected 150 kg from one 75 kg player and three 25 kg bags. The collapse threshold is 190 kg; persistent overload starts wobbling and then breaks into four physical parts. Values remain Inspector-tunable.
- Impact threshold is 30 N·s. An impact starts a warning wobble; impact alone does not break the module. Removing an overload before the duration expires restores the module to its saved resting pose.
- The load probe extends about 2 m above the deck and spans the platform width. It ignores the scaffold's own rigidbody and other scaffolding modules.

## Verification performed

- Unity 6000.3.25f1 recompile completed with `compilationFailed=false`; after clearing the Editor Console, it reported zero errors and warnings.
- Confirmed through Unity that the dev scene contains the intended player, module, bags, and ramp, and that only `Playground.unity` remains enabled in Build Settings.
- Confirmed there are no EditMode tests in the project.
- Entered and exited Play Mode. Full gameplay checks were not possible in the headless Editor session: no keyboard device was available and no Game view render target was open.
- `git diff --check` reported trailing spaces in Unity-generated empty metadata fields (`userData`, `assetBundleName`, and `assetBundleVariant`); the generated `.meta` file was left unchanged per project rules.

## Final result

The change is ready as a draft PR for technical review, but gameplay acceptance is not confirmed. The earlier PR should be superseded by this branch and closed only after the replacement draft PR is open.

## Known limitations

Player traversal over the steps, standing and walking on the top platform, stable stacking, wobble timing and recovery, four-part breakup, and ramp-impact tuning were not verified through interactive Play Mode.

## Unresolved issues or follow-up work

- Run the review's complete Play Mode checklist in `Dev_DePo4l.unity`, record observed wobble duration and impact behavior, and adjust Inspector defaults if the gameplay feel requires it.
- Keep the replacement PR in Draft until that playtest is completed.
