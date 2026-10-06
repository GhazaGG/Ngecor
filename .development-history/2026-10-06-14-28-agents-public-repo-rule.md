# AGENTS.md rule 13: the repo is public (no secrets, no paid assets)

## Task summary
The owner was worried about the repo traffic (1,160 clones from 426 unique cloners in 14 days, but only 7 unique visitors) and asked whether the repo should stay public. Private repos on GitHub Free lose rulesets and branch protection. The repo stays public, and a rule was added so that developers' AI tools never commit secrets or paid assets.

## Relevant previous context
- Ruleset "Protect main" (PR required, approval, code-owner review) only works on this personal-account repo while it is public, unless the owner has GitHub Pro.
- `AGENTS.md` rule 1 already forbids installing Asset Store packages that aren't recorded in DECISIONS. `docs/PROJECT_STRUCTURE.md` reserves `Assets/ThirdParty/` for external assets.
- PR #66 (open, not merged) adds rule 12, which forbids committing AI-generated plans.

## Changes made
- `AGENTS.md`: added hard rule 13. The repo is public; never commit secrets or credentials (API keys, tokens, passwords, UGS service accounts, `.env`) or paid or restricted-license assets. Stop and ask the lead if a task needs either.

## Files affected
- `AGENTS.md`, this report.

## Technical decisions
- Numbered 13, not 12, to avoid conflicting with PR #66. Main shows 11 → 13 until #66 merges.
- Stay public instead of going private on GitHub Free. Losing branch protection is riskier for this team (weaker AI tools that may push to `main`) than having prototype content visible.
- The traffic pattern (hundreds of unique cloners but only 7 visitors, starting the day the repo became active) points to automated cloning (secret scanners, indexers, crawlers) rather than interested people. This is an inference from GitHub's traffic graphs, not verified.

## Verification performed
- Scanned `origin/main` for secrets (api key, secret, token, password, private key, authorization). The only hits were empty Unity defaults (`ps4NPTitleSecret`, `metroCertificatePassword`).
- The Unity Cloud fields (`cloudProjectId`, `organizationId`) are empty, and `cloudEnabled` is 0.
- No `ThirdParty/` or Asset Store files, and 0 Git LFS files (bot clones don't use LFS bandwidth). The largest tracked file is about 88 KB.

## Final result
The repo stays public with ruleset protection, and AI tools are now told explicitly what must never be committed.

## Known limitations
- Commit author emails in history are public. Switching to a GitHub noreply email only affects new commits.
- Anything already public should be considered copied; making the repo private later does not undo existing clones.

## Unresolved issues or follow-up work
- Move to private with GitHub Pro (or Pro through the Student Developer Pack) before any paid asset, service credential, or commercially sensitive content is needed.
- Merge or close PR #66 so the rule numbering becomes continuous (11, 12, 13).
