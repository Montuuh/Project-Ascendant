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

        // §8.3.3 Berry Pouch — healing consumables restore +X% HP. Returns the boosted heal amount.
        public static int ApplyHealBonus(int baseHeal, IReadOnlyList<RelicSO> relics, BattleConfigSO cfg)
        {
            if (cfg != null && baseHeal > 0 && Holds(relics, "berry_pouch"))
                return Mathf.FloorToInt(baseHeal * cfg.BerryPouchHealMultiplier);
            return baseHeal;
        }
    }
}
