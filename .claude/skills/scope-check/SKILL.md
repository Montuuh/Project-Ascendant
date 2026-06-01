---
name: scope-check
description: >
  Check whether a proposed feature, task, or idea is in scope for the vertical
  slice of Project Ascendant. Use when you're not sure if something should be
  built now, when a new idea comes up mid-sprint, or when asked "is this in
  scope?", "should we build this now?", or "does this belong in the VS?". Gives
  a clear in/out verdict. Canonical VS definition is in .claude/docs/vertical-slice.md.
---

# Scope Check — Project Ascendant

## Canonical VS Definition

**Always read:** `.claude/docs/vertical-slice.md` (supersedes older §1.6 summaries).

Region 1 end-to-end includes:
- 3 starters (Bulbasaur/Charmander/Squirtle, **Vanguard branches only**)
- 3 wild recruitable lines (Caterpie, Pidgey, Geodude — all 3 stages each)
- 6 node types (Wild, Trainer, Center, Shop, Mystery, Gym)
- 15 relics, 10 consumables, 5 held items, 3 TMs
- 4 trainer archetypes, 1 Elite, R1 Gym Leader 3-phase boss
- Full Trauma System, Hub stub (2 kiosks), 3 difficulty modifiers, ~10 achievements
- All 6 status conditions, GameRNG determinism, save/load between every node
- Accessibility tier (colorblind, reduced motion, rebinding, subtitles)

**Run length target:** ~90–100 minutes for a winning run.

## Explicitly Out of Scope (post-VS)

Regions 2/3, Victory Road, League, Champion, Specialist/Support branches,
Tier 2/3 relic meta-unlocks, Bestiary Veteran/Master tiers, meta-unlocked starters,
full Mystery/relic/held-item catalogs, multi-profile saves, localization beyond en-US,
mobile portability, daily seeds/leaderboards.

**Note:** Meta-progression **systems** (Trainer XP, Trauma, Hub stub) **are IN** VS;
expanding **content pools** beyond VS numbers is OUT.

## Evaluation Protocol

For any proposed item:

1. **Serves VS definition?** Yes / No / Partially
2. **Blocker?** Without it, VS is broken or unplayable
3. **Infrastructure?** Architecture enabling VS (not content expansion)
4. **Gold-plating?** Nice-to-have but not required for VS ship

## Verdict Format

```
SCOPE VERDICT: [IN / OUT / DEFER]

Reason: [one sentence]

If IN: estimated impact + owning Epic.
If OUT: backlog label (post-VS / post-launch).
If DEFER: condition that moves it IN scope.
```

## Epic Phase Hint

| Phase | Epics | Status (2026-06) |
|-------|-------|-------------------|
| A–C | 1–12 | Complete or near-complete |
| D | 13–17 | **Current sprint candidates** |

When unsure, flag to `producer` and read Notion Epic page before building.
