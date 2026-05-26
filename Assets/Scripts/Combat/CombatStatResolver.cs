using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.1.1 + §4.2.6 + Epic 4 Task 4.6.3 — resolves effective combat stats by
    // combining (base species stat + growth-curve at level) with the stat-stage
    // multiplier ladder authored in BattleConfigSO.StatStageMultipliers.
    //
    // Stat-stage range is clamped to [-6, +6] per §4.2.6. The 13-entry lookup
    // table in BattleConfigSO is indexed (stage + 6).
    //
    // Effective stats are floored to at least 1 to avoid divide-by-zero in the
    // damage formula's (Atk/Def) ratio. Per OPEN G4, the formula itself has no
    // minimum-damage clamp — only this stat floor at the input layer.
    public static class CombatStatResolver
    {
        public static int EffectiveAttack(PokemonInstance instance, BattleConfigSO config)
        {
            int baseAtk = BaseAttack(instance);
            float multiplier = GetStageMultiplier(instance, Stat.Attack, config);
            return Mathf.Max(1, Mathf.FloorToInt(baseAtk * multiplier));
        }

        public static int EffectiveDefense(PokemonInstance instance, BattleConfigSO config)
        {
            int baseDef = BaseDefense(instance);
            float multiplier = GetStageMultiplier(instance, Stat.Defense, config);
            return Mathf.Max(1, Mathf.FloorToInt(baseDef * multiplier));
        }

        // Base = species BaseStats + per-level growth curve accumulation (§5.2.3).
        private static int BaseAttack(PokemonInstance instance)
        {
            if (instance?.Species == null) return 1;
            int growth = instance.Species.GrowthCurve != null
                ? instance.Species.GrowthCurve.GetAttackAt(instance.Level)
                : 0;
            return instance.Species.BaseStats.BaseAtk + growth;
        }

        private static int BaseDefense(PokemonInstance instance)
        {
            if (instance?.Species == null) return 1;
            int growth = instance.Species.GrowthCurve != null
                ? instance.Species.GrowthCurve.GetDefenseAt(instance.Level)
                : 0;
            return instance.Species.BaseStats.BaseDef + growth;
        }

        private static float GetStageMultiplier(PokemonInstance instance, Stat stat, BattleConfigSO config)
        {
            int stage = 0;
            if (instance != null && instance.StatStages != null)
                instance.StatStages.TryGetValue(stat, out stage);

            int clamped = Mathf.Clamp(stage, -6, 6);
            // BattleConfigSO.StatStageMultipliers is the 13-entry ladder authored
            // per §4.2.6. Index 0 = stage -6, index 6 = stage 0 (1.0×), index 12 = +6.
            return config.StatStageMultipliers[clamped + 6];
        }
    }
}
