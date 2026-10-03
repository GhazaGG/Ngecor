# Create PLAYER-003, align cement bag feel, record push and test-norm decisions

## Task summary
The owner's AI, while working on MAT-001 (#15), found that player contact push cannot move a heavy cement bag, and drafted a new ticket. The owner asked whether a separate issue was needed and then delegated the decisions to the lead. The draft was reviewed, corrected, and created as #73. Related tickets and DECISIONS were updated.

## Relevant previous context
- `docs/DECISIONS.md` "Player mendorong objek fisika lewat kontak" (2026-10-02) and "Kontrak material bulk".
- PR #67 reviews deferred load/target-speed push to #14/#46 and told Ryo not to raise global `Push Strength` to 20.
- PR #69 review noted `_pushStrength: 5` is stored explicitly in `Player.prefab`.
- The lead took over #15 and #18 from DePo4l (inactive) on 2026-10-02.

## Changes made
- **#73 PLAYER-003 created** (M2, P1, assignee RyoFPS): force-capped, human-strength push (about 250–300 N, 1 unit = 1 kg), a 25 kg cement bag that slides slowly, a 5-bag stack that topples gradually, no force stacking across colliders, existing push rules kept, a pure-function force calculation with one automated test, and a wheelbarrow re-test after PR #67 merges.
- **#14 and #46** now list PLAYER-003 (#73) as a blocker.
- **#15 MAT-001** rewritten as a full bag: 25 kg, effective friction about 0.6–0.75, bounciness 0, flattened box with high angular drag, 1.5 m drop test, and push/carry/throw feel moved out to #73, #46, and #12.
- **#12 INT-004** gained an AC: the same throw force sends a 25 kg object much shorter than a 1 kg object.
- **`docs/DECISIONS.md`:** new entries "Tenaga dorong player dan massa sak semen" and "Norma tes otomatis".

## Files affected
- `docs/DECISIONS.md`, this report. The rest were GitHub issues.

## Technical decisions
- A separate ticket, because the push logic is in `PlayerMovement.cs` (player system) and #15 is a standalone object. The earlier suggestion to accept the bag as a "wall" was withdrawn in favor of fixing the cause.
- Mass is 25 kg everywhere (the draft said 40 kg). A 40 kg bag needs about 350 N at friction 0.9, which exceeds the 250–300 N cap, so the draft's own acceptance criteria could not pass. The 25 kg bag needs about 180–220 N.
- A new field name is required for the force cap. The old `_pushStrength` (value 5, stored in `Player.prefab`) would otherwise be read as 5 N.
- PR #67 does not block PLAYER-003: it only changes wheelbarrow prefab files. The wheelbarrow is re-tested afterwards.
- Automated tests are required only for pure, critical logic (here, the force calculation). Feel and physics are tested manually.

## Verification performed
- Read `PlayerMovement.cs` lines 89–102 on `origin/main`. The calculation (push of about 25 N, friction threshold of about 4 kg) is from the code and Unity defaults, **not tested in Play Mode**.
- Checked that no existing issue covers push, and that PR #67 touches only prefab files.
- Re-read the edited #15 acceptance criteria on GitHub.

## Final result
PLAYER-003 exists with consistent numbers, owner, and dependencies. #15 no longer depends on push behavior.

## Known limitations
- The force numbers (250–300 N, 25 kg, friction 0.6–0.75) are starting values and need Play Mode tuning.
- The owner's AI work on #15 was reported as uncommitted and local on `feat/cement-bag`. It should be pushed as a WIP draft PR.

## Unresolved issues or follow-up work
- Ryo has #67 changes requested, #14, #44, and now #73. Watch his WIP and consider moving #44 away.
- The `_pushStrength` rename must be checked in PLAYER-003's PR (look for a stale value in `Player.prefab`).
- A bag-tearing ticket (chaos moment "sak pecah") does not exist yet and should follow the cement bag work.
