<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b48146b3a0fe94ca2bd05c -->
<!-- Exported: 2026-05-25T22:41:34.930Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🔒 Locked


**Last Updated:** 2026-05-24 (full lock — node specs, biome pools, catching, mystery events, shops, map seeding)


**Cross-references:** Topic 2 (§2.1.2 node categories, branching map), Topic 4 (§4.5 Victory Road nodes — adjacent), Topic 6 (§6.5 starter unlocks; §6.6 relic tiers; achievement triggers), Topic 8 (shop inventory, consumables, relics, Held Items).


---


# §7.1 Scope


Defines every node type the player encounters on the branching Region map (and at Cities) other than the boss/Gym (§4.4.4) and Victory Road (§4.5). Specifies encounter generation, reward tables, biome-to-species mapping, catching mechanics, Mystery Event design, City Shop curation, trainer archetypes, and map seeding.


---


# §7.2 Region Map Structure


Per §2.1.2, each Region is a branching ladder. Topic 7 locks the exact dimensions and topology.


## §7.2.1 Per-Region Topology


| Layer | Content                                                      | Notes                                                                      |
| ----- | ------------------------------------------------------------ | -------------------------------------------------------------------------- |
| 0     | **Starter Layer** — single forced entry node                 | Always a Wild Pokémon Area (§2.1.2). Guarantees early recruitment.         |
| 1     | 3-wide branch                                                | Standard nodes                                                             |
| 2     | 3-wide branch                                                | Standard nodes                                                             |
| 3     | 3-wide branch + 1 Elite (always present in one lane)         | Tier escalation                                                            |
| 4     | **Branch Point Layer** — 2 lanes diverging to different Gyms | Per §2.1.2 Gym branch choice. Each lane's downstream nodes are independent |
| 5     | 2-wide per branch                                            | Continues toward respective Gym                                            |
| 6     | 2-wide per branch + guaranteed Pokémon Center node           | Pre-Gym restoration                                                        |
| 7     | **Gym Layer** — single node per branch                       | Boss fight (§4.4.4)                                                        |


**Layer count:** 8 per Region (0-indexed). 3 Regions per run = 24 traversal layers + 2 City interstitials + Victory Road.


## §7.2.2 Node-Type Distribution (per Region)


Across both lanes (12 standard nodes per Region after Layer 0, before Gym):


| Node Type                          | Count per Region | Notes                               |
| ---------------------------------- | ---------------- | ----------------------------------- |
| Wild Pokémon Area                  | 2                | Plus the guaranteed Layer 0         |
| Trainer Battle                     | 4                | Standard trainers                   |
| Elite Trainer                      | 1                | Always at Layer 3                   |
| Pokémon Center                     | 1                | Always at Layer 6 (pre-Gym)         |
| Shop (regional sub-shop, not City) | 1                | At Layer 2 or 5                     |
| Mystery Event                      | 2                | Distributed across Layers 1–5       |
| Tutor / Daycare service node       | 1                | Layer 2 or 5 (alternates with Shop) |


**Seeding determinism:** Per Engineering Pillar 3 (§1.3.2), the map is generated from the run seed. Same seed = same map every time.


## §7.2.3 Connection Rules

- Every node connects to 1-3 nodes in the next layer.
- Within a layer, no two adjacent nodes share the same node type (prevents "Mystery Event sandwich").
- Both branches at Layer 4 must lead to all 8 distance-7 layers symmetrically (so the player isn't punished by node-count for branch choice).

---


# §7.3 Wild Pokémon Area Nodes


The recruitment workhorse. Promised in §2.1.2, fully defined here.


## §7.3.1 Wild Area Sub-Biomes


Seven biome variants, each with its own species pool and visual theming:


| Biome                  | Region Availability     | Species Theming                    |
| ---------------------- | ----------------------- | ---------------------------------- |
| **Meadow** 🌾          | R1 (primary), R2 (rare) | Normal, Bug, Grass starters        |
| **Cave** 🕳️           | R1, R2, R3              | Rock, Ground, Fighting, dual-types |
| **River/Lake** 💧      | R1, R2                  | Water, Bug-Water dual-types        |
| **Sea** 🌊             | R2 (primary)            | Water (deep-water variants), Ice   |
| **Power Plant** ⚡      | R2, R3                  | Electric, Steel (post-launch)      |
| **Volcano Slope** 🔥   | R3                      | Fire, Rock-Fire, Ground            |
| **Sky / Cliffs** 🦅    | R3                      | Flying, Bug-Flying, Psychic        |
| **Abandoned Tower** 👻 | R3 (rare)               | Ghost, Poison, Psychic             |


When a Wild Pokémon Area node generates, it samples a biome from the Region's eligible set, weighted by Region (each Region has a "primary" biome that appears more often).


## §7.3.2 Encounter Composition


Each Wild Area node offers the player **3 species choices** (visible up-front, per Pillar 1):

- 2 Common-rarity species from the biome's pool.
- 1 Uncommon-rarity species from the biome's pool.
- ~10% of nodes per Region: replace the Uncommon with a Rare species (seeded surprise).

The player picks one species → enters a catching encounter with that species (§7.3.4).


## §7.3.3 Biome-to-Species Pool (Launch ~30 species mapping)


Illustrative pool assignment for the launch ~30 evolution lines:


| Biome           | Common                            | Uncommon                          | Rare               |
| --------------- | --------------------------------- | --------------------------------- | ------------------ |
| Meadow          | Caterpie, Pidgey, Rattata, Weedle | Oddish, Bellsprout, Mankey        | Eevee              |
| Cave            | Zubat, Geodude, Diglett           | Onix, Machop                      | Aerodactyl, Lapras |
| River/Lake      | Magikarp, Poliwag                 | Psyduck, Krabby                   | Lapras             |
| Sea             | Tentacool, Shellder, Horsea       | Staryu, Seel                      | Dratini            |
| Power Plant     | Voltorb, Magnemite                | Pikachu, Electabuzz               | Zapdos (post-VS)   |
| Volcano Slope   | Vulpix, Growlithe                 | Magmar, Slugma-equivalent         | Moltres (post-VS)  |
| Sky / Cliffs    | Pidgey (variant), Spearow         | Doduo, Hoothoot-equivalent        | Articuno (post-VS) |
| Abandoned Tower | Gastly                            | Haunter (pre-recruited!), Drowzee | Cubone, Mr. Mime   |


**Multi-pool species (e.g., Pidgey in Meadow + Sky):** identical species, different seasonal flavor.


## §7.3.4 Catching Mechanic — LOCKED


Per the open question logged in Topic 7's scaffold, catching is a **mini-combat** governed by Telegraphed-Tactics rules — not a probability roll.


### §7.3.4.1 Catching encounter flow

1. The wild Pokémon appears at full HP. Player Active Team enters at current HP (per §2.4 — HP persists).
2. Combat begins. **Pokéball is added as a free single-use consumable card** in the player's Consumable Pile for this combat only.
3. Combat plays out normally. The wild Pokémon does NOT attempt to flee.
4. To catch: the player must:
    - Reduce the wild Pokémon's HP below 50% AND
    - Apply the **Pokéball** consumable.
5. Pokéball success rule (deterministic, no RNG):
    - **HP ≥ 50%** at time of throw: **catch fails**, Pokéball is consumed, combat continues. (Pokéball returns to hand next turn unless this was the only one — note: free Pokéball is 1-use; additional Pokéballs from the Shop are usable here.)
    - **HP < 50%, no status condition:** **catch succeeds**, combat ends.
    - **HP < 50% + any status condition on wild Pokémon:** **catch succeeds at any HP** (status condition expands the catch window).
    - **HP ≤ 0:** the wild Pokémon faints. The recruit is lost.
6. On successful catch: combat ends; the wild Pokémon enters the Box (or triggers the Swap-or-Skip prompt per §2.3.1).
7. On failed combat (Active Team wipe): run-failure event fires per §3.3.6.

### §7.3.4.2 Tier-2 Pokéballs (post-launch acquisition layer)


The launch ships only the basic Pokéball. Post-launch may add Great Ball (catches at HP ≤ 65%) and Ultra Ball (catches at HP ≤ 80% OR HP < 50% without status). Architecture supports the tier via a `CatchHPThreshold` field on the `PokéballConsumableSO`.


### §7.3.4.3 Catch design rationale


Pure-deterministic catching aligns with Pillar 1 (Telegraphed Tactics). The player has full information: HP threshold visible on Pokéball hover; "Apply Status to expand catch window" telegraphed in the tutorial.


Failed catches are still possible (wild Pokémon downed below 0) but never feel like "RNG screwed me" — they feel like "I committed to damage when I should have used Pokéball."


## §7.3.5 Wild Pokémon Stat Tier


Wild Pokémon recruited in a Region are statted to the Region's tier:

- R1 Wild Pokémon: Level 5–10 baseline.
- R2 Wild Pokémon: Level 12–20.
- R3 Wild Pokémon: Level 22–30.

A late-Region recruit catches up faster (higher base level), making Region-3 recruitment viable rather than wasted.


---


# §7.4 Trainer Battle Nodes


Standard combat encounters featuring a human trainer fielding 1–2 Pokémon, sequentially. Distinct from Elite (§7.5) and Boss (§4.4).


## §7.4.1 Trainer Archetypes (Launch — 8 archetypes)


| Archetype            | Composition                            | Tactical Identity                                                       |
| -------------------- | -------------------------------------- | ----------------------------------------------------------------------- |
| **Bug Catcher**      | 1–2 Bug Pokémon                        | High volume, low individual threat. Often Confusion riders.             |
| **Lass / Youngster** | 1 randomized Pokémon                   | Generalist; easy difficulty floor.                                      |
| **Hiker**            | 1 Rock/Ground Pokémon                  | Slow but durable. Heavy Defense.                                        |
| **Sailor / Swimmer** | 1–2 Water Pokémon                      | Status-heavy (Confusion, Burn-shred via Scald-equivalent).              |
| **Engineer**         | 1 Electric or Steel-equivalent Pokémon | Buff-Stall focus; sets up before strike.                                |
| **Hex Maniac**       | 1 Ghost/Psychic                        | Vision-disruption — generates Unknown intents.                          |
| **Ace Trainer**      | 2 high-stat varied Pokémon             | Mid-Region threat; multi-type.                                          |
| **Rocket Grunt**     | 1–2 Poison/Dark Pokémon                | Aggressive Cleave/Backstrike-tagged kits. Story flavor for future arcs. |


## §7.4.2 Trainer Rewards


| Reward               | Amount                                                                        |
| -------------------- | ----------------------------------------------------------------------------- |
| Trainer XP           | 5 XP per cleared trainer node (per §6.3.2)                                    |
| Poké Dollars         | 50–150₽ based on archetype tier                                               |
| Loot drop            | 50% Common item / 30% Common relic / 20% Uncommon item — seeded               |
| Bestiary kill credit | Each defeated trainer Pokémon counts toward Bestiary kill thresholds (§4.3.9) |


## §7.4.3 Trainer Encounter Generation


When a Trainer Battle node spawns, the seed picks one archetype from the Region's eligible list. Archetype eligibility expands per Region — Bug Catcher is R1-only; Rocket Grunt R2-R3; Ace Trainer R3-only.


---


# §7.5 Elite Trainer Nodes


Distinct from Gym Leaders. One Elite per Region (always Layer 3).


## §7.5.1 Elite Trainer Design Rules

- **Composition:** 2 Pokémon, sequential. Both with 2-phase design (Phase 1 / Phase 2 standard).
- **Difficulty:** between Trainer Battle and Gym Leader. A real mid-Region threat.
- **Reward:** 1 guaranteed Uncommon relic + Trainer XP bonus + Poké Dollar windfall (~300₽).
- **Trainer archetype:** drawn from the Ace Trainer pool, or a Region-flavor archetype (e.g., "Rocket Lieutenant" in R3).
- **No type lock:** Elite Trainers do NOT have a single-type identity (that's reserved for Gym Leaders). This makes them a different kind of test than the Gym ahead.

---


# §7.6 Pokémon Center Nodes (Region-internal)


Distinct from the City Pokémon Center (§2.1.4). One Region-internal Center per Region, always at Layer 6 — the pre-Gym pit stop.


## §7.6.1 Service Offerings


| Service                  | Effect                                                                                                           |
| ------------------------ | ---------------------------------------------------------------------------------------------------------------- |
| **Heal**                 | Full restore of all Box Pokémon to Effective Max HP (per §2.4.2). Free.                                          |
| **Move Tutor (limited)** | Curated 3-move offer per Pokémon. Choose 1 Pokémon → learn 1 move. Free. Once per visit.                         |
| **Therapy (Trauma)**     | Remove 1 Trauma stack from 1 Pokémon. Cost: 100₽ × (1 + stack count). Repeatable while affordable. (Per §6.2.4.) |


The Center node fires no combat. Pure utility. The player leaves and proceeds to the Gym.


---


# §7.7 Shop Nodes (Region-internal)


Distinct from the City Shop (§7.8.2). Smaller inventory; lighter curation.


## §7.7.1 Region Shop Inventory


| Slot Type                      | Count               | Pricing           |
| ------------------------------ | ------------------- | ----------------- |
| Consumables                    | 3 slots, randomized | 30–100₽ each      |
| Common Relic                   | 1 slot              | 150₽              |
| Uncommon Relic                 | 1 slot              | 300₽              |
| Pokéball                       | 1 slot              | 50₽               |
| Special slot (Held Item OR TM) | 1 slot              | Varies (250–500₽) |


Inventory is seeded per visit. The Region Shop appears once per Region at Layer 2 or 5.


## §7.7.2 Re-roll mechanic


For 25₽, the player can re-roll the Region Shop's inventory once per visit. Up to 3 re-rolls per visit total at escalating cost: 25₽ → 50₽ → 100₽.


---


# §7.8 City Interstitials (per §2.1.4, expanded)


Cities are post-Gym-1 and post-Gym-2 rest-and-restock zones. Three sequential events: Pokémon Center → Curated Shop → Reflection. Topic 7 fully specifies each.


## §7.8.1 City Pokémon Center (expanded from §2.1.4)


The City PC offers more than the Region-internal Center:


| Service                  | Effect                                                                               | Cost               |
| ------------------------ | ------------------------------------------------------------------------------------ | ------------------ |
| **Heal**                 | Full restore (same as Region Center)                                                 | Free               |
| **Move Tutor (curated)** | 5-move offer per Pokémon; Pokémon may also learn a tutor-exclusive move (per §5.4.2) | Free               |
| **Therapy**              | Trauma Salve service per §6.2.4                                                      | 100₽ × (1 + stack) |
| **Daycare**              | Deposit 1 Pokémon; receive +1 level instantly; that Pokémon skips next combat        | 200₽               |
| **PC Box**               | Inspect / reorder Box (also available from Map View)                                 | Free               |


## §7.8.2 City Curated Shop


The City Shop is the run's biggest economic surface. **Inventory is curated** based on team composition:


### §7.8.2.1 Curation algorithm


```javascript
For each item in the master shop pool:
   score = BaseRelevance
         + (TypeMatchToTeamComposition × 2)
         + (BuildArchetypeAlignment × 3)  -- Vanguard/Specialist/Support detected
         + (RarityScalar × Region)
         - (RecentDrop × 1.5)             -- avoid duplicate offerings
         + SeededJitter
The top 8 by score populate the shop, sorted by category.
```


### §7.8.2.2 City Shop slot layout (8 slots)


| Slot | Content                                                                  |
| ---- | ------------------------------------------------------------------------ |
| 1–2  | Consumables (typically tier-1 Potions, status cures)                     |
| 3    | Consumable tier-2 (Super Potion, Radar Scope, etc.)                      |
| 4    | Common Relic                                                             |
| 5    | Uncommon Relic                                                           |
| 6    | Rare Relic (50% chance present; otherwise replaced with second Uncommon) |
| 7    | Held Item (curated to team)                                              |
| 8    | TM (curated to team's CompatibleSpecies)                                 |


### §7.8.2.3 City Shop pricing


City pricing is ~30% higher than Region Shop pricing for equivalent items — players are paying for selection quality.


### §7.8.2.4 Sell mechanic


The City Shop accepts sells: any held inventory item can be sold for 30% of its listed buy price. This is the only Poké Dollar exit valve (Region Shops do not buy).


## §7.8.3 City Reflection — Region Modifier Selection


Per §2.1.4: 3 Region Modifiers offered, player picks 1.


### §7.8.3.1 Launch Region Modifier Pool (12 modifiers)


| Modifier              | Effect                                                                 | Tier                        |
| --------------------- | ---------------------------------------------------------------------- | --------------------------- |
| **Hand of Plenty**    | +1 max hand size for the next Region                                   | Strong                      |
| **Swap Fuel**         | Lead heals 5 HP per manual swap                                        | Strong                      |
| **Lucky Draw**        | Draw 1 extra consumable card on turn 1 of each combat                  | Medium                      |
| **Type Affinity**     | All moves of [chosen type] +10% damage for next Region                 | Strong (player-chosen type) |
| **Status Mastery**    | Status conditions applied by player last +1 turn                       | Medium                      |
| **Iron Skin**         | All Pokémon take −1 damage from Cleave intents                         | Niche                       |
| **Pocket Healer**     | First combat per node grants +5% Heal to all team on victory           | Medium                      |
| **Coin Purse**        | All Poké Dollar drops × 1.5 for next Region                            | Medium                      |
| **Bestiary Whisper**  | First Unknown intent of each combat is revealed                        | Niche                       |
| **Sturdy Lead**       | Lead Pokémon survives one lethal hit at 1 HP per combat (1 use/combat) | Strong                      |
| **Mass Mobilization** | Step-Forward and Step-Backward effects also draw 1 card                | Niche                       |
| **Trauma Resistance** | Each Trauma stack reduces MaxHP by 4% instead of 5% (cap unchanged)    | Strong                      |


Per City, the 3-modifier offering is seeded from this pool, weighted to surface options that synergize with current team composition (Type Affinity surfaces the player's most-common move type).


### §7.8.3.2 Modifier persistence


Per §2.1.4.1, both Region Modifiers stack and persist to run end. Confirmed and reaffirmed here.


---


# §7.9 Mystery Event Nodes


Mystery Events are themed, scripted vignettes with branching choices and rewards. The roguelike anchor for "story moments" and player expression.


## §7.9.1 Mystery Event Surface


When entered, a Mystery Event shows:

- A flavor scene (illustration + 1–3 paragraphs of text).
- 2–3 branching choices.
- Each choice has a visible cost or outcome preview where the design intent allows; some choices are deliberately Unknown (the gamble).

## §7.9.2 Launch Mystery Event Catalog (12 launch events)


| Event                     | Description                                      | Choices                                                                                                                                                        |
| ------------------------- | ------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Mysterious Stone**      | A mossy stone hums faintly.                      | (a) Take the stone → random Evolution Item. (b) Leave it.                                                                                                      |
| **Wandering Tutor**       | An old trainer offers to teach a forgotten move. | (a) Free Move Tutor service. (b) Decline (gain 100₽ as compensation).                                                                                          |
| **Berry Bush**            | A bush heavy with berries.                       | (a) Eat now → +30% HP to all Box. (b) Take berries → +3 Potion consumables.                                                                                    |
| **Daycare Recovery**      | An old couple offer to rest one Pokémon.         | (a) Remove all Trauma stacks from one Pokémon; that Pokémon skips next combat. (b) Decline. (See §6.2.4.)                                                      |
| **Slot Booth**            | A traveling carny offers a coin flip.            | (a) Wager 100₽ → 50% gain 250₽ / 50% lose. (b) Decline.                                                                                                        |
| **Wounded Pokémon**       | A wild Pokémon, badly hurt.                      | (a) Heal it → it joins your Box (free recruitment) but starts with 2 Trauma stacks. (b) Battle it (catch encounter, full HP — risky but unwounded). (c) Leave. |
| **Trainer's Dare**        | A rival challenges you.                          | (a) Accept (Elite Trainer fight, double rewards). (b) Decline (no penalty).                                                                                    |
| **Cursed Trinket**        | A glittering trinket on a pedestal.              | (a) Take it (random Rare relic; 30% chance to apply 2 Trauma stacks to a random Pokémon). (b) Leave.                                                           |
| **Old Map**               | A torn map fragment.                             | (a) Read it → reveals all node contents 2 layers ahead for this Region. (b) Sell to next shop for +200₽.                                                       |
| **Lost Backpack**         | An unattended pack.                              | (a) Take consumables (3 random consumables). (b) Take coins (200₽). (c) Leave (chance of returning later).                                                     |
| **Pokémon Bond Ceremony** | A shrine to bonding.                             | (a) Choose one Pokémon → permanently +1 stat to chosen stat. (b) Leave.                                                                                        |
| **Stat Trade**            | A wandering sage offers a stat swap.             | (a) Choose a Pokémon → swap Attack and Defense values for the rest of the run. (b) Leave.                                                                      |


## §7.9.3 Mystery Event Risk Profiles


Each event is tagged by risk profile:


| Profile      | % of pool | Description                                        |
| ------------ | --------- | -------------------------------------------------- |
| **Safe**     | 30%       | All choices net-positive or net-zero               |
| **Tradeoff** | 50%       | Choices net-positive but at some clear cost        |
| **Gamble**   | 20%       | At least one choice has explicitly unknown outcome |


The player can identify the profile from the event's visual badge (🟢 Safe / 🟡 Tradeoff / 🔴 Gamble) — Pillar 1 compliance even within "Mystery."


## §7.9.4 Event Repeatability


Events do not repeat within a single run. Once an event fires, it is removed from the seeded pool for the remainder of the run.


---


# §7.10 Region Aesthetic Specs


Per §2.1.2, the three Regions are aesthetically themed. Topic 7 locks the surface; Topic 10 owns final art/audio direction.


## §7.10.1 Region 1 — Verdant Route 🌿

- **Biome focus:** Meadow primary; River and Cave secondary.
- **Palette:** Saturated greens, soft yellows, sky blues. Cheerful and warm.
- **Audio:** Light flute-and-string; bird ambient. Combat: bright upbeat motif.
- **Enemy roster theme:** Bug, Normal, Grass, with the occasional Water from the river biome.
- **Gym types eligible:** Rock, Water, Bug, Normal (per §4.4.4.2).
- **City flavor:** "Pallet Plaza" — small-town comfort.

## §7.10.2 Region 2 — Coastal Cliffs 🌊

- **Biome focus:** Sea primary; River, Power Plant secondary.
- **Palette:** Cool blues, weathered grays, deep purples on the cliffs. Dynamic and dramatic.
- **Audio:** Crashing waves; gull cries; orchestral strings. Combat: tense building motif.
- **Enemy roster theme:** Water, Electric (storm-flavor), Bug (Sea variants).
- **Gym types eligible:** Fire, Grass, Electric, Poison.
- **City flavor:** "Vermilion Harbor" — bustling port.

## §7.10.3 Region 3 — Volcanic Highlands 🔥

- **Biome focus:** Volcano Slope primary; Cave, Sky, Abandoned Tower secondary.
- **Palette:** Reds, oranges, blacks, purples. Saturated and intense.
- **Audio:** Heavy percussion; brass; tremolo strings. Combat: high-tempo aggressive motif.
- **Enemy roster theme:** Fire, Rock, Psychic, Ghost.
- **Gym types eligible:** Psychic, Ground, Fighting, Ice.
- **City flavor:** N/A (Region 3 ends in Victory Road, not a City). Replaced by Victory Road's pre-Summit ambience.

---


# §7.11 Map Generation Algorithm


Pseudocode for deterministic map seeding (Topic 9 owns implementation):


```javascript
seed = RunState.Seed XOR RegionIndex
rng = SeededRNG(seed)

graph = LadderGraph(layers=8, defaultWidth=3)
graph.Layer(0).SetSingleNode(WildPokemonArea)
graph.Layer(4).EnableBranching()  -- two divergent paths

for layer in 1..7:
    foreach node in graph.Layer(layer):
        node.NodeType = WeightedSample(rng, NodeTypeDistribution(layer, region))

ApplyConstraint_NoAdjacentSameType(graph, rng)
ApplyConstraint_CenterAtLayer6(graph)
ApplyConstraint_EliteAtLayer3(graph)
ApplyConstraint_GymAtLayer7(graph)

foreach node:
    node.PreviewContent = LoadContentForNode(node.NodeType, rng)

return graph
```


Constraints are applied iteratively with fallback re-rolls if no valid configuration emerges within a turn count. Always converges within ~5 iterations on tested seeds.


---


# §7.12 Reward Tables — Master Reference


Aggregated reward tables for every node type. Single source for systems-designer balance review.


| Node Type               | Combat? | Poké Dollar | XP (Trainer)             | Drop                      | Special                       |
| ----------------------- | ------- | ----------- | ------------------------ | ------------------------- | ----------------------------- |
| Wild Pokémon Area       | Yes     | 0–25₽       | 5 (clear) + 10 (recruit) | Recruitment               | Catching mechanic             |
| Trainer Battle          | Yes     | 50–150₽     | 5                        | Loot table                | —                             |
| Elite Trainer           | Yes     | 300₽        | 25                       | Guaranteed Uncommon relic | —                             |
| Pokémon Center (Region) | No      | —           | —                        | —                         | Heal + Therapy + 1 Move Tutor |
| Shop (Region)           | No      | (spend)     | —                        | —                         | Curated inventory + re-roll   |
| Tutor / Daycare         | No      | (varies)    | —                        | —                         | Move learning OR XP boost     |
| Mystery Event           | Varies  | Varies      | Varies                   | Varies                    | Choice-driven                 |
| Gym Leader              | Yes     | 500₽        | 50                       | Rare relic                | Badge                         |
| City Pokémon Center     | No      | (varies)    | —                        | —                         | Full slate                    |
| City Shop               | No      | (spend)     | —                        | —                         | 8-slot curated                |
| City Reflection         | No      | —           | —                        | —                         | Region Modifier               |


---


# §7.13 Vertical Slice Carve-Out


| System                        | In VS                                                                         | Out of VS                                       |
| ----------------------------- | ----------------------------------------------------------------------------- | ----------------------------------------------- |
| Region 1 map (Layers 0–7)     | ✅ Full                                                                        | —                                               |
| Wild Pokémon Areas + 3 biomes | ✅ Meadow + Cave + River                                                       | Sea, Power Plant, Sky, etc.                     |
| Catching mechanic             | ✅ Pokéball v1                                                                 | Great/Ultra Ball tiers                          |
| Trainer Battle nodes          | ✅ 4 archetypes (Bug Catcher, Lass, Hiker, Sailor)                             | Engineer, Hex Maniac, Ace Trainer, Rocket Grunt |
| Elite Trainer node            | ✅ 1 archetype                                                                 | —                                               |
| Region Pokémon Center         | ✅ Full                                                                        | —                                               |
| Region Shop                   | ✅ Full + re-roll                                                              | —                                               |
| Mystery Events                | ✅ 4 launch events (Mysterious Stone, Berry Bush, Wandering Tutor, Slot Booth) | Full 12-event catalog                           |
| City interstitial             | ❌ Not in VS (VS ends at Gym 1)                                                | —                                               |
| Map generation algorithm      | ✅ Single-region                                                               | Cross-region pacing tuning                      |

