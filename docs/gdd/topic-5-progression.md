<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b4813ea29ae0c992898d01 -->
<!-- Exported: 2026-05-19T23:10:23.122Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🔒 Locked


**Last Updated:** 2026-05-15 (migrated from Drive; §5.5.4 Lead Aura section added)


**Cross-references:** Topic 3 (Lead mechanic, move modifiers), Topic 4 (Mastery Moves, Branch synergy with Boons/Badges), Topic 6 (meta-progression), Topic 8 (Held Items — Lead Aura source).


---


# §5.1 Overview


Progression in Project Ascendant operates on two levels:

- **Within a run:** Pokémon gain XP, level up, and evolve. Evolution is the primary deck-manipulation event — a permanent, irreversible choice that changes a Pokémon's move cards and identity for the rest of the run. TMs and Move Tutors provide secondary mid-run customization. Passive abilities granted by evolution add a third layer of build identity.
- **Across runs:** Trainer XP, meta-unlocks, and Bestiary mastery (§4.3.9) accumulate persistently. Defined in Topic 6 (Roguelike Progression).

---


# §5.2 XP and Leveling


## §5.2.1 XP sources

- All Pokémon in the Active Team that participated in a combat earn XP at combat end.
- XP amount scales with enemy difficulty tier (wild encounter < trainer < elite trainer < Gym Leader < Elite Four < Champion).
- Pokémon in the Box but not in the Active Team earn no XP from that combat — team selection has a progression consequence.

## §5.2.2 Level-up timing

- Level-ups are processed between nodes, not mid-combat.
- When a Pokémon reaches its level-up threshold after a combat, the Map View displays a "Level Up" notification. Stat increases are applied immediately.
- If a Pokémon reaches an evolution-eligible level, the evolution prompt appears in the Map View — player chooses when to trigger it (see §5.3).

## §5.2.3 Stat growth on level-up

- Each level-up increases Attack, Defense, and HP by flat amounts derived from the species' growth curve (ScriptableObject).
- Growth curves are custom-tuned for the roguelike context — not Gen I stat tables.
- The damage formula does not include level directly; stat growth is the mechanism by which level translates to combat power.

## §5.2.4 Target level arc

- Pokémon reach first evolution threshold around end of Region 1 / start of Region 2.
- Pokémon reach second (final) evolution threshold around end of Region 2 / start of Region 3.
- Single-stage Pokémon (no evolution) receive enhanced stat growth per level to compensate.

---


# §5.3 Branching Evolutions


Evolution is not a stat buff — it is a deck-manipulation event that permanently rewrites a Pokémon's move cards and identity.


## §5.3.1 Evolution Trigger

- Evolution becomes available when a Pokémon reaches its level threshold.
- The player sees a "Ready to Evolve" indicator on the Pokémon in the Map View.
- Evolution is **player-initiated** — triggered by the player from the Map View between nodes. The player may delay evolution for tactical reasons (preserving current move synergies, waiting for a better moment).
- Once triggered, the Branch Selection screen opens (see §5.3.3). This choice is permanent and irreversible.
- Evolution can also be triggered immediately via the Early Evolution option at a Victory Road Training Grounds node.

## §5.3.2 Evolution Items (selected Pokémon only)


Certain Pokémon (primarily Eevee-line and stone-based evolutions) require both a level threshold and a specific Evolution Item to access particular branches. Items are found at shops, in drops, or through specific event nodes.

- Standard 2-stage evolutions never require items — level threshold alone is sufficient.
- Items unlock **additional branches** not available through level-only evolution (e.g., a Fire Stone makes the "Flareon path" available for Eevee alongside the default branches).
- Items do not block standard evolution — they only add options.

## §5.3.3 Branch Selection


When evolution is triggered, the player is presented with the Branch Selection screen:

- Each branch is displayed with a clear identity label (e.g., "Vanguard," "Specialist") and a before/after comparison of every move that changes.
- Moves that are **upgraded** show the base move → evolved move transition with diff highlights (changed Power, new modifiers, new status riders).
- Moves that are **retained** are shown unchanged.
- Moves that are **added** are shown as new additions.
- The branch's passive ability reward (if any) is displayed.
- The player confirms their choice — the choice is permanent, with a confirmation dialogue ("This cannot be undone").

**Branch count:**

- **2 branches:** standard for most Pokémon.
- **3 branches:** reserved for starter Pokémon and high-rarity species, reflecting their greater mechanical depth.

## §5.3.4 Branch Archetypes


Three branch archetypes serve as design guidelines — not rigid rules. Each Pokémon's branches are themed to the species' identity, drawing from these archetypes where appropriate:


| Archetype      | Move Tendency                                         | Modifier Tendency                                               | Passive Reward                                                                    |
| -------------- | ----------------------------------------------------- | --------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| **Vanguard**   | Melee-forward; high Power offensive moves             | Step-Forward and Step-Backward modifiers introduced or expanded | Crit chance passive (flat % added to PokemonInstance.CritChance)                  |
| **Specialist** | Ranged-forward; balanced offensive with status riders | Status applier effects on offensive moves                       | Species-specific passive (e.g., Torrent, Blaze, Overgrow — type-boost at low HP)  |
| **Support**    | Defensive and Utility-heavy                           | Healing effects, shield riders, stat-stage modifiers            | Team-sustain passive (e.g., "Healer: restores 3 HP to a random ally at turn end") |


No Pokémon is required to have all three archetypes — a naturally physical species (Machop) may have only Vanguard and Specialist; a naturally supportive species (Chansey) may have only Specialist and Support. Archetype assignment should feel thematically faithful to the species.


## §5.3.5 What Changes on Evolution


Evolution upgrades 1–2 of the 4 base moves, retains 1–2 unchanged, and optionally adds 1 new move (replacing a retained base move). The specific changes depend on the chosen branch.


**Evolution stage 1 (mid-form) — typical changes:**

- 1–2 moves upgraded (higher Power, added effect modifiers or status riders).
- 1–2 moves retained unchanged.
- 0–1 new moves added (branch-dependent).
- 0–1 SF/SB modifiers introduced (Vanguard branch).

**Evolution stage 2 (final form) — typical changes:**

- 1–2 moves upgraded further from mid-form versions.
- 1 signature move added (unique to this final form and branch — strongest move in the kit).
- 1–2 SF/SB modifiers likely present (especially Vanguard branch).
- Branch-specific secondary passive ability granted (see §5.5).

**Final evolution continues the first branch's direction** — choosing Vanguard at stage 1 locks the Pokémon into Vanguard-archetype upgrades at stage 2, with one sub-choice available (e.g., "Heavy Brawler Blastoise" vs "Aqua-Jet Duelist Blastoise" within the Vanguard path).


## §5.3.6 Move-Set Construction Rules


Every Pokémon's 4-move kit is designed according to these guidelines, ensuring both kit coherence and tactical variety across the team:


### §5.3.6.1 Pre-evolution kit template

- 1–2 Offensive moves (mix of Melee and Ranged, species-dependent).
- 1 Defensive or Utility move.
- 1 Utility or Offensive move (species-dependent).
- 0 SF/SB modifiers (or very rarely 1 for thematically appropriate species).
- AP cost range: 0–2.

### §5.3.6.2 Mid-evolution kit template

- 1–2 Offensive moves (at least one upgraded from base; branch-dependent direction).
- 1 Defensive or Utility move (possibly upgraded).
- 1 additional move (new or retained, branch-dependent).
- 0–1 SF/SB modifiers (Vanguard branch introduces 1 here).
- AP cost range: 1–3.

### §5.3.6.3 Final evolution kit template

- 2 Offensive moves (at least one high-Power; one may be ultimate-eligible at 3–4 AP).
- 1 Defensive or Utility move.
- 1 signature move (branch-unique, strongest in kit).
- 1–2 SF/SB modifiers (Vanguard branch typically has 2; others have 1 or 0).
- AP cost range: 1–4 (full range; 4-AP ultimate eligible with setup).

---


# §5.4 TM & Move Tutor System


Two systems provide mid-run move customization outside of evolution.


## §5.4.1 TMs (Technical Machines)

- **What they are:** consumable items found in shops, combat drops, or specific event nodes.
- **What they do:** teach a specific named move to a compatible Pokémon, **permanently replacing one of their 4 current moves** for the rest of the run. **Mastery Moves are excluded** — see §4.3.9.2.
- **Compatibility:** each TM move has a `CompatibleSpecies[]` list in its ScriptableObject. A TM cannot be used on an incompatible Pokémon — the UI greys out incompatible options.
- **Usage:** applied from the inventory in the Map View. The player selects the TM → selects a compatible Pokémon → selects which of the 4 current moves to replace. Permanent, confirmed with a dialogue.
- **Single-use:** the TM item is consumed on use. It does not persist after use.
- **Design intent:** TMs create a secondary customization axis — the "happy accident" discovery moment. Finding a Thunderbolt TM compatible with Clefairy opens a build path the player didn't anticipate.

## §5.4.2 Move Tutors

- **What they are:** a service available at specific nodes — Pokémon Center (City nodes) and Training Grounds (Victory Road).
- **What they do:** the Tutor offers a curated list of learnable moves for each of the player's Pokémon. The player selects one Pokémon and one move to learn, replacing one of the Pokémon's 4 current moves.
- **Compatibility:** each Pokémon species has a `TutorLearnset[]` in their ScriptableObject — the moves available to them from tutors. Distinct from TM compatibility (tutors teach different moves than TMs).
- **Cost:** free as part of the node offering (City nodes; Training Grounds). May cost Poké Dollars at shop-style tutors.
- **Repeatable:** the player can use the Move Tutor service at every City they visit — but they only pass through 2 Cities per run, plus Victory Road's Training Grounds.
> ⚠️ **Open Sev-3 gap:** Tutor Learnset evolution updates needed. See BACKLOG gap #14.

---


# §5.5 Ability System


Pokémon in Project Ascendant may have passive abilities — always-on effects that modify combat behavior without costing AP or occupying a card slot.


## §5.5.1 Ability Assignment

- **Pre-evolution:** no passive ability (or a very simple one — e.g., "Swift Swim: draw 1 extra card on the first turn of rain-active combats").
- **First evolution:** the Pokémon gains its primary passive ability — a species-defining trait that reflects the Pokémon's identity.
- **Final evolution:** the Pokémon gains a secondary passive ability determined by the chosen evolution branch — reinforcing the branch's tactical identity.

## §5.5.2 Ability Categories


| Category               | Description                                              | Example                                                            |
| ---------------------- | -------------------------------------------------------- | ------------------------------------------------------------------ |
| **Combat passive**     | Modifies damage, AP, or card draw in specific conditions | Torrent: Water moves deal +20% damage when HP < 30%                |
| **Vision passive**     | Reveals enemy information                                | Keen Eye: all Unknown intents revealed at combat start             |
| **Positional passive** | Modifies Lead/swap behavior                              | Intimidate: on entering Lead, lower all enemy Attack by one stage  |
| **Type passive**       | Immunity or enhanced interaction with a specific type    | Levitate: immune to Ground-type moves and Electric Terrain effects |
| **Survival passive**   | Modifies faint/damage threshold behavior                 | Sturdy: survive one lethal hit at 1 HP per combat                  |
| **Aura passive**       | Grants a Lead Aura while this Pokémon is Lead            | (See §5.5.4)                                                       |


## §5.5.3 Key Ability Designs


### §5.5.3.1 Keen Eye (Vision passive — combat-start intent reveal)

- At the start of any combat where this Pokémon is in the Active Team, all Unknown intents for that combat are permanently revealed.
- Combines with Soul Badge (first-2-turns reveal) and Radar Scope consumable for a full vision build.
- Available on: Noctowl line, Pidgeot line, Starmie line (examples — species list defined in content pass).

### §5.5.3.2 Foresight (Utility move card — not a passive)

- A separate design from Keen Eye — Foresight is a Utility move card (0 AP, single-use per combat per draw) that reveals all Unknown intents for the current turn only.
- Available on: specific Pokémon learnsets (e.g., Eevee, Drowzee, Natu line).
- Note: Foresight-as-move and Keen Eye-as-ability serve different build axes — Foresight is active (costs a card draw and play), Keen Eye is passive (always on).

### §5.5.3.3 Levitate (Type passive)

- The Pokémon is treated as non-grounded — immune to Ground-type moves and unaffected by Electric Terrain's damage bonus and Paralysis block.
- Available on: Gastly line, Koffing line, Misdreavus, Abra line (examples).

### §5.5.3.4 Torrent / Blaze / Overgrow (Combat passive — starter-exclusive)

- When this Pokémon's HP drops below 30%, their primary type's moves deal +20% damage.
- Torrent (Squirtle line), Blaze (Charmander line), Overgrow (Bulbasaur line).
- These are granted at first evolution — not present in the base starter form.

### §5.5.3.5 Intimidate (Positional passive)

- When this Pokémon enters the Lead position (via manual swap, Step-Forward, or faint-replacement), all enemy Pokémon's Attack drops by one stage.
- Available on: Growlithe line, Gyarados (examples).

## §5.5.4 Lead Aura — Ability/Item Gated Positional Buff


**Lead Aura is not a default mechanic.** Most Pokémon do not have one. A Pokémon gains a Lead Aura only through:

- A specific **passive ability** (e.g., a hypothetical "Drought" ability that grants Fire-type Lead Aura).
- A specific **Held Item** equipped to the Pokémon (e.g., a "Fire Plate" Held Item that grants Fire-type Lead Aura while equipped). Held Items defined in Topic 8.

**Effect:** While the Pokémon with a Lead Aura source occupies the Lead position, all bench Pokémon's moves of the Lead Aura's specified type deal **+5% damage**.


**Activation rule:** The Aura activates the moment the Pokémon enters the Lead slot (via manual swap, Step-Forward, or faint-replacement) and deactivates the moment they leave. Visible as a persistent buff icon under the Lead's portrait.


**Stacking:** If a Pokémon has both an ability-granted Aura and an item-granted Aura, the auras stack additively (e.g., +5% from ability + +5% from item = +10% total). Distinct-type auras on the same Pokémon also stack independently.


**Design intent:** Lead Aura adds a fourth axis to the Lead-swap decision (who absorbs / what melee cards unlock / what swap cost / **what bench buff is active**) while remaining an opt-in build pillar. Players who chase Lead Aura builds invest acquisition slots in matching abilities and Held Items.


**Architecture:** `AbilitySO.GrantsLeadAura : Type?` and `HeldItemSO.GrantsLeadAura : Type?`. `BattleManager.OnLeadChange` event fires the buff recalculation. Fits the existing event-bus pattern.


---


# §5.6 Evolution Branch — Worked Example (Squirtle Line)


To make the system concrete, here is a full worked example for the Squirtle evolution line:


**Squirtle (base form) — 4 moves:**

- Tackle (Melee, 1 AP, Offensive)
- Water Gun (Ranged, 1 AP, Offensive)
- Withdraw (Melee, 1 AP, Defensive)
- Tail Whip (Utility, 0 AP, lowers enemy Defense one stage)
- **Passive:** none.

**Evolution threshold:** Level 16.


**→ Wartortle, Branch A: Vanguard**

- Tackle → **Skull Bash** (Melee, 2 AP, Offensive, Step-Backward, +50% Power vs Tackle)
- Water Gun → Water Gun (retained)
- Withdraw → Withdraw (retained)
- Tail Whip → **Aqua Jet** (Melee, 1 AP, Offensive, Step-Forward)
- **Passive gained:** Torrent (Water moves +20% when HP < 30%)
- **CritChance passive:** +10% added to PokemonInstance.CritChance

**→ Wartortle, Branch B: Specialist**

- Tackle → Tackle (retained)
- Water Gun → **Water Pulse** (Ranged, 2 AP, Offensive, 25% Confusion rider)
- Withdraw → **Iron Defense** (Melee, 1 AP, Defensive, raises Defense 2 stages, Step-Backward)
- Tail Whip → **Charm** (Utility, 0 AP, lowers enemy Attack 2 stages)
- **Passive gained:** Torrent (Water moves +20% when HP < 30%)

**Evolution threshold (Wartortle → Blastoise):** Level 36.


**→ Blastoise, Vanguard sub-branch A1: "Heavy Brawler"**

- Skull Bash → **Hydro Crash** (Melee, 3 AP, Offensive, Step-Forward, very high Power — ultimate eligible)
- Water Gun → **Surf** (Ranged, 2 AP, Offensive, hits all enemies for 70% damage — Cleave equivalent)
- Withdraw → **Aqua Ring** (Defensive, 1 AP, restores `floor(MaxHP/8)` HP at turn end for 3 turns)
- Aqua Jet → Aqua Jet (retained)
- **Secondary passive gained:** Shell Armor (Lead Blastoise takes −2 incoming damage; enhances Boulder Badge to −3 if held)

**→ Blastoise, Vanguard sub-branch A2: "Aqua-Jet Duelist"**

- Skull Bash → **Skull Bash+** (Melee, 2 AP, Offensive, Step-Backward, higher Power than base Skull Bash, now also lowers enemy Defense 1 stage)
- Water Gun → **Hydro Pump** (Ranged, 3 AP, Offensive, highest single-target damage in kit)
- Withdraw → Withdraw (retained)
- Aqua Jet → **Aqua Jet+** (Melee, 1 AP, Offensive, Step-Forward, now ignores 1 point of enemy Defense)
- **Secondary passive gained:** Swift Swim (draw 1 extra skill card on the first turn of Rain Dance-active combats)
