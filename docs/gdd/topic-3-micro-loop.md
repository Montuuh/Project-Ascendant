<!-- AUTO-GENERATED SNAPSHOT — DO NOT EDIT DIRECTLY -->
<!-- Last updated from Notion: 2026-06-05T14:35:00.000Z -->

**Status:** 🟢 In Progress


**Last Updated:** 2026-05-15 (migrated from Drive; Faint precedence patch applied)


**Cross-references:** Topic 2 (Map View loadout), Topic 4 (Intent system, Mastery Moves), Topic 5 (move kits, evolution upgrades).


---


# §3.1 Combat Overview


Combat is the moment-to-moment expression of Project Ascendant. Every encounter is a discrete, atomic event (no mid-combat save) that follows a fixed five-phase loop. The player's goal is to read enemy intents, manage Lead positioning, and convert hand-state into damage before their Active Team is wiped.


A combat resolves to one of two outcomes:

- **Victory:** all enemies defeated — or, in a wild encounter, the wild Pokémon is **successfully caught**. A catch ends the combat as a Victory and awards full combat XP (§7.3.4).
- **Defeat:** all 3 Active Team Pokémon faint.

**There is no draw state.** If the player kills the last enemy with a card play during the Action Phase, combat ends in Victory immediately — the enemy's Resolution Phase actions are cancelled. Victory and Defeat are checked continuously; the first condition met ends the combat.


Flee mechanics are not in scope for launch; specific consumables may simulate disengagement if designed later.


---


# §3.2 The Five Combat Phases


Each turn cycles through five phases. Each phase fires a corresponding event on the Event Bus to support decoupled UI / audio / relic logic.


## §3.2.1 Combat Start (once per encounter)

- Active Team is locked. The Skill Deck is built from the 4 moves of each Active Pokémon (12 cards baseline; up to 15 with Mastery Moves — see §4.3.9). The Consumable Pile is built from the consumables the player holds in inventory.
- Lead position is set to the first slot of the Active Team (player ordering on the Map View determines initial Lead).
- **Active Team enters combat at their current HP**, carried from the previous combat or last healing event. There is no automatic HP restoration on combat start.
- All status conditions and stat stage modifiers begin cleared (these reset only at combat end of the previous combat — see §4.2.7).
- Field effects (if any are triggered by the encounter) apply at this moment.

## §3.2.2 Draw Phase (start of each turn)

- Player draws **5 skill cards** from the Skill Deck and **2 consumable cards** from the Consumable Pile.
- Player AP refills to base (3 AP), modified by relics, Badges, and Region Modifiers.
- Swap counter resets to 0.
- Confusion discards (if any) resolve at this point (see §4.2.3).

## §3.2.3 Intent Phase

- Each enemy reveals one intent. Intents target **positions** (not individual Pokémon) — if an intent targets the bench-left slot, it hits whoever is in that slot when Resolution fires, regardless of swapping during the Action Phase.
- Intent display always shows: action type, damage/effect magnitude, and the targeted slot (with the Pokémon currently occupying it labeled for clarity).
- Intent types: Attack(N, slot), Cleave(N), Backstrike(N, slot), Buff(stat), Stall(effect), Status(condition, slot), Unknown. See §4.3.2 for full intent specification.

## §3.2.4 Action Phase

- Player has 3 AP to spend on:
    - Playing skill cards (0–3 AP each; rare 4-AP "ultimate" cards exist but require setup such as combos, passives, or consumables to make payable).
    - Playing consumables (typically free or low-cost; see §3.5).
    - Swapping the Lead manually (see §3.3.1).
- **Card effects resolve immediately when played.** If a card play kills an enemy or fulfills a Victory/Defeat condition, combat ends at that moment.
- Skill cards have two descriptive axes plus optional effect modifiers:
    - **Role:** Offensive, Defensive, or Utility.
    - **Range:** Melee or Ranged.
    - **Effect modifiers** (optional): Step-Forward, Step-Backward, status appliers, and others defined in Topic 4.
- **Melee cards** can only be played from the Lead position, unless they carry the Step-Forward modifier.
- **Ranged cards** can be played from any position.
- Cards can only be played from Pokémon that are in the Active Team and not fainted.
- Hand-state is fully visible. Hovering a card previews its calculated damage on the targeted position.
- The player ends the phase by clicking **End Turn**. (Future: Add auto-skip within X conditions)

## §3.2.5 Resolution Phase

- Enemies execute their telegraphed intents in declared order (slot order for multi-enemy: supports first, lead enemy last — see §4.3.6).
- For position-targeted intents, the hit lands on whoever currently occupies the targeted slot — _not_ on the Pokémon that was there when the intent was revealed.
- Status effects tick (Burn, Poison damage; etc.; see §4.2).
- If a Pokémon faints during resolution, the faint rule fires (see §3.3.5).
- The hand is sent to the discard pile (skill cards); used consumables are set aside until combat end; turn ends.
- Fires `OnEnemyTurn`, `OnDamageApplied`, `OnFaint`, `OnTurnEnd`.

Victory and Defeat resolve at any point during Resolution when their conditions are met.


---


# §3.3 The Lead Mechanic (load-bearing)


The Lead is the central tactical concept of combat.


## §3.3.1 Core rules

- **Damage absorption:** the Lead takes 100% of single-target enemy damage by default. Some enemy abilities can Cleave (damage all slots) or Backstrike (damage a specific bench slot). All such non-default targeting is always telegraphed in the Intent Phase.
- **Manual Lead swap cost:** swap costs scale within a turn — **1st swap = 1 AP, 2nd swap = 2 AP, 3rd swap = 3 AP.** The counter resets at turn start. Only manual swaps increment this counter; Step-Forward and Step-Backward do not.
- **Defensive swap discount:** a manual Lead swap reduces the AP cost of the _first_ Defensive-tagged card played after the swap, that turn, by 1 (minimum 0). The discount applies only to manual swaps, **not** to Step-Forward or Step-Backward bundled position changes.
- **Melee/Ranged interaction:** Lead position determines which Melee cards in hand are playable (unless they carry Step-Forward); Ranged cards are unaffected. Lead choice is therefore both a _who-absorbs-damage_ and a _what-cards-come-online_ decision.

## §3.3.2 Step-Forward (Melee modifier)

- Cards tagged Step-Forward:
    - If played from a bench Pokémon, that Pokémon becomes the Lead before the effect resolves.
    - If played from the Lead Pokémon, the effect resolves normally with no position change.
- Does not increment the swap counter.
- Does not receive the defensive swap discount.
- Allowed on any Role (Offensive, Defensive, Utility).

## §3.3.3 Step-Backward (Melee modifier)

- Cards tagged Step-Backward:
    - If played from the Lead Pokémon, the effect resolves first, then the Lead swaps with a bench Pokémon of the player's choice.
    - If no non-fainted, non-frozen bench Pokémon exists, the effect still resolves and the Lead remains Lead.
- Does not increment the swap counter.
- Does not receive the defensive swap discount.
- Allowed on any Role (Offensive, Defensive, Utility).

## §3.3.4 Modifier exclusivity

- A single card may carry either Step-Forward or Step-Backward, but not both.
- Both modifiers are exclusive to Melee cards. Ranged cards do not need positional modifiers because they play from any position by default.

## §3.3.5 Faint resolution


Two distinct cases:

- **If the Lead Pokémon faints:** the player chooses any non-fainted bench Pokémon to take the Lead position at no AP cost. The Active Team must always have a Lead while any Pokémon remain non-fainted.
- **If a bench Pokémon faints (from Cleave, Backstrike, or DoT):** the Pokémon simply leaves the Active Team. The Lead remains unchanged. No swap prompt.

In both cases, the fainted Pokémon's 4 moves (plus Mastery Move if applicable — see §4.3.9) are removed from the Skill Deck _and_ the discard pile entirely.


### §3.3.5.1 Faint precedence rule


Faint resolution always takes precedence over position-restricting status conditions. If the Lead Pokémon is Frozen (position-locked) and faints in the same Resolution Phase, the Freeze position-lock is voided by the faint, and the player selects a new Lead from the non-fainted bench at no AP cost, as standard.


## §3.3.6 All-faint


If all 3 Active Team Pokémon faint, combat ends in Defeat.


---


# §3.4 The Skill Deck

- Built from the 4 moves of each Active Pokémon (12 cards baseline). Each mastered Pokémon in the Active Team adds one Mastery Move (see §4.3.9), expanding the deck by 1 card per mastered Pokémon (max 15 cards at full mastery).
- Cards played from hand go to the discard pile. When the Skill Deck empties, the discard pile reshuffles into it.
- Fainted Pokémon's moves are purged from both Skill Deck and discard pile at the moment of fainting.
- Cards are not permanently destroyed during combat unless a specific effect describes it.
- Cards from a specific Pokémon are only playable while that Pokémon is in the Active Team and not fainted.

---


# §3.5 Consumables

- The Consumable Pile is built at combat start from the player's persistent inventory.
- Each turn, 2 consumable cards are drawn from the pile alongside the skill cards.
- Consumables provide combat-flow utility: AP grants, card retention for next turn, healing, status clearing, stat buffs, intent-revealing, and other effects.
- Within a single combat, each consumable can be used **once**. After use, that consumable is set aside and not redrawn for the remainder of the combat.
- At combat end, all consumables are automatically restored to the player's persistent inventory — consumables are _not_ expendable resources; they are a per-combat-use roster.
- Consumables are **upgradable**: Potion → Super Potion → Hyper Potion → Max Potion (and similar tiers for other consumable lines). Upgrades are awarded through specific node types, relics, or events (defined in Topic 8).

---


# §3.6 Move Tag Taxonomy


Every move card is classified along two primary axes, plus optional effect modifiers. The taxonomy is data-driven (ScriptableObject fields) and consumed by the Lead mechanic, AI targeting, and relic effects.


| Axis                        | Values                                             | Effect on play                                       |
| --------------------------- | -------------------------------------------------- | ---------------------------------------------------- |
| Role                        | Offensive / Defensive / Utility                    | Determines manual-swap discount eligibility          |
| Range                       | Melee / Ranged                                     | Melee = Lead-only (unless SF); Ranged = any position |
| Effect Modifiers (optional) | Step-Forward, Step-Backward, status appliers, etc. | SF/SB are Melee-only and mutually exclusive          |


**Move design guidelines:**

- No mandatory-Ranged rule — Pokémon kits are designed freely based on species identity and tactical role.
- Ranged moves are generally scaled to ~70–80% damage of Melee moves at equivalent AP cost (via the `RangeModifier` field — see §4.1.1).
- Step-Forward and Step-Backward moves are scarce on pre-evolution Pokémon and become more common as Pokémon evolve. Evolution upgrades existing moves into SF/SB variants where thematically appropriate (see §5.3).

---


# §3.7 Action Economy Summary


| Resource        | Base                    | Refresh               | Notes                                             |
| --------------- | ----------------------- | --------------------- | ------------------------------------------------- |
| AP              | 3                       | Each turn             | Modified by relics, Region Modifiers, consumables |
| Hand Size       | 5 skill + 2 consumable  | Each turn             | StS-style discard-and-reshuffle                   |
| Swap Counter    | 0                       | Each turn             | Manual swaps only; SF/SB do NOT increment         |
| Skill Deck      | 12 cards                | Reshuffles when empty | One Pokémon = 4 cards (+1 if mastered)            |
| AP cost ceiling | 3 standard / 4 ultimate | /                     | Caps single-card dominance                        |


---


# §3.8 Combat-State Pointer to Topic 9 (added 2026-05-24)


The five-phase combat loop is implemented as a sub-state machine under the `CombatState` HSM node. Topic 9 §9.5.2 details the full HSM transitions. The 5 phases here map 1:1 to: `DrawPhase`, `IntentPhase`, `ActionPhase` (with `PlayerActing` / `PlayerEndTurn` sub-states), `ResolutionPhase` (with `ApplyEnemyIntents` / `ApplyStatusTicks` / `ResolveFaints` / `CheckVictoryDefeat` sub-states), and `TurnEnd` (transition back to `DrawPhase`).


# §3.9 Consumable Catalog Pointer (added 2026-05-24)


The 24 launch consumables (healing, status cures, combat utility, Pokéballs) and tiered upgrade chains are specified in Topic 8 §8.2.


# §3.10 Trauma Compatibility (added 2026-05-24)


Within combat, `MaxHP` references resolve to `EffectiveMaxHP` per §6.2. This includes:

- Healing consumable caps.
- Move-effect heal caps.
- Burn / Poison DoT formula: `floor(EffectiveMaxHP / 16)` (NOT BaseMaxHP).
- Leftovers Held Item regen: `floor(EffectiveMaxHP / 16)` (per §8.4.4).

Stat-stage debuffs (§4.2.6) operate on Attack/Defense and are unaffected by Trauma.

