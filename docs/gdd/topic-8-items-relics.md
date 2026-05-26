<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b48173bab9e5239b63f813 -->
<!-- Exported: 2026-05-26T16:28:42.376Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🔒 Locked


**Last Updated:** 2026-05-24 (locked — full Consumables, 50-relic catalog, Held Items system, TM list)


**Cross-references:** Topic 1 (§1.6 50 relics target), Topic 3 (§3.5 consumable rules), Topic 4 (§4.2.7 status cures, §4.5.1.1 rare relic drops), Topic 5 (§5.4.1 TMs, §5.5.4 Lead Aura), Topic 6 (§6.6 relic tier unlocks, §6.5 starter unlock relics), Topic 7 (§7.7 Region Shop, §7.8.2 City Shop).


---


# §8.1 Three-System Taxonomy


Project Ascendant has three distinct item categories. Boundaries are LOCKED here:


| System                    | Scope                                | Lifespan                     | Inventory Slot                            | Authoring      |
| ------------------------- | ------------------------------------ | ---------------------------- | ----------------------------------------- | -------------- |
| **Consumables** (§8.2)    | In-combat, per-Pokémon-or-team tools | Restored at combat end       | Inventory list, unlimited capacity        | `ConsumableSO` |
| **Trainer Relics** (§8.3) | Persistent run-state modifiers       | Until run end                | Inventory list, ~6-8 typical hold, no cap | `RelicSO`      |
| **Held Items** (§8.4)     | Per-Pokémon equipment, always-on     | Until run end OR re-equipped | One slot per Pokémon                      | `HeldItemSO`   |


**Boundary resolution for edge cases:**

- "Once-per-combat free swap" → **Relic** (passive effect, no draw cost, persists run-wide).
- "Single-use stat boost during combat" → **Consumable** (per-combat tool).
- "+10% Fire damage to one specific Pokémon" → **Held Item** (per-Pokémon scope).
- "TMs" → A **Consumable category** (used once from inventory, but never appears in the Consumable Pile during combat). See §8.5.

---


# §8.2 Consumables (24 launch consumables)


## §8.2.1 Consumable Rules (recap from §3.5)

- The Consumable Pile is built at combat start from the persistent inventory.
- 2 consumable cards are drawn per turn alongside skill cards.
- Each consumable can be used **once per combat**.
- At combat end, ALL consumables return to inventory.
- Consumables are upgradable through tiered chains (Potion → Super Potion → ...).

## §8.2.2 Healing Consumables


| Item             | Tier      | AP | Effect                                                                                                                                                                                         |
| ---------------- | --------- | -- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Potion**       | 1         | 1  | Restore 30 HP to one Pokémon                                                                                                                                                                   |
| **Super Potion** | 2         | 1  | Restore 60 HP to one Pokémon                                                                                                                                                                   |
| **Hyper Potion** | 3         | 1  | Restore 120 HP to one Pokémon                                                                                                                                                                  |
| **Max Potion**   | 4         | 1  | Fully restore one Pokémon to Effective Max HP                                                                                                                                                  |
| **Revive**       | (special) | 2  | **Revives a fainted Pokémon to 50% Effective Max HP.** Only consumable that grants in-combat revival (per §2.4.3 exception). 1 charge per inventory copy (purchased rare; Mystery Event drop). |


## §8.2.3 Status Cures


| Item              | AP | Effect                                              |
| ----------------- | -- | --------------------------------------------------- |
| **Antidote**      | 0  | Cures Poison on one Pokémon                         |
| **Burn Heal**     | 0  | Cures Burn                                          |
| **Paralyze Heal** | 0  | Cures Paralysis                                     |
| **Awakening**     | 0  | Cures Sleep                                         |
| **Ice Heal**      | 0  | Cures Freeze                                        |
| **Full Heal**     | 1  | Cures any primary status + Confusion on one Pokémon |


## §8.2.4 Combat Utility


| Item            | AP | Effect                                                   |
| --------------- | -- | -------------------------------------------------------- |
| **Ether**       | 1  | Gain +2 AP this turn                                     |
| **X Attack**    | 1  | +1 Attack stage on one Pokémon for this combat           |
| **X Defense**   | 1  | +1 Defense stage on one Pokémon for this combat          |
| **Guard Spec**  | 1  | Prevent next status condition application on one Pokémon |
| **Sharp Lens**  | 1  | +20% crit chance on all moves for this combat            |
| **Radar Scope** | 0  | Reveal all Unknown intents for this combat               |
| **Smoke Bomb**  | 1  | Skip next enemy intent (1 enemy in multi-enemy fights)   |
| **Card Pocket** | 0  | Retain up to 2 skill cards in hand for next turn         |
| **Quick Claw**  | 2  | This turn only: first card played costs 0 AP             |


## §8.2.5 Pokéballs (catching — see §7.3.4)


| Item         | Effect                                                                                                  |
| ------------ | ------------------------------------------------------------------------------------------------------- |
| **Pokéball** | Catch wild Pokémon at HP < 50% (or with any status). 50₽ at Shop; free copy granted on Wild Area entry. |


## §8.2.6 Consumable Upgrade Chains


Upgrade paths (a higher-tier item replaces the lower automatically when found, OR the player can upgrade explicitly at the City Shop):

- Potion → Super → Hyper → Max Potion.
- (No upgrade chain for status cures or utility items — each is its own item.)

---


# §8.3 Trainer Relics — 50 Launch Catalog


50 launch relics organized by rarity tier and synergy category. Tier reflects in-run drop weight (§6.6.2); Meta-tier (§6.6.1) governs which relics are in the pool for a given run's player.


## §8.3.1 Rarity Distribution


| Rarity   | Count | Drop Weight | Typical Source                                                 |
| -------- | ----- | ----------- | -------------------------------------------------------------- |
| Common   | 25    | 60%         | Trainer drops, Region Shops, Common Mystery Events             |
| Uncommon | 18    | 30%         | Elite drops, City Shops, Tradeoff Mystery Events               |
| Rare     | 7     | 10%         | Gym Leader drops, Victory Road Gauntlet, Gamble Mystery Events |


## §8.3.2 Synergy Categories


Five categories. Each relic is tagged with one primary + up to one secondary category. The City Shop curation algorithm (§7.8.2.1) uses these tags.

1. **Lead-Economy** — interacts with Lead position, swap counter, swap mechanics.
2. **Card-Economy** — draw, retention, cycling, hand-size modifiers.
3. **Combat** — damage, defense, crit, status, immunity.
4. **Meta-Acquisition** — XP, Poké Dollar, recruitment slots, Bestiary.
5. **Status** — applying, curing, prolonging, exploiting status conditions.

## §8.3.3 Common Relics (25)


| Relic                  | Category     | Effect                                                                                                                                   |
| ---------------------- | ------------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| **Smoke Ball**         | Combat       | First combat per Region: take −20% damage from first enemy attack                                                                        |
| **Quick Claw Charm**   | Card-Economy | Once per combat: replay the last skill card you played, free                                                                             |
| **Berry Pouch**        | Combat       | Healing consumables restore +20% HP                                                                                                      |
| **Soothe Bell**        | Combat       | Each Pokémon at full HP at turn start: +5% damage on next attack                                                                         |
| **Coin Pouch**         | Meta         | All Poké Dollar drops × 1.25                                                                                                             |
| **Soft Sand**          | Combat       | Ground-type moves +15% damage                                                                                                            |
| **Mystic Water**       | Combat       | Water-type moves +15% damage                                                                                                             |
| **Charcoal**           | Combat       | Fire-type moves +15% damage                                                                                                              |
| **Miracle Seed**       | Combat       | Grass-type moves +15% damage                                                                                                             |
| **Hard Stone**         | Combat       | Rock-type moves +15% damage                                                                                                              |
| **Sharp Beak**         | Combat       | Flying-type moves +15% damage                                                                                                            |
| **Magnet**             | Combat       | Electric-type moves +15% damage                                                                                                          |
| **Twisted Spoon**      | Combat       | Psychic-type moves +15% damage                                                                                                           |
| **Black Belt**         | Combat       | Fighting-type moves +15% damage                                                                                                          |
| **Pink Bow**           | Combat       | Normal-type moves +15% damage                                                                                                            |
| **Cleanse Tag**        | Status       | First status condition applied to your Pokémon per combat is blocked                                                                     |
| **Lucky Egg Token**    | Meta         | All in-run XP × 1.15                                                                                                                     |
| **Exp Share**          | Meta         | Box (non-Active-Team) Pokémon earn 50% of Active Team XP from each combat                                                                |
| **Defense Curl Charm** | Combat       | Every 3 manual swaps: +1 Defense stage on the new Lead                                                                                   |
| **Quick Draw**         | Card-Economy | First turn of each combat: draw +1 skill card                                                                                            |
| **Brave Charm**        | Combat       | Pokémon with HP < 50% deal +10% damage                                                                                                   |
| **Battle Hat**         | Card-Economy | At turn end, if you played 0 manual swaps: retain 1 skill card                                                                           |
| **Recycle Tag**        | Card-Economy | First time per combat the discard pile reshuffles: draw +1                                                                               |
| **Hiker's Coat**       | Combat       | Lead takes −10% damage from Cleave intents                                                                                               |
| **Wide Lens**          | Combat       | Moves with status riders: +15% application rate (deterministic equivalent: status now applies "before damage" instead of "after damage") |


## §8.3.4 Uncommon Relics (18)


| Relic                | Category     | Effect                                                                                                            |
| -------------------- | ------------ | ----------------------------------------------------------------------------------------------------------------- |
| **Choice Specs**     | Card-Economy | First Ranged move each turn costs 0 AP; subsequent Ranged moves cost +1                                           |
| **Choice Band**      | Card-Economy | First Melee move each turn costs 0 AP; subsequent Melee moves cost +1                                             |
| **Move Echo**        | Card-Economy | Playing 3 different moves from the same Pokémon in one turn: gain +2 AP next turn                                 |
| **Type Resonance**   | Combat       | Active Team members sharing a primary type buff each other's matching moves by +10% per shared member             |
| **Adrenal Surge**    | Combat       | When a Pokémon faints, all remaining Active Team Pokémon: +1 Attack stage                                         |
| **Reactor Core**     | Card-Economy | Max hand size increased by 1 skill card                                                                           |
| **Hand-Off Pouch**   | Card-Economy | At turn start, you may discard 1 card to draw 1                                                                   |
| **Cycle Cell**       | Card-Economy | When skill deck reshuffles, gain +1 AP next turn                                                                  |
| **Status Lance**     | Status       | Status conditions applied by player: last +1 turn (Paralysis 4 turns, Confusion 4 turns; Sleep/Freeze unaffected) |
| **Pressure Plate**   | Combat       | When a 3+ AP move is played, next move that turn costs −1 AP                                                      |
| **Vital Pendant**    | Combat       | Once per combat at HP < 25%: heal to 50% Effective Max HP                                                         |
| **Trauma Salve**     | Meta         | Single-charge: remove all Trauma stacks from one chosen Pokémon. Consumed on use. (See §6.2.4.)                   |
| **Tactician's Coin** | Combat       | First manual swap each combat costs 0 AP (in addition to draw discount per §3.3.1)                                |
| **Steady Aim**       | Combat       | Crit damage multiplier: 1.5× → 1.75×                                                                              |
| **Lure Module**      | Meta         | Wild Pokémon Areas offer +1 species choice (4 instead of 3)                                                       |
| **Battle Tracker**   | Meta         | After defeating an enemy: log type matchup data; +5% Witnessed reveal rate on similar species this run            |
| **Healer's Kit**     | Status       | Status cures restore +15 HP to the cured Pokémon                                                                  |
| **Bond Bracelet**    | Combat       | When the Lead Pokémon's HP drops below 50% for the first time, all bench Pokémon: +1 Defense stage                |


## §8.3.5 Rare Relics (7)


| Relic                 | Category     | Effect                                                                                                                                 |
| --------------------- | ------------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| **Master Ball Charm** | Meta         | Once per run: convert any Pokéball use into a guaranteed catch (overrides HP threshold)                                                |
| **Champion's Crest**  | Combat       | Whenever a Pokémon defeats an enemy (kill credit): +5% damage permanently for that Pokémon, this run. Max +25% per Pokémon.            |
| **Time Spinner**      | Combat       | Once per combat: skip the entire enemy turn (Resolution Phase fires no enemy actions)                                                  |
| **Phoenix Feather**   | Combat       | Once per run: when an Active Team Pokémon would faint, prevent it; restore them to 1 HP. Consumes the relic.                           |
| **Sage's Tome**       | Card-Economy | Max hand size +2 skill cards; max AP per turn +1                                                                                       |
| **Crown of Echoes**   | Combat       | The first move each combat is added as a free copy in hand on turn 2                                                                   |
| **Soul Link**         | Combat       | Choose one Pokémon at relic acquisition. That Pokémon and its Lead-position partner gain +10% damage when both are alive. Permanently. |


## §8.3.6 Relic Anti-Synergy & Conflict Resolution


Some relics have explicit interactions. Resolution rules:

- **Choice Band vs Choice Specs:** Independent. A player can hold both. Each operates on its own AP discount cycle (Melee vs Ranged), but the discount only triggers on the first move of that type per turn.
- **Phoenix Feather vs Sturdy/Last Stand Boon:** Resolution order — Sturdy → Last Stand → Phoenix Feather. Phoenix Feather is the last fallback; the others fire first (per Pokémon-specific designation).
- **Trauma Salve vs Hub upgrade "Trauma Salve Cache":** Independent. The Hub upgrade guarantees one in the City 1 shop; the player can still find/buy more.
- **Type-boost relics (Soft Sand, Mystic Water, etc.):** Stacking with field effects (§4.3.8) and Held Items multiplies normally — each is a multiplicative term in the damage formula.

---


# §8.4 Held Items (18 launch Held Items)


## §8.4.1 Held Item Rules — LOCKED

- **Slot count:** **One** Held Item per Pokémon. (Resolved from open question.)
- **Equip mechanic:** Drag-and-drop from inventory to Pokémon portrait in the Map View. The previously equipped item returns to inventory.
- **Persistence:** Held Items persist across combats, nodes, and Regions until manually un-equipped or until run end.
- **Box transfer:** A Pokémon released from the Box drops its Held Item to inventory (no loss).
- **Mid-combat changes:** None. Held Item changes are Map View only.
- **Acquisition sources:** Trainer Battle 20% drop rate, Elite Trainer guaranteed slot, City Shop slot 7, Mystery Event rewards. NOT in standard wild loot pool.

## §8.4.2 Type-Boost Held Items (8)


| Item              | Effect                                   | Acquisition              |
| ----------------- | ---------------------------------------- | ------------------------ |
| **Charcoal**      | Wearer's Fire-type moves +20% damage     | Trainer drops, City Shop |
| **Mystic Water**  | Wearer's Water-type moves +20% damage    | (same)                   |
| **Magnet**        | Wearer's Electric-type moves +20% damage | (same)                   |
| **Miracle Seed**  | Wearer's Grass-type moves +20% damage    | (same)                   |
| **NeverMeltIce**  | Wearer's Ice-type moves +20% damage      | (same)                   |
| **Black Belt**    | Wearer's Fighting-type moves +20% damage | (same)                   |
| **Sharp Beak**    | Wearer's Flying-type moves +20% damage   | (same)                   |
| **Twisted Spoon** | Wearer's Psychic-type moves +20% damage  | (same)                   |


_Note: Same names as the Common relic equivalents at §8.3.3. The two are distinct: the relic version applies to the whole party, the Held Item version applies to only the wearer at a stronger multiplier (15% relic vs 20% item). Held Item is more focused._


## §8.4.3 Type Plates — Lead Aura Source (5)


Per §5.5.4, Type Plates are the canonical Lead Aura source via Held Items.


| Item             | Effect                                                                                | Acquisition                                 |
| ---------------- | ------------------------------------------------------------------------------------- | ------------------------------------------- |
| **Splash Plate** | Wearer grants Water Lead Aura: while wearer is Lead, all bench Water moves +5% damage | Rare drop, Mystery Event "Mysterious Stone" |
| **Flame Plate**  | Wearer grants Fire Lead Aura                                                          | (same)                                      |
| **Zap Plate**    | Wearer grants Electric Lead Aura                                                      | (same)                                      |
| **Meadow Plate** | Wearer grants Grass Lead Aura                                                         | (same)                                      |
| **Mind Plate**   | Wearer grants Psychic Lead Aura                                                       | (same)                                      |


## §8.4.4 Sustain & Defensive Held Items (3)


| Item           | Effect                                                                                 |
| -------------- | -------------------------------------------------------------------------------------- |
| **Leftovers**  | Wearer restores `floor(MaxHP/16)` (min 1) HP at end of each Resolution Phase           |
| **Eviolite**   | If wearer is NOT fully evolved: +20% Defense                                           |
| **Focus Sash** | Once per combat: survive a lethal hit at 1 HP. Consumed on use. Re-arms at combat end. |


## §8.4.5 Tempo Held Items (2)


| Item             | Effect                                                                                |
| ---------------- | ------------------------------------------------------------------------------------- |
| **Choice Band**  | Wearer's Melee moves +25% damage; wearer's Ranged moves are unplayable while equipped |
| **Choice Scarf** | Wearer's moves cost −1 AP (min 0); only one of wearer's moves can be played per turn  |


## §8.4.6 Held Item Acquisition Rules

- Held Items are TIER-AWARE: the launch pool is in Tier 1 (meta-unlocked from start). Future tiers may add Type Plates of additional types and exotic items.
- The **Lure Module relic** (§8.3.4) does NOT affect Held Item drop weight.
- Each Trainer drop's 20% Held Item slot can roll any of the 18 launch items uniformly, unless biased by the City curation algorithm.

---


# §8.5 TMs (Technical Machines) — 15 launch TMs


TMs are a Consumable-class item but never enter the in-combat Consumable Pile. They are applied from the Map View per §5.4.1.


| TM # | Move                                   | Type         | Power | AP | Compatible Pokémon (sample)                                        |
| ---- | -------------------------------------- | ------------ | ----- | -- | ------------------------------------------------------------------ |
| 01   | Mega Punch                             | Fighting     | 80    | 2  | Mankey, Machop, Hitmonchan, Snorlax, generalist                    |
| 02   | Ice Beam                               | Ice          | 90    | 3  | Squirtle line, Lapras, Seel, Articuno                              |
| 03   | Thunderbolt                            | Electric     | 90    | 3  | Pikachu, Magnemite, Electabuzz, Eevee (Jolteon path)               |
| 04   | Flamethrower                           | Fire         | 90    | 3  | Charmander line, Vulpix, Growlithe, Eevee (Flareon path)           |
| 05   | Surf                                   | Water        | 80    | 3  | Most Water-types                                                   |
| 06   | Psychic                                | Psychic      | 90    | 3  | Abra line, Drowzee, Mr. Mime, Mewtwo                               |
| 07   | Earthquake                             | Ground       | 100   | 3  | Diglett line, Sandshrew line, Geodude line                         |
| 08   | Solar Beam                             | Grass        | 120   | 4  | Grass starters, Oddish line, Bellsprout line (Ultimate-tier; 4 AP) |
| 09   | Hyper Beam                             | Normal       | 150   | 4  | Most Normal-types (Ultimate-tier)                                  |
| 10   | Toxic                                  | Poison       | —     | 1  | Poison-types (applies Poison; no damage)                           |
| 11   | Body Slam                              | Normal       | 85    | 2  | Generalist; very broad compatibility                               |
| 12   | Iron Tail                              | Steel/Normal | 80    | 2  | Tail-bearing Pokémon (broad)                                       |
| 13   | Roar of Time-equivalent / Dragon Pulse | Dragon       | 85    | 3  | Dragonair line, Charizard, post-VS variants                        |
| 14   | Shadow Ball                            | Ghost        | 80    | 2  | Gastly line, Drowzee line (Psy/Ghost overlap)                      |
| 15   | Foresight (Utility)                    | Normal       | —     | 0  | Most Pokémon (reveals Unknown intents this turn — see §5.5.3.2)    |


**TM acquisition:** Found in Region Shop slot 8 (City Shop slot 8 special) at 250–500₽; Trainer Battle 5% drop slot; specific Mystery Event rewards. Mastery Moves cannot be replaced by TMs per §4.3.9.2.


**Compatibility:** Each TM ScriptableObject ships with `CompatibleSpecies[]`. UI greys-out incompatible options. Mastery Moves slot is exempt.


---


# §8.6 Inventory UI (combat & Map View)

- **Map View inventory tab:** Three sections — Consumables, Held Items (with portrait pairing for equipped items), Relics. Plus Trainer Tokens, Poké Dollars, Pokéballs.
- **In-combat consumable display:** The 2-card Consumable hand per §3.5; "view full inventory" is not available mid-combat (atomic combat rule). The player must enter combat with the inventory they intended.
- **Sort options:** By name, rarity, category. Filter by "Equipped only", "Tradeable" (Sellable at City Shop).

---


# §8.7 Architecture & ScriptableObject Schema


Light spec; Topic 9 owns full architecture.


```javascript
ConsumableSO:
    Id : string
    DisplayName : string
    Icon : Sprite
    APCost : int
    Effect : ConsumableEffect (polymorphic; HealEffect, StatusCureEffect, etc.)
    Tier : int
    UpgradeTo : ConsumableSO (nullable)

RelicSO:
    Id : string
    DisplayName : string
    Icon : Sprite
    Rarity : RarityTier
    MetaTier : int  -- Tier 1/2/3 unlock status (§6.6.1)
    Categories : List<SynergyCategory>
    OnAcquireHook : ScriptableHook
    OnEvent : Dictionary<EventBusChannel, ScriptableHook>

HeldItemSO:
    Id : string
    DisplayName : string
    Icon : Sprite
    GrantsLeadAura : Type? (per §5.5.4)
    OnEquipHook : ScriptableHook
    OnEvent : Dictionary<EventBusChannel, ScriptableHook>

TMSO:
    Id : string
    MoveTeach : MoveSO
    CompatibleSpecies : List<PokemonSpeciesSO>
```


`ScriptableHook` is an event-driven, data-bound hook that consumes context (PokemonInstance, DamageContext, etc.) and returns an effect. Detailed in Topic 9.


---


# §8.8 Vertical Slice Carve-Out


| System                  | In VS                                        | Out of VS                                   |
| ----------------------- | -------------------------------------------- | ------------------------------------------- |
| Consumables             | ✅ All 24                                     | —                                           |
| Trainer Relics (Tier 1) | ✅ Common (25) + Uncommon (~10)               | Full Uncommon roster, Rare relics, Tier 2/3 |
| Held Items              | ✅ 8 type-boost + Leftovers + Focus Sash      | Type Plates, Eviolite, Choice items         |
| TMs                     | ✅ 6 TMs (TM01, TM02, TM05, TM10, TM11, TM15) | Full 15-TM roster                           |
| Inventory UI            | ✅ Full                                       | —                                           |


---


# §8.9 Open Items Resolved (was Sev-2)

- **§2.1.2.1 Held Items subsystem** (BACKLOG gap #10): ✅ Resolved (§8.4).
- **Three-way taxonomy boundaries** (Topic 8 open Q1): ✅ Resolved (§8.1).
- **Held Item slot count per Pokémon** (Topic 8 open Q2): ✅ Resolved — 1 slot (§8.4.1).
- **TM relationship** (Topic 8 open Q3): ✅ Resolved — TM is a Consumable subclass with Map-View-only application; not in combat Consumable Pile.
- **Relic rarity curve** (Topic 8 open Q4): ✅ Resolved (§8.3.1).
- **Starting Relic pool curation** (Topic 8 open Q5): ✅ Resolved — Common-and-Uncommon only, never Rare (per §6.6.3, reaffirmed here).
