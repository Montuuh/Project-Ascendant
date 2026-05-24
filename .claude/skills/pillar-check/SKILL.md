---
name: pillar-check
description: >
  Quickly evaluate any proposal, feature, or implementation against Project
  Ascendant's 5 design pillars. Use when you want a fast yes/no/flag on whether
  something fits the game's design intent. Faster than a full balance-check.
  Use balance-check when you also need number evaluation.
---

# Pillar Check — Project Ascendant

Evaluate any proposal against the 5 design pillars.

## The 5 Pillars

1. **Telegraphed tactics over reactive RNG** — players can always plan; randomness lives in what options exist, not whether plans work.
2. **Every swap is a decision** — Lead-swapping has meaningful AP, defense, and tempo trade-offs.
3. **Synergy is sculpted, not drafted** — power comes from interactions between moves/evolutions/relics, never from one source.
4. **Identity through Evolution** — branching evolutions are the primary creative expression within a run.
5. **Cheerful core, regional flavor** — base tone is faithful-Pokémon; Regions add flavor without changing mechanics.

**Tiebreaker:** Pokémon thematic faithfulness loses to pacing and pillar integrity.

## Output Format

For each pillar, mark: ✅ aligned / ⚠️ tension (explain briefly) / ❌ violation

Any ❌: stop. Ask user: (a) override pillar for this proposal, (b) revise proposal, (c) revise pillar via Pillar Revision Protocol.

End with ONE recommendation: Proceed / Proceed with notes / Block.
