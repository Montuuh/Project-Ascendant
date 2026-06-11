using System.Collections.Generic;
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

            // §6.3.4 (CL-019 — Q18): Tokens are NO LONGER earned per-run here. They are granted at the
            // Battle Pass track's milestone levels in GrantLevelUnlocks. r.TokensGained stays 0; the
            // run-end flow reads the milestone Tokens from the granted milestones (RunEndService).
            int newLevel = LevelForXP(meta.TrainerXP, config);
            meta.TrainerLevel = newLevel;
            r.NewLevel = newLevel;

            meta.TrainerXPToNextLevel = newLevel >= config.MaxTrainerLevel
                ? 0
                : Mathf.Max(0, config.CumulativeXPForLevel(newLevel + 1) - meta.TrainerXP);
            return r;
        }

        // §6.3 / §6.5.2 / §6.6.1 — grant the milestone unlocks for every Trainer Level newly reached in
        // (oldLevel, newLevel]. Adds starter/relic ids to the Meta unlock sets (idempotent) and returns
        // the milestones granted this commit (for the run-summary / Hub readout). Pure; caller persists.
        // Crossing multiple levels at once (a big run) grants all spanned milestones.
        public static List<TrainerLevelMilestone> GrantLevelUnlocks(
            MetaProgressionSO meta, MetaProgressionConfigSO config, int oldLevel, int newLevel)
        {
            List<TrainerLevelMilestone> granted = new();
            if (meta == null || config == null || config.LevelMilestones == null) return granted;
            if (newLevel <= oldLevel) return granted;

            meta.UnlockedStarterIds ??= new List<string>();
            meta.UnlockedRelicIds ??= new List<string>();
            meta.ClaimedLevelMilestones ??= new List<int>();

            for (int i = 0; i < config.LevelMilestones.Count; i++)
            {
                TrainerLevelMilestone m = config.LevelMilestones[i];
                if (m == null || m.Level <= oldLevel || m.Level > newLevel) continue;

                bool any = AddNew(meta.UnlockedStarterIds, m.StarterIds);
                any |= AddNew(meta.UnlockedRelicIds, m.RelicIds);

                // §6.3.4/§6.3.5 (CL-019 — Q18) — grant the level's Battle Pass Tokens once (claimed-set
                // idempotency, mirroring ClaimedPokedexMilestones). Token-only milestones still count.
                if (m.TrainerTokens > 0 && !meta.ClaimedLevelMilestones.Contains(m.Level))
                {
                    meta.TrainerTokens += m.TrainerTokens;
                    meta.ClaimedLevelMilestones.Add(m.Level);
                    any = true;
                }

                if (any) granted.Add(m);
            }
            return granted;
        }

        // Adds each non-empty id from src into dest if absent. Returns true iff at least one was added.
        private static bool AddNew(List<string> dest, List<string> src)
        {
            if (src == null) return false;
            bool added = false;
            for (int i = 0; i < src.Count; i++)
            {
                string id = src[i];
                if (string.IsNullOrEmpty(id) || dest.Contains(id)) continue;
                dest.Add(id);
                added = true;
            }
            return added;
        }
    }
}
