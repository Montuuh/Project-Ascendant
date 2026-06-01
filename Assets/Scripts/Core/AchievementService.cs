using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.7 + Epic 11 Task 11.5 — achievement detection + award. The wiring layer calls Report when
    // a trigger occurs; this advances every matching incomplete achievement, completes the ones that
    // reach their target, and grants their Trainer-XP reward (§6.3.2 source) into the MetaProgressionSO
    // (immediate, recomputing Level). Pure C# (no disk) — the caller persists at the §6.10 save trigger.
    public static class AchievementService
    {
        public static bool IsCompleted(MetaProgressionSO meta, string id)
        {
            return meta?.CompletedAchievementIds != null && meta.CompletedAchievementIds.Contains(id);
        }

        public static int GetProgress(MetaProgressionSO meta, string id)
        {
            if (meta?.AchievementProgress == null) return 0;
            for (int i = 0; i < meta.AchievementProgress.Count; i++)
                if (meta.AchievementProgress[i].Key == id) return meta.AchievementProgress[i].Value;
            return 0;
        }

        // §6.7 — report `amount` occurrences of a trigger. Returns achievements newly completed by this
        // call (for the unlock notification). Idempotent for already-completed achievements.
        public static List<AchievementSO> Report(AchievementTrigger trigger, int amount,
            IReadOnlyList<AchievementSO> registry, MetaProgressionSO meta, MetaProgressionConfigSO cfg)
        {
            List<AchievementSO> completed = new();
            if (trigger == AchievementTrigger.None || amount <= 0 || registry == null || meta == null)
                return completed;

            for (int i = 0; i < registry.Count; i++)
            {
                AchievementSO a = registry[i];
                if (a == null || a.Trigger != trigger) continue;
                if (IsCompleted(meta, a.AchievementId)) continue;

                int target = a.TargetCount < 1 ? 1 : a.TargetCount;
                int progress = GetProgress(meta, a.AchievementId) + amount;

                if (progress >= target)
                {
                    ClearProgress(meta, a.AchievementId);
                    (meta.CompletedAchievementIds ??= new List<string>()).Add(a.AchievementId);
                    AwardXP(meta, a.TrainerXPReward, cfg);
                    completed.Add(a);
                }
                else
                {
                    SetProgress(meta, a.AchievementId, progress);
                }
            }
            return completed;
        }

        // §6.3.2 — achievement XP banks directly into Meta (immediate), recomputing Trainer Level.
        private static void AwardXP(MetaProgressionSO meta, int reward, MetaProgressionConfigSO cfg)
        {
            if (reward <= 0) return;
            meta.TrainerXP += reward;
            if (cfg == null) return;
            meta.TrainerLevel = TrainerProgression.LevelForXP(meta.TrainerXP, cfg);
            meta.TrainerXPToNextLevel = meta.TrainerLevel >= cfg.MaxTrainerLevel
                ? 0 : Mathf.Max(0, cfg.CumulativeXPForLevel(meta.TrainerLevel + 1) - meta.TrainerXP);
        }

        private static void SetProgress(MetaProgressionSO meta, string id, int value)
        {
            meta.AchievementProgress ??= new List<StringIntPair>();
            for (int i = 0; i < meta.AchievementProgress.Count; i++)
                if (meta.AchievementProgress[i].Key == id)
                {
                    meta.AchievementProgress[i] = new StringIntPair { Key = id, Value = value };
                    return;
                }
            meta.AchievementProgress.Add(new StringIntPair { Key = id, Value = value });
        }

        private static void ClearProgress(MetaProgressionSO meta, string id)
        {
            if (meta.AchievementProgress == null) return;
            for (int i = meta.AchievementProgress.Count - 1; i >= 0; i--)
                if (meta.AchievementProgress[i].Key == id) meta.AchievementProgress.RemoveAt(i);
        }
    }
}
