using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §8.3 + Epic 12 Task 12.3 — runtime dispatch for the run's held Trainer Relics, keyed by
    // RelicSO.Id at the relevant combat/run seams. Mirrors AbilityResolver (gap #42): the generic
    // ScriptableHook event-bus (§8.7) is post-VS; for the 15 VS relics, Id-dispatch is equally faithful
    // with a fraction of the surface. Combat-multiplier relics live here; meta (₽/XP) + stateful relics
    // dispatch at their own seams (RewardApplier / XPAwarder / SwapManager / CardPlayService).
    public static class RelicResolver
    {
        public static bool Holds(IReadOnlyList<RelicSO> relics, string id)
        {
            if (relics == null) return false;
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] != null && relics[i].Id == id) return true;
            return false;
        }

        // §8.3.3 — outgoing-damage multiplier from held relics, for a PLAYER attacker (relics are
        // run-state, player-side; the caller guards enemy attackers). Multiplicative, baseline 1.0.
        public static float OutgoingDamageMultiplier(PokemonInstance attacker,
                                                     IReadOnlyList<RelicSO> relics, BattleConfigSO cfg)
        {
            float mult = 1f;
            if (attacker == null || cfg == null) return mult;
            int max = PokemonVitals.MaxHP(attacker);

            // Brave Charm — Pokémon with HP < 50% deal +X% damage.
            if (Holds(relics, "brave_charm") && attacker.CurrentHP * 2 < max)
                mult *= cfg.BraveCharmDamageMultiplier;

            // Soothe Bell — at full HP, +X% on the next attack (VS approximation: full HP now).
            if (Holds(relics, "soothe_bell") && attacker.CurrentHP >= max)
                mult *= cfg.SootheBellDamageMultiplier;

            return mult;
        }

        // §8.3.7 (CL-021 — Q10) — Legendary outgoing-damage multiplier for a PLAYER attacker (relics are
        // player run-state; the caller guards enemy attackers). Composes: Type Mastery (super-effective
        // moves), Evolution's Edge (fully-evolved attacker), Apex Predator (the Lead at full HP — the
        // caller resolves Lead + full-HP). Multiplicative, baseline 1.0.
        public static float LegendaryOutgoingMultiplier(
            PokemonInstance attacker, bool superEffective, bool isLeadAtFullHP,
            IReadOnlyList<RelicSO> relics, BattleConfigSO cfg)
        {
            float mult = 1f;
            if (cfg == null) return mult;
            if (superEffective && Holds(relics, "type_mastery"))
                mult *= 1f + cfg.LegendaryTypeMasteryBonus;
            if (IsFullyEvolved(attacker) && Holds(relics, "evolutions_edge"))
                mult *= 1f + cfg.LegendaryEvolutionsEdgeBonus;
            if (isLeadAtFullHP && Holds(relics, "apex_predator"))
                mult *= 1f + cfg.LegendaryApexPredatorBonus;
            return mult;
        }

        // §8.3.7 — a Pokémon is fully evolved when its species has no further evolution branches.
        public static bool IsFullyEvolved(PokemonInstance p)
            => p?.Species != null && (p.Species.Branches == null || p.Species.Branches.Count == 0);

        // §8.3.3 Berry Pouch — healing consumables restore +X% HP. Returns the boosted heal amount.
        public static int ApplyHealBonus(int baseHeal, IReadOnlyList<RelicSO> relics, BattleConfigSO cfg)
        {
            if (cfg != null && baseHeal > 0 && Holds(relics, "berry_pouch"))
                return Mathf.FloorToInt(baseHeal * cfg.BerryPouchHealMultiplier);
            return baseHeal;
        }

        // §8.3.3 Lucky Egg Token + §8.3.7 (CL-021) Living Legend — all in-run XP ×multiplier(s),
        // stacking multiplicatively. Returns the boosted XP.
        public static int ApplyXpMultiplier(int baseXp, IReadOnlyList<RelicSO> relics, ProgressionConfigSO cfg)
        {
            if (cfg == null || baseXp <= 0) return baseXp;
            float mult = 1f;
            if (Holds(relics, "lucky_egg_token")) mult *= cfg.LuckyEggXPMultiplier;
            if (Holds(relics, "living_legend")) mult *= cfg.LivingLegendXPMultiplier;
            return Mathf.FloorToInt(baseXp * mult);
        }

        // §8.3.3 Quick Draw — +1 skill card on the FIRST turn of each combat.
        public static int QuickDrawBonus(IReadOnlyList<RelicSO> relics, int turnNumber)
            => (turnNumber == 1 && Holds(relics, "quick_draw")) ? 1 : 0;

        // §8.3.4 Move Echo — true when a Pokémon has played `threshold` distinct moves this turn and the
        // bonus hasn't already been granted this turn (grant once → +N AP next turn).
        public static bool MoveEchoTriggers(int distinctMoves, int threshold, bool alreadyGrantedThisTurn,
                                            IReadOnlyList<RelicSO> relics)
            => !alreadyGrantedThisTurn && distinctMoves >= threshold && Holds(relics, "move_echo");

        // §8.3.4 Choice Specs / Choice Band — the first Ranged / Melee move each turn costs 0 AP;
        // subsequent moves of that type cost +1. Returns the adjusted AP cost.
        public static int ApplyChoiceCost(int apCost, MoveSO move, IReadOnlyList<RelicSO> relics,
                                          int rangedPlayedThisTurn, int meleePlayedThisTurn)
        {
            if (move == null) return apCost;
            if (move.Range == MoveRange.Ranged && Holds(relics, "choice_specs"))
                return rangedPlayedThisTurn == 0 ? 0 : apCost + 1;
            if (move.Range == MoveRange.Melee && Holds(relics, "choice_band"))
                return meleePlayedThisTurn == 0 ? 0 : apCost + 1;
            return apCost;
        }
    }
}
