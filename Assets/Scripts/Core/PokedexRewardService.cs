using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.9 — Pokédex completion-% milestone rewards. Filling out the Pokédex is the long-tail
    // meta goal: reaching a seen-% threshold grants tokens / relic-pool unlocks / starter unlocks
    // (authored on MetaProgressionConfigSO.PokedexMilestones), each claimed exactly once across runs.
    // This is the "Completion → Tokens" loop plus broader unlock hooks. Evaluated at run-end.
    //
    // Pure C#; the caller persists Meta at the §6.10 trigger (run-end SaveMeta).
    public static class PokedexRewardService
    {
        // Grants every not-yet-claimed milestone whose PercentThreshold is met by seen/total.
        // Returns the milestones granted this call (for the run-summary readout).
        public static List<PokedexCompletionMilestone> GrantCompletionMilestones(
            MetaProgressionSO meta, MetaProgressionConfigSO cfg, int seenCount, int totalSpecies)
        {
            List<PokedexCompletionMilestone> granted = new();
            if (meta == null || cfg == null || cfg.PokedexMilestones == null || totalSpecies <= 0)
                return granted;

            int pct = Mathf.Clamp(Mathf.FloorToInt(100f * seenCount / totalSpecies), 0, 100);

            meta.ClaimedPokedexMilestones ??= new List<int>();
            meta.UnlockedRelicIds ??= new List<string>();
            meta.UnlockedStarterIds ??= new List<string>();

            for (int i = 0; i < cfg.PokedexMilestones.Count; i++)
            {
                PokedexCompletionMilestone m = cfg.PokedexMilestones[i];
                if (m == null || m.PercentThreshold > pct) continue;
                if (meta.ClaimedPokedexMilestones.Contains(m.PercentThreshold)) continue;

                meta.ClaimedPokedexMilestones.Add(m.PercentThreshold);
                if (m.TrainerTokens > 0) meta.TrainerTokens += m.TrainerTokens;
                AddNew(meta.UnlockedRelicIds, m.RelicIds);
                AddNew(meta.UnlockedStarterIds, m.StarterIds);
                granted.Add(m);
            }
            return granted;
        }

        private static void AddNew(List<string> dest, List<string> src)
        {
            if (src == null) return;
            for (int i = 0; i < src.Count; i++)
            {
                string id = src[i];
                if (!string.IsNullOrEmpty(id) && !dest.Contains(id)) dest.Add(id);
            }
        }
    }
}
