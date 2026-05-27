using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.2.6 + Epic 4 Task 4.6 — central API for the ±6 stat-stage ladder.
    // Storage lives on PokemonInstance.StatStages (Dictionary<Stat,int>);
    // multiplier lookup lives on BattleConfigSO.StatStageMultipliers (13 entries).
    //
    // CombatStatResolver already reads the dictionary directly during damage
    // calculation — this class only owns the WRITE path (modify/reset) and a
    // few read helpers for callers that don't want to touch the dictionary.
    //
    // Boss-phase persistence (§4.4.3.1): stages are NEVER cleared on phase
    // transition. ResetAll is only called by CombatController at combat-end.
    public static class StatStageManager
    {
        public const int MinStage = -6;
        public const int MaxStage = +6;

        // Per §4.2.6 — clamps to [-6, +6]. Returns the post-clamp stage.
        // Stages are stored on the Pokémon between turns and persist across
        // boss phase transitions (§4.4.3.1).
        public static int Modify(PokemonInstance target, Stat stat, int delta)
        {
            if (target == null) return 0;
            target.StatStages.TryGetValue(stat, out int current);
            int next = Mathf.Clamp(current + delta, MinStage, MaxStage);
            target.StatStages[stat] = next;
            return next;
        }

        public static int GetStage(PokemonInstance target, Stat stat)
        {
            if (target == null) return 0;
            target.StatStages.TryGetValue(stat, out int s);
            return s;
        }

        // Per §4.2.6 — convert a stage offset to its multiplier via the
        // 13-entry BattleConfigSO ladder. Returns 1.0 on null config.
        public static float GetMultiplier(int stage, BattleConfigSO config)
        {
            if (config == null || config.StatStageMultipliers == null
                               || config.StatStageMultipliers.Length != 13)
            {
                return 1f;
            }
            int clamped = Mathf.Clamp(stage, MinStage, MaxStage);
            return config.StatStageMultipliers[clamped + 6];
        }

        // Per §4.2.6 — combat-end reset. Called by CombatController on
        // OnCombatEnded. NOT called on boss phase transition (§4.4.3.1).
        public static void ResetAll(PokemonInstance target)
        {
            if (target == null) return;
            target.StatStages.Clear();
        }
    }
}
