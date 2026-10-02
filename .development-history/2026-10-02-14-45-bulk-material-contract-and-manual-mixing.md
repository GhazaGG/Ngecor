# Bulk material contract, shovel, and manual mixing

## Task summary
Team Lead design-discussion session. Defined one shared contract for bulk material quantity, because four tickets with different owners move material amounts. The owner approved the contract, asked for a shovel that turns spills into piles, and made manual mixing with a shovel the base way to make concrete. The mixer becomes an M4 upgrade.

## Relevant previous context
- `2026-10-02-14-21-review-build-001-scaffolding-proposal.md` listed the bulk-quantity contract as the most urgent open design gap.
- Tickets involved: #16 MAT-002 (DePo4l), #17 MIX-001, #19 BUILD-002, #45 MIX-002 (all GhazaGG).
- The concept brief already lists "Mixer rusak → aduk manual" as soft failure.

## Changes made
- **`docs/DECISIONS.md`:**
  - New entry "2026-10-02 — Kontrak material bulk".
  - New summary row.
- **The contract:**
  - Material is stored as type plus an integer amount, in world containers only.
  - There's one shared container component.
  - Transfer happens only by pouring or by shovel scoop/dump, and units are conserved.
  - A tilted container spills, and the spill becomes a pile that merges with nearby piles of the same type.
  - A cement bag turns into N cement units.
  - The MVP recipe is cement + sand. Gravel and water come later as data.
  - Concrete freshness moves with the concrete, and a mixed batch takes the oldest age.
  - The sand pile is finite.
  - The wheelbarrow isn't a bulk container in the MVP.
  - In multiplayer, the container state belongs to the host.
- **New issues:**
  - #54 MAT-004 "Shovel and material piles": M2, P1, unassigned.
  - #55 MIX-003 "Manual concrete mixing with shovel": M2, P1, assigned to GhazaGG. Co-op mixing is counted per mix action.
- **Issue updates:**
  - #16 is rewritten as the first container implementation. The bucket is filled through the Inspector, so the ticket doesn't depend on the shovel. The developer must propose a pour binding and send an implementation plan before coding.
  - #17 is moved to M4. It's now framed as an upgrade with Solves/Creates, and is blocked by #55 and #16.
  - #19 and #45 are now blocked by #55 instead of the mixer.
  - #15 now blocks #55 instead of #17.
  - EPIC-003 #31 now lists #54 and #55 and notes that the mixer moved.

## Files affected
- `docs/DECISIONS.md`, this report. Everything else was GitHub issues.

## Technical decisions
- Amounts are integers rather than float volume, so conservation is exact and easy to test.
- Pouring is an explicit action, and spilling is passive when a container tilts. The current grab holds objects upright, so tilt control would be a new system.
- Spills become piles rather than disappearing (owner request). Merging piles by radius keeps the object count within the performance budget.
- With manual mixing as the base, M2's "Done When: membuat concrete" no longer depends on the mixer. That lightens M2, and the mixer gets a real Solves/Creates.

## Verification performed
- Re-read the edited issue bodies for blocker lines, milestones, and links to #54 and #55.
- The DECISIONS anchor `#2026-10-02--kontrak-material-bulk` follows the same slug pattern as the existing entries. It hasn't been rendered on GitHub yet.
- No Unity change.

## Final result
The contract is recorded in a docs PR, and the ticket graph now runs: #16 container → #54 shovel → #55 manual mix → #19 pour → #45 freshness. The mixer is in M4.

## Known limitations
- Issue links point to the DECISIONS anchor on `main`, which resolves only after this PR merges.
- The unit sizes (bag = N units, capacities, mix work) still need tuning in playtests.

## Unresolved issues or follow-up work
- #54 needs an owner at sprint planning. RyoFPS is the candidate after #8.
- #16 is a shared component built by a junior developer, so the lead must review its implementation plan before coding starts.
- Pour, scoop, and mix input bindings still need a decision (#16).
- Wheelbarrow as a bulk container: revisit after the M2 logistics playtest.
- Follow-up tickets: mixer jam (chaos #4) after #17/#45; cement on the ground ruined by rain later.
