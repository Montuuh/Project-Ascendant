<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b4816c83d2c74682cef77c -->
<!-- Exported: 2026-05-19T23:10:23.651Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🟢 In Progress (next deep-dive topic)


**Last Updated:** 2026-05-15 (scaffolded from BACKLOG; Trauma System design options pending lock-in)


**Cross-references:** Topic 1 (§1.6 starter unlocks, §1.7 difficulty modifiers), Topic 2 (§2.4.4 Trauma penalty hook), Topic 4 (§4.3.9 Bestiary tiers overlap).


---


# Scope


Persistent meta-progression. Trainer XP curve, Trainer Hub structure, starter unlock paths, relic-pool expansion unlocks, achievement system, optional difficulty modifiers (Ascension/heat-style), and the new **Trauma (permanent faint penalty)** system.


---


# Open Questions (must resolve to lock this topic)

1. **Trauma System lock-in:** Option A (Trauma Stacks, recommended), B (Diminishing Revival), C (Skill Suppression), D (drop entirely), or E (Hybrid A + soft cap).
2. **Trauma application timing:** Instant at faint moment, or only at Pokémon Center revival.
3. **Trauma clearing mechanism in-run:** Never / rare relic (Trauma Salve) / Move Tutor paid service.
4. **Mastery Move authoring scope:** Full ~30 lines, or curated subset for launch.
5. **Mastery Move upgrade timing on evolution:** Immediate, or after one combat.
6. **Topic 6 priority order:** Trainer XP & Hub / Starter unlocks / Relic pool expansion / Trauma integration / Difficulty modifiers / Achievement system. Pick top 3 for deep dive.

---


# Trauma System — Design Options (DESIGN PENDING)


Each time a Pokémon faints during a run, they accrue a permanent run-scoped penalty (cleared only at run end). This rewards conservative play, raises the stakes of risky Lead positions, and creates long-tail consequences that affect mid-to-late-run strategy.


## Option A — Trauma Stacks (recommended)

- Each faint applies one permanent "Trauma" stack to that Pokémon.
- Each stack reduces `MaxHP` by 5% (multiplicative against MaxHP, not stacking additively on the same base).
- Effective Max HP = `BaseMaxHP × (0.95 ^ TraumaStacks)`.
- Soft cap: maximum 5 stacks (-22.6% Max HP at cap). Faints beyond 5 do not stack further.
- Trauma is **visible** in the Map View as a badge on the Pokémon's portrait.
- **Pros:** Cleanest UI; doesn't break deck economy; encourages roster diversity.
- **Cons:** Punishes specific Pokémon, may discourage using high-investment carries late-run.

## Option B — Diminishing Revival

- Pokémon Centers revive fainted Pokémon to a reduced effective Max HP cap based on prior faint count: 100% → 90% → 75% → 60% → 50%.
- The reduced cap is the new ceiling — subsequent healing can fill back up to it but not beyond.
- **Pros:** Penalty hits only at Pokémon Center moments, preserving in-Region tempo. Easier to reason about.
- **Cons:** Less granular than Option A. Pokémon that never revisit a Pokémon Center are uncapped.

## Option C — Skill Suppression

- Each faint randomly locks one of the Pokémon's 4 move cards for the duration of the next combat the Pokémon participates in.
- The locked card is highlighted in the Map View; player sees which card will be unavailable.
- Can be cleared by specific consumables/relics (e.g., "Mind Mend").
- **Pros:** Most thematic ("the Pokémon is shaken"); deck-economy focused.
- **Cons:** Heaviest impact since deck variety is core. Discourages risk-taking too aggressively.

## Option E — Hybrid (Option A + soft cap)

- Trauma stacks at -5%/faint but caps at -25% (5 stacks), preventing infinite spiral. Equivalent to Option A with the cap made explicit.

**Mitigation paths (regardless of option):**

- Don't field the wounded Pokémon in risky fights (encourages Box management).
- Healing relics & Held Items (e.g., Leftovers) reduce faint frequency.
- Evolution raises base stats — Trauma is multiplicative on MaxHP, but evolution growth still nets positive.
- Optional: Trauma Salve relic clears 1 Trauma stack from a chosen Pokémon (rare late-run drop).

---


# Scaffolding Bullets (from Drive original, to be developed)

- Desbloquear nuevos iniciales (unlock new starters).
- Desbloquear nuevas reliquias (unlock new relics).

---


# Notes Pulled from BACKLOG

- §2.1.6 promises XP awards on failed runs but doesn't spec the curve.
- §1.6 promises 6 starters (3 default + 3 meta-unlocked) for the vertical slice — Topic 6 must define unlock conditions.
- §1.7 commits to a stackable difficulty-modifier system — architecture must be data-driven.
- Bestiary tiers (§4.3.9) are post-vertical-slice and partially overlap with this topic.
