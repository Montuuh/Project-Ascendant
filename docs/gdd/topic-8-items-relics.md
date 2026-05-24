<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b48173bab9e5239b63f813 -->
<!-- Exported: 2026-05-19T23:10:24.469Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🟡 Pending (high-priority gap)


**Last Updated:** 2026-05-15 (scaffolded from BACKLOG; Held Items concept seed added)


**Cross-references:** Topic 1 (§1.6 50 relics target), Topic 3 (§3.5 consumable rules), Topic 4 (§4.2.7 status-curing consumables, §4.5.1.1 rare relic drops), Topic 5 (§5.4.1 TMs, §5.5.4 Lead Aura via Held Items).


---


# Scope — Three Subsystems


## 8.1 Consumables


In-combat tool inventory. Tiered upgrade chains (Potion → Super Potion → Hyper Potion → Max Potion, etc.). ~20-25 launch consumables. Includes status cures (Antidote, Burn Heal, Ice Heal, Awakening, Paralyze Heal, Full Heal), HP restoration, AP grants, intent revealers (Radar Scope), card retention tools, etc. Per §3.5: consumables are not expendable — they return to inventory at combat end.


## 8.2 Trainer Relics


Persistent run-state modifiers. ~50 launch relics target. Rarity tiers (Common / Uncommon / Rare). Synergy categories: Lead-economy (interact with swap counter, AP), Card-economy (draw, retention, cycling), Combat (damage, defense, status, crit), Meta-acquisition (XP, gold, recruitment slots). Starting Relic offered at run start (§2.1.1).


## 8.3 Held Items — Concept Seed


Held Items are equipment attached to individual Pokémon, distinct from Consumables (per-combat tools) and Trainer Relics (run-wide passive modifiers). Each Pokémon can equip **one** Held Item at a time. Held Items provide always-on, Pokémon-scoped passive effects that pair with the Pokémon's species, type, or evolution branch.


**Design intent:** Held Items are the **per-Pokémon expression layer** of build identity, sitting between move kits (defined by species + evolution) and Trainer Relics (defined by run-state). A player who finds a Choice Band early can build their run around a melee-Vanguard Lead; a player who finds Leftovers can sustain a Support-archetype tank.


**Acquisition:** Held Items drop from Trainer Battles, Elite encounters, specific Mystery Event nodes, and curated Shops. They are NOT in the standard wild-encounter loot pool (would dilute the consumable economy).


**Swapping & removal:** Held Items can be re-equipped freely between combats via the Map View. Removing a Held Item returns it to the inventory; replacing one swaps the equipped item out.


**Launch Held Item families (concept list, not finalized):**

- **Type-boost items:** Charcoal (+10% Fire-move damage), Mystic Water (+10% Water), Magnet (+10% Electric), etc.
- **Sustain items:** Leftovers (regen `floor(MaxHP/16)` per turn), Big Root (healing moves heal +30% on this Pokémon).
- **Tempo items:** Choice Band (Melee moves +25% damage but disable all Ranged moves on this Pokémon), Choice Scarf (this Pokémon's moves cost -1 AP but only one move can be played per turn from them).
- **Defensive items:** Eviolite (+20% Defense if not fully evolved), Focus Sash (survive one lethal hit at 1 HP per combat).
- **Synergy items:** Light Clay (field effects last +2 turns), **Type Plates** (grant Lead Aura of matching type — see §5.5.4 for Lead Aura mechanics).

---


# Open Questions

1. Three-way taxonomy boundaries: where does a borderline mechanic land (e.g., "once-per-combat free swap" — relic, held item, or consumable)?
2. Held Item slot count per Pokémon: 1 (recommended) or 2?
3. TM relationship: TMs are technically consumables per §5.4.1 — should they be a separate inventory category in Topic 8 or sit under Consumables (8.1)?
4. Relic rarity curve: drop weights per tier; relic-pool meta-expansion gating (Topic 6 dependency).
5. Starting Relic pool: how curated for new-player onboarding vs experienced-player variety?

---


# Drive Original Content


_The Drive document had only the placeholder_ _`asd`_ _here. Full content authoring begins when this topic is unblocked._

