using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.3 + Epic 11 Task 11.3 — the persistent Trainer-XP / Level / Token logic. A run accrues
    // Trainer XP into RunStateSO.TrainerXPEarnedThisRun (§6.3.2 award table); at run-end the total is
    // COMMITTED here into the MetaProgressionSO (§6.3.4 two-track: XP drives Level automatically, Tokens
    // are banked for manual Hub spend). Pure C#; persistence is the caller's job (§6.10 save trigger =
    // run-end / Pokémart purchase).
    public static class TrainerProgression
    {
        public struct CommitResult
        {
            public int XpGained;
            public int TokensGained;
            public int OldLevel;
            public int NewLevel;
            public bool LeveledUp => NewLevel > OldLevel;
        }

        // §6.3.3 — the highest Trainer Level whose cumulative-XP threshold is met by totalXP, clamped to
        // the prestige cap. Monotonic: CumulativeXPForLevel is non-decreasing in level.
        public static int LevelForXP(int totalXP, MetaProgressionConfigSO config)
        {
            if (config == null) return 1;
            int level = 1;
            while (level < config.MaxTrainerLevel && config.CumulativeXPForLevel(level + 1) <= totalXP)
                level++;
            return level;
        }

        // §6.3.4 — commit a run's accrued Trainer XP: bank XP, recompute Level, award Tokens (capped per
        // run), refresh XP-to-next. Returns what changed (for the run-summary UI). Does NOT persist —
        // the caller saves at the §6.10 trigger.
        public static CommitResult CommitRun(MetaProgressionSO meta, int runXpEarned, MetaProgressionConfigSO config)
        {
            CommitResult r = default;
            if (meta == null || config == null) return r;
            int gained = runXpEarned < 0 ? 0 : runXpEarned;

            r.OldLevel = meta.TrainerLevel < 1 ? 1 : meta.TrainerLevel;
            r.XpGained = gained;
            meta.TrainerXP += gained;

            int tokens = config.TokensForRun(gained);
            meta.TrainerTokens += tokens;
            r.TokensGained = tokens;

            int newLevel = LevelForXP(meta.TrainerXP, config);
            meta.TrainerLevel = newLevel;
            r.NewLevel = newLevel;

            meta.TrainerXPToNextLevel = newLevel >= config.MaxTrainerLevel
                ? 0
                : Mathf.Max(0, config.CumulativeXPForLevel(newLevel + 1) - meta.TrainerXP);
            return r;
        }
    }
}
