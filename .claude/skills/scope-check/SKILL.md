---
name: scope-check
description: >
  Check whether a proposed feature, task, or idea is in scope for the vertical
  slice of Project Ascendant. Use when you're not sure if something should be
  built now, when a new idea comes up mid-sprint, or when asked "is this in
  scope?", "should we build this now?", or "does this belong in the VS?". Gives
  a clear in/out verdict and explains why. Protects against scope creep.
---

# Scope Check — Project Ascendant

## Vertical Slice Definition (the only thing in scope)

Per GDD §1.6:
- Region 1 playable end-to-end
- 6 starter Pokémon options (3 default + 3 meta-unlocked)
- 8 recruitable Pokémon lines
- 15 relics
- 1 Gym Leader (one of the 4 Region-1-tier types: Rock, Water, Bug, Normal)
- All node types implemented (Combat, Recruitment, Utility, Gym)
- Working save/load

**Run length target:** ~90-100 minutes for a winning run.

## Out of Scope (explicitly post-VS)

- Regions 2 and 3
- Elite Four and Champion
- Meta-progression (Trainer Hub, XP, starter unlocks) — *architecture* in scope, *content* is not
- 50-relic pool (15 for VS; expand post-VS)
- Multi-gen Pokémon
- Multiplayer
- Daily seeds and leaderboards
- Bestiary tiers and Mastery Moves (can stub, not implement)
- Live-ops, community features

## Evaluation Protocol

For any proposed item, answer:

1. **Does it serve the VS definition?** Yes / No / Partially
2. **Is it a blocker for VS?** (Without it, VS is broken or unplayable)
3. **Is it infrastructure that enables VS?** (Architecture, not content)
4. **Is it gold-plating?** (Makes VS nicer but isn't required)

## Verdict Format

```
SCOPE VERDICT: [IN / OUT / DEFER]

Reason: [one sentence]

If IN: estimated impact on VS timeline.
If OUT: suggested backlog label (post-VS / post-launch / never).
If DEFER: specific condition that would move it IN scope.
```
