using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §6.8.3 + Epic 11 Task 11.6 — pure aggregation of the active run difficulty modifiers.
    // XP multipliers stack MULTIPLICATIVELY (§6.8.3: ×1.15 and ×1.20 → ×1.38); mechanical effects
    // combine (enemy-stat multiplicative; hide-intents OR; route-branches min). Null/empty = baseline.
    public static class DifficultyModifiers
    {
        // §6.8.3 — product of every active modifier's Trainer-XP multiplier. Baseline 1.0.
        public static float XPMultiplier(IReadOnlyList<DifficultyModifierSO> active)
        {
            float mult = 1f;
            if (active != null)
                for (int i = 0; i < active.Count; i++)
                    if (active[i] != null && active[i].TrainerXPMultiplier > 0f)
                        mult *= active[i].TrainerXPMultiplier;
            return mult;
        }

        // §6.3.4 application helper — scale a run's accrued Trainer XP by the active multiplier, floored.
        public static int ApplyXP(int runXp, IReadOnlyList<DifficultyModifierSO> active)
        {
            if (runXp <= 0) return 0;
            return UnityEngine.Mathf.FloorToInt(runXp * XPMultiplier(active));
        }

        // §6.8.2 Iron Will — product of enemy Max-HP multipliers. Baseline 1.0.
        public static float EnemyStatMultiplier(IReadOnlyList<DifficultyModifierSO> active)
        {
            float mult = 1f;
            if (active != null)
                for (int i = 0; i < active.Count; i++)
                    if (active[i] != null && active[i].EnemyStatMultiplier > 0f)
                        mult *= active[i].EnemyStatMultiplier;
            return mult;
        }

        // §6.8.2 Dense Fog — true if any active modifier hides enemy intents.
        public static bool HidesIntents(IReadOnlyList<DifficultyModifierSO> active)
        {
            if (active != null)
                for (int i = 0; i < active.Count; i++)
                    if (active[i] != null && active[i].HideAllEnemyIntents) return true;
            return false;
        }

        // §6.8.2 One Path — tightest route-branch cap across active modifiers (min). Baseline 3.
        public static int MaxRouteBranches(IReadOnlyList<DifficultyModifierSO> active)
        {
            int cap = 3;
            if (active != null)
                for (int i = 0; i < active.Count; i++)
                    if (active[i] != null && active[i].MaxRouteBranchChoices < cap)
                        cap = active[i].MaxRouteBranchChoices;
            return cap < 1 ? 1 : cap;
        }
    }
}
