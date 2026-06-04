<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Last updated from Notion: 2026-06-04T23:45:00.000Z -->

**Status:** 🟢 In Progress


**Last Updated:** 2026-05-25 (§4.3.9.2 Mastery Move system redesigned — pool-based meta-progression with per-species achievement unlock tiers)


**Cross-references:** Topic 2 (Region Modifiers, Cities), Topic 3 (combat phases, Lead mechanic), Topic 5 (move kits, evolutions, passives), Topic 6 (Trauma System).


**Patches applied this migration:** §4.3.4.1 (Empty-slot resolution), §4.3.9.2 (Mastery Move evolution), §4.4.3.1 (Stat stage persistence), §4.7.1 (Champion buff cap revised to +5%/+20%).


---


# §4.1 Damage Formula & Type System


## §4.1.1 Damage Calculation

> ✅ Adopted (2026-05-26) — Epic 4 Tasks 4.2 + 4.3 raised 6 Sev-3 clarifications; the reasonable defaults below are now canonical (adopted in code).
> **G1 — Algebraic combination of formula variables is not written.** The variables table lists Power, Atk, Def, Divisor, Crit, STAB, TypeEff, Range, but no equation combines them. Code default (matches existing `BattleConfigSO.cs` header comment): `floor( Power × (Atk/Def) × Range × Crit × STAB × TypeEff / Divisor )`.
> **G2 — RangeModifier ordering is unspecified** (spec only locks Crit-before-STAB-and-TypeEff). Code default: Range is folded into BaseDamage (before Crit), since Range is intrinsic to the move's power profile.
> **G3 — Floor-only-at-end (task 4.2.4) makes multiplication commutative**, so the Crit-before-STAB-and-TypeEff ordering rule has zero numerical effect. Code default: preserve the ordering in the `DamageBreakdown` struct field order so the breakdown panel reads Power → Crit → STAB → TypeEff → Range — i.e., the rule is treated as presentational, not arithmetic.
> **G4 — Minimum-damage clamp is not specified.** Code default: no clamp. Only TypeEff=0 (immunity) produces 0 damage. Open question: should a non-immune hit ever floor to 0?
> **G5 —** **`PokemonType`** **enum has 18 entries (Dark/Steel/Fairy from Gen II+); spec says "Gen I 15 types".** Code default: implement Gen I 15×15 chart; the 3 extras return 1.0× (neutral) with a TODO. Open question: ship as-is for VS, or trim the enum to 15.
> **G6 — §4.1.2 worked example 3 contains an arithmetic error.** Listed as `Ground vs Water/Rock = 1.0× × 0.5× = 0.5×`. In Gen I: Ground→Water = 1.0× (no relation), Ground→Rock = 2.0× (super-effective). Product = **2.0×**, not 0.5×. Code default: implement Gen I exactly; tests assert 2.0× for this matchup. Worked examples 1, 2, 4 verified correct.
> **Blocked:** nothing — implementation proceeds with defaults; ratify or amend before any tuning pass.

The base damage formula is a simplified adaptation of the Gen I formula, tuned for the roguelike deckbuilder's shorter fight economy (target: 5–8 turns per combat encounter):


| Variable          | Description                                                                              | Source                                |
| ----------------- | ---------------------------------------------------------------------------------------- | ------------------------------------- |
| Power             | Base damage value of the played move                                                     | MoveData ScriptableObject             |
| Attack            | Attacker's offensive stat (Physical or Special, matched to move category)                | PokemonInstance runtime stat          |
| Defense           | Defender's defensive stat (Physical or Special, matched to move category)                | Target PokemonInstance stat           |
| Divisor           | Normalization constant — **TBD via playtesting during vertical slice**                   | Global balance config                 |
| CritMultiplier    | **1.5×** if the move crits; **1.0×** otherwise                                           | Derived from crit system — see §4.1.3 |
| STAB              | Same-Type Attack Bonus: **1.5×** if card type matches Pokemon's type; otherwise **1.0×** | Derived at runtime                    |
| TypeEffectiveness | Type chart multiplier — see §4.1.2                                                       | Derived from type chart lookup        |
| RangeModifier     | **0.75×** for Ranged moves; **1.0×** for Melee moves                                     | MoveData ScriptableObject field       |


**Formula ordering is intentional.** Crit is applied to BaseDamage before STAB and TypeEffectiveness, preventing exponential stacking of all modifiers while preserving the satisfaction of critting on a favorable matchup.


**Level and stats:**

- Pokémon level does not appear in the damage formula. Level-ups grant stat increases (Attack, Defense, HP) which feed into the formula as inputs.
- Stat values are determined by the species' base stats (ScriptableObject) modified by level, run-state buffs (relics, Badges), and temporary combat modifiers (status effects, move effects).
- All HP and stat values are custom-tuned for the roguelike context and do not replicate Gen I mainline stat tables.

**Unified offense and defense:** All moves use a unified **Attack** stat (attacker) versus **Defense** stat (defender) regardless of move type. The Physical/Special split is not implemented. Each Pokémon instance tracks a single offensive stat (Attack) and a single defensive stat (Defense), simplifying both the damage formula and the player-facing stat readout.


**UI presentation:**

- Final calculated damage is displayed on the card while hovering over a targeted position.
- Hover preview also shows: type effectiveness badge, STAB indicator if applicable, crit chance for that move (e.g., 25% crit chance or ALWAYS CRITS), and a redundancy flag if a crit-chance bonus is active on an AlwaysCrit move.
- The underlying formula is never shown to the player.

## §4.1.2 Type System


The full **Gen I type chart** (15 types) is implemented. Type effectiveness follows the Gen I matrix exactly.


**Type effectiveness multipliers:**


| Matchup                | Multiplier | UI Label              |
| ---------------------- | ---------- | --------------------- |
| Double super effective | 4.0×       | SUPER EFFECTIVE ×4    |
| Super effective        | 2.0×       | SUPER EFFECTIVE       |
| Neutral                | 1.0×       | _(no label)_          |
| Resisted               | 0.5×       | NOT VERY EFFECTIVE    |
| Double resisted        | 0.25×      | NOT VERY EFFECTIVE ×2 |
| Immune                 | 0.0×       | IMMUNE                |


**Dual-type Pokémon:**

- A Pokémon may have up to two types (as in Gen I).
- Type effectiveness is the **product** of both type multipliers:
    - Fire vs Grass/Poison = 2.0× × 1.0× = **2.0×**
    - Electric vs Water/Flying = 2.0× × 2.0× = **4.0×**
    - Ground vs Water/Rock = 1.0× × 0.5× = **0.5×**
    - Normal vs Ghost/Poison = 0.0× × 1.0× = **0.0×** (immunity overrides)

**Immunities** are implemented as in Gen I (Ghost immune to Normal/Fighting, Ground immune to Electric, etc.). They are displayed in the Intent Phase and on card hover — never hidden from the player.


**STAB:** 1.5× when the card's type matches the playing Pokémon's type (or either type for dual-type Pokémon). Otherwise 1.0×.


## §4.1.3 Crit System


Crits are a **scarce, investment-gated mechanic** — not a base-rate RNG layer.


**Base crit chance: 0%.** No move crits unless a source below applies.


**Three sources of crit chance:**


| Source                            | Type                   | Effect                                                                                          |
| --------------------------------- | ---------------------- | ----------------------------------------------------------------------------------------------- |
| **Card effect** (AlwaysCrit)      | Per-move               | That move always crits (100%) regardless of other modifiers                                     |
| **Consumable** (e.g., Sharp Lens) | Temporary, per-combat  | Grants +X% crit chance to all moves for this combat                                             |
| **Evolution offensive path**      | Permanent, per-Pokémon | Choosing an offensive evolution branch grants +X% crit chance passively for the rest of the run |


**Stacking:** Consumable and Evolution passive crit chances are additive. AlwaysCrit card effects are independent. Target soft-cap for non-AlwaysCrit crit chance via stacking: ~30–35% (tuned through consumable and evolution passive values).


**Crit multiplier: 1.5×**, applied to BaseDamage before STAB and TypeEffectiveness.


**UI transparency:** hover preview always shows current effective crit chance per card. Flags redundancy when a crit-chance bonus is active on an AlwaysCrit move. Converts crit RNG into informed variance — preserving the spirit of the Telegraphed-Tactics pillar.


## §4.1.5 Stat Architecture


| Stat    | Description                                                    |
| ------- | -------------------------------------------------------------- |
| HP      | Current / Max hit points                                       |
| Attack  | Unified offensive modifier — used by all moves                 |
| Defense | Unified defensive modifier — used against all moves            |
| Level   | Current level; determines stat values via species growth curve |


Stat modifiers from status effects, relics, Badges, and move effects are applied as **temporary multipliers** on top of base stats — never edits to the base values.


---


# §4.2 Status Conditions


## §4.2.1 Overview

> ✅ Adopted (2026-05-26) — Epic 4 Task 4.5 raised 4 Sev-3 clarifications; the reasonable defaults below are now canonical (adopted in code).
> **G7 — First DoT tick timing.** §4.2.2.1/§4.2.2.2 say Burn/Poison apply DoT "at end of each Resolution Phase" but don't say whether the application turn counts. **Default:** status applied in turn N takes effect starting turn N+1, consistent with Sleep/Freeze's "the turn after Sleep is applied" wording. Application turn = telegraph only.
> **G8 — Stat-stage × status-modifier interaction order.** §4.2 doesn't specify how Burn's −25% Atk and Poison's −15% Def combine with the stat-stage ladder. **Default:** stat-stage first, then status multiplier on top, multiplicative: `EffAtk = floor(BaseAtk × StageMul × StatusMul)`.
> **G9 — Freeze ×1.5 Fire damage window.** §4.2.2.5 says "that turn". **Default:** active while Freeze duration > 0 (the unplayable / position-locked turn), matching G7.
> **G10 — Sleep + inbound Step-Backward.** §4.2.2.4 says position is NOT locked. **Default:** the Sleeping Pokémon CAN be the destination of another Pokémon's SB and CAN be manually swapped; only its OWN cards are unplayable (so it cannot initiate SF).
> **Blocked:** nothing — implementation proceeds with defaults; ratify or amend before tuning pass.

Status conditions are debuffs applied to individual Pokémon by move effects, enemy abilities, or field effects. All six classical Gen I conditions are implemented with **RNG-dependent behaviors redesigned as deterministic equivalents** to preserve the Telegraphed-Tactics design pillar.


All status conditions are **Pokémon-specific** — they apply to one Pokémon, not to the global AP pool or hand state. Status conditions are **cleared at combat end automatically.**


## §4.2.2 Primary Status Conditions


A Pokémon can carry one primary status at a time. Applying a new primary status to an already-statused Pokémon replaces the existing one.


### §4.2.2.1 Burn 🔥

- **Effect:** the Burned Pokémon takes `floor(MaxHP / 16)` damage (minimum 1) at the end of each Resolution Phase. The Pokémon's **Attack stat is reduced by 25%** for the duration.
- **Duration:** permanent until cured or combat ends.
- **Tactical read:** offensive disruption — pressures the player to swap the burned Pokémon out of an offensive role. Their damage output drops by ~25% while damage accumulates each turn.
- **Type immunity:** Fire-type Pokémon cannot be Burned.

### §4.2.2.2 Poison ☠️

- **Effect:** the Poisoned Pokémon takes `floor(MaxHP / 16)` damage (minimum 1) at the end of each Resolution Phase. The Pokémon's **Defense stat is reduced by 15%** for the duration.
- **Duration:** permanent until cured or combat ends.
- **Tactical read:** defensive disruption — Poisoned Pokémon take amplified incoming damage. Pressures the player to swap the affected Pokémon off Lead to avoid compounding HP loss.
- **Type immunity:** Poison-type and Steel-type Pokémon cannot be Poisoned.

**Burn vs Poison — design symmetry:** Burn weakens the Pokémon's _offense_ (reduced Attack); Poison weakens the Pokémon's _defense_ (reduced Defense). Both deal flat DoT damage. Both create swap incentives, but for different tactical reasons — Burn to preserve damage output, Poison to avoid amplified incoming damage.


### §4.2.2.3 Paralysis ⚡

- **Effect:** all moves belonging to the Paralyzed Pokémon cost **+1 AP** while the condition is active, regardless of whether the Pokémon is Lead or bench. Swapping the Paralyzed Pokémon in or out costs no extra AP.
- **Duration:** 3 turns.
- **Tactical read:** disrupts the affected Pokémon's move economy specifically. The player can route around it by playing other Pokémon's cards or swapping the Paralyzed Pokémon out at normal cost.
- **Type immunity:** Electric-type Pokémon cannot be Paralyzed.

### §4.2.2.4 Sleep 💤

- **Effect:** the Sleeping Pokémon's cards **cannot be played** the turn after Sleep is applied. The Pokémon's position is **NOT locked** — it can be manually swapped in or out, and can be the target of another Pokémon's Step-Backward move. However, the sleeping Pokémon cannot initiate its own Step-Forward (its cards are unplayable).
- **Duration:** 1 turn (the turn immediately following application).
- **Telegraphing:** Sleep application is shown in the Intent Phase the turn it is applied. The unplayable state is shown on the affected Pokémon's cards during the affected turn.
- **Tactical read:** creates urgency — the player has one turn to use the affected Pokémon's cards before they go offline. Position can still be managed, so the player can move the sleeping Pokémon to safety (e.g., to bench) even while their cards are unplayable.
- **Type immunity:** none.

### §4.2.2.5 Freeze 🧊

- **Effect:** identical card-unplayable rule as Sleep, but additionally the Pokémon is **position-locked** — it cannot be manually swapped in or out, cannot be the target of another Pokémon's Step-Backward, and obviously cannot initiate Step-Forward. Additionally, the Frozen Pokémon takes **×1.5 damage from Fire-type moves** during that turn (the "thaw window").
- **Duration:** 1 turn (the turn immediately following application).
- **Telegraphing:** Freeze application is shown in the Intent Phase.
- **Tactical read:** Freeze is the heavier sibling to Sleep — same offensive lockout, but the Pokémon is genuinely stuck in place. If the Frozen Pokémon is the Lead and faces an incoming heavy attack, the player cannot swap them out — they must absorb the damage. Fire-type players can exploit the thaw window with bonus damage.
- **Type immunity:** Fire-type and Ice-type Pokémon cannot be Frozen.
- **Faint precedence:** see §3.3.5.1 — if a Frozen Lead faints in the same Resolution, the Freeze position-lock is voided by the faint.

**Sleep vs Freeze — design symmetry:** both lock the Pokémon's card usage for 1 turn. Sleep allows position manipulation (the Pokémon is drowsy, not paralyzed); Freeze locks the position entirely (the Pokémon is stuck in ice). Freeze adds a Fire vulnerability as compensation for the lack of escape options.


## §4.2.3 Secondary Status Condition


Secondary statuses **can coexist with a primary status and stack across the Active Team.**


### §4.2.3.1 Confusion 💫

- **Effect:** at the start of each affected turn's Draw Phase, **1 random skill card is discarded** from the drawn hand for each Confused Pokémon. **Consumable cards are immune to Confusion discard** — only skill cards are affected.
- **Stacking:** each Confused Pokémon discards 1 skill card independently. With all 3 Active Pokémon Confused, up to 3 skill cards are discarded per turn (leaving as few as 2 skill cards from a 5-card draw).
- **Duration:** 3 turns per Pokémon (tracked individually).
- **Telegraphing:** Confusion application is shown in the Intent Phase. The discard is shown at the start of the affected turn.
- **Tactical read:** disrupts planning by shrinking effective hand size. Consumable immunity ensures the player always retains the tools to recover (Full Heal consumable can cure Confusion).
- **Multi-Confusion safety floor:** Even with all 3 Active Team Pokémon Confused (3 skill cards discarded per turn from a 5-card draw), the player always retains 2 skill cards + 2 consumable cards, totaling 4 playable cards minimum. This is the design safety floor for Confusion stacking.

## §4.2.4 Type-Condition Immunities


Encoded as a `StatusImmunities[]` array on the `PokemonSpeciesSO` ScriptableObject. Checked at the point of status application — immune Pokémon cannot receive that condition. Attempting to apply a status to an immune Pokémon shows an IMMUNE indicator in the UI.


| Type     | Immune to        |
| -------- | ---------------- |
| Fire     | Burn, Freeze     |
| Ice      | Freeze           |
| Electric | Paralysis        |
| Poison   | Poison condition |
| Steel    | Poison condition |


## §4.2.5 Status Duration Summary


| Condition | Type      | Duration  | Primary Effect                          | Secondary Effect | Position Lock |
| --------- | --------- | --------- | --------------------------------------- | ---------------- | ------------- |
| Burn      | Primary   | Permanent | floor(MaxHP/16) dmg/turn                | Attack -25%      | No            |
| Poison    | Primary   | Permanent | floor(MaxHP/16) dmg/turn                | Defence -15%     | No            |
| Paralysis | Primary   | 3 turns   | All moves +1 AP cost                    | —                | No            |
| Sleep     | Primary   | 1 turn    | Cards unplayable                        | —                | No            |
| Freeze    | Primary   | 1 turn    | Cards unplayable                        | ×1.5 Fire damage | Yes           |
| Confusion | Secondary | 3 turns   | −1 skill card/turn per Confused Pokémon | —                | No            |


## §4.2.6 Stat Stage Modifiers


Distinct from status conditions. Tracked in `PokemonInstance.StatModifiers` as a `Dictionary<Stat, float>`. Do not occupy a primary or secondary status slot.

- Applied as **multipliers** on base stats (not additive edits).
- Stage ladder (Gen I faithful): ×0.25 / ×0.33 / ×0.5 / ×0.66 / **×1.0** / ×1.5 / ×2.0 / ×2.5 / ×3.0. Range: ±6 stages.
- Reset at combat end.

## §4.2.7 Curing Status Conditions

- **Consumables:** Antidote (Poison), Paralyze Heal (Paralysis), Awakening (Sleep), Burn Heal (Burn), Ice Heal (Freeze), Full Heal (any primary status + Confusion). Defined in Topic 8.
- **Move effects:** specific moves can cure status conditions (e.g., a "Refresh" Utility card).
- **Swapping the Lead:** does **not** cure status. The condition persists regardless of position.
- **End of combat:** all status conditions and stat stage changes are cleared automatically.

---


# §4.3 Enemy AI & Intent System


## §4.3.1 Intent System Overview


Enemy behavior in Project Ascendant is **fully telegraphed** — every enemy action is revealed to the player in the Intent Phase before the player acts. This is the direct mechanical expression of the Telegraphed-Tactics design pillar.


Intents are selected via a **context-aware scoring function** evaluated fresh each turn — not a naive weighted pool. The result is AI that behaves intelligently and adaptively while remaining fully deterministic given the run seed.


## §4.3.2 Intent Types


Intents target **positions (slots)**, not specific Pokémon. The displayed target shows the slot label plus the Pokémon currently occupying it for player clarity. If the player swaps that Pokémon out during the Action Phase, the intent still targets the original _slot_ — whoever occupies that slot when Resolution fires takes the hit.


| Intent                    | Display                               | Description                                     |
| ------------------------- | ------------------------------------- | ----------------------------------------------- |
| Attack(N, Lead)           | ⚔️ N dmg → Lead                       | Single-target damage to the Lead slot           |
| Attack(N, slot)           | ⚔️ N dmg → [slot, currently: X]       | Single-target damage to a specific bench slot   |
| Cleave(N)                 | ⚔️ N dmg → ALL SLOTS                  | Damages all non-fainted slots for N each        |
| Backstrike(N, target)     | 🎯 N dmg → [slot, currently: X]       | Targets a specific bench slot, bypassing Lead   |
| Buff(stat)                | ⬆️ [stat]                             | Enemy raises one of its own stats by one stage  |
| Stall(effect)             | 🛡️ [effect]                          | Enemy applies a defensive effect to itself      |
| Status(condition, target) | 💢 [condition] → [slot, currently: X] | Applies a status condition to a slot's occupant |
| Unknown                   | ❓                                     | Intent is hidden. See §4.3.5                    |


**Position-targeting tactical implication:** every Backstrike intent has a clear counter-play — swap a sturdy Pokémon into the targeted slot to absorb the hit, or swap a fragile Pokémon out of harm's way. Positional play becomes both offensive (who can play which moves) _and_ defensive (where do I want my Pokémon when the hit lands).


## §4.3.3 Enemy AI — Context-Aware Scoring


Each turn, the enemy evaluates all available intents through a scoring function and selects the highest-scoring available option:


```javascript
Score(intent) = BaseWeight
              × TypeEffectivenessMultiplier
              × StatusStateModifier
              × HPStateModifier
              × CooldownGate
```


**TypeEffectivenessMultiplier:**

- Attack vs super-effective target: ×2.0 scoring bonus.
- Attack vs resisted target: ×0.5 penalty.
- Attack vs immune target: ×0.0 — AI never attacks into an immunity.
- Status vs type-immune Pokémon: ×0.0 — AI never wastes a status move on an immune target.

**StatusStateModifier:**

- Status intent vs already-statused Pokémon (primary status): ×0.0 — AI never applies a redundant primary status.
- Status intent vs Pokémon with only secondary status (Confusion): full weight — primary status can still be applied.

**HPStateModifier:**

- Player Pokémon HP < 30%: Attack intents targeting that Pokémon receive ×2.0 bonus — AI tries to finish weakened Pokémon.
- Enemy own HP < 40%: aggressive intents receive elevated priority — the AI plays urgently when losing.
- Enemy own HP > 70%: setup intents receive elevated priority — the AI invests in long-game pressure when healthy.

**CooldownGate:** 0 if the intent is on cooldown; 1 if available. High-impact moves carry cooldowns that the AI tracks and plans around.


**Randomness floor:** each turn, a **10–15% chance** (seeded, deterministic) causes the AI to select a non-top-scored intent. Prevents the scoring function from being fully reverse-engineered while preserving intelligent aggregate behaviour.


## §4.3.4 Cleave and Backstrike Rules


**Cleave:**

- Damages all non-fainted slots for N each (Lead + bench).
- N is typically 50–70% of the enemy's standard single-target damage.
- Always displayed as: `⚔️ [N] dmg → ALL SLOTS`.

**Backstrike:**

- Targets a specific bench slot, bypassing the Lead.
- **Target slot selection:** AI scores potential target slots by current occupant — selects the slot whose occupant maximizes expected damage (lowest-HP target vs best type-effectiveness target).
- **Elite/Boss targeting:** scripted or type-matchup-based, often locked to a specific slot at intent declaration.
- Always displayed as: `🎯 [N] dmg → [slot, currently: X]`.
- **Player counter-play:** swap a different Pokémon into the targeted slot before Resolution; the hit lands on the new occupant.

### §4.3.4.1 Empty-slot resolution

- **Cleave** damages every currently occupied non-fainted slot (1 to 3 targets). Cleave never fizzles — if only 1 Pokémon remains in the Active Team, Cleave deals damage to that single occupant.
- **Backstrike** targets a _specific_ bench slot declared in the Intent Phase. If the target slot is empty (no occupant or the slot's previous occupant has fainted) at Resolution time, the Backstrike fizzles — no damage is dealt, and the intent does not redirect to the Lead. This preserves Backstrike's identity as a position-punishing move: if the player leaves no bench, they cannot be Backstruck.

## §4.3.5 Unknown Intent & Revelation System


**UI:** a ❓ icon replaces the intent display. The player knows the enemy will act but not how.


**Three-tier revelation system:**


| Tier           | Trigger                                                  | Scope                                                            |
| -------------- | -------------------------------------------------------- | ---------------------------------------------------------------- |
| **Witnessed**  | Enemy uses the move/ability in combat                    | Revealed for rest of this combat; permanently logged in Bestiary |
| **Scouted**    | Player uses Foresight (move) or Radar Scope (consumable) | All Unknown intents revealed for this combat only                |
| **Researched** | Keen Eye passive ability or specific relic               | All Unknown intents revealed at combat start for this run        |


**Usage scope:**

- Standard enemies: at most one Unknown intent per encounter.
- Boss-tier: a recurring portion of the boss's intent pool is Unknown — incentivizing investment in uncovering tools.

**Boss counter-intel mode:** when a boss's full intent pool is revealed, boss-tier AI slightly deprioritizes its top-scored intent to break predictability. Standard enemies play optimally regardless of player knowledge.

> ⚠️ **Open Sev-2 gap:** Counter-intel mode mechanism needs full spec. See BACKLOG gap #9.

## §4.3.6 Multi-Enemy Encounters


Introduced in Region 3 as a mechanical accent. Structure: one Lead enemy + one or two support enemies.

- All enemies reveal intents simultaneously in the Intent Phase.
- Resolution order: supports first (in slot order); Lead enemy last.
- Player selects a specific enemy for each offensive card played.

**Support roles:**


| Role         | Primary Behavior                            |
| ------------ | ------------------------------------------- |
| **Healer**   | Restores Lead enemy HP each turn            |
| **Buffer**   | Applies stat buffs to Lead enemy            |
| **Debuffer** | Applies status conditions to player Pokémon |
| **Attacker** | Additional damage source                    |


Each encounter mixes support roles deliberately to prevent a dominant kill-order strategy. All support AI uses the same scoring function — a Debuffer never applies redundant status; a Healer prioritizes healing when the Lead is low HP.


Support HP pools are reduced — designed to be eliminated within 2–3 turns. Supports that survive beyond their expected lifespan escalate their threat.


## §4.3.7 Boss & Elite Phase Design


Elite enemies and bosses follow scripted intent sequences with condition-based phase transitions. Within each phase, the AI scoring function still selects optimal targets — phases constrain intent _type_, not intent _target_.


**Signature phase types:**


| Phase Type            | Description                                                                 |
| --------------------- | --------------------------------------------------------------------------- |
| **Mass Attack Phase** | All intents this phase are Attack or Cleave type                            |
| **Mass Status Phase** | All intents this phase are Status type; AI applies conditions strategically |
| **Setup Phase**       | Boss buffs itself aggressively before a devastating follow-up               |
| **Evolution Phase**   | Boss Pokémon evolves mid-fight; telegraphed one turn in advance             |
| **Signature Phase**   | Boss uses its highest-impact move, bypassing cooldown                       |


**Mid-fight evolution** (Gym Leader tier and above):

- One turn before: `✨ [Pokémon name] is gathering energy — EVOLUTION IMMINENT.`
- The player has one turn to burst below the threshold or prepare.
- On evolution: stats increase, move pool updates, intent sequence resets to new phase pattern.

## §4.3.8 Field Effects


Field effects are environmental modifiers set at encounter start, persisting for the full combat unless overwritten. Introduced in Region 3 as a mechanical accent. The AI scoring function accounts for active field effects when selecting intents.


**Launch field effects (3 at launch):**


### §4.3.8.1 ☀️ Sunny Day (Weather)

- Fire moves: ×1.5 damage.
- Water moves: ×0.5 damage.

### §4.3.8.2 🌧️ Rain Dance (Weather)

- Water moves: ×1.5 damage.
- Fire moves: ×0.5 damage.

### §4.3.8.3 ⚡ Electric Terrain (Terrain)

- Electric moves: ×1.3 damage to grounded Pokémon.
- Paralysis cannot be applied to grounded Pokémon.
- Grounded = all Pokémon except Flying-type or those with the Levitate ability tag.

**Field effect UI:** active field effects displayed persistently in combat UI. Card hover damage previews account for active field effects automatically.


## §4.3.9 Bestiary & Species Mastery (post-vertical-slice)


The **Bestiary** tracks cumulative experience with each enemy species across all runs. Two functions: knowledge accumulation (reducing Unknown intents on familiar enemies) and mastery rewards.


### §4.3.9.1 Mastery tiers (thresholds scale by species rarity)


| Tier         | Common Wild | Uncommon Wild | Rare / Legendary | Reward                                                                         |
| ------------ | ----------- | ------------- | ---------------- | ------------------------------------------------------------------------------ |
| **Familiar** | 10 kills    | 5 kills       | 2 kills          | All Unknown intents permanently revealed at combat start for this species      |
| **Veteran**  | 30 kills    | 15 kills      | 5 kills          | Player's own Pokémon of this species become **Shiny** (visual variant)         |
| **Master**   | 50 kills    | 25 kills      | 10 kills         | Unlocks a unique **Mastery Move** permanently added to this species' move pool |


### §4.3.9.2 Mastery Move system


Every Pokémon species has a Mastery Move line defined across its evolution stages — a dedicated **5th card slot** that exists beyond the active 4-move configuration. Unlike the 4 pool moves, the Mastery card occupies a permanent slot: the player cannot swap it out or reconfigure it, and TMs, Move Tutors, and evolution upgrades do not affect it.


**All Pokémon have a base Mastery Move.** The Lv1 Mastery card is defined in the base-form `PokemonSpeciesSO` for every species, regardless of how many evolution stages they have. A two-stage species (e.g., Geodude → Golem) has Mastery Lv1 and Lv2; a three-stage species has Lv1, Lv2, and Lv3. Each tier is defined in the corresponding stage's `PokemonSpeciesSO`.


**Mastery tiers — one per evolution stage:**

- **Mastery Lv1 (Base form):** Low-to-mid power, 1 AP cost, no SF/SB modifier. Defined in the base-form `PokemonSpeciesSO`.
- **Mastery Lv2 (Stage 1 evolution — or final form for 2-stage lines):** Mid-to-high power, 1–2 AP cost. May carry a SF/SB modifier or a status rider.
- **Mastery Lv3 (Stage 2 / final evolution — 3-stage lines only):** High power, 2–3 AP cost. Signature-tier with branch-themed effect modifiers. May rival the branch signature move.

**Mastery tier advancement:** When a Pokémon evolves, their Mastery Move automatically upgrades to the next tier — provided that tier has been unlocked via meta-progression. If Lv2 has not been unlocked, the Pokémon retains Lv1 after evolution until the Lv2 achievement is earned.


**Unlocking Mastery tiers — per-species meta-progression:**


Mastery tier access for each species is tracked persistently across runs in `MetaProgressionSO`. Tiers unlock independently:


| Tier                      | Unlock Category                  | Criteria                                                                                                                                               |
| ------------------------- | -------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Lv1 — Familiar Bond**   | General progression              | Win 3 combats with this Pokémon in the Active Team, OR recruit/capture this Pokémon for the first time, OR complete any run with it in the Active Team |
| **Lv2 — Trusted Partner** | Species-specific achievement     | Unique per species — themed to combat identity (see §5.11)                                                                                             |
| **Lv3 — Deep Bond**       | High-difficulty species-specific | Requires sustained multi-run effort (see §5.11); three-stage lines only                                                                                |


**Deck-size integration:**

- Deck size scales with how many Active Team members have Mastery unlocked: 0 = 12 cards (baseline); 1 = 13; 2 = 14; 3 = 15.
- Hand size remains fixed at 5 skill cards per turn, regardless of deck size.
- When a Pokémon with an unlocked Mastery faints, 5 cards are removed from the Skill Deck and discard pile (4 pool moves + 1 Mastery) instead of 4.

**Slot immutability:** The Mastery slot is the only card in a Pokémon's hand that the player cannot reconfigure. It cannot be replaced by TMs, Move Tutors, or any other customisation system. Its tier can only advance (via evolution + achievement unlock), never downgrade.


**Design intent:** The Mastery system rewards sustained investment in specific Pokémon across multiple runs. A player who repeatedly fields Squirtle unlocks its Lv1 Mastery early; completing Squirtle-specific challenges eventually grants Blastoise a Lv3 Mastery that rivals the branch signature move. Mastery is the meta-game layer that makes a well-travelled Pokémon feel genuinely more powerful in the hands of a trainer who has earned it.


Full Mastery Move achievement catalog: see §5.11.

- Mastery Moves are unique to their species' evolution line and stronger than base learnset moves of equivalent AP cost.

**Shiny implementation:** AI-generated alternate sprites per species; runtime hue-shift shader as programmatic fallback. No hand-drawn alternate sprites required.


---


# §4.4 Boss Design


## §4.4.1 Overview


Boss encounters are the climactic expression of Project Ascendant's combat system. They differ from standard encounters in four ways:

1. **Multi-phase structure:** condition-triggered phase transitions escalate behavior mid-fight.
2. **Scripted intent sequences:** defined patterns with condition-based branches; scoring function still picks optimal targets within each phase.
3. **Unique mechanics:** mid-fight evolution, forced phase types, and encounter-specific signature mechanics.
4. **Permanent rewards:** Gym Leaders award Badges; all boss encounters award rare relics and meta-progression XP.

## §4.4.2 Boss Tiers & Pokémon Count


| Tier               | Count | Pokémon fielded                      | Phase depth                        | Total fights |
| ------------------ | ----- | ------------------------------------ | ---------------------------------- | ------------ |
| **Gym Leader**     | 3     | 2 each, sequential                   | 2 phases; ace gets 3               | 6            |
| **Elite Four 1–2** | 2     | 2 each, sequential                   | 2 phases; ace gets 3               | 4            |
| **Elite Four 3–4** | 2     | 3 each, sequential                   | 2 phases; ace gets 3               | 6            |
| **Champion**       | 1     | 5; sequential (final 2 simultaneous) | 2–3 phases; ace gets 3 + evolution | 5            |
| **Total**          |       |                                      |                                    | **21**       |


## §4.4.3 Phase Structure


Every boss Pokémon has a minimum two-phase structure. Phase thresholds are visible to the player as markers on the enemy HP bar.


**Standard two-phase template:**


| Phase       | Trigger  | Behavior                                                                                    |
| ----------- | -------- | ------------------------------------------------------------------------------------------- |
| **Phase 1** | HP > 50% | Standard sequence. Setup-oriented — Buff, Status, methodical Attack. Boss reads the player. |
| **Phase 2** | HP ≤ 50% | Forced phase type activates. Boss plays urgently and aggressively.                          |


**Three-phase template (ace Pokémon of each tier):**


| Phase       | Trigger  | Behavior                                                                                                                          |
| ----------- | -------- | --------------------------------------------------------------------------------------------------------------------------------- |
| **Phase 1** | HP > 50% | Standard sequence.                                                                                                                |
| **Phase 2** | HP ≤ 50% | Signature phase — escalated aggression.                                                                                           |
| **Phase 3** | HP ≤ 20% | Last-stand: cooldowns reset; signature move fires without cooldown; Sturdy passive may activate (survive one lethal hit at 1 HP). |


### §4.4.3.1 Stat stage persistence across phase transitions


Stat stage modifiers (both player-applied debuffs on the boss and boss-applied buffs on itself) persist across phase transitions. A boss that has been debuffed -2 Attack in Phase 1 enters Phase 2 still at -2 Attack. Dispelling boss buffs remains a valuable player investment regardless of phase progression.


## §4.4.4 Gym Leaders & Branching Gym Paths


### §4.4.4.1 Branching path structure


Each Region's map contains a **branch point** where two diverging paths lead to two different Gym Leaders. The player chooses one path — the other is permanently abandoned for this run. Both Gym types and their Badge rewards are **fully visible when the choice is made.** This preserves the Telegraphed-Tactics pillar at the macro-loop level.


### §4.4.4.2 Gym type pool — structured random


Gym types are seeded-randomly drawn from a tiered pool per Region:


| Region   | Difficulty tier | Types in pool                  |
| -------- | --------------- | ------------------------------ |
| Region 1 | Early-game      | Rock, Water, Bug, Normal       |
| Region 2 | Mid-game        | Fire, Grass, Electric, Poison  |
| Region 3 | Late-game       | Psychic, Ground, Fighting, Ice |


No two paths within a Region share the same type. 12 total Gym types in the pool; 3 Badges earned per run; 9 types missed per run — driving replayability. 220 possible 3-Badge combinations from a 12-Badge pool.


### §4.4.4.3 Gym Leader design rules

- **2 Pokémon, sequential.** Second is the ace (3-phase design, mid-fight evolution eligibility at 50% HP).
- Type identity consistent with the drawn type — full team and moveset reflect that type.
- A field effect matching their type is set at encounter start and persists for the full fight.
> 📝 Design note (2026-05-29): §4.4.4.3 sets a type-matching field for the Gym fight, but type fields (e.g. Rock) have no damage multiplier defined yet — only Weather (Sunny/Rain) and Terrain (Electric) do (§4.3.8). VS behaviour: the field is set for flavour/telegraph but is mechanically inert until type-field multipliers are designed (post-VS).
> Blocked: the damage/utility effect of a Gym type field. VS stub (Task 8.5.5): FieldState.GymTypeField is set at encounter start and persists (marker only, no multiplier). See BACKLOG gap #33.

## §4.4.5 Badge Pool (12 Total)


Twelve Badges in total — one per Gym type. **3 Badges earned per run** from Gym Leaders (one per Region from the chosen path). Up to 1 additional Badge per run from rare in-run bonus sources (see §4.5.3). Maximum 4 Badges per run from a pool of 12.


Each Badge is permanently active from the point of award until run end.


### §4.4.5.1 REGION 1 TIER — Early Game Types


**🪨 Boulder Badge (Rock-type)**


**Effect:** "Your Lead Pokémon reduces all incoming damage by 1 (minimum 0)."


**Playstyle:** durability. Rewards keeping a tanky Lead in play. Flat reduction is meaningful early, self-balancing late.


**Synergizes with:** Cascade; Rainbow; Fist.


**💧 Cascade Badge (Water-type)**


**Effect:** "After you manually swap the Lead, draw 1 additional skill card this turn."


**Playstyle:** tempo. Swapping generates card advantage — transforms AP cost into a card-draw investment.


**Synergizes with:** Boulder; Marsh; Earth; Hive.


**🐛 Hive Badge (Bug-type)**


**Effect:** "Whenever a skill card cycles back into the Skill Deck from the discard pile, it has a 20% chance to generate a free copy in your hand the following turn."


**Playstyle:** deck-cycling rewards. High-velocity, low-cost card play generates bonus draws over time.


**Synergizes with:** Cascade; Marsh; Normal.


**⭐ Normal Badge (Normal-type)**


**Effect:** "Your Pokémon's base stats are treated as 10% higher when calculating damage dealt and received."


**Playstyle:** universal efficiency. The "safe choice" — always useful, never spectacular, never degenerate. The accessible option for players not yet committed to a specific build direction.


**Synergizes with:** everything moderately. Intentional.


### §4.4.5.2 REGION 2 TIER — Mid Game Types


**🔥 Volcano Badge (Fire-type)**


**Effect:** "Your Offensive cards that cost 3 or more AP deal +20% damage."


**Playstyle:** high-cost burst. Rewards building toward 3-AP power moves and Ultimate cards.


**Synergizes with:** Soul; Earth; Fist.


**🌿 Rainbow Badge (Grass-type)**


**Effect:** "At the start of each turn, if your Lead Pokémon has a status condition, they restore 3 HP."


**Playstyle:** status resilience. Rewards staying in with a statused Lead — partially offsets Burn/Poison DoT damage.


**Synergizes with:** Boulder; Glacier.


**⚡ Thunder Badge (Electric-type)**


**Effect:** "Once per turn, the first Ranged move you play costs 1 less AP (minimum 0)."


**Playstyle:** Ranged efficiency. Partially offsets the 70–80% Ranged damage scaling — cheaper Ranged moves become more economically competitive.


**Synergizes with:** Cascade; Soul.


**💜 Marsh Badge (Poison-type)**


**Effect:** "When you successfully apply a status condition to an enemy, draw 1 skill card."


**Playstyle:** status-offensive. Each successfully applied status triggers a card draw — creates a resource loop.


**Synergizes with:** Cascade; Glacier; Hive.


### §4.4.5.3 REGION 3 TIER — Late Game Types


**🔮 Soul Badge (Psychic-type)**


**Effect:** "At the start of each combat, all Unknown intents are revealed for the first 2 turns."


**Playstyle:** vision. Thematically inseparable from Psychic — foresight as a Badge mechanic.


**Synergizes with:** Thunder; Volcano; Clear Mind League Boon.


**🌍 Earth Badge (Ground-type)**


**Effect:** "Your Step-Forward and Step-Backward moves cost 1 less AP (minimum 0)."


**Playstyle:** positional mastery. Makes SF/SB play more economically viable — premium AP cost reduced, enabling more aggressive positional combo turns.


**Synergizes with:** Cascade; Volcano; Fist.


**🥊 Fist Badge (Fighting-type)**


**Effect:** "Your Melee moves deal +25% damage."


**Playstyle:** melee-dominant. Heavily rewards physical attacker builds and Lead-centric play.


**Synergizes with:** Boulder; Earth; Volcano.


**❄️ Glacier Badge (Ice-type)**


**Effect:** "Whenever an enemy has a status condition applied to them, their next attack deals 15% less damage."


**Playstyle:** debuff-control. Every status landed is also a defensive play — proactive status application reduces incoming damage universally.


**Synergizes with:** Marsh; Rainbow.


### §4.4.5.4 Full 12-Badge Summary


| Badge   | Type     | Tier | Core Effect                                    |
| ------- | -------- | ---- | ---------------------------------------------- |
| Boulder | Rock     | R1   | Lead takes −1 incoming damage                  |
| Cascade | Water    | R1   | Manual swap → draw 1 card                      |
| Hive    | Bug      | R1   | Deck cycling has 20% chance to copy a card     |
| Normal  | Normal   | R1   | All stats treated as 10% higher                |
| Volcano | Fire     | R2   | 3+ AP Offensive cards deal +20% damage         |
| Rainbow | Grass    | R2   | Statused Lead restores 3 HP per turn           |
| Thunder | Electric | R2   | First Ranged move per turn costs −1 AP         |
| Marsh   | Poison   | R2   | Applying status → draw 1 card                  |
| Soul    | Psychic  | R3   | Unknown intents revealed for the first 2 turns |
| Earth   | Ground   | R3   | SF/SB moves cost −1 AP                         |
| Fist    | Fighting | R3   | Melee +15% (bench) / +25% (Lead)               |
| Glacier | Ice      | R3   | Statused enemy attacks deal −15% damage        |


---


# §4.5 Victory Road


Victory Road is a dedicated preparation zone between Gym 3 and the League — a small branching mini-map (3–4 layers, 2–3 lanes) with specialized node types not found in Regions. It is the most challenging non-boss traversal content in the game and the player's final preparation window.


## §4.5.1 Victory Road node types


### §4.5.1.1 ⚔️ Gauntlet Battle Node

- Elite-tier trainer fight. **No pre-fight healing** — the player enters at whatever HP they have. Difficulty between Gym 3 and Elite 1.
- **Reward:** choice of one of three rare relics (higher rarity pool than standard drops).
- 1–2 Gauntlet nodes available on different paths per run.

### §4.5.1.2 🦕 Apex Pokémon Node

- Special recruitment opportunity — one high-rarity Pokémon species exclusive to Victory Road, not recruitable in any Region.
- Already partially evolved or with a unique starting moveset.
- One per Victory Road per run, on a specific seeded path — not guaranteed on every route.

### §4.5.1.3 🏋️ Training Grounds Node


A risk-free team upgrade node — no fight required. Designed for players who want to strengthen their existing team rather than acquire a new Pokémon or risk a Gauntlet fight.


Player selects one Pokémon and one upgrade type:


| Upgrade Type         | Effect                                                                                                                     |
| -------------------- | -------------------------------------------------------------------------------------------------------------------------- |
| **Stat Boost**       | Permanently increase one stat (Attack / Defence / Special / HP) by a flat amount for the rest of the run                   |
| **Move Upgrade**     | Enhance one existing move: increase Power, reduce AP cost by 1, add an effect modifier (SF/SB), or add a status rider      |
| **Level Push**       | Grant bonus XP equivalent to one level-up; stats increase accordingly                                                      |
| **Early Evolution**  | If the selected Pokémon is eligible to evolve but hasn't, trigger their evolution now with full branch choice presented    |
| **Move Replacement** | Replace one move with a stronger alternative from the Pokémon's learnset (normally unlocked at a later level or evolution) |


Only one upgrade granted per Training Grounds node. Upgrade types cannot be combined.


### §4.5.1.4 🏔️ Summit Preparation Node (mandatory final node)

- **Full HP restore** across all Box Pokémon — the last meaningful healing before the unbroken League gauntlet.
- **League Boon selection** — 3 Boons offered from a seeded pool of 6; player picks 1 (see §4.5.2).
- **League roster preview** — type identities and silhouettes of all Elite Four members and the Champion revealed. Pokémon names and movesets remain hidden.
- **Confirmation gate:** "Enter the League?" — one-way commitment.

Note: consumables are automatically restored at every combat end, so no explicit consumable refresh is needed at the Summit node.


## §4.5.2 League Boons


Selected at the Victory Road Summit node. Three are offered from a seeded pool of 6; the player picks one. Active **only during the 5 League encounters** (Elite 1–4 + Champion). Stronger than Badges — temporary scope permits higher power.


| Boon                 | Effect                                                                                     | Playstyle                                             |
| -------------------- | ------------------------------------------------------------------------------------------ | ----------------------------------------------------- |
| **Battle Hardened**  | All Pokémon start each League fight with a Shield = 15% max HP                             | Defensive; synergises with Boulder Badge              |
| **Flow State**       | First manual swap of each League fight costs 0 AP                                          | Swap-heavy; synergises with Cascade Badge             |
| **Last Stand**       | Each Pokémon survives one lethal hit at 1 HP per League fight (once per Pokémon per fight) | Endurance; universal                                  |
| **Type Mastery**     | Super Effective moves deal an additional ×0.25 bonus damage during League fights           | Type-optimal builds; rewards wide type coverage       |
| **Clear Mind**       | All Unknown intents are permanently revealed for all League encounters                     | Vision: synergises with Soul Badge + Keen Eye         |
| **Evolution's Edge** | All fully-evolved Pokémon deal +15% damage during League fights                            | Rewards complete evolution before entering the League |


## §4.5.3 Bonus Badge Sources (post-vertical-slice)


Up to 1 additional Badge per run from rare in-run bonus sources:


### §4.5.3.1 Secret Tournament node (rare map event)

- Appears at most once per run in Region 2 or Region 3.
- Offers a Badge from a Gym type outside the current run's pool — a type unavailable through normal Gym pathing.
- Badge type is seeded — predictable on repeat runs with the same seed.

### §4.5.3.2 Victory Road Perfect Clear (skill reward)

- Clearing a Gauntlet Battle node without any Active Team Pokémon fainting earns a bonus Badge — the Badge corresponding to the Gym path not chosen in the matching Region tier.
- Rewards skilled play and partially compensates players for the "missed path" choice.

---


# §4.6 Elite Four


Four sequential Elite Four encounters forming the League's main gauntlet. No map navigation; **micro-rests between fights restore 30% HP**.


**Team size (escalating):**

- **Elite 1–2:** 2 Pokémon each (warm-up + ace). Recalibration difficulty.
- **Elite 3–4:** 3 Pokémon each (two encounters + ace). Full-length encounters.

## §4.6.1 Difficulty curve


| Fight    | Difficulty           | Notes                                      |
| -------- | -------------------- | ------------------------------------------ |
| Elite 1  | Easiest League fight | Recalibration — slightly easier than Gym 3 |
| Elite 2  | Moderate             | Meaningful step up                         |
| Elite 3  | Hard                 | Near-Elite 4 difficulty                    |
| Elite 4  | Very Hard            | Hardest non-Champion fight                 |
| Champion | Hardest              | Categorically harder than all Elite Four   |


**Design rules:**

- Distinct type identity per member.
- Mixed field effects — conditions the player hasn't faced at full boss intensity before.
- Counter-intel mode active when fully scouted (§4.3.5).
- Ace Pokémon always has 3-phase design.

---


# §4.7 League Champion


The final boss — the most mechanically complex encounter in the game.


**Team:** 5 Pokémon covering multiple types. No single type counter works across the full team.


**Format:** sequential for Pokémon 1–3. Pokémon 4 and 5 fielded **simultaneously** (lead + support hybrid) — the only encounter in the game using this format. The support provides Healing or Buffing, dramatically increasing the Champion's final Pokémon's staying power.


**Phase depth:** every Champion Pokémon has 2 phases minimum. The ace (Pokémon 5) has 3 phases and triggers mid-fight evolution at 50% HP.


## §4.7.1 Champion Signature Mechanic — Full Team Synergy


Each defeated Champion Pokémon passively buffs all remaining ones: **+5% Attack per defeated ally**, stacking (max +20% with 4 defeated). Displayed from encounter start ("Each fallen ally empowers their teammates") as a live buff stack on remaining enemy HP bars.


_(Note: Previous draft used +10%/+40%. Reduced for balance — playtest may re-tune.)_


---


# §4.8 Open Sev-2/3 Resolutions (added 2026-05-24)


This section resolves remaining BACKLOG-tracked combat-system gaps. Each is now part of the canonical spec.


## §4.8.1 Counter-Intel Mode Mechanism — RESOLVED (was BACKLOG gap #9)


When a boss's full intent pool is revealed (via Keen Eye, Soul Badge + Foresight + Radar combinations, Clear Mind League Boon, etc.), the boss's AI scoring (§4.3.3) applies a **Counter-Intel modifier**:

- The boss's top-scored intent for the turn has its score multiplied by **0.7** (a -30% penalty).
- The remaining intents are unchanged.
- This causes the boss to occasionally play its 2nd or 3rd best option, breaking the player's perfect-prediction loop.
- The Randomness Floor (10-15%) is **disabled** when Counter-Intel is active — Counter-Intel replaces it as the unpredictability source.
- Standard (non-boss) enemies do NOT use Counter-Intel. They always play optimally regardless of player knowledge — full information is the player's reward.

**Player-facing display:** when Counter-Intel is active in a boss fight, a small badge "Counter-Intel Active" displays under the boss's portrait. Pillar 1 compliance — the system itself is transparent, only its turn-by-turn output is varied.


## §4.8.2 Field Effect Category Stacking — RESOLVED (was BACKLOG gap #13)


**Weather and Terrain are independent categories. Both can be active simultaneously.** Only one Weather effect and one Terrain effect may be active at a time; applying a second of the same category overwrites the first.

- Multiplicative stacking: a Fire move under Sunny Day (×1.5) AND Electric Terrain (no fire interaction) = ×1.5 net. Sunny Day + a hypothetical "Fire Terrain" would stack multiplicatively to ×2.25.
- The launch field effects (§4.3.8) are 2 Weather + 1 Terrain. Post-launch may add additional Terrain effects (Grassy Terrain, Misty Terrain).

## §4.8.3 AlwaysCrit vs Crit-Reduction Edge Case — RESOLVED (was BACKLOG gap #15)


**At launch, no source of crit-reduction exists.** The system architecturally supports negative crit-chance modifiers (the calculation clamps to 0% floor, 100% ceiling), but no relic, ability, status, or move at launch applies one. If a future content addition introduces crit-reduction:

- AlwaysCrit cards bypass the crit-chance system entirely (they always crit, period).
- Stackable crit-chance % (consumable + evolution passive) is reduced first.
- The clamp floor is 0%.

UI hover preview will display the effective crit chance after all modifications (including future crit-reduction).


## §4.8.4 Confusion Soft-Lock Mitigation — RESOLVED (was BACKLOG gap #11)


Confusion's design safety floor (§4.2.3.1) ensures the player always retains 4 playable cards minimum even with all 3 Active Team Pokémon Confused (2 skill + 2 consumables, Confusion is skill-only). Full Heal consumable cures Confusion. **No additional pity-timer is needed.**


Architecture commitment: a `MAX_CONFUSION_STACKS = 3` constant (one per Active Team Pokémon) is enforced — Confusion cannot stack multiple times on the same Pokémon. The status either applies or is already-applied (no re-application boosts duration; new application resets the 3-turn timer).


## §4.8.5 Tutor Learnset Evolution Updates — RESOLVED (was BACKLOG gap #14)


Each `PokemonSpeciesSO` carries its own `TutorLearnset[]`. **Tutor Learnsets are stage-aware:** the `TutorLearnset` field lives on the species-stage SO, not the evolution line. When a Pokémon evolves, the Move Tutor service at the next visited City offers the evolved form's TutorLearnset — broader and more powerful moves than the pre-evolution.


A Pokémon visiting a Move Tutor sees only the moves available to their current evolution stage. Pre-evolution Squirtle can learn Water Gun upgrades; Wartortle can learn Hydro Cannon (mid-form); Blastoise can learn Hydro Pump (final). Each stage's TutorLearnset is authored individually in content.


This means a player who delays evolution to use the pre-form's tutor moves can do so, but loses access to the evolved tutor moves until they re-visit a tutor post-evolution.

