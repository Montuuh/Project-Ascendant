using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §6.6.3 + Epic 12 Task 12.11 — the pre-run Starting Relic offer: N distinct relics drawn from
    // the Common-and-Uncommon pool, NEVER Rare (Starting Relics set a build direction, not the build).
    // Seeded → deterministic. Pure C#.
    public static class StartingRelicService
    {
        public static List<RelicSO> Offer(IReadOnlyList<RelicSO> pool, GameRNG rng, int count)
        {
            List<RelicSO> candidates = new();
            if (pool != null)
                foreach (RelicSO r in pool)
                    if (r != null && r.Rarity != RarityTier.Rare) candidates.Add(r); // §6.6.3 — never Rare

            List<RelicSO> offer = new();
            while (offer.Count < count && candidates.Count > 0)
            {
                int i = rng != null ? rng.Range(0, candidates.Count) : 0;
                offer.Add(candidates[i]);
                candidates.RemoveAt(i); // distinct
            }
            return offer;
        }
    }
}
