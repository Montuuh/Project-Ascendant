<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Last updated from Notion: 2026-06-09T23:16:00.000Z -->

**Status:** 🟢 In Progress


**Last Updated:** 2026-06-10 (CL-008: §5.12.3 enriched — AvailableAbilities pool, PrimaryAbility legacy-flagged)


**Cross-references:** Topic 3 (Lead mechanic, move modifiers), Topic 4 (Mastery Moves, Branch synergy with Boons/Badges), Topic 6 (meta-progression), Topic 8 (Held Items — Lead Aura source).


---


# §5.1 Overview


Progression in Project Ascendant operates on two levels:

- **Within a run:** Pokémon gain XP, level up, and evolve. Evolution is the primary deck-manipulation event — a permanent, irreversible choice that changes a Pokémon's move cards and identity for the rest of the run. TMs and Move Tutors provide secondary mid-run customization. Passive abilities granted by evolution add a third layer of build identity.
- **Across runs:** Trainer XP, meta-unlocks, and Pokédex mastery (§4.3.9) accumulate persistently. Defined in Topic 6 (Roguelike Progression).

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
> ✅ Adopted (2026-05-31): §5.2 XP is data-driven — `ProgressionConfigSO` holds the XP-per-tier (Wild/Trainer/Elite/Gym) and the XP→level curve `Base+(L−1)·Slope`; per-species `PokemonSpeciesSO.EvolveLevel` records the evolution level. The config is the source of truth and is tuned via playtest.
> Blocked: final XP / level-curve / per-species evolution-threshold calibration (systems-designer). The interim values are placeholders, not balance-approved.

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

- Each branch is displayed with a clear identity label (e.g., "Vanguard," "Specialist") and a full preview of how that branch affects the Pokémon's **Learned Move Pool** (see §5.10):
    - Moves that are **upgraded** show the current move → evolved version with diff highlights (changed Power, new modifiers, new status riders). Upgraded moves auto-apply to the active 4 if the old version was active.
    - Moves that are **added** (new pool entries not present before) are shown as new additions — these expand the pool, giving more configuration options post-evolution.
    - Moves that are **unaffected** by this branch are noted as retained.
- The branch's passive ability reward (if any) is displayed.
- After confirming, the player may immediately reconfigure their active 4 from the updated pool before re-entering the Map View.
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


Evolution operates on the **Learned Move Pool** (see §5.10). It is a purely additive event — no moves are removed from the pool. A branch does two things simultaneously:

1. **Upgrades:** selected pool moves are replaced in-place by their evolved versions. If the old version was in the active 4, the upgraded version takes that active slot automatically.
2. **Additions:** new moves are added to the pool, expanding it beyond its pre-evolution size. The player then reconfigures their active 4 from the larger pool before re-entering the Map View.

**Evolution stage 1 (mid-form) — typical changes:**

- 1–2 pool moves upgraded (higher Power, added modifiers or status riders — branch-dependent).
- 0–1 new moves added to the pool (branch-dependent; expands pool to 5 if 1 addition).
- 0–1 SF/SB modifiers introduced on upgraded moves (Vanguard branch).
- Primary passive ability granted (see §5.5.1).

**Evolution stage 2 (final form) — typical changes:**

- 1–2 further upgrades from mid-form versions already in the pool.
- 1 signature move added (unique to this branch and final form — strongest move in the kit; expands pool).
- 1–2 SF/SB modifiers on the highest-impact moves (especially Vanguard branch).
- Branch-specific secondary passive ability granted (see §5.5).
- Sub-branch choice available for starter and high-rarity species (e.g., "Heavy Brawler Blastoise" vs "Aqua-Jet Duelist Blastoise") — this choice determines which upgrades and additions are applied.

**Final evolution continues the first branch's direction** — choosing Vanguard at stage 1 locks the Pokémon into Vanguard-archetype upgrades at stage 2, with one sub-choice available within that path.


**Pool size progression (typical):**

- Base form: 4 moves (pool = active 4; no surplus)
- Stage 1 evolution: 4–5 moves (1 addition possible; player picks active 4)
- Stage 2 evolution + TMs/Tutors: 5–8 moves (player picks active 4 from growing pool)

The growing pool creates meaningful out-of-combat decisions: which 4 of my learned moves do I want active for this encounter?


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
- **What they do:** teach a specific named move to a compatible Pokémon, **permanently adding it to their Learned Move Pool** for the rest of the run. The player then reconfigures their active 4 from the expanded pool. **Mastery Moves are excluded** — the Mastery slot cannot be reached by TMs (see §4.3.9.2).
- **Compatibility:** each TM move has a `CompatibleSpecies[]` list in its ScriptableObject. A TM cannot be used on an incompatible Pokémon — the UI greys out incompatible options.
- **Usage:** applied from the inventory in the Map View (out of combat only). The player selects the TM → selects a compatible Pokémon → the move is added to that Pokémon's Learned Move Pool. The player may immediately reconfigure their active 4. Permanent, confirmed with a dialogue.
- **Single-use:** the TM item is consumed on use. It does not persist after use.
- **Design intent:** TMs create a secondary customization axis — the "happy accident" discovery moment. Finding a Thunderbolt TM compatible with Clefairy opens a build path the player didn't anticipate.

## §5.4.2 Move Tutors

- **What they are:** a service available at specific nodes — Pokémon Center (City nodes) and Training Grounds (Victory Road).
- **What they do:** the Tutor offers a curated list of learnable moves for each of the player's Pokémon. The player selects one Pokémon and one move to learn, which is **permanently added to that Pokémon's Learned Move Pool**. The player then reconfigures their active 4 from the expanded pool.
- **Compatibility:** each Pokémon species has a `TutorLearnset[]` in their ScriptableObject — the moves available to them from tutors. Distinct from TM compatibility (tutors teach different moves than TMs).
- **Cost:** free as part of the node offering (City nodes; Training Grounds). May cost Poké Dollars at shop-style tutors.
- **Repeatable:** the player can use the Move Tutor service at every City they visit — but they only pass through 2 Cities per run, plus Victory Road's Training Grounds.
> ✅ **Resolved (2026-05-24):** Tutor Learnsets are stage-aware — see §5.7.1.

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

> ⚠️ **SUPERSEDED for the Squirtle line by §5.12.2 (CL-007, 2026-06-09).** The line is now **moves-only / one species per stage**: Squirtle→Wartortle→Blastoise (one SO each — gap #46 closed), with **3 archetype branches per evolving stage** (Vanguard/Specialist/Support, free per stage), **no ability/crit grant**, and a **lighter payload** (≤2 upgrades at stage 1; +1 signature, additive, at stage 2). Squirtle-line kits: Vanguard {Tackle→Skull Bash, Tail Whip→Aqua Jet} → +Hydro Crash; Specialist {Water Gun→Water Pulse, Tail Whip→Charm} → +Hydro Pump; Support {Withdraw→Iron Defense, Tail Whip→Aqua Ring} → +Aqua Fortress. The Vanguard/A1/A2 example below is retained for historical reference only.

To make the pool system concrete, here is a full worked example for the Squirtle evolution line using the **Learned Move Pool** (§5.10):


**Squirtle (base form)**

- Learned Pool: {Tackle, Water Gun, Withdraw, Tail Whip}
- Active 4 (player-configured, out of combat): all four (pool = active; no surplus)
- Mastery (if Lv1 unlocked): _Aqua Tail Lv1_ — Melee, 1 AP, 65 Power (5th slot — fixed, not configurable)
- **Passive:** none.

**Evolution threshold:** Level 16.


**→ Wartortle, Branch A: Vanguard**


Pool upgrades (auto-apply to active 4 if old version was active):

- Tackle → **Skull Bash** (Melee, 2 AP, Offensive, Step-Backward — +50% Power vs Tackle)
- Tail Whip → **Aqua Jet** (Melee, 1 AP, Offensive, Step-Forward)

Pool additions: none.


Updated pool: {Skull Bash, Water Gun, Withdraw, Aqua Jet}


Active 4 (auto-updated): Skull Bash | Water Gun | Withdraw | Aqua Jet


**Passive gained:** Torrent (Water moves +20% when HP < 30%) + CritChance +10%


**Mastery (if Lv2 unlocked):** _Aqua Tail Lv2_ — Melee, 2 AP, 95 Power, Step-Forward


**→ Wartortle, Branch B: Specialist**


Pool upgrades:

- Water Gun → **Water Pulse** (Ranged, 2 AP, Offensive, 25% Confusion rider)
- Withdraw → **Iron Defense** (Melee, 1 AP, Defensive, raises Defense 2 stages, Step-Backward)
- Tail Whip → **Charm** (Utility, 0 AP, lowers enemy Attack 2 stages)

Pool additions: none. Tackle is retained unchanged in the pool.


Updated pool: {Tackle, Water Pulse, Iron Defense, Charm}


Active 4 (auto-updated): Tackle | Water Pulse | Iron Defense | Charm


**Passive gained:** Torrent (Water moves +20% when HP < 30%)


**Mastery (if Lv2 unlocked):** _Aqua Tail Lv2_ — Melee, 2 AP, 95 Power, Step-Forward


**If the player uses TM05 Surf before evolving (example of TM pool expansion):**


Pool additions: +Surf (Ranged, 2 AP, Offensive — hits all enemies, Cleave equivalent)


Updated pool: {Skull Bash, Water Gun, Withdraw, Aqua Jet, **Surf**} — 5 moves; player picks active 4


Player decision: drop Water Gun (it upgrades at Blastoise anyway) or Withdraw?


**Evolution threshold (Wartortle → Blastoise):** Level 36.


**→ Blastoise, Vanguard sub-branch A1: "Heavy Brawler"** (continuing from Vanguard Wartortle)


Pool upgrades:

- Skull Bash → **Hydro Crash** (Melee, 3 AP, Offensive, Step-Forward — very high Power, ultimate eligible)
- Water Gun → **Surf** (Ranged, 2 AP, Offensive, Cleave equivalent) _[if already in pool via TM, no duplicate added — pool deduplicates]_
- Withdraw → **Aqua Ring** (Defensive, 1 AP — restores `floor(MaxHP/8)` HP at turn end for 3 turns)

Pool additions: none beyond upgrades.


Updated pool: {Hydro Crash, Surf, Aqua Ring, Aqua Jet}


Active 4 (auto-updated): Hydro Crash | Surf | Aqua Ring | Aqua Jet


**Secondary passive gained:** Shell Armor (Lead Blastoise takes −2 incoming damage; enhances Boulder Badge to −3 if held)


**Mastery (if Lv3 unlocked):** _Aqua Tail Lv3_ — Melee, 3 AP, 130 Power, Step-Forward, ignores 2 Defense


**→ Blastoise, Vanguard sub-branch A2: "Aqua-Jet Duelist"** (continuing from Vanguard Wartortle)


Pool upgrades:

- Skull Bash → **Skull Bash+** (Melee, 2 AP, Offensive, Step-Backward — higher Power, also lowers enemy Defense 1 stage)
- Water Gun → **Hydro Pump** (Ranged, 3 AP, Offensive — highest single-target damage in kit)
- Aqua Jet → **Aqua Jet+** (Melee, 1 AP, Offensive, Step-Forward — ignores 1 point of enemy Defense)

Pool additions: none beyond upgrades.


Updated pool: {Skull Bash+, Hydro Pump, Withdraw, Aqua Jet+}


Active 4 (auto-updated): Skull Bash+ | Hydro Pump | Withdraw | Aqua Jet+


**Secondary passive gained:** Swift Swim (draw 1 extra skill card on the first turn of Rain Dance-active combats)


**Mastery (if Lv3 unlocked):** _Aqua Tail Lv3_ — Melee, 3 AP, 130 Power, Step-Forward, ignores 2 Defense


**Pool system summary — Squirtle's full progression:**

- Base form: 4 pool moves (pool = active 4, no choice needed)
- After evolving to Wartortle: 4 pool moves (2 upgraded in-place, same size)
- After TM05 Surf: 5 pool moves → player picks active 4
- After evolving to Blastoise: 4 pool moves (Surf merge deduplicates; upgrades consolidate)
- After a post-Blastoise Move Tutor visit: 5 pool moves → player picks active 4 again

By late-run, a Blastoise who has received 2 TMs and 1 Tutor visit has 7 pool entries and picks active 4 — a sustained deckbuilding decision before each hard node.


---


# §5.7 Open Gaps — RESOLVED (added 2026-05-24)


## §5.7.1 Tutor Learnset Evolution Updates — RESOLVED (was BACKLOG gap #14)


See §4.8.5 — Tutor Learnsets are stage-aware. The `TutorLearnset[]` lives on the per-stage species SO. Evolving a Pokémon changes which moves the Move Tutor service offers for that Pokémon — broader and more powerful at higher stages.


# §5.8 Ability Catalog — Full Launch Pool (added 2026-05-24)


Locks the species-ability assignment surface. ~30 launch abilities; each Pokémon line gets a primary ability at first evolution and a branch-dependent secondary at final evolution.


| Ability                     | Category   | Description                                                                                                                |
| --------------------------- | ---------- | -------------------------------------------------------------------------------------------------------------------------- |
| Torrent                     | Combat     | Water moves +20% when HP < 30% (Squirtle line)                                                                             |
| Blaze                       | Combat     | Fire moves +20% when HP < 30% (Charmander line)                                                                            |
| Overgrow                    | Combat     | Grass moves +20% when HP < 30% (Bulbasaur line)                                                                            |
| Static                      | Combat     | When dealing damage with Electric move, 25% chance to apply Paralysis (Pikachu line)                                       |
| Keen Eye                    | Vision     | All Unknown intents revealed at combat start (Pidgey, Hoothoot, etc.)                                                      |
| Foresight (passive variant) | Vision     | First Unknown intent per turn revealed (separate from move card §5.5.3.2)                                                  |
| Levitate                    | Type       | Immune to Ground; unaffected by Electric Terrain (Gastly line, Koffing line)                                               |
| Intimidate                  | Positional | On entering Lead, lower all enemy Attack -1 stage (Growlithe line, Gyarados)                                               |
| Sturdy                      | Survival   | Survive one lethal hit at 1 HP per combat (Geodude line, Onix line)                                                        |
| Swift Swim                  | Combat     | Draw +1 card on turn 1 of Rain Dance combats (Magikarp line, water Pokémon)                                                |
| Pickup                      | Meta       | 25% chance per combat win to find a random consumable (Meowth line)                                                        |
| Limber                      | Status     | Immune to Paralysis beyond type immunity (Persian, Hitmonlee)                                                              |
| Insomnia                    | Status     | Immune to Sleep beyond type immunity (Hypno, Drowzee line)                                                                 |
| Compoundeyes                | Combat     | Player's status-rider moves always apply (no rider miss) (Butterfree line)                                                 |
| Volt Absorb                 | Type       | Electric moves heal this Pokémon instead of damaging (Voltorb evolution path branch)                                       |
| Water Absorb                | Type       | Water moves heal instead of damaging (Vaporeon path)                                                                       |
| Drought (Aura)              | Aura       | Grants Fire Lead Aura while Lead (specialty evolution path; ~3 Pokémon)                                                    |
| Drizzle (Aura)              | Aura       | Grants Water Lead Aura while Lead                                                                                          |
| Shell Armor                 | Combat     | Lead receives −2 damage per hit (Blastoise Vanguard A1)                                                                    |
| Tough Claws                 | Combat     | Melee moves +15% damage (Persian, Charizard Vanguard)                                                                      |
| Snipe                       | Combat     | Ranged moves +15% damage (Specialist evolution branch frequency)                                                           |
| Healer                      | Support    | Heals random ally for 3 HP at turn end (Support evolution branch)                                                          |
| Friend Guard                | Support    | Bench Pokémon take −10% damage from Cleave (Chansey path)                                                                  |
| Speed Boost                 | Combat     | +1 AP on turn 2 of every combat (Eevee/Jolteon path)                                                                       |
| Adaptability                | Combat     | STAB multiplier 1.5× → 1.75× (Eevee final branches)                                                                        |
| Anger Point                 | Combat     | When critted, all moves next turn AlwaysCrit (Primeape)                                                                    |
| Mold Breaker                | Combat     | Player's moves ignore enemy type immunities (rare; Champion-tier earned)                                                   |
| Multiscale                  | Combat     | Lead at full HP takes −50% damage from first hit per combat (Dragonite path)                                               |
| Magic Guard                 | Survival   | Immune to status DoT (Burn/Poison damage; status effects still apply)                                                      |
| Sheer Force                 | Combat     | Status-rider moves: status applies as normal, plus +20% damage; loses ability to apply some self-buffs (boss-tier ability) |


**Vertical Slice subset:** Torrent, Blaze, Overgrow, Static, Keen Eye, Levitate, Sturdy, Intimidate, Swift Swim, Shell Armor.


# §5.9 Pre-Evolution Pokémon Passive Note (added 2026-05-24)


Per §5.5.1, pre-evolution Pokémon either have **no passive** or a very simple one (e.g., Swift Swim on Magikarp). Launch convention: ~70% of pre-evolution Pokémon have no passive; ~30% have a minor passive. First evolution always grants a meaningful passive; final evolution always grants a branch-dependent secondary.


# §5.10 Learned Move Pool


Each Pokémon maintains a **Learned Move Pool** — the complete set of moves they have access to. The player configures which 4 of these moves are **active** (contributing cards to the Skill Deck) from the Map View, out of combat, at any time between nodes.


## §5.10.1 Pool composition


The pool starts at 4 moves (the Pokémon's base kit). It grows through:

- **Evolution:** branches upgrade existing pool entries in-place and optionally add new moves (see §5.3.5).
- **TMs:** add one new move to the pool (see §5.4.1).
- **Move Tutors:** add one new move to the pool (see §5.4.2).

Moves are **never removed** from the pool. A move upgraded by evolution (e.g., Tackle → Skull Bash) replaces the old entry in-place — same pool slot, advanced version. If the TM-taught move is already in the pool (e.g., TM05 Surf added, then Blastoise evolution also grants Surf), the pool deduplicates — no duplicate entries.


## §5.10.2 Active 4 configuration


The player selects which 4 pool moves are active from the **Move Management screen** — accessible from any Pokémon's detail view in the Map View, **out of combat only, at any point between nodes**.

- Reconfiguration is free and unlimited between combat nodes.
- The active 4 are the cards contributed to the Skill Deck for that Pokémon.
- The Mastery Move (§4.3.9.2) is always the 5th card — it is not part of pool configuration and cannot be toggled off.

## §5.10.3 Auto-upgrade on evolution


When an evolution branch upgrades a pool move:

- If the move **was active** (in the current active 4), the upgraded version automatically takes that active slot. The player is shown what changed post-evolution and may immediately reconfigure their active 4 before re-entering the Map View.
- If the move **was not active** (sitting in the pool but benched), it upgrades in-place without affecting the current active 4.

## §5.10.4 Design intent


The pool gives the player a growing library of options and a recurring tactical out-of-combat decision: "Which 4 do I want for the next encounter?" This decision is informed by the visible upcoming node type, scouted enemy information, and the player's current relic and badge synergies.


Every pool addition (TM, Tutor, evolution addition) is a net gain — never a forced replacement at the deck level. Trade-offs emerge from the fixed active-4 budget, which never grows. The expanding pool makes TMs and Tutors feel genuinely powerful (more options, not slot pressure), while the fixed active-4 keeps the Skill Deck consistent and intentional — matching the design pillar of synergy being sculpted, not drafted.


---


# §5.11 Mastery Move Progression & Achievement Catalog


Each Pokémon species has up to three Mastery tiers (Lv1–Lv3), tracked persistently in `MetaProgressionSO`. Two-stage lines cap at Lv2. All tiers are defined across the species' stage-specific `PokemonSpeciesSO` assets.


## §5.11.1 Lv1 Mastery — "Familiar Bond"


**Universal unlock category** — satisfied by any ONE of the following across any number of runs:

- Win 3 combats with this Pokémon in the Active Team.
- Recruit or capture this Pokémon for the first time.
- Complete any run (win or loss) with this Pokémon in the Active Team.

Once unlocked, Lv1 Mastery is permanently available for this species in all future runs. The base-form Mastery card appears in the Skill Deck from the Pokémon's first combat in any qualifying run.


## §5.11.2 Lv2 Mastery — "Trusted Partner"


**Species-specific achievement** — unique to each species, themed to their combat identity. Examples:


| Species Line     | Lv2 Achievement                                                                                                 |
| ---------------- | --------------------------------------------------------------------------------------------------------------- |
| Bulbasaur line   | Apply Poison or Sleep to 5 enemies across any number of runs using this Pokémon's moves                         |
| Charmander line  | Deal a single hit of 40+ damage with a Fire-type move using this Pokémon                                        |
| Squirtle line    | Absorb a combined 60+ incoming damage with Defensive moves across any number of runs while this Pokémon is Lead |
| Caterpie line    | Win a combat without this Pokémon taking any damage (DoT excluded)                                              |
| Pidgey line      | Use Step-Forward or Step-Backward 10+ times in combats with this Pokémon across any number of runs              |
| Geodude line     | Survive a lethal hit via the Sturdy ability while this Pokémon is Lead                                          |
| Generic fallback | Win 3 runs with this Pokémon in the Active Team                                                                 |


For two-stage lines, Lv2 is the highest tier — it unlocks the final-form Mastery card.


## §5.11.3 Lv3 Mastery — "Deep Bond" (three-stage lines only)


**High-difficulty species-specific achievement** — requires sustained, intentional multi-run effort. Examples:


| Species Line                | Lv3 Achievement                                                                                                          |
| --------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Bulbasaur line (Venusaur)   | Win a run with Venusaur as Lead for 60%+ of all combat turns tracked across that run                                     |
| Charmander line (Charizard) | Win a run without using any consumable items across all combats                                                          |
| Squirtle line (Blastoise)   | Win a run without any Active Team Pokémon fainting in any combat                                                         |
| Caterpie line (Butterfree)  | Apply at least one status condition to every enemy in a combat encounter, achieved 5 times across runs with this Pokémon |
| Pidgey line (Pidgeot)       | Win a run on the "Ruthless" difficulty modifier with Pidgeot in the Active Team                                          |


Lv3 unlocks the final-form Stage 2 Mastery card permanently. All future runs featuring a fully-evolved member of this line field the Lv3 Mastery.


## §5.11.4 Mastery Move design guidelines (content)


Mastery Moves must exceed the base learnset at equivalent AP cost and feel species-defining. Target output per tier:


| Tier    | Power Range | AP Cost | Modifiers                        | Notes                                                                                      |
| ------- | ----------- | ------- | -------------------------------- | ------------------------------------------------------------------------------------------ |
| **Lv1** | 60–80       | 1       | None                             | Clean and reliable — the "always useful" fallback card                                     |
| **Lv2** | 85–110      | 1–2     | SF or SB, or 1 status rider      | Begins to express the species' tactical identity                                           |
| **Lv3** | 110–140     | 2–3     | Composite, species-unique effect | Rivals the branch signature move; the effect is unreplicable by any other card in the game |


The Lv3 Mastery's unique effect should reflect the player's deep investment in that Pokémon — a reward that feels earned, not found.


## §5.11.5 Vertical Slice scope

- Lv1 unlock logic and tracking is implemented for all 6 VS Pokémon lines.
- Lv2 and Lv3 achievement tracking infrastructure is in place (`MetaProgressionSO.MasteryAchievements[]`); specific check logic per species is **stubbed** for the post-VS content pass.
- All 6 Mastery Lv1 move assets are authored in their base-form species SOs.
- Lv2 and Lv3 move assets are authored for all VS species; achievement unlock gates are stubbed.
- The full achievement catalog (all launched species) is a post-VS content task.
> ✅ Resolved (R4-3): the additive Learned Move Pool + per-Pokémon Move Manager are implemented (§5.10) — learned moves accumulate; the active 4 are configured separately; the Mastery slot remains immutable.
> Blocked: the Pokémon Center Move Tutor (Epic 9 Task 9.5.3) and TM/Tutor learning generally. VS resolution (user-confirmed 2026-05-29): the Move Tutor REPLACES a chosen `CurrentMoves` slot (Mastery untouched per §4.3.9.2) instead of adding to a pool. Build the §5.10 additive pool post-VS and revisit Tutor/TM learning then. See BACKLOG gap #36.

---


# §5.12 Progression Redesign (2026-06-07) — supersedes affected sections


Adopted via the design pass (open-questions Q12–Q16; change-log CL-006…010). Where this section conflicts with §5.2.1, §5.3.3–§5.3.5, §5.3.6.1, §5.5, or §5.10, **this section governs**. Cadence/curve numbers (learn levels, stat deltas, Dojo pricing) are systems-designer tuning via `ProgressionConfigSO` / per-species data — placeholder until calibrated.


## §5.12.1 Move acquisition — level-gated learnset (CL-006)

- Base-form Pokémon **start with 2 moves** (not 4). Each species has a **level-up learnset** (ordered `(level, move)`); a Pokémon knows every learnset move with `level ≤ current level`.
- **Deck contribution =** **`min(known moves, 4)`** per Pokémon — the active-4 cap is unchanged and Mastery remains the immutable 5th. The deck **thickens as you level and recruit**: a run _starts with a single starter_ (a ~2-card skill deck), grows toward ~6 cards as you recruit a full base-form team, and reaches ~12 by Gym 1 as those Pokémon level to their 4-move kits. Learnset levels are **clamped below each species' evolution level** so no move is ever lost to evolving early (validated by a content test).
- **Recruited wilds** derive their known moves from spawn level, so late recruits stay viable.
- Moves **beyond the learnset** come from the **Dojo** (off-learnset tutor moves, §7.14), **TMs**, and **evolution**. The lean natural learnset is deliberate — scarcity is what makes those sources matter.

## §5.12.2 Evolution — focused upgrade, free archetype each stage (CL-007)

- At **each** evolution the player chooses an **archetype independently** from the species' available 2–3 (Vanguard / Specialist / Support). Stage 1 no longer locks stage 2 (Vanguard→Specialist is allowed); each pick is permanent.
- Payload per evolution: **stat upscale + improve 1–2 existing pool moves + maybe +1 new pool move** (the final-evolution new move is the species **signature**). The heavy multi-upgrade + sub-branch (A1/A2) rewrite of §5.3.5 is removed.

## §5.12.3 Abilities — kept, decoupled, earned (CL-008)

- Abilities are **no longer auto-granted by evolution**. One passive slot per Pokémon, **taught at the Dojo** (the ability-learner, §7.14). The ~30-ability roster (§5.8) stays as content.
- Each species carries an **`AvailableAbilities`** **pool** — a curated list of 1–N abilities that can be taught at a Dojo. The Dojo shows all entries in this pool for the chosen Pokémon. The currently-equipped ability is included in the listing, allowing a **swap** rather than forcing a new-learn only. Teaching always **sets or replaces** the single passive slot.
- **`PrimaryAbility`** **(legacy field):** the old auto-grant field from the pre-CL-008 model. Nulled on all Vertical Slice species by CL-007/CL-008. Kept in code for serialisation compatibility only — **do not use for new species**. New and updated species must populate `AvailableAbilities` instead.

## §5.12.4 The Dojo (CL-009 — full spec in Topic 7 §7.14)

- The Move Tutor leaves Pokémon Centers and becomes a standalone **Dojo** map node that teaches off-learnset moves **and** abilities for Poké Dollars. Pokémon Centers now offer heal + Trauma therapy only.

## §5.12.5 XP distribution — whole Box (CL-010)

- All Box Pokémon earn combat XP: **Active 100% / benched 75%** baseline. The **Exp Share** relic (Topic 8 §8.3.3) lifts benched Pokémon to **100%**.
