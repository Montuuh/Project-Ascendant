<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Source: https://www.notion.so/3610450715b4816c83d2c74682cef77c -->
<!-- Exported: 2026-05-26T16:28:12.862Z -->
<!-- To update: run `node docs/scripts/export-gdd.js` and commit -->

**Status:** 🔒 Locked


**Last Updated:** 2026-05-24 (Trauma System locked Option E + cap; meta-progression spec authored; starter unlocks, relic pool, difficulty, achievements defined)


**Cross-references:** Topic 1 (§1.6 starter unlocks, §1.7 difficulty modifiers), Topic 2 (§2.4.4 Trauma penalty hook, §2.4.2 healing formula), Topic 4 (§4.3.9 Bestiary tiers — adjacent system), Topic 5 (§5.2 XP & Leveling — in-run XP is separate), Topic 8 (Trauma Salve relic, Type Plates for starter unlocks).


---


# §6.1 Scope & Philosophy


Topic 6 governs everything that persists between runs. Three macro guarantees:

1. **Failure is fuel.** Every run, won or lost, materially improves the player's next run via Trainer XP and Bestiary mastery (Bestiary detailed in §4.3.9).
2. **Unlocks expand the option space, never the power floor.** Meta-unlocks add new Pokémon, relics, and starters — they do not buff base stats, damage, or HP. A first-run player and a 100-hour player face the same numerical baseline; the veteran simply has more _choices_ available.
3. **Trauma System** (per-run permanent faint penalty) creates the in-run consequence layer that prevents the meta-progression-fuel design from feeling consequence-free.

Anti-pattern explicitly rejected: "permanent stat upgrades" that gradually trivialize content. Project Ascendant's meta-progression is **horizontal** (more options) rather than **vertical** (more power).


---


# §6.2 Trauma System — LOCKED (Option E: Hybrid Stacks + Soft Cap)


Locks the open decision tabled in §2.4.4 and the BACKLOG Trauma Options. Decision rationale and rejected options preserved in §6.2.7 for archival traceability.


## §6.2.1 Mechanic


Each time a Pokémon's `CurrentHP` reaches 0 during combat (the fainted state, per §2.4.1), they accrue **one Trauma stack** for the remainder of the current run. Trauma is **per Pokémon instance** and is cleared only by run end or by an explicit Trauma-clearing source (§6.2.4).


**Effective Max HP formula:**


```javascript
EffectiveMaxHP = floor( BaseMaxHP × (1 − 0.05 × min(TraumaStacks, 5)) )
```


| Stacks  | Multiplier    | Effective Max HP (Base = 100) |
| ------- | ------------- | ----------------------------- |
| 0       | 1.00          | 100                           |
| 1       | 0.95          | 95                            |
| 2       | 0.90          | 90                            |
| 3       | 0.85          | 85                            |
| 4       | 0.80          | 80                            |
| 5 (cap) | 0.75          | 75                            |
| 6+      | 0.75 (capped) | 75                            |


**Soft cap rationale:** Beyond 5 stacks, additional faints accrue no further penalty. This prevents an "unrecoverable spiral" anti-pattern — a Pokémon that has fainted six times is functionally indistinguishable from one that has fainted five.


## §6.2.2 Application Timing — LOCKED


Trauma stacks **apply instantly at the moment of faint**, during the Resolution Phase, immediately after the faint resolution defined in §3.3.5.

- **In-combat visibility:** The Trauma badge does NOT appear during combat. The fainting Pokémon is already removed from the Active Team for the remainder of that combat (§3.3.5), so the penalty is irrelevant until the Map View.
- **Map View visibility:** Trauma stacks are displayed as a small numerical badge over the Pokémon's portrait (`⚠ 2` for two stacks). Hovering shows current Effective Max HP and the formula.
- **First-faint reveal:** The first time a Trauma stack is applied during a run, a one-time tutorial popup explains the system. Subsequent applications are silent.

**Healing interaction:** All healing events (§2.4.2) compute against `EffectiveMaxHP`. A Pokémon Center fully restores the Pokémon to `EffectiveMaxHP`, NOT to `BaseMaxHP`. This is the load-bearing consequence of Trauma — full heals never "undo" prior faints, only restore the current Trauma-adjusted ceiling.


## §6.2.3 Stack Persistence Rules

- **Within a run:** Stacks persist across combats, nodes, Cities, Region transitions, and Victory Road. They do NOT decay over time.
- **Across runs:** Stacks are discarded at run end (win or lose) along with the rest of run-state. Trauma is a run-scoped concept only.
- **Box deposit:** A Pokémon in the Box (but not Active Team) still carries its Trauma stacks. Trauma is owned by the `PokemonInstance`, not by the Active Team slot.
- **Recruitment:** A newly recruited wild Pokémon starts with 0 Trauma stacks regardless of how many faints occurred in the run prior. Trauma is per-instance, not run-cumulative.
- **Evolution interaction:** Trauma stacks **carry through evolution** (Squirtle → Wartortle keeps its Trauma stacks). Rationale: evolution is identity continuity. The new BaseMaxHP is multiplied by the same Trauma factor. Net Effective Max HP still increases because the evolution stat gain dwarfs the multiplicative loss.

## §6.2.4 Trauma Clearing Sources — LOCKED


Three explicit paths to remove Trauma stacks within a run. All three are scarcity-gated to preserve the in-run consequence:


| Source                                       | Type                             | Cost                                                 | Effect                                                                                                                     |
| -------------------------------------------- | -------------------------------- | ---------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **Trauma Salve**                             | Uncommon Relic (single-charge)   | Drop at Cities, Gym rewards, Mystery nodes           | Removes ALL Trauma stacks from one chosen Pokémon. Consumed on use.                                                        |
| **Therapy (Move Tutor service)**             | City Pokémon Center node service | Poké Dollars — base 100₽ × (1 + StackCount)          | Removes 1 Trauma stack from one chosen Pokémon. Repeatable while affordable.                                               |
| **Daycare Recovery (Mystery Event variant)** | Rare Region-2/3 Mystery node     | Skip the node's reward; one Pokémon stays at Daycare | Removes all Trauma stacks from one chosen Pokémon. The Pokémon is excluded from the next 1 combat (returns automatically). |


**Design intent:** The player can always recover, but recovery has cost — gold, an inventory slot (Trauma Salve), or a tempo sacrifice (Daycare). This keeps Trauma a meaningful threat throughout the run while preventing soft-locks on unlucky early faint clusters.


## §6.2.5 Telegraphing & UI


Per Pillar 1 (Telegraphed Tactics), Trauma is never a hidden cost:

- **Map View badge:** `⚠ N` over portrait. Hover tooltip shows Effective Max HP and full stack count.
- **Pre-combat preview:** Before entering a combat node, the team-selection panel shows each Pokémon's Effective Max HP and Trauma stack count.
- **Combat HP bar:** The HP bar maximum reflects `EffectiveMaxHP`. There is no visual indication of the "lost" ceiling — the HP bar simply caps at the current effective amount. This is intentional: the bar reflects the truth of the moment, not a phantom max.
- **Faint feedback:** When a faint applies a Trauma stack, the post-combat result screen shows `+1 Trauma → [Pokémon]` with the new effective max preview.

## §6.2.6 Edge Cases & Resolutions


| Edge case                                                                            | Resolution                                                                                                                     |
| ------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| Pokémon at exactly 1 HP takes lethal damage — multiple Trauma stacks in one combat?  | One faint = one stack. A Pokémon cannot faint twice in a combat (already removed from Active Team on first faint, per §3.3.5). |
| Sturdy passive saves at 1 HP — does it accrue Trauma?                                | No. Sturdy explicitly prevents the faint. No HP-reaches-0 event fires, no Trauma.                                              |
| Last Stand League Boon — fight-end with all Pokémon at 1 HP via Last Stand — Trauma? | No. Last Stand likewise prevents the faint event.                                                                              |
| Trauma Salve used on a 0-stack Pokémon?                                              | UI greys out the option. Salve cannot be wasted.                                                                               |
| Therapy cost when StackCount = 0?                                                    | Therapy option hidden in Tutor UI.                                                                                             |
| Champion (final boss) sees Trauma stacks on player team?                             | No. Trauma is player-side persistent state, not visible to AI scoring. Boss AI ignores it.                                     |
| Apex Pokémon recruited from Victory Road (§4.5.1.2) — starts with Trauma?            | Always 0 stacks at recruitment.                                                                                                |


## §6.2.7 Rejected Alternatives (Archival)


For traceability — preserving the design conversation that led to Option E:

- **Option A (uncapped stacks):** Rejected — unrecoverable spiral on unlucky cluster faints in Region 3.
- **Option B (diminishing revival, Pokémon-Center-only):** Rejected — penalty hits only at Pokémon Center, which means a Pokémon that faints in Region 3 Layer 8 takes zero penalty before the League. Defeats the purpose.
- **Option C (skill suppression — lock one move next combat):** Rejected — deck-economy impact is too volatile. Discourages risk-taking too aggressively and fights against Pillar 2 (Every swap is a decision) by punishing the very repositioning the system rewards.
- **Option D (drop entirely):** Rejected — invalidates the user-requested consequence layer.
- **Option E (chosen):** Hybrid A + cap. Visible, telegraphed, recoverable via three explicit paths, never spirals.

---


# §6.3 Trainer XP & Trainer Level


## §6.3.1 Trainer XP — what it is


Trainer XP is the persistent meta-currency. Earned during runs (won or lost). Spent on permanent unlocks via the Trainer Hub (§6.4). Trainer Level is the player's "account level" and gates content tier unlocks.


**Trainer XP is not power.** Spending Trainer XP unlocks new starter Pokémon, new relics in the run-pool, new difficulty modifiers, and new Hub upgrades — but never increases damage, HP, or any baseline combat number. Per §6.1.


## §6.3.2 Trainer XP Sources


| Source                        | XP Award                 | Notes                                                           |
| ----------------------------- | ------------------------ | --------------------------------------------------------------- |
| Combat node cleared           | 5 XP                     | Wild, Trainer, Elite all flat. Encourages every combat.         |
| Recruitment                   | 10 XP                    | One-time per species per run. Encourages diverse rosters.       |
| Evolution triggered           | 15 XP                    | One-time per Pokémon per run.                                   |
| Gym Leader defeated           | 50 XP                    | × 3 per winning run.                                            |
| Victory Road Gauntlet cleared | 75 XP                    | Per Gauntlet node.                                              |
| Elite Four (per member)       | 100 XP                   | × 4.                                                            |
| Champion defeated             | 250 XP                   | Win bonus.                                                      |
| Run failed bonus              | floor(Run_Progress × 50) | Where Run_Progress is the count of layers cleared. Caps at 400. |
| Bestiary tier promotion       | 25 / 75 / 200 XP         | Familiar / Veteran / Master per species (one-time).             |
| Achievement unlock            | 50–500 XP                | See §6.7.                                                       |


**Average per run:**

- **Failed run (Region 1 wipe):** ~80–150 XP
- **Failed run (Region 3 wipe):** ~400–600 XP
- **Won run (no achievements):** ~900–1200 XP
- **Won run (achievement-heavy):** up to ~2000 XP

## §6.3.3 Trainer Level Curve


Trainer Level gates Hub upgrades and unlock tiers. Curve is **soft-logarithmic** — early levels come fast; later levels gate long-tail mastery content.


```javascript
XP to reach Trainer Level N (cumulative) = floor( 500 × N^1.6 )
```


| Trainer Level | Cumulative XP | XP from previous |
| ------------- | ------------- | ---------------- |
| 1             | 0             | —                |
| 2             | 500           | 500              |
| 3             | 1,517         | 1,017            |
| 4             | 3,031         | 1,514            |
| 5             | 5,000         | 1,969            |
| 7             | 10,348        | —                |
| 10            | 19,952        | —                |
| 15            | 43,267        | —                |
| 20            | 75,789        | —                |
| 25            | 117,151       | —                |
| 30            | 167,290       | —                |


**Target pacing:**

- Trainer Level 5 ≈ end of first weekend of play (~10 hours).
- Trainer Level 10 ≈ unlocks all 3 meta-starters + 2 Hub upgrades.
- Trainer Level 20 ≈ "completionist" tier; all run-content unlocks visible.
- Trainer Level 30 ≈ prestige cap; future Ascension-mode entry.

## §6.3.4 Currencies — Two-Track System


To avoid the "XP-funnel" trap (one bar, every unlock competes), Project Ascendant uses a two-track meta-currency model:

- **Trainer XP:** Drives Trainer Level. Earned passively, spent automatically (level-ups are free).
- **Trainer Tokens:** Spent manually at the Hub for chosen unlocks. Earned per run: `floor(TrainerXP_Earned_This_Run / 100)`. Capped at 50 per run.

**Why two tracks:** XP guarantees progress (every run feels productive); Tokens give the player **agency** (choose what to unlock next). Standard roguelike meta-progression pattern.


---


# §6.4 Trainer Hub


The Trainer Hub is the post-run / pre-run menu space. It is **not** a 3D explorable environment for launch — it is a clean 2D menu hub styled as a Pokémon Center interior, with selectable kiosks.


## §6.4.1 Hub Layout (5 Kiosks)


| Kiosk                | Function                                                                        | Unlock                                    |
| -------------------- | ------------------------------------------------------------------------------- | ----------------------------------------- |
| **PC Terminal**      | View Bestiary, run history, statistics, achievements                            | Available from start                      |
| **Trainer Card**     | View Trainer Level, total XP, Tokens, profile stats                             | Available from start                      |
| **Pokémart Counter** | Spend Trainer Tokens on unlocks (starters, relic pool slots, Hub upgrades)      | Available from start                      |
| **Daycare Lady**     | Configure starting roster (post-meta-unlock), difficulty modifiers, run options | Trainer Level 3                           |
| **Mystery Door**     | Daily Seed run, leaderboard view, prestige Ascension entry                      | Post-launch (Trainer Level 15 to preview) |


## §6.4.2 Hub Upgrade Tree


Persistent upgrades to the Hub itself, unlocked at the Pokémart for Trainer Tokens. Each upgrade is a **quality-of-life** or **option-expanding** unlock — never raw power.


| Upgrade                            | Cost (Tokens) | Effect                                                                                                         | Prereq           |
| ---------------------------------- | ------------- | -------------------------------------------------------------------------------------------------------------- | ---------------- |
| **Expanded Box (8 slots)**         | 30            | Default Box capacity 6 → 8 for all future runs                                                                 | Trainer Level 5  |
| **Curated Starting Relic +1**      | 25            | Run start offers 4 Starting Relics instead of 3                                                                | Trainer Level 4  |
| **Bestiary Insight**               | 40            | First combat against any unseen species at Familiar tier reveals 1 random intent for free                      | Trainer Level 6  |
| **Apex Pokémon Reveal**            | 50            | Victory Road Apex Pokémon species shown on Region 3 entry                                                      | Trainer Level 8  |
| **Second Starter Slot (Twin Run)** | 100           | Run start lets you choose TWO starters; Box starts at +1 capacity to absorb. Active Team size is unchanged (3) | Trainer Level 12 |
| **Difficulty Modifier Slot +1**    | 75            | Allows stacking 2 difficulty modifiers per run (vertical slice ships with 1-slot baseline)                     | Trainer Level 10 |
| **Trauma Salve Cache**             | 20            | Each run, the City 1 shop is guaranteed to stock at least 1 Trauma Salve                                       | Trainer Level 5  |


Hub upgrades are one-time purchases. Once bought, they are permanent.


## §6.4.3 Trainer Card — Statistics Surface


The Trainer Card displays:

- Trainer Level + XP progress bar
- Token balance
- Total runs (won / lost)
- Fastest run time
- Highest difficulty cleared
- Bestiary completion %
- Achievement completion %
- Total Pokémon recruited / evolved / mastered
- Favorite Lead (most-Lead-turns species)

Surface for both pride and goal-setting. No mechanical effect — pure profile.


---


# §6.5 Starter Pokémon Unlocks


Promised in §1.6: 6 starters total (3 default + 3 meta-unlocked). Locked specifications:


## §6.5.1 Default Starters (Available from Run 1)


| Starter        | Type         | Branch Archetypes Available                |
| -------------- | ------------ | ------------------------------------------ |
| **Bulbasaur**  | Grass/Poison | Vanguard, Specialist, Support (3 branches) |
| **Charmander** | Fire         | Vanguard, Specialist, Support (3 branches) |
| **Squirtle**   | Water        | Vanguard, Specialist, Support (3 branches) |


Per §5.3.3, starter Pokémon get 3 evolution branches (versus 2 for standard species).


## §6.5.2 Meta-Unlocked Starters


Three additional starters, each tied to a clear unlock criterion. Designed to widen build diversity rather than escalate power.


| Starter     | Type     | Unlock Criterion                                                                              | Token Cost | Design Slot Filled                                                                        |
| ----------- | -------- | --------------------------------------------------------------------------------------------- | ---------- | ----------------------------------------------------------------------------------------- |
| **Pikachu** | Electric | Complete first run (any outcome reaching Region 2)                                            | 50         | The "iconic mascot" pick; rewards persistence. Ranged-leaning kit.                        |
| **Eevee**   | Normal   | Win one run AND recruit any 4 different evolutions across runs                                | 100        | The "branch-flex" pick; 4 evolution branches (vs 3 for others). Build diversity exemplar. |
| **Riolu**   | Fighting | Defeat Champion using a team containing no fully-evolved Pokémon ("Underdog Run" achievement) | 75         | The "late bloomer" pick; weaker early, strong final form (Lucario). Reward for mastery.   |


Each meta-starter ships with full 3-branch (or 4-branch for Eevee) evolution lines. Token cost is paid once; starter becomes permanently selectable.


## §6.5.3 Starter-Specific Run Modifiers


When a meta-unlocked starter is selected, a small thematic run-modifier activates:

- **Pikachu runs:** Light Ball Held Item starts equipped to Pikachu (+25% Electric move damage on Pikachu only).
- **Eevee runs:** First Mystery node visited is guaranteed to be a "Stone Cache" (offers a free evolution stone of player's choice from 4).
- **Riolu runs:** Starting Trainer Relic pool is biased toward Fighting-synergy relics.

These modifiers are flavor-grade; balance-neutral by intent.


---


# §6.6 Relic Pool Expansion


Per §1.6: ~50 launch relics target. Topic 8 will author the relic content; Topic 6 governs which relics are _available_ in a given run's pool.


## §6.6.1 Relic Availability Tiers


| Tier                    | # Relics | Available From    | Unlock Mechanism                                        |
| ----------------------- | -------- | ----------------- | ------------------------------------------------------- |
| **Tier 1 — Foundation** | 20       | Run 1             | Always in pool                                          |
| **Tier 2 — Discovered** | 20       | Progressive       | Unlocked individually by triggering specific run events |
| **Tier 3 — Mastery**    | 10       | Trainer Level 10+ | Unlocked via Tokens at the Pokémart, 5 Tokens each      |


**Tier 2 example unlocks (illustrative — Topic 8 will finalize):**

- "Smoke Ball" relic unlocks after fleeing 3 wild combats (across any runs) — fits its thematic identity.
- "Lucky Egg" unlocks after earning XP in 50 combats.
- "Soothe Bell" unlocks after winning a run without using the Trauma Salve relic.

This creates ongoing discovery — even at Trainer Level 20, a relic the player hasn't yet triggered is still locked.


## §6.6.2 Per-Run Pool Construction


When a run starts, the active relic pool is computed:

- All Tier 1 relics (20).
- All unlocked Tier 2 relics (variable, 0–20).
- All unlocked Tier 3 relics (variable, 0–10).
- A run-seed-stable subset of this pool drives all in-run relic drops.

**Drop weighting:** Common 60% / Uncommon 30% / Rare 10%, regardless of unlocked tier composition. Tier ≠ rarity (Tier is META-unlock status; rarity is in-run drop weight).


## §6.6.3 Starting Relic Curation


Per §2.1.1, the player picks 1 of 3 Starting Relics at run start (4 if Curated Starting Relic +1 Hub upgrade owned). The Starting Relic pool is **biased toward Common-and-Uncommon, never Rare** — Starting Relics set a build direction, not the build itself.


---


# §6.7 Achievement System


Achievements are challenge goals that grant Trainer XP and serve as the primary unlock signal for Tier 2 relics and meta-starters.


## §6.7.1 Achievement Categories (8 launch categories)


| Category           | Example                                                                                                                                  | XP Reward |
| ------------------ | ---------------------------------------------------------------------------------------------------------------------------------------- | --------- |
| **First Steps**    | Win your first combat. Complete your first run.                                                                                          | 50–100    |
| **Recruitment**    | Recruit 25 different species. Recruit a Pokémon at full Box.                                                                             | 100–200   |
| **Evolution**      | Trigger 10 evolutions. Evolve into all 3 branches of one species (across runs).                                                          | 150–250   |
| **Mastery**        | Master one species (Master tier, §4.3.9). Master 10 species.                                                                             | 200–400   |
| **Combat**         | Win a fight without taking damage. Win a fight using only Ranged moves. Win using only one Pokémon's cards.                              | 100–300   |
| **Boss**           | Defeat each Gym Leader (one per Region tier). Defeat Champion. Defeat Champion in under 90 minutes.                                      | 200–500   |
| **Build Identity** | Win a run with all-Water team. Win a run with no evolution triggered ("Pure Form" run). Win using only the starter ("Solo Trainer" run). | 300–500   |
| **Endurance**      | Win 5 consecutive runs. Survive Region 3 without Pokémon Center healing.                                                                 | 200–500   |


**Achievement count target:** ~50 launch achievements distributed across the categories above.


## §6.7.2 Achievement Surface


PC Terminal displays the full achievement list, sorted by category. Each achievement shows: name, description (or `???` if hidden), reward, progress bar where applicable. Completed achievements display the completion date.


## §6.7.3 Hidden vs Visible Achievements

- **Visible (~80%):** Description shown from the start. Players can chase them.
- **Hidden (~20%):** Description revealed only upon completion. Reserved for discovery-flavor achievements ("First time using a 4-AP Ultimate move" type beats).

---


# §6.8 Difficulty Modifier System


Per §1.7: a stackable difficulty-modifier system (Ascension/heat-style). Architecture commitment was already made; Topic 6 locks the content surface.


## §6.8.1 Modifier Structure


Each modifier is a `DifficultyModifierSO` ScriptableObject. At run start, the player selects 0–N modifiers from the unlocked pool. Default `N = 1`; Hub upgrade `Difficulty Modifier Slot +1` raises it to 2. (Post-launch Ascension mode raises further.)


Each modifier specifies:

- **Display name + flavor text**
- **Mechanical effect** (one or more)
- **XP multiplier** (modifier stacks multiplicatively on Trainer XP earned this run)
- **Unlock criterion** (typically a prior achievement or Trainer Level threshold)

## §6.8.2 Launch Modifier Pool (10 modifiers)


| Modifier               | Effect                                                                                                              | XP Mult | Unlock                            |
| ---------------------- | ------------------------------------------------------------------------------------------------------------------- | ------- | --------------------------------- |
| **Iron Will**          | All wild encounters have +20% HP                                                                                    | ×1.15   | Trainer Level 3                   |
| **Tight Schedule**     | League micro-rest heals 20% instead of 30%                                                                          | ×1.15   | Trainer Level 4                   |
| **No Refunds**         | Consumables are expended after use (do NOT return at combat end)                                                    | ×1.30   | Trainer Level 6                   |
| **Dense Fog**          | All non-boss enemies start with one Unknown intent                                                                  | ×1.15   | Trainer Level 5                   |
| **Box Squeeze**        | Box capacity is 4 instead of 6 (cannot expand via Hub upgrade)                                                      | ×1.20   | Trainer Level 7                   |
| **Trauma Surge**       | Trauma stacks reduce Max HP by 7% instead of 5% (cap unchanged)                                                     | ×1.20   | Trainer Level 8                   |
| **Greater Threats**    | Region 1 enemies use Region 2 stat tier; Region 2 → Region 3 tier; Region 3 → Champion tier                         | ×1.40   | Trainer Level 10                  |
| **Faint Echo**         | Fainted Pokémon's discarded cards are NOT removed from the discard pile until end of next turn (jamming hand draws) | ×1.20   | Trainer Level 9                   |
| **One Path**           | Both Gym branches show the same Gym type — no informed choice                                                       | ×1.10   | Trainer Level 4                   |
| **Master's Challenge** | All bosses gain one extra phase (Phase 3 universal, ace gets Phase 4)                                               | ×1.50   | Trainer Level 15 + Champion clear |


## §6.8.3 Stacking Rules

- XP multipliers stack **multiplicatively**: 2 modifiers with ×1.15 and ×1.20 → ×1.38.
- Mechanical effects stack **additively where comparable**, otherwise independently. E.g., Trauma Surge + Iron Will both apply fully.
- Mutually exclusive pairs (rare): authored on the SO as a `ConflictsWith[]` list. UI prevents conflicting selections.

## §6.8.4 No "Inverse" Difficulty


There is no "make the game easier" modifier in Project Ascendant. Easier play = play without modifiers. Lowest difficulty = baseline. This is intentional — preserves the "every run is real" feel; eliminates the "I won but on baby mode" embarrassment.


---


# §6.9 Bestiary Integration (cross-ref to §4.3.9)


The Bestiary system is fully defined in §4.3.9 (Combat). Topic 6 specifies:

- **Bestiary persistence:** Tracked across all runs, persists per-account. Stored as a single `BestiaryProgressSO` runtime instance, serialized to disk via the SaveSystem (Topic 9).
- **Trainer XP awards:** Bestiary tier promotions (§4.3.9.1) award one-time XP per §6.3.2.
- **PC Terminal surface:** Bestiary is the primary content of the PC Terminal kiosk. Browsable by species, filterable by tier (Unfamiliar / Familiar / Veteran / Master).
- **Mastery Move authoring scope — LOCKED:** Launch ships Mastery Moves for the ~30 implemented evolution lines. Mastery Move design and writing is **out of vertical slice scope** but is a launch requirement. Mastery tier (Master, the 50/25/10 kill threshold) is the gating mechanism for these unlocks — i.e., Mastery Moves exist as content but are post-VS earning content.

---


# §6.10 Save/Load — Meta-Progression Persistence


Detailed implementation lives in Topic 9. Topic 6 specifies the persistence surface:

- **`MetaProgressionSO`** — singleton ScriptableObject; the runtime instance is serialized to disk.
    - `TrainerXP : int`
    - `TrainerLevel : int` (derived but cached)
    - `Tokens : int`
    - `UnlockedStarters : string[]` (species IDs)
    - `UnlockedRelicsTier2 : string[]`
    - `UnlockedRelicsTier3 : string[]`
    - `UnlockedDifficultyModifiers : string[]`
    - `UnlockedHubUpgrades : string[]`
    - `Achievements : Dictionary<string, AchievementProgress>`
    - `BestiaryProgress : Dictionary<string, SpeciesMastery>`
    - `Statistics : RunStatisticsSummary`
- **Save trigger:** At run end (any outcome) AND on every Pokémart purchase.
- **Format:** Binary serialization with a JSON debug-export option. Versioned schema (a `SchemaVersion` field for forward migration).
- **Corruption recovery:** Last-known-good backup retained automatically.

---


# §6.11 Cross-System Dependencies Resolved


This locks several previously-open spec questions surfaced by Topics 1–5:

- **§1.6 promise — 6 starters:** ✅ Resolved (§6.5).
- **§1.6 promise — 50 relics:** ✅ Surface defined (§6.6); content authored in Topic 8.
- **§1.7 commitment — stackable difficulty modifiers:** ✅ Resolved (§6.8).
- **§2.1.6 promise — XP awards on failed runs:** ✅ Resolved (§6.3.2 run-failed bonus formula).
- **§2.4.4 hook — Trauma System spec:** ✅ Resolved (§6.2).
- **§4.3.9 — Mastery Move authoring scope:** ✅ Resolved (§6.9 — full ~30 lines as launch content).

---


# §6.12 Vertical Slice Carve-Out


Topic 6 systems in the Region 1 vertical slice:


| System                                 | In VS                                          | Out of VS                                        |
| -------------------------------------- | ---------------------------------------------- | ------------------------------------------------ |
| Trauma System                          | ✅ Full implementation                          | —                                                |
| Trainer XP award + Trainer Level curve | ✅                                              | —                                                |
| Trainer Hub menu                       | ✅ Skeletal (Trainer Card + PC Terminal kiosks) | Daycare Lady, Mystery Door kiosks                |
| Starter unlocks                        | ✅ Defaults only (3 starters)                   | Meta-unlock paths (3 additional starters)        |
| Relic Tier system                      | ✅ Tier 1 (20 relics) only                      | Tier 2 / Tier 3 unlock plumbing                  |
| Achievement system                     | ✅ ~10 achievements                             | Full 50-achievement scope                        |
| Difficulty modifiers                   | ✅ 2-3 modifiers wired through                  | Full 10-modifier pool                            |
| Bestiary                               | ✅ Tracking & Familiar tier reveal              | Veteran/Master rewards (shinies, Mastery Moves)  |
| Save/load                              | ✅ Full meta-persistence                        | Schema migration logic (no v0→v1 to migrate yet) |


---


# §6.13 Glossary Additions for Topic 6

- **Trauma stack:** Per-Pokémon-instance, per-run counter incremented on faint. Multiplicatively reduces MaxHP by 5% per stack, capped at 5 stacks (-25%).
- **Effective Max HP:** `BaseMaxHP × (1 − 0.05 × min(TraumaStacks, 5))`. The current HP ceiling for all healing.
- **Trainer XP:** Persistent meta-XP. Drives Trainer Level.
- **Trainer Level:** Account-level metric. Gates Hub upgrades and modifier unlocks.
- **Trainer Token:** Earned-per-run currency. Spent on chosen unlocks. 1 Token per 100 Trainer XP earned that run, capped at 50/run.
- **Trainer Hub:** Pre/post-run menu space; kiosk-driven.
- **Hub upgrade:** Permanent quality-of-life or option-expanding unlock purchased with Tokens.
- **Tier 1/2/3 relic:** Meta-unlock status of a relic (NOT in-run rarity).
- **Difficulty modifier:** Run-start opt-in challenge layer; multiplies Trainer XP earned this run.
