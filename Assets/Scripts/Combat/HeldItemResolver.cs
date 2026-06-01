using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §8.4 + Epic 12 Task 12.5/12.6 — runtime for the wearer's equipped Held Item. Like
    // RelicResolver, dispatch is data-driven off the HeldItemSO fields (the §8.7 ScriptableHook
    // event-bus is post-VS, gap #42). Type-boost (§8.4.2) and Leftovers regen (§8.4.4) for the VS 5.
    public static class HeldItemResolver
    {
        // §8.4.2 — the wearer's matching-type moves deal ×WearerDamageMultiplier (e.g. Charcoal Fire +20%).
        public static float OutgoingDamageMultiplier(PokemonInstance attacker, MoveSO move)
        {
            if (attacker?.HeldItem == null || move == null) return 1f;
            HeldItemSO item = attacker.HeldItem;
            if (item.WearerDamageMultiplier > 1f && item.BoostsType == move.Type)
                return item.WearerDamageMultiplier;
            return 1f;
        }

        // §8.4.4 Leftovers — HP restored at end of Resolution: floor(EffectiveMaxHP / Divisor), min 1.
        // 0 if the wearer has no regen item, is fainted, or already at full.
        public static int LeftoversRegen(PokemonInstance p, EconomyConfigSO economy)
        {
            if (p?.HeldItem == null || p.HeldItem.LeftoversRegenDivisor <= 0) return 0;
            if (p.CurrentHP <= 0) return 0; // §2.4.1 — no regen for the fainted
            int max = economy != null ? PokemonVitals.EffectiveMaxHP(p, economy) : PokemonVitals.MaxHP(p);
            if (p.CurrentHP >= max) return 0;
            int regen = max / p.HeldItem.LeftoversRegenDivisor;
            return Mathf.Max(1, regen);
        }
    }
}
