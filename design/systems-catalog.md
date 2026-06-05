# Project Ascendant — Master Systems Catalog

> **Author:** `gdd-steward` (master designer), from the Game Codex + GDD snapshot
> **2026-06-05**. This is a *design* enumeration — every system, what it is, what
> it does, its key numbers, and what it touches. It is the companion to
> [`systems-index.md`](systems-index.md) (which tracks *implementation* status).
>
> **Drift rule:** descriptions reflect current canon incl. playtest overrides.
> Treat shape/interactions as reliable; verify exact numbers against the cited §
> before locking a balance pass. When this doc and a topic file disagree, the
> topic file wins.
>
> **How to read an entry:** **N. Name** `§ref` — purpose. Then a brief
> description, key numbers, and *Touches:* (systems it depends on or feeds).

---

## Master Index (90 systems, 10 domains)

- **A. Run & Macro Structure** — 1 Run Lifecycle · 2 Region Map Generation · 3 Node System · 4 Box & Active Team · 5 Map-View Loadout · 6 HP Economy · 7 City Interstitials · 8 Region Modifiers · 9 Region Escalation
- **B. Core Combat Loop** — 10 Five-Phase Turn Loop · 11 Lead Mechanic · 12 Step-Forward / Step-Backward · 13 AP & Swap Economy · 14 Skill Deck / Hand / Draw · 15 Consumable Pile · 16 Faint Resolution · 17 Victory/Defeat Resolution
- **C. Combat Resolution** — 18 Damage Formula · 19 Type System · 20 Crit System · 21 Status Conditions · 22 Stat-Stage Modifiers · 23 Move Tag Taxonomy
- **D. Enemy & AI** — 24 Intent System · 25 AI Scoring Function · 26 Intent Types & Position-Targeting · 27 Cleave / Backstrike · 28 Unknown Intent & Revelation · 29 Multi-Enemy Encounters · 30 Field Effects · 31 Counter-Intel Mode
- **E. Bosses & Challenge Arc** — 32 Boss Phase System · 33 Gym Leaders & Branching Paths · 34 Badge System · 35 Mid-Fight Evolution · 36 Victory Road · 37 League Boons · 38 Elite Four · 39 Champion & Team Synergy
- **F. In-Run Progression** — 40 XP & Leveling · 41 Stat Growth Curves · 42 Branching Evolution · 43 Branch Archetypes · 44 Learned Move Pool & Active-4 · 45 TM System · 46 Move Tutor System · 47 Ability System · 48 Lead Aura · 49 Move-Kit Construction · 50 Mastery Move System
- **G. Cross-Run Meta-Progression** — 51 Trauma System · 52 Trainer XP & Level · 53 Trainer Tokens · 54 Trainer Hub · 55 Hub Upgrade Tree · 56 Starter Unlocks · 57 Relic Pool & Tiers · 58 Achievement System · 59 Difficulty Modifiers · 60 Pokédex & Species Mastery · 61 Meta Save/Load
- **H. Nodes, Content & Economy** — 62 Wild Areas & Biomes · 63 Catching & Pokéball Economy · 64 Trainer Battles & Archetypes · 65 Elite Trainer Nodes · 66 Pokémon Center Nodes · 67 Shop Nodes & Curation · 68 Mystery Events · 69 Reward & Loot Tables · 70 Poké Dollar Economy · 71 Item Taxonomy · 72 Consumables · 73 Trainer Relics · 74 Held Items · 75 TMs
- **I. Technical Foundation** — 76 ScriptableObject Architecture · 77 Event Bus · 78 Hierarchical State Machine · 79 Factory & Pooling · 80 Seeded RNG & Streams · 81 Determinism & Replay · 82 Save System · 83 Addressables · 84 Input System
- **J. Presentation** — 85 Combat Screen UI · 86 Map View UI · 87 Card Anatomy & Damage Preview · 88 Audio System · 89 Accessibility · 90 Localization

A dedicated **Scaling Spine** (how progression scales against the map) and a
**Design Tensions / Open Questions** section follow the catalog.

---

# A. Run & Macro Structure (Topic 2)

**1. Run Lifecycle** `§2.1` — the spine of a single playthrough.
Pre-Run setup → Region ×3 → Victory Road → League → End. Win = beat Champion;
lose = Active Team wiped. Target ~90–100 min. Every run end (win or lose) feeds
Trainer XP. *Touches:* every gameplay system; bookended by Trainer Hub.

**2. Region Map Generation** `§7.2 v2` — seeded branching tree the player traverses.
**12 layers** (L0 entry → L11 Gym), generated deterministically from
`RunSeed XOR RegionIndex`. L0 = choice of 3 entry nodes (no forced Wild, but a Wild
is reachable by L1–2). L1–8 trunk: each node links to 1–3 children, **fan-in ≤2**,
edges don't cross, node types weighted per layer, **1 Elite guaranteed ≈L7**. **L9
Gym Fork** splits into two sub-lanes, each telegraphing its Gym's type + Badge.
L9–10 each have a **guaranteed Pokémon Center**. L11 = two terminal Gyms; you fight
only your route's. Constraints: no two adjacent same-type nodes, both sub-lanes equal
length, no dead ends before the fork. *(The old 8-layer fixed-lane model §7.2.1/§7.11
is superseded.)* *Touches:* Node System, Gym pool, determinism, save/load.

**3. Node System** `§2.1.2 / Topic 7` — what occupies each map position.
Categories: Combat (Wild, Trainer, Elite), Recruitment (Wild Areas, special events),
Utility (Center, Shop, Tutor/Daycare, Mystery), Region Boss (Gym). Each node previews
its content (Pillar 1). Entering a node locks the Active Team. *Touches:* every node
subsystem (62–68), Loadout, Reward tables.

**4. Box & Active Team** `§2.3` — the roster container and the combat squad.
**Box** = persistent run roster, capacity **6 → 8** (relic/Hub). **Active Team** = the
**3** brought into a fight, drawn from the Box; only they deck-contribute and earn XP.
Overflow on recruit → **Swap-or-Skip** (release is permanent; no deposit pool). The
central tension: a benched Pokémon contributes nothing. *Touches:* Deck, XP, Loadout,
Catching, Trauma (per-instance).

**5. Map-View Loadout** `§2.1.2.1` — the between-nodes command screen.
Reorder Box, pick the Active 3 (Lead = first slot), inspect moves/HP/evolution/items,
trigger evolutions, reconfigure each Pokémon's active 4, manage Held Items, save & quit.
All team/loadout changes happen here only — never mid-combat. *Touches:* Box, Evolution,
Move Pool, Held Items, Save.

**6. HP Economy** `§2.4` — HP as a persistent resource, not a per-combat reset.
A Pokémon entering at 30% HP starts the fight at 30%. **Faint = CurrentHP 0** (no
separate flag); fainted Pokémon stay in the Box until healed >0. No in-combat revival
by default (only the Revive consumable). Healing taxonomy: Full heal (Center/Summit),
partial % (League 30% micro-rest), consumable, move — all compute against
**EffectiveMaxHP** (Trauma-adjusted). *Touches:* Trauma, Healing nodes, Status DoT.

**7. City Interstitials** `§2.1.4 / §7.8` — rest-and-restock between Regions (after Gym 1 & 2).
Three sequential events: **Pokémon Center** (full heal + fuller Tutor/Daycare/Therapy) →
**Curated Shop** (8 slots, team-aware) → **Reflection** (pick 1 of 3 Region Modifiers).
The macro-loop differentiation beat. *Touches:* Region Modifiers, Shop curation, Trauma
clearing, Move Tutor.

**8. Region Modifiers** `§2.1.4.1 / §7.8.3` — semi-permanent run buffs chosen at Cities.
12 in the launch pool; pick 1 per City; **up to 2 active**, persist to run end. Examples:
+1 hand size, Lead heals 5 HP/swap, Type Affinity +10%, Trauma Resistance. Offering is
team-weighted. *Touches:* combat economy, Trauma, run differentiation.

**9. Region Escalation** `§2.2` — escalation is mechanical, not numeric.
R1 baseline (tutorial) · R2 introduces status-on-enemy-intents · R3 introduces multi-enemy
+ field effects · League combines all and each boss layers a signature. Keeps later Regions
from being "R1 with bigger numbers." *Touches:* AI, Status, Field Effects, Multi-enemy.

---

# B. Core Combat Loop (Topic 3)

**10. Five-Phase Turn Loop** `§3.2` — the atomic combat heartbeat.
Combat Start (once) → **Draw → Intent → Action → Resolution** → loop. No mid-combat
save. Each phase raises an Event Bus event; maps 1:1 to the `CombatState` sub-HSM.
Victory/Defeat checked continuously. *Touches:* HSM, Event Bus, every combat system.

**11. Lead Mechanic** `§3.3` — the signature moment-to-moment decision.
The Lead absorbs **100% of single-target** enemy damage by default; gates which **Melee**
cards are playable; choosing it weighs *who absorbs / what cards come online / what swap
cost / what Lead Aura is active*. Cleave/Backstrike are the telegraphed exceptions to "Lead
absorbs." *Touches:* Swap economy, Move taxonomy, Lead Aura, Faint resolution.

**12. Step-Forward / Step-Backward** `§3.3.2-3` — Melee modifiers that bundle a position change.
**SF:** play from bench → that Pokémon becomes Lead *before* the effect. **SB:** play from
Lead → effect resolves, *then* swap to a chosen bench. Neither increments the swap counter;
neither gets the defensive discount; both Melee-only and mutually exclusive. The combo
enabler — Earth Badge / Mass Mobilization reward them. *Touches:* Lead, Swap counter, Evolution
(SF/SB grow with evolution), Badges.

**13. AP & Swap Economy** `§3.3.1 / §3.7` — the per-turn resource budget.
**3 AP** base (relic/Badge/Region-mod modified), refilled each Draw. Cards 0–3 AP (rare 4-AP
ultimates). **Manual swap cost: 1st = 1 AP, 2nd = 2, 3rd = 3**, counter resets per turn,
**only manual swaps increment it**. Defensive swap discount: −1 AP on the first Defensive card
after a manual swap. *Touches:* Lead, Deck, Paralysis (+1 AP), relics (Ether, Choice items,
Move Echo), Badges (Earth, Thunder).

**14. Skill Deck / Hand / Draw** `§3.4` — the deck built from your party's moves.
**12 baseline** (4 active moves × 3 Active), up to **15** with Mastery (+1 per mastered Active
member). Draw **5 skill cards/turn**; **hand stays 5 regardless of deck size**. Empty deck
reshuffles the discard. A fainted Pokémon's cards are purged from deck *and* discard. *Touches:*
Move Pool (which 4 are active), Mastery, Faint, Confusion (discards), card-economy relics.

**15. Consumable Pile** `§3.5` — the per-combat utility hand.
Built at combat start from persistent inventory; draw **2/turn**; each usable **once per combat**;
**all returned to inventory at combat end** (not expendable — except under the No Refunds
difficulty). Upgradable chains (Potion→Super→Hyper→Max). Pokéball is a counted exception (see 63).
*Touches:* Inventory, Healing, Status cures, Catching, difficulty modifiers.

**16. Faint Resolution** `§3.3.5` — what happens when a Pokémon hits 0 HP.
Lead faints → player picks any non-fainted bench as new Lead (**0 AP**). Bench faints → just
leaves, Lead unchanged, no prompt. The fainted Pokémon's 4 moves (+Mastery) are purged from deck
AND discard. **Freeze precedence (§3.3.5.1):** a Frozen Lead that faints voids the position-lock.
Each faint applies **+1 Trauma**. *Touches:* Lead, Deck, Trauma, Status, Sturdy/Last Stand.

**17. Victory/Defeat Resolution** `§3.1` — combat end conditions.
Victory = all enemies down; Defeat = all 3 Active faint. No draw. Checked continuously — a lethal
Action-phase card play ends combat instantly (enemy Resolution cancelled). *Touches:* the loop,
XP award, Reward tables, run-failure.

---

# C. Combat Resolution (Topic 4 §4.1–4.2)

**18. Damage Formula** `§4.1.1` — how a hit's number is produced.
`floor( Power × (Atk/Def) × Range × Crit × STAB × TypeEff / Divisor )`. **No level term**
(level→stats→formula). **Unified Attack/Defense** (no Physical/Special split). Divisor is the
global tuning knob, **TBD via playtest**. Floor applied only at the end. No min-damage clamp
(only immunity = 0). *Touches:* Type, Crit, STAB, Status (Atk/Def mods), Range, relics/Badges/
field effects (all multiplicative terms), damage preview UI.

**19. Type System** `§4.1.2` — Gen-I 15-type effectiveness.
Multipliers ×4 / ×2 / ×1 / ×0.5 / ×0.25 / ×0 (immunity overrides). Dual-type = product. STAB
1.5× when the card's type matches the user (either type if dual). Always shown to the player.
*Touches:* Damage, AI scoring, type-immunity statuses, type-boost relics/items.

**20. Crit System** `§4.1.3` — scarce, investment-gated, not base RNG.
Base **0%**; sources = AlwaysCrit cards (100%), consumables (Sharp Lens), offensive-evolution
passives; additive, soft-cap ~30–35%. Multiplier **1.5×**, applied before STAB/TypeEff. Hover
always shows effective crit%. *Touches:* Damage, Evolution passives, consumables, relics (Steady
Aim 1.75×).

**21. Status Conditions** `§4.2` — deterministic redesigns of Gen-I RNG ailments.
One primary at a time (replaces) + Confusion as a coexisting secondary; all cleared at combat end.
**Burn** (`floor(EffMaxHP/16)`/turn + −25% Atk, permanent, Fire-immune); **Poison** (same DoT,
−15% Def, Poison/Steel-immune); **Paralysis** (that Pokémon's moves +1 AP, 3t, Electric-immune);
**Sleep** (own cards unplayable, position free, 1t); **Freeze** (cards unplayable + position-locked
+ ×1.5 Fire dmg, 1t, Fire/Ice-immune); **Confusion** (discard 1 random skill card/turn per Confused,
3t, floor of 4 playable cards). DoT computes on EffectiveMaxHP. *Touches:* Damage, Stat stages,
Trauma (EffMaxHP), AI, Lead/swap (Freeze lock), cures, Badges (Glacier/Rainbow/Marsh).

**22. Stat-Stage Modifiers** `§4.2.6` — temporary multiplicative Atk/Def shifts.
Gen-I ladder ×0.25…×3.0, range ±6, reset at combat end. Multiplicative on base (stage then status).
Distinct from status; tracked separately. *Touches:* Damage, status interaction order, Intimidate,
boss buffs (persist across phases), X Attack/Defense consumables.

**23. Move Tag Taxonomy** `§3.6` — the data axes every move card carries.
Role {Offensive/Defensive/Utility} × Range {Melee/Ranged} + optional modifiers (SF/SB, status
riders). Drives the Lead mechanic, AI targeting, relic triggers, swap discount eligibility. Ranged
≈ 70–80% of Melee damage (RangeModifier 0.75). *Touches:* Lead, Damage, relics/Badges keyed to
Role/Range, Move-kit construction.

---

# D. Enemy & AI (Topic 4 §4.3)

**24. Intent System** `§4.3.1` — full telegraphing, the engine of Pillar 1.
Every enemy action is revealed in the Intent Phase before the player acts. *Touches:* AI scoring,
position-targeting, UI intent display, Unknown reveal.

**25. AI Scoring Function** `§4.3.3` — context-aware, deterministic intent selection.
`Score = BaseWeight × TypeEff × StatusState × HPState × CooldownGate`. Never attacks into immunity,
never applies redundant primary status, finishes low-HP targets (×2 at <30%), plays urgently when
losing / sets up when healthy. **Randomness floor 10–15%** (seeded) prevents reverse-engineering.
*Touches:* Type, Status, HP, Counter-intel, Multi-enemy roles.

**26. Intent Types & Position-Targeting** `§4.3.2` — intents target **slots, not Pokémon**.
Attack / Cleave / Backstrike / Buff / Stall / Status / Unknown. A hit lands on whoever occupies the
targeted slot at Resolution — so swapping is both offense (cards) and defense (who eats the hit).
*Touches:* Lead, swap, UI, Cleave/Backstrike rules.

**27. Cleave / Backstrike** `§4.3.4` — the two ways enemies bypass "Lead absorbs."
**Cleave:** all non-fainted slots for N each (≈50–70% of single-target); **never fizzles** (min 1
target). **Backstrike:** a specific bench slot bypassing Lead; **fizzles if the slot is empty**
(doesn't redirect) — punishes leaving a bench, rewards leaving none. *Touches:* Lead, positioning,
relics (Hiker's Coat, Iron Skin), Friend Guard.

**28. Unknown Intent & Revelation** `§4.3.5` — hidden intents and how to uncover them.
❓ hides an intent. 3 tiers: **Witnessed** (seen in combat → logged in Pokédex), **Scouted**
(Foresight move / Radar Scope), **Researched** (Keen Eye / relic → revealed at combat start). Bosses
keep a recurring Unknown portion. *Touches:* Pokédex, abilities (Keen Eye), consumables, Soul Badge,
Clear Mind Boon, Counter-intel.

**29. Multi-Enemy Encounters** `§4.3.6` — R3 mechanical accent: 1 lead + 1–2 supports.
Simultaneous intents; Resolution order supports-first, lead-enemy-last; player targets a specific
enemy per offensive card. Support roles: Healer/Buffer/Debuffer/Attacker, reduced HP (die in 2–3
turns) — surviving ones escalate. *Touches:* AI scoring (shared), targeting, Champion #4–5 format.

**30. Field Effects** `§4.3.8` — environmental modifiers set at encounter start, full-combat.
R3 accent. Launch: **Sunny Day** (Fire ×1.5, Water ×0.5), **Rain Dance** (inverse), **Electric
Terrain** (Electric ×1.3 to grounded, Paralysis-blocks grounded). Weather and Terrain are independent
categories that can coexist (multiplicative). Gym fights set a type field (currently flavour-only —
gap #33). *Touches:* Damage, AI, Gym Leaders, abilities (Levitate, Swift Swim).

**31. Counter-Intel Mode** `§4.8.1` — anti-perfect-prediction for fully-scouted bosses.
When a boss's whole pool is revealed, its top intent ×0.7 and the randomness floor is disabled;
standard enemies always play optimally (full info is the player's reward). Transparent (badge shown).
*Touches:* AI scoring, Unknown reveal, boss design.

---

# E. Bosses & Challenge Arc (Topic 4 §4.4–4.7)

**32. Boss Phase System** `§4.4.3` — condition-triggered escalation mid-fight.
≥2 phases (P1 >50% HP setup; P2 ≤50% forced aggressive type); aces get 3 (P3 ≤20% last-stand:
cooldowns reset, signature fires, Sturdy possible). Thresholds shown on the HP bar; stat stages
persist across phases. **21 boss Pokémon/run.** *Touches:* AI, mid-fight evolution, HP bar UI, stat
stages.

**33. Gym Leaders & Branching Paths** `§4.4.4` — the Region climaxes and the macro choice.
A seeded **2-of-4-type** fork per Region (R1 Rock/Water/Bug/Normal · R2 Fire/Grass/Electric/Poison
· R3 Psychic/Ground/Fighting/Ice); both types + Badges visible at the fork (Pillar 1); the unchosen
is abandoned. 2 Pokémon (ace = 3-phase, evo-eligible), type-locked, type field set. 12 types, 3 per
run, 9 missed → replayability (220 three-Badge combos). *Touches:* Map fork, Badges, Field effects,
mid-fight evolution.

**34. Badge System** `§4.4.5` — permanent run-modifiers from Gyms.
**12 Badges, 1 per type; 3 earned per run** (one per Region), **max 4** via bonus sources (Secret
Tournament, Victory Road Perfect Clear). Each is a build-shaping passive (Boulder −1 incoming on
Lead, Cascade draw-on-swap, Fist Melee +25%, Earth SF/SB −1 AP, Soul reveal 2 turns, etc.).
*Touches:* combat economy, type/Role/Range systems, build synergy, League Boons.

**35. Mid-Fight Evolution** `§4.3.7` — Gym-tier+ bosses evolve during the fight.
Telegraphed one turn ("EVOLUTION IMMINENT"); player can burst below threshold or prepare; on evo,
stats rise, move pool updates, intent sequence resets. *Touches:* Boss phases, AI, telegraph UI.

**36. Victory Road** `§4.5` — the pre-League preparation gauntlet (after Gym 3).
A small branching mini-map (3–4 layers) with exclusive nodes: **Gauntlet** (no heal, rare-relic
choice; Perfect Clear → bonus Badge), **Apex Pokémon** (VR-only recruit), **Training Grounds** (free
upgrade: stat/move/level/early-evo/replace), **Summit** (full heal + Boon pick + League preview +
one-way gate). *Touches:* Healing, recruitment, Evolution, relics, League Boons, bonus Badge.

**37. League Boons** `§4.5.2` — League-only power picks (stronger than Badges).
6 in pool, **pick 1 of 3** at the Summit, active only across the 5 League fights: Battle Hardened
(15% shield), Flow State (first swap free), Last Stand (survive lethal once/Pokémon), Type Mastery
(+0.25 SE), Clear Mind (full reveal), Evolution's Edge (+15% if fully evolved). *Touches:* Faint/
Trauma (Last Stand prevents faint), Unknown reveal, type system, Badges.

**38. Elite Four** `§4.6` — the League's main gauntlet.
4 sequential boss-trainers, escalating difficulty, distinct types, mixed field effects, counter-intel
when scouted, ace always 3-phase. Team size 2 (E1–2) → 3 (E3–4). 30% micro-rest between fights.
*Touches:* Boss phases, field effects, micro-rest healing, Boons.

**39. Champion & Team Synergy** `§4.7` — the final boss and its signature.
5 Pokémon; #4 & #5 fielded **simultaneously** (only such fight); ace 3-phase + mid-fight evolution.
**Signature: +5% Attack per defeated ally, cap +20%** (reduced from +10/+40). *Touches:* Multi-enemy
format, boss phases, team synergy display.

---

# F. In-Run Progression (Topic 5)

**40. XP & Leveling** `§5.2` — how Pokémon grow within a run.
Only the **Active Team** earns XP at combat end (benched lag → Exp Share relic gives 50%). XP scales
by enemy tier (wild < trainer < elite < Gym < Elite < Champion). Level-ups process **between nodes**.
Data-driven: `ProgressionConfigSO` holds XP-per-tier + curve `Base+(L−1)·Slope`; per-species
`EvolveLevel`. **Calibration is placeholder** (blocked on systems-designer). *Touches:* Stat growth,
Evolution thresholds, Map pacing (see Scaling Spine), Active Team selection.

**41. Stat Growth Curves** `§5.2.3` — per-species level→stat mapping.
Each level-up adds flat Atk/Def/HP from the species `StatGrowthCurve` SO (custom-tuned, not Gen-I
tables). Single-stage Pokémon get enhanced growth to compensate for no evolution. Feeds the damage
formula as inputs (level never appears directly). *Touches:* Damage, Evolution, XP.

**42. Branching Evolution** `§5.3` — the signature run-to-run deck rewrite.
Player-initiated from Map View at a level threshold (delayable); permanent. **2 branches standard;
3 for starters/high-rarity** (Eevee 4). Some branches gated by Evolution Items (add, never block).
Purely additive to the **Learned Move Pool**: upgrades-in-place + additions. Grants a passive ability.
Final form continues the chosen branch's direction (+ one sub-choice). *Touches:* Move Pool, Abilities,
Crit/type passives, Mastery tier advance, XP, Trauma (carries through).

**43. Branch Archetypes** `§5.3.4` — the three design templates for branches (guidelines).
**Vanguard** (melee-forward, SF/SB, crit passive) · **Specialist** (ranged, status riders, type
passive) · **Support** (defensive/utility, heal-shield, team passive). Assigned per species identity;
not every species has all three. *Touches:* Evolution, Move-kit construction, Abilities, Shop curation
(archetype detection).

**44. Learned Move Pool & Active-4** `§5.10` — the growing library vs the fixed budget.
Each Pokémon accumulates moves (start 4); the player configures which **4 are active** (deck-
contributing) out of combat, free/unlimited between nodes. Pool grows via evolution/TM/Tutor; **moves
never removed**; dedupes. **The active-4 budget never grows** — that's the trade-off ("synergy
sculpted, not drafted"). Mastery is the always-on 5th. *Touches:* Deck, Evolution, TM, Tutor, Mastery,
Loadout UI.

**45. TM System** `§5.4.1` — single-use move-teaching items.
Add a named move to a compatible Pokémon's pool permanently (`CompatibleSpecies[]` gated, Mastery-
exempt). The "happy accident" discovery axis. 15 launch TMs. *Touches:* Move Pool, Shops, drops,
Mystery Events.

**46. Move Tutor System** `§5.4.2` — node-service move learning.
At City Centers and VR Training Grounds; offers a curated, **stage-aware** `TutorLearnset[]`; learn 1
move per visit. *(VS interim: REPLACES a CurrentMoves slot; full additive-pool behaviour is post-VS —
gap #36.)* *Touches:* Move Pool, City/VR nodes, evolution stage.

**47. Ability System** `§5.5` — always-on passives, no AP or card slot.
None/trivial pre-evo → primary at first evo → branch-secondary at final. Categories: Combat/Vision/
Positional/Type/Survival/Aura. ~30 launch abilities (Torrent/Blaze/Overgrow, Keen Eye, Levitate,
Intimidate, Sturdy, Swift Swim, Shell Armor…). *Touches:* Damage, AI reveal, Lead entry, type immunity,
faint prevention, Lead Aura.

**48. Lead Aura** `§5.5.4` — opt-in positional bench buff.
Ability- or Held-Item-(Type Plate)-gated. While the wearer is Lead, **bench moves of the aura type
+5%**; auras stack additively. Adds a fourth axis to the Lead decision. *Touches:* Lead, Abilities,
Type Plates, damage.

**49. Move-Kit Construction** `§5.3.6` — the authoring rules for every 4-move kit.
Templates per stage (pre/mid/final) governing Offensive/Defensive/Utility mix, SF/SB scarcity (grow
with evolution), and AP range (0–2 → 1–3 → 1–4). Ensures kit coherence + team variety. *Touches:*
Move taxonomy, Evolution, content authoring.

**50. Mastery Move System** `§4.3.9.2` — the immutable, earned 5th card.
A permanent 5th slot per Pokémon, never touched by TM/Tutor/evolution; auto-advances Lv1→Lv2→Lv3 with
evolution **if** that tier is unlocked (per-species, cross-run). Deck size +1 per Active member with
Mastery unlocked. Power targets Lv1 60–80/1AP → Lv3 110–140/2–3AP. *Touches:* Pokédex mastery tiers,
Deck size, Evolution, meta-progression.

---

# G. Cross-Run Meta-Progression (Topic 6)

**51. Trauma System** `§6.2` — the in-run consequence of fainting (Option E).
**+1 stack per faint, −5% MaxHP multiplicative, cap 5 (−25%).**
`EffectiveMaxHP = floor(BaseMaxHP × (1 − 0.05·min(stacks,5)))`. Per-instance, run-scoped, carries
through evolution; recruits start at 0. **All healing AND DoT** compute on EffectiveMaxHP. Clear via
Trauma Salve / Therapy (100₽×(1+stacks)) / Daycare Recovery. Sturdy & Last Stand prevent the faint →
no stack. *Touches:* HP economy, healing, status DoT, faint, difficulty (Trauma Surge), relics/events.

**52. Trainer XP & Level** `§6.3` — persistent account progression (NOT power).
Earned every run (won or lost); `cumulative XP = floor(500 × N^1.6)` (soft-log). Gates Hub upgrades
and unlock tiers. Run-failed bonus = `floor(layers_cleared × 50)`, cap 400. *Touches:* Tokens, Hub,
unlocks, Pokédex promotions, achievements.

**53. Trainer Tokens** `§6.3.4` — the agency currency (two-track model).
`floor(run XP / 100)`, **cap 50/run**; spent manually at the Pokémart on chosen unlocks. XP guarantees
progress; Tokens give choice. *Touches:* Starters, relic tiers, Hub upgrades, difficulty unlocks.

**54. Trainer Hub** `§6.4` — the pre/post-run menu space (2D, Pokémon-Center-styled).
5 kiosks: PC Terminal (Pokédex/stats/achievements), Trainer Card (level/XP/tokens), Pokémart (spend
Tokens), Daycare Lady (roster/difficulty config @ Lvl 3), Mystery Door (daily seed/leaderboard @
post-launch). *Touches:* every meta system; bookends the Run Lifecycle.

**55. Hub Upgrade Tree** `§6.4.2` — permanent QoL/option unlocks (never raw power).
Expanded Box (8), +1 Starting Relic, Pokédex Insight, Apex Reveal, Twin Run, **Difficulty Slot +1**,
Trauma Salve Cache — Token-priced, Level-gated, one-time. *Touches:* Box, Starting relics, difficulty
slots, starters.

**56. Starter Unlocks** `§6.5` — widening the opening, not the power floor.
3 default (Bulbasaur/Charmander/Squirtle) + 3 meta (**Pikachu** @ reach R2, **Eevee** @ win + 4 evos
(4 branches), **Riolu** @ Underdog-Run achievement). Each ships full branch lines + a flavour run-
modifier. *Touches:* Tokens, achievements, Evolution branch counts.

**57. Relic Pool & Tiers** `§6.6` — which of the 50 relics are available this run.
Meta-tiers: **T1 Foundation (20, always)** / **T2 Discovered (20, event-unlocked)** / **T3 Mastery
(10, Token-bought @ Lvl 10+)**. Tier ≠ rarity. In-run drop weight Common 60 / Uncommon 30 / Rare 10.
Starting relics Common–Uncommon only. *Touches:* drops, shops, achievements (T2 triggers), Tokens.

**58. Achievement System** `§6.7` — challenge goals that grant XP and drive unlocks.
~50 across 8 categories (First Steps, Recruitment, Evolution, Mastery, Combat, Boss, Build Identity,
Endurance); ~80% visible / ~20% hidden; primary unlock signal for T2 relics + meta-starters.
*Touches:* Trainer XP, relic/starter unlocks, Pokédex, stat tracking.

**59. Difficulty Modifiers** `§6.8` — stackable Ascension/heat layer.
10 launch modifiers, each multiplies run XP; default **1 slot** (→2 via Hub). Examples: Iron Will
(+20% wild HP), Greater Threats (shift region enemy tiers up), Dense Fog (Unknown intents), No Refunds
(consumables expended), Master's Challenge (extra boss phase). **No "easier" modifier** — baseline is
the floor. *Touches:* enemy scaling, Trauma, consumables, XP multiplier, the Scaling Spine.

**60. Pokédex & Species Mastery** `§4.3.9 / §6.9` — cross-run knowledge accumulation.
Tracks kills per species across all runs. Tiers (common/uncommon/rare counts): **Familiar** (10/5/2 →
reveal that species' Unknown intents), **Veteran** (30/15/5 → your own become **Shiny**), **Master**
(50/25/10 → unlock the species **Mastery Move**). Persisted per account. *Touches:* Unknown reveal,
Mastery Move, Trainer XP (tier promotions), PC Terminal.

**61. Meta Save/Load** `§6.10 / §9.8` — persistence of cross-run state.
`MetaProgressionSO` (XP, level, tokens, unlocks, achievements, Pokédex, statistics) serialized at run
end + every Pokémart purchase; binary + JSON-debug; versioned schema; last-known-good backup. *Touches:*
all meta systems, Save System.

---

# H. Nodes, Content & Economy (Topic 7 & 8)

**62. Wild Areas & Biomes** `§7.3` — the recruitment workhorse.
8 biomes (Meadow/Cave/River/Sea/Power Plant/Volcano/Sky/Tower) each with a species pool + theming.
Each node offers **3 species** (2 Common + 1 Uncommon; ~10% upgrade Uncommon→Rare), visible up front.
Pick one → catching encounter. Wild stat tier scales by Region (R1 L5–10, R2 L12–20, R3 L22–30).
*Touches:* Catching, Box, Map gen, Region aesthetics, the Scaling Spine.

**63. Catching & Pokéball Economy** `§7.3.4` — deterministic recruitment mini-combat.
No roll: catch needs **HP < 50% + apply Pokéball**; **any status → catch at any HP**; HP ≥ 50% at
throw fails (ball still spent); HP ≤ 0 loses the recruit. **Pokéball = counted run resource** (start 3
+ 1/region + shop buys; spent 1/attempt success or fail; catch card appears only if count >0).
*(Supersedes free-ball-per-encounter.)* *Touches:* Consumable pile, Box overflow, status, economy,
Master Ball Charm relic.

**64. Trainer Battles & Archetypes** `§7.4` — standard human-trainer combats.
1–2 Pokémon, sequential. 8 launch archetypes (Bug Catcher, Lass, Hiker, Sailor, Engineer, Hex Maniac,
Ace Trainer, Rocket Grunt), each a tactical identity; eligibility expands per Region. Rewards: 5 XP,
50–150₽, loot, Pokédex credit. *Touches:* AI, Reward tables, Pokédex, Region escalation.

**65. Elite Trainer Nodes** `§7.5` — the mid-Region threat (1/region, late trunk).
2 Pokémon, both 2-phase; difficulty between Trainer and Gym; **no type lock** (unlike Gyms); reward =
guaranteed Uncommon relic + XP + ~300₽. *Touches:* Boss-lite phases, relic drops, Map gen.

**66. Pokémon Center Nodes** `§7.6` — pre-Gym pit stop (Region-internal, L9–10).
Full heal (to EffMaxHP, free) + limited Move Tutor + Therapy. No combat. The City Center (§7.8.1) is
the fuller version (adds Daycare, PC Box). *Touches:* HP economy, Trauma clearing, Move Tutor.

**67. Shop Nodes & Curation** `§7.7 / §7.8.2` — the buy/sell economy surfaces.
**Region Shop:** small, randomized, re-rollable (25/50/100₽). **City Shop:** 8 slots, **team-aware
curation** (scores by type-match, build archetype, rarity×region, anti-duplicate), ~30% pricier, the
only sell valve (30% of buy price). *Touches:* Poké Dollars, relics/items/TMs, build archetypes,
inventory.

**68. Mystery Events** `§7.9` — scripted vignettes with branching choices.
12 launch events tagged 🟢 Safe 30% / 🟡 Tradeoff 50% / 🔴 Gamble 20% (risk visible even in "Mystery"
— Pillar 1); never repeat within a run. Outcomes span items, recruits, Trauma clearing, gambles,
stat trades. *Touches:* economy, recruitment, Trauma, Evolution items, relics.

**69. Reward & Loot Tables** `§7.12` — the master per-node reward reference.
Aggregates Poké Dollars, XP, drops, and specials for every node type. The systems-designer's single
balance surface. *Touches:* every combat/utility node, economy, relic drops.

**70. Poké Dollar Economy** `§7.4.2 etc.` — the in-run spending currency.
Earned from trainers (50–150₽), elites (~300₽), gyms (500₽), events; spent at shops, tutors, therapy,
daycare; only exit valve = City Shop sells. *Touches:* Shops, Center services, Trauma therapy, Coin
Pouch/Coin Purse modifiers.

**71. Item Taxonomy** `§8.1` — the three-system boundary.
**Consumables** (in-combat, per-combat, restored) / **Relics** (persistent run-state, no slot cap) /
**Held Items** (1 per Pokémon, always-on). TMs = consumable class, Map-View-only. *Touches:* all item
subsystems, authoring SOs.

**72. Consumables** `§8.2` — 24 launch in-combat tools.
Potions (30/60/120/Max=full to EffMaxHP), Revive (only in-combat revival → 50%), status cures, utility
(Ether +2 AP, X Atk/Def, Sharp Lens, Radar Scope, Card Pocket, Quick Claw), Pokéball. *Touches:*
Consumable pile, healing, status, crit, catching.

**73. Trainer Relics** `§8.3` — 50 persistent run-modifiers.
25 Common / 18 Uncommon / 7 Rare; 5 synergy categories (Lead-Economy, Card-Economy, Combat, Meta-
Acquisition, Status). Notables: Move Echo (+1 AP, reduced from +2), Exp Share, Phoenix Feather, Sage's
Tome, Trauma Salve, type charms. *Touches:* nearly every combat/economy system via event hooks.

**74. Held Items** `§8.4` — 18 per-Pokémon equippables, 1 slot.
Type-boost (+20% wearer), **Type Plates** (Lead Aura source), Leftovers (`floor(MaxHP/16)`/turn),
Eviolite (+20% Def if unevolved), Focus Sash, Choice Band/Scarf. *Touches:* Damage, Lead Aura, sustain,
Loadout.

**75. TMs** `§8.5` — 15 launch move-teaching consumables.
See system 45. Power/AP/type per TM; `CompatibleSpecies[]` gated; Mastery-exempt. *Touches:* Move Pool,
shops, drops.

---

# I. Technical Foundation (Topic 9)

**76. ScriptableObject Architecture** `§9.3` — data-driven everything.
Definition SOs (immutable, Addressables) vs Runtime SOs (RunState/MetaProgression, mutated, saved).
PokemonInstance = plain poolable C# class. Zero hardcoded balance literals. *Touches:* all content,
save, factory.

**77. Event Bus** `§9.4` — the decoupling backbone (hybrid).
SO event channels (designer-facing, cross-system fan-out) + static code EventBus (internal/high-freq);
synchronous, subscription-ordered for determinism. *Touches:* every system; UI never owns state.

**78. Hierarchical State Machine** `§9.5` — game-flow control.
GameState → Hub/Run/GameOver; `CombatState` sub-machine maps 1:1 to the 5 phases. *Touches:* combat
loop, map, hub, transitions logged for replay.

**79. Factory & Pooling** `§9.6` — zero-alloc hot paths.
Pools for PokemonInstance, MoveCard, IntentData, Enemy, DamageContext. *Touches:* combat performance,
recruitment lifecycle.

**80. Seeded RNG & Streams** `§9.7` — deterministic randomness.
GameRNG xorshift32; **5 isolated streams** (Map/Combat/Loot/Mystery/Encounter);
`streamSeed = RunSeed XOR FNV1a(name)`; `UnityEngine.Random` forbidden. *(Open: resume re-rolls
per-stream cursors — gap #45.)* *Touches:* map gen, drops, crits, encounters, determinism.

**81. Determinism & Replay** `§9.7.4` — seed + InputLog → bit-exact replay.
Foundation for regression tests, daily seeds, leaderboard verification. Integer final-damage; float
identity tested cross-platform. *Touches:* RNG, input, save, testing.

**82. Save System** `§9.8` — layered, atomic, recoverable.
Meta (run end + purchase) / Run (every node) / Settings; write-temp→checksum→atomic-rename; backup;
versioned schema; **no mid-combat save**. *Touches:* RunState, MetaProgression, determinism.

**83. Addressables** `§9.9` — content loading (no `Resources.Load`).
Region-banded content groups, async via Awaitable/UniTask. *Touches:* all content, performance.

**84. Input System** `§9.10` — new Input System only.
Combat/Map/UI action maps; committed inputs logged to InputLog for replay (hover/scroll not logged).
*Touches:* determinism, accessibility (rebinding).

---

# J. Presentation (Topic 10)

**85. Combat Screen UI** `§10.2` — the readability surface (Pillar in practice).
Top status bar / enemy zone (40%) / Active Team (30%, Lead raised 1.25× gold-framed) / hand bar (AP
pips, 5 skill + 2 consumable, End Turn). Boss HP bars show phase markers. *Touches:* every combat
system via event subscriptions; UI never owns state.

**86. Map View UI** `§10.3` — the between-nodes navigator.
Active Team + reorderable Box (left), branching map graph with node-type icons + current location
(right), utility bar. Trauma badges on portraits. *Touches:* Loadout, Box, Map gen, Trauma.

**87. Card Anatomy & Damage Preview** `§10.2.3-4` — the information-complete card.
Type band, AP pips, range/modifier/status icons; **hover shows final calculated damage + breakdown +
crit% + riders + redundancy warnings**; unplayable cards desaturated, never hidden. *Touches:* Damage
formula, type/crit/status, Move taxonomy.

**88. Audio System** `§10.5` — layered orchestral-electronic, regionally themed.
Combat stems crossfade (base/low-HP/setup/signature); per-action SFX; status motifs; boss tracks;
mix targets (−14 LUFS master). *Touches:* combat events, regions, bosses.

**89. Accessibility** `§10.6` — mandatory launch features.
Colorblind modes + icon patterns, text scaling, reduced motion, always-on damage preview, SFX
subtitles, full rebinding, pause anywhere; screen-reader plumbing post-launch. *Touches:* UI, input,
type display, animation.

**90. Localization** `§10.10` — all strings externalized.
Namespaced keys, Unity Localization package; en-US launch, es/fr/ja roadmap. *Touches:* all UI text.

---

# The Scaling Spine — how progression tracks the map

*This is the connective tissue you asked about: how XP, leveling, evolution, enemy
tiers, and Trauma scale against the 12-layer × 3-Region map + Victory Road + League.*

**The intended power curve (design targets):**

| Beat | Map position | Player level target | Wild recruit band | Enemy tier |
|---|---|---|---|---|
| Run start | R1 L0 | Starter ~L5 | R1: L5–10 | R1 baseline |
| First evolution | ~end R1 / start R2 | ~L16 (Squirtle→Wartortle) | R2: L12–20 | R2 (+status intents) |
| Final evolution | ~end R2 / start R3 | ~L36 (Wartortle→Blastoise) | R3: L22–30 | R3 (+multi-enemy, fields) |
| League | post-VR | fully evolved | — | Champion-tier |

**How the pieces lock together:**

1. **XP gating evolution gates deck identity.** Only the Active 3 earn XP (§5.2.1), and
   evolution is the primary deck-rewrite (§5.3). So *team selection is a progression
   commitment*: the 3 you field level and evolve; the bench stagnates (Exp Share relic
   softens this to 50%). The map's forced encounters + type matchups pressure you to
   rotate — which fights the leveling curve. **This is the core scaling tension.**

2. **Enemy tier rises by Region, not by layer.** §2.2 makes escalation *mechanical*
   (status → multi-enemy → fields), while *numeric* tier steps at Region boundaries
   (§7.3.5 wild bands; boss tiers §4.4.2). So within a Region the player out-levels the
   trash slightly by the Gym; across a boundary the floor jumps. The 12-layer trunk is
   the "catch-up runway" before each Gym.

3. **Late recruits are designed to catch up.** A R3 wild enters at L22–30 (§7.3.5), far
   above a R1 wild's L5–10, so swapping in a fresh Pokémon late isn't dead weight — the
   higher base level + steeper single-stage growth (§5.2.4) closes the gap fast.

4. **Trauma erodes the HP axis as difficulty climbs.** Every faint shaves 5% of MaxHP
   (cap 25%, §6.2), and healing only ever restores to the *eroded* ceiling. So the deeper
   you are (R3 + the 5-fight League on 30% rests), the more a faint cluster compounds —
   precisely where enemy tier is highest. Trauma clearing sources are scarcity-gated to
   keep this a real threat without a soft-lock.

5. **Difficulty modifiers bend the spine on purpose.** Greater Threats shifts each Region
   to the *next* Region's enemy tier; Trauma Surge deepens the HP erosion; Iron Will pads
   wild HP. These are the explicit levers that re-tension the whole curve (§6.8).

---

# Design Tensions & Open Questions (master designer flags)

*Surfaced to help you finish the design. None are blocking; all are "decide before a
real balance pass."*

1. **The XP curve and damage Divisor are still placeholders.** `ProgressionConfigSO`
   (XP-per-tier + `Base+(L−1)·Slope`) and the formula Divisor (§4.1.1) are explicitly
   TBD/blocked on systems-designer. **Until these are calibrated, the whole Scaling Spine
   above is intent, not proof.** The first thing to lock when finishing the design.

2. **Does the 12-layer trunk supply enough XP to hit L16 by end-R1 and L36 by end-R2?**
   R1 climbs ~11 levels, R2 climbs ~20 — a steeper R2 slope. Needs an encounter-count ×
   XP-per-tier check against the curve. If short, either bump R2 XP, lower the Blastoise
   threshold, or add a Region-2 leveling node.

3. **Active-only XP vs. type-coverage rotation.** The map rewards swapping Pokémon for
   type matchups, but XP punishes it (benched Pokémon stall). Is Exp Share (50%) enough,
   or does the design want a baseline bench-XP trickle? This directly shapes whether 6–8
   Box slots feel usable or vestigial.

4. **Gym type fields are mechanically inert** (gap #33) — set for flavour/telegraph but no
   multiplier defined. Either design type-field multipliers or accept them as pure
   telegraph. Affects how much a Gym's home-field actually matters.

5. **"One Path" difficulty vs. the Gym fork** (§7.2 note) — currently modeled as a route-
   count cap, but specced as "same Gym type on both branches." Needs reconciliation.

6. **VS Move Tutor replaces a slot; the additive pool (§5.10) is post-VS** (gap #36). The
   pool system is the more elegant design — confirm the post-VS path so TM/Tutor "feel
   powerful, not slot-pressure" lands as intended.

7. **Resume re-rolls per-stream RNG cursors** (gap #45) — encounters/loot can differ after
   a save/resume, nicking the determinism pillar. Worth closing before leaderboards.

8. **Single-stage Pokémon balance.** They get "enhanced growth" (§5.2.4) and a Mastery cap
   at Lv2, but no branch identity or signature move. Are they competitive picks, or always
   dominated by evolving lines? A deliberate niche (e.g., earlier power, Held-Item synergy)
   would make them real choices.

---

*End of catalog. To go deeper on any single system, ask the steward for that §.*
