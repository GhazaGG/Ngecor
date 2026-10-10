# Team Lead review of PR #93 (BUILD-001 scaffold impact threshold)

## Task summary
Reviewed DePo4l's PR #93 at head `c7dc12d`, a follow-up to the impact-threshold tuning note on PR #84. Posted CHANGES_REQUESTED.

## Relevant previous context
- PR #84 (merged): impact counted only for dynamic bodies on side contacts (`|normal.y| < 0.5`), ignored while wobbling, threshold 50 N·s. My approval note asked the owner to choose 50 (a walking push of a sack into a post collapses) or about 75 (only harder hits).
- Earlier reviews on #93 from the owner's account asked for a real player action that collapses the scaffold and a safe ordinary push.

## Changes made
- PR #93 review (CHANGES_REQUESTED):
  - Accepted the 75 N·s threshold and the author's numbers: a running throw at about 97 N·s collapses the scaffold, an ordinary push at about 56 N·s doesn't.
  - Must Fix: commit `c7dc12d` removed the side-contact filter and checks horizontal momentum for every contact, including objects landing on the deck. Because drops inherit the player's horizontal velocity (INT-005), a player carrying a 25 kg sack at about 3.5 m/s who drops it onto the platform while walking produces about 87 N·s, which triggers an uncancellable impact wobble and collapse. Deck landings are load measured by the probe, not impacts. Suggested restoring `|normal.y| >= 0.5 → continue` while keeping the horizontal-momentum check for side contacts.
  - Asked for tests: running throw into a side post collapses; a walking drop onto the deck doesn't; a ground-level W push into a post doesn't (not yet proven); Console clean.
  - Noted that the owner confirms 75 N·s during the feel review.

## Files affected
- This report only.

## Verification performed
- Read the PR diff (prefab threshold 50 → 75, script default 75, `OnCollisionEnter` logic) and the author's 2026-10-10 Play Mode report.
- `git merge-tree` against `main`: clean.
- Unity wasn't run. The deck-drop collapse is derived from the code and the INT-005 drop velocity rule.

## Final result
PR #93 needs the side-contact filter restored before approval.
