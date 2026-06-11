using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §7.3.1 + CL-018 (Q21) — resolves the weighted biome options for a Wild Area sample.
    //
    // Baseline: the config's per-Region BiomeWeight list (the default primary-biome weighting). With
    // Naturalist's Lens active, the steered biome becomes DOMINANT — its weight is multiplied by the
    // modifier's boost factor, overriding the default primary — while every other eligible biome keeps
    // its weight (dominant, NOT exclusive: secondary biomes still appear, so the §7.3.2 three-species
    // offer never starves and the recruit pool is concentrated rather than locked).
    //
    // The steered biome must be in the Region's eligible set (the guard): an ineligible / null chosen
    // biome falls back to the top non-primary eligible biome (a VS auto-surface — the explicit biome
    // sub-picker is the documented follow-up, mirroring Type Affinity's most-common-type auto-surface).
    public static class WildAreaBiomeWeighting
    {
        // The biome Naturalist's Lens steers toward: the explicit chosen biome if it is eligible (present
        // in regionBiomes with positive weight); else the highest-weighted NON-primary eligible biome
        // (auto-surface); else null (≤1 eligible biome → nothing to steer toward).
        public static BiomeSO ResolveSteerBiome(IReadOnlyList<BiomeWeight> regionBiomes, BiomeSO chosenBiome)
        {
            if (regionBiomes == null || regionBiomes.Count == 0) return null;

            // 1) Explicit, eligible player choice wins.
            if (chosenBiome != null)
                for (int i = 0; i < regionBiomes.Count; i++)
                    if (regionBiomes[i].Biome == chosenBiome && regionBiomes[i].Weight > 0f)
                        return chosenBiome;

            // 2) Auto-surface: identify the primary (highest weight), then steer toward the top runner-up.
            BiomeSO primary = null; float primaryW = float.NegativeInfinity;
            for (int i = 0; i < regionBiomes.Count; i++)
            {
                BiomeWeight bw = regionBiomes[i];
                if (bw.Biome != null && bw.Weight > 0f && bw.Weight > primaryW) { primaryW = bw.Weight; primary = bw.Biome; }
            }

            BiomeSO runnerUp = null; float runnerW = float.NegativeInfinity;
            for (int i = 0; i < regionBiomes.Count; i++)
            {
                BiomeWeight bw = regionBiomes[i];
                if (bw.Biome != null && bw.Weight > 0f && bw.Biome != primary && bw.Weight > runnerW)
                { runnerW = bw.Weight; runnerUp = bw.Biome; }
            }
            return runnerUp; // null when only one eligible biome exists
        }

        // Build the weighted (biome, weight) options for a Wild Area sample. With steerActive, the
        // resolved steer biome's weight is multiplied by boost (dominant); all other eligible biomes
        // keep their configured weight. Biomes with null SO or weight ≤ 0 are dropped (ineligible).
        public static List<(BiomeSO value, float weight)> BuildOptions(
            IReadOnlyList<BiomeWeight> regionBiomes, bool steerActive, BiomeSO chosenBiome, float boost)
        {
            List<(BiomeSO value, float weight)> opts = new();
            if (regionBiomes == null) return opts;

            BiomeSO steer = steerActive ? ResolveSteerBiome(regionBiomes, chosenBiome) : null;
            float b = boost > 0f ? boost : 1f;

            for (int i = 0; i < regionBiomes.Count; i++)
            {
                BiomeWeight bw = regionBiomes[i];
                if (bw.Biome == null || bw.Weight <= 0f) continue;
                float w = bw.Biome == steer ? bw.Weight * b : bw.Weight;
                opts.Add((bw.Biome, w));
            }
            return opts;
        }
    }
}
