using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §8.3.7 (CL-021 — Q10) — the choice-only Legendary acquisition logic. Legendaries are never
    // random drops: a 1-of-N pick is offered at each Gym victory, the Victory Road Summit, and the Black
    // Market. The run may hold at most 2 Legendaries (the apex-sculpt cap); at the cap a pick-moment
    // offers a Rare relic / skip instead (caller's responsibility). Pure C#; seeded → deterministic.
    public static class LegendaryPickService
    {
        // §8.3.7 — max Legendaries held per run (the Pillar-3 apex cap). Tunable placeholder.
        public const int MaxLegendariesPerRun = 2;

        // How many Legendary relics the run currently holds.
        public static int HeldCount(IReadOnlyList<RelicSO> held)
        {
            int n = 0;
            if (held != null)
                for (int i = 0; i < held.Count; i++)
                    if (held[i] != null && held[i].Rarity == RarityTier.Legendary) n++;
            return n;
        }

        // §8.3.7 — may the run still take a Legendary pick? False once the 2-cap is reached.
        public static bool CanPick(IReadOnlyList<RelicSO> held) => HeldCount(held) < MaxLegendariesPerRun;

        // §8.3.7 — a seeded "1 of N" offer from the Legendary pool, excluding already-held Legendaries
        // (and any non-Legendary entries, defensively). Returns up to `count` distinct via a partial
        // Fisher–Yates shuffle. Empty when the cap is reached or no candidates remain.
        public static List<RelicSO> BuildOffer(
            IReadOnlyList<RelicSO> pool, IReadOnlyList<RelicSO> held, GameRNG rng, int count)
        {
            List<RelicSO> offer = new();
            if (!CanPick(held) || pool == null || count <= 0) return offer;

            List<RelicSO> candidates = new();
            for (int i = 0; i < pool.Count; i++)
            {
                RelicSO r = pool[i];
                if (r == null || r.Rarity != RarityTier.Legendary) continue;
                if (IsHeld(held, r)) continue;
                candidates.Add(r);
            }

            int n = count < candidates.Count ? count : candidates.Count;
            for (int i = 0; i < n; i++)
            {
                int j = rng != null ? rng.Range(i, candidates.Count) : i;
                (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                offer.Add(candidates[i]);
            }
            return offer;
        }

        private static bool IsHeld(IReadOnlyList<RelicSO> held, RelicSO r)
        {
            if (held == null) return false;
            for (int i = 0; i < held.Count; i++) if (held[i] == r) return true;
            return false;
        }
    }
}
