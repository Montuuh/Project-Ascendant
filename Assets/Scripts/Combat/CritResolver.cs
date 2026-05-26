using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.1.3 + Epic 4 Task 4.4 — crit-chance resolver.
    //
    // Rules (§4.1.3):
    //   • Base crit chance: 0%. Nothing crits unless a source applies.
    //   • Three sources:
    //       1) AlwaysCrit (move-level)            — 100%, independent.
    //       2) Consumable temp boost (per-combat) — additive with source 3.
    //       3) Evolution passive (per-Pokémon)    — additive with source 2.
    //   • Stacking: sources 2 + 3 sum, clamped to [0, 1]. Source 1 overrides
    //     and short-circuits the RNG roll (no stream consumption on AlwaysCrit).
    //   • Crit multiplier (1.5×) lives in BattleConfigSO and is applied by
    //     DamageCalculator — NOT here. This resolver owns the bool decision.
    //
    // RNG: caller passes the CombatRNG stream from RNGStreams (per §9.7.2 —
    // "CombatController — crit checks, AI randomness floor"). No RNG inside
    // the calculator means determinism + testability.
    public static class CritResolver
    {
        // Resolve with an RNG roll. Used by the combat loop at Resolution time.
        public static CritResult Resolve(in CritInputs inputs, GameRNG rng)
        {
            bool alwaysCrit = inputs.Move != null && inputs.Move.AlwaysCrit;
            float rawSum = inputs.CombatTempBonus + inputs.PermanentPassiveBonus;
            bool hasChanceBonus = rawSum > 0f;

            // AlwaysCrit short-circuits — no RNG roll consumed, chance reported as 1.0.
            if (alwaysCrit)
            {
                return new CritResult(
                    isCrit: true,
                    resolvedChance: 1f,
                    isAlwaysCrit: true,
                    hasChanceBonus: hasChanceBonus,
                    isRedundant: hasChanceBonus);
            }

            float chance = Mathf.Clamp01(rawSum);
            bool isCrit = chance > 0f && rng != null && rng.Range01() < chance;

            return new CritResult(
                isCrit: isCrit,
                resolvedChance: chance,
                isAlwaysCrit: false,
                hasChanceBonus: hasChanceBonus,
                isRedundant: false);
        }

        // Per Task 4.4.4 — preview for the hover-UI panel. No RNG consumed.
        // IsCrit on the returned struct is meaningless for preview; consult
        // ResolvedChance + IsAlwaysCrit + IsRedundant.
        public static CritResult Preview(in CritInputs inputs)
        {
            bool alwaysCrit = inputs.Move != null && inputs.Move.AlwaysCrit;
            float rawSum = inputs.CombatTempBonus + inputs.PermanentPassiveBonus;
            bool hasChanceBonus = rawSum > 0f;

            if (alwaysCrit)
            {
                return new CritResult(
                    isCrit: true,
                    resolvedChance: 1f,
                    isAlwaysCrit: true,
                    hasChanceBonus: hasChanceBonus,
                    isRedundant: hasChanceBonus);
            }

            return new CritResult(
                isCrit: false,
                resolvedChance: Mathf.Clamp01(rawSum),
                isAlwaysCrit: false,
                hasChanceBonus: hasChanceBonus,
                isRedundant: false);
        }

        // ── Plumbing helpers (call sites: Task 4.1 combat loop) ──────────────

        // Per §4.1.3 source 3 — pull the permanent crit-chance bonus from the
        // attacker's currently selected evolution branch. Returns 0 for un-evolved
        // or Support-branch Pokémon.
        public static float GatherPassiveBonus(PokemonInstance attacker)
        {
            if (attacker == null || attacker.SelectedBranch == null) return 0f;
            return Mathf.Clamp01(attacker.SelectedBranch.CritChanceBonus);
        }

        // Per §4.1.3 source 2 — sum the per-combat crit-chance boost from all
        // currently-active consumable effects. The combat loop maintains the
        // list of active consumables; this helper extracts the boost.
        public static float GatherConsumableBonus(IReadOnlyList<ConsumableSO> activeConsumables)
        {
            if (activeConsumables == null || activeConsumables.Count == 0) return 0f;

            float sum = 0f;
            for (int i = 0; i < activeConsumables.Count; i++)
            {
                ConsumableSO consumable = activeConsumables[i];
                if (consumable == null) continue;
                if (consumable.Effect is CritBoostConsumableEffectSO boost)
                    sum += boost.CritChanceBoost;
            }
            return sum; // intentionally un-clamped here — Resolve clamps the final sum.
        }
    }
}
