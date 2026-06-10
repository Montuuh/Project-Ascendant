<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Last updated from Notion: 2026-06-10T09:02:00.000Z -->

**Status:** 🟢 In Progress


**Last Updated:** 2026-05-15 (migrated from Drive; HP Economy patch applied)


**Cross-references:** Topic 1 (vertical slice), Topic 3 (combat phases), Topic 4 (Victory Road, League), Topic 6 (Trauma System).


---


# §2.1 Macro Loop ("The Run")


A run is the path from selecting a Starter to either defeating the Champion or having your Active Team wiped beyond recovery. A run unfolds in five phases.


## §2.1.1 Pre-Run Setup

- The player selects one **Starter Pokémon** from the unlocked pool. Default unlocks: Bulbasaur, Charmander, Squirtle. Additional Starters are unlocked through meta-progression (Topic 6).
- The player selects one **Starting Relic** (Trainer Item) — three offered, pick one.
- The player selects one **Region 1 Modifier** — three offered, pick one (per **CL-016**, §7.8.3). It applies for Region 1 only, then is re-chosen at each City (§2.1.4.1).
- The starting Box contains _only_ the Starter. The Active Team is the Starter alone until recruitment expands it.
- A run seed is generated (or pulled from the daily seed, post-launch).

## §2.1.2 Region Traversal (x3 regions)

- Each Region is presented as a **branching ladder map** (~6–8 layers tall, ~3 lanes wide), styled visually as a Pokémon overworld biome (e.g., Region 1 = _Verdant Route_, Region 2 = _Coastal Cliffs_, Region 3 = _Volcanic Highlands_).
- The player navigates layer by layer; each step offers 1–3 connected nodes.
- **Node 1 of every Region is always a Wild Pokémon Area** with multiple variant options (different biomes/sub-areas). This guarantees an early recruitment opportunity each Region.
- Each Region's map contains a **Gym branch point** — two diverging paths leading to two different Gym Leaders (different types, different Badges). The player chooses one path; the other is permanently abandoned for this run. (See §4.4.4.)
- Node categories (defined fully in Topic 7 — Scenario):
    - **Combat nodes** — wild Pokémon, trainers, elite trainers.
    - **Recruitment nodes** — Wild Pokémon Areas (catch attempts) and Special Events (unique/legendary recruits).
    - **Utility nodes** — Pokémon Center, Shop, Daycare/Move Tutor, Mystery Events.
    - **Region Boss node** — Gym Leader; mandatory final layer.

### §2.1.2.1 Map View


Between nodes, the player sees the upcoming map and accesses a persistent **Map View**, where they can:

- Reorder the Box and select the Active Team of 3 (Map-View Loadout, see §2.3).
- Inspect each Pokémon's moves, level, current HP, evolution status, and equipped held items.
- Inspect held relics and consumables.
- Trigger pending evolutions (see §5.3).
- Save & quit (the game auto-saves on every node entry).

## §2.1.3 Region Climax: Gym Leader

- The final node of every Region is a Gym Leader fight (the one chosen at the branch point).
- Each Gym has a **type identity** drawn from a tiered pool (see §4.4.4 for full pool design).
- Each Gym Leader is a **multi-phase encounter** (signature mechanic activates at HP thresholds), giving each Gym fight meaningful weight given that there are only three in a run.
- Defeating the Gym Leader awards:
    - A **Badge** (a permanent run-modifier active from this point until run end; see §4.4.5).
    - A **rare relic drop**.
    - Passage to the next phase (City after Gyms 1 and 2; Victory Road after Gym 3).

## §2.1.4 Post-Gym Interstitials: Cities


After defeating Gym 1 and Gym 2, the player enters a **City** — a rest-and-restock zone between Regions, modelled (per **CL-015**, Q1) as a **Choice Plaza**: an StS-style Act-end hub with a **limited visit budget** rather than a fixed sequence. The **Curated Shop** and the **Reflection node** are always available; the player may additionally visit only **2 of** the premium nodes — a real "what do I prioritise?" decision. Full node detail in §7.8.4.

- **Curated Shop (always):** rotating relic / consumable / TM inventory, weighted to the player's current team composition.
- **Reflection node (always, closes the City):** pick one of three **Region Modifiers** that apply only during the next Region (e.g., "+1 max hand size," "Lead heals 5 HP per swap"). The macro-loop equivalent of StS's Act-end campsite — where runs begin to differentiate from each other.
- **Premium nodes (pick 2):** **City Gym** (risky optional fight for a 4th Badge + Rare relic, §7.8.4 / §4.5.3); **Pokémon Center** (full heal + Trauma therapy + Daycare — now an _optional_ visit, no longer guaranteed); **Grand Dojo** (city-tier move/ability tutor, §7.14); **Black Market** (a Rare/Epic relic at an HP or Trauma cost).

### §2.1.4.1 Region Modifier stacking


**Per CL-016 (Q2), Region Modifiers are per-Region, not run-long-stacking.** Exactly **1 modifier is active per Region**, re-chosen at the start of each Region — a **pre-R1 pick at run setup (§2.1.1)**, then **City 1 (applies R2)** and **City 2 (applies R3)** — applying to **that Region only**: it expires when the Region ends, and modifiers do not stack or accumulate. This supersedes the previous "up to 2 active, persist to run end" rule and makes the modifier descriptions' "for the next Region" wording canonical. Relics and Badges remain the run-long stacking systems.


## §2.1.5 Victory Road (post-Gym 3, pre-League)


After defeating Gym 3, the player enters **Victory Road** — a dedicated pre-League preparation zone with its own branching mini-map (3–4 layers, 2–3 lanes). Victory Road is the most challenging non-boss traversal content in the game and the player's final window to prepare for the League. All paths converge at the Summit Preparation node. Full design detailed in §4.5.


## §2.1.6 League Finale

> ⚠️ **DEFERRED (2026-06-05)** — the League finale is parked until the R1 → Victory Road loop is complete (see Topic 4 §4.6/§4.7). Spec retained; not the current build target.

After Victory Road's Summit, the player enters the League — **5 sequential fights** with no map navigation:

- **Elite Four** — four sequential boss-tier trainers, each with a distinct type identity.
- **Champion** — final boss; multi-phase encounter; a 5-Pokémon team scaled to challenge a fully-evolved player team.

Between League fights: a **micro-rest** (30% HP restoration). No shop, no recruitment. This is the endurance pacing beat that distinguishes the League from Region traversal.


Defeating the Champion = run victory.


## §2.1.7 End of Run

- _Run failed (Active Team wiped):_ run-state is discarded; **Trainer XP** is awarded based on progress. (Detailed in Topic 6 — Roguelike Progression.)
- _Run won:_ full Trainer XP + victory bonus + run summary screen + (post-launch) leaderboard submission.
- Either outcome returns the player to the Trainer Hub.

**Total climactic encounters per run: 8** (3 Gyms + 4 Elite + 1 Champion). At the Pokémon level, this is 21 boss-tier Pokémon fights (see §4.4.2 for breakdown).


---


# §2.2 Region Escalation Philosophy


To prevent later Regions from feeling like "Region 1 with bigger numbers," each Region introduces a **mechanical accent** alongside its aesthetic theme:

- **Region 1 — Verdant Route:** baseline mechanics. Acts as the tutorial Region.
- **Region 2 — Coastal Cliffs:** introduces _status conditions on enemy intents_ (Burn, Paralysis become regularly telegraphed enemy actions).
- **Region 3 — Volcanic Highlands:** introduces _multi-enemy encounters_ (1 lead + 1–2 supports) and _enemy field effects_ (weather/terrain).
- **League:** combines all prior mechanics; each boss layers a unique signature mechanic on top.

These accents are placeholder hooks to be tuned in later topics. The commitment now: escalation is mechanical, not numeric.


---


# §2.3 Box & Active Team

- **Box:** the persistent run-roster of Pokémon. Default capacity = 6, upgradable to 8 via specific relics or meta-unlocks.
- **Active Team:** the 3 Pokémon brought into a given combat. Drawn from the Box.
- **Map-View Loadout:** the player freely reorders the Box and selects the Active Team on the Map View. Changes are committed via a **Confirm** gesture. The Active Team is **locked** the moment a node is entered. Loadout changes are only possible from the Map View (between nodes), not during combat.
- A Pokémon in the Box but not in the Active Team **contributes nothing to the deck** for that fight. This is the central tension of team selection.

## §2.3.1 Box overflow on recruitment


When the player attempts to recruit a Pokémon while the Box is at capacity (6 by default; 8 with relic/meta-unlock expansion), a forced "Swap or Skip" prompt appears:

- **Swap:** Choose one Box Pokémon to release. The released Pokémon is permanently removed from the run. The new Pokémon takes their place.
- **Skip:** Decline the recruitment. No Pokémon is gained or lost.

There is no "Deposit" pool. The Box is the only Pokémon storage container in a run.


---


# §2.4 HP Economy


Pokémon HP is a **persistent resource that carries across combats and nodes**, not a per-combat reset. A Pokémon entering combat at 30% HP starts that combat at 30% HP.


## §2.4.1 Fainted state definition


A Pokémon's `CurrentHP` reaching 0 is the fainted state. There is no separate `IsFainted` flag — 0 HP and Fainted are equivalent. A fainted Pokémon (`CurrentHP == 0`) cannot be in the Active Team; they remain in the Box until healed above 0 HP.


## §2.4.2 Healing event taxonomy

- **Full heal** (Pokémon Center, Victory Road Summit Preparation): sets all Box Pokémon's `CurrentHP` to `MaxHP × (1 - TraumaPenalty)`, where `TraumaPenalty` is determined by the Trauma System (see Topic 6 — Roguelike Progression). Revives fainted Pokémon to their Trauma-adjusted Max HP.
- **Partial heal — percentage-of-Max** (League micro-rest, 30%): for every Box Pokémon, sets `CurrentHP` to `max(CurrentHP, floor(EffectiveMaxHP × 0.3))`, where `EffectiveMaxHP = MaxHP × (1 - TraumaPenalty)`. Revives fainted Pokémon to 30% of their Trauma-adjusted Max HP.
- **Consumable heal** (in-combat items): restores the listed flat or percentage amount up to `EffectiveMaxHP`. Cannot revive fainted Pokémon during combat unless the consumable explicitly states "Revives" (no current launch consumable carries this flag).
- **Move effect heal** (in-combat moves): same rule as consumable heal.

## §2.4.3 In-combat revival is forbidden by default


Fainting during combat is a permanent loss of that Pokémon's deck contribution for the remainder of that combat. This is intentional — fainting is a meaningful tactical loss, not a recoverable one.


## §2.4.4 Fainting consequence — Trauma


Each time a Pokémon faints (reaches 0 HP), it accrues a permanent run-scoped penalty. See Topic 6 for the full Trauma System specification.


---


# §2.5 Macro Loop — Visual Summary


_Gym types per Region are seeded-randomly drawn from a tiered pool (4 types per tier × 3 tiers = 12 total Gym types). Player earns 3 Badges per run — one per Region from the chosen path. Up to 1 bonus Badge available through rare in-run sources. Maximum 4 Badges per run from a pool of 12._


---


# §2.6 Trauma System Reference (added 2026-05-24 — pointer to §6.2)


Topic 2 §2.4.4 hooks the Trauma System. The full mechanic specification, application rules, clearing sources, and edge cases are specified in Topic 6 §6.2. Summary anchor:

- **Stack:** +1 per faint, multiplicatively reduces Effective Max HP by 5%, soft-capped at 5 stacks (-25%).
- **Persistence:** Across combats, nodes, Cities. Cleared at run end OR via Trauma Salve relic / Therapy service / Daycare Mystery Event.
- **Healing:** All healing events compute against `EffectiveMaxHP`, not `BaseMaxHP`.

See §6.2 for full spec.


# §2.7 City Sequence Detail Pointer (added 2026-05-24)


Per §2.1.4, the City is a **Choice Plaza** (CL-015, §7.8.4 — limited visit budget; Center now optional). Topic 7 §7.8 expands the City Pokémon Center service catalog (now includes Daycare for +1 level, PC Box reorder, Therapy for Trauma), the City Shop curation algorithm (8 slots, team-aware), and the launch Region Modifier pool (12 modifiers).

