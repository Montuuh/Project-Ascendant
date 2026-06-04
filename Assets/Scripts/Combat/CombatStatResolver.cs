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
    //
    // Per §4.4.5.1 — Normal Badge: +10% to base stats (applied before stat stages).
    public static class CombatStatResolver
    {
        public static int EffectiveAttack(PokemonInstance instance, BattleConfigSO config,
            System.Collections.Generic.IReadOnlyList<BadgeSO> badges = null)
        {
            int baseAtk = BaseAttack(instance);
            // Per §4.4.5.1 — Normal Badge: +10% base stats (for damage dealt AND received).
            baseAtk = ApplyNormalBadge(baseAtk, badges);
            float stageMul = GetStageMultiplier(instance, Stat.Attack, config);
            // Per OPEN G8 — stat-stage first, then status modifier on top
            // (multiplicative). Burn applies -25% per §4.2.2.1.
            float statusMul = StatusModifiers.GetAttackMultiplier(
                instance?.PrimaryStatus ?? StatusCondition.None, config);
            return Mathf.Max(1, Mathf.FloorToInt(baseAtk * stageMul * statusMul));
        }

        public static int EffectiveDefense(PokemonInstance instance, BattleConfigSO config,
            System.Collections.Generic.IReadOnlyList<BadgeSO> badges = null)
        {
            int baseDef = BaseDefense(instance);
            // Per §4.4.5.1 — Normal Badge: +10% base stats (for damage dealt AND received).
            baseDef = ApplyNormalBadge(baseDef, badges);
            float stageMul = GetStageMultiplier(instance, Stat.Defense, config);
            // Per OPEN G8 — stat-stage first, then status modifier on top.
            // Poison applies -15% per §4.2.2.2.
            float statusMul = StatusModifiers.GetDefenseMultiplier(
                instance?.PrimaryStatus ?? StatusCondition.None, config);
            return Mathf.Max(1, Mathf.FloorToInt(baseDef * stageMul * statusMul));
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

        // Per §4.4.5.1 — Normal Badge: +10% to base stats (for damage dealt AND received).
        // Applied before stat stages and status modifiers.
        private static int ApplyNormalBadge(int baseStat, System.Collections.Generic.IReadOnlyList<BadgeSO> badges)
        {
            if (badges == null) return baseStat;
            for (int i = 0; i < badges.Count; i++)
                if (badges[i] != null && badges[i].BadgeId == "normal_badge")
                    return Mathf.FloorToInt(baseStat * 1.1f);
            return baseStat;
        }
    }
}
