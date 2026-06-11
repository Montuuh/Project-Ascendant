using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §7.3.1 + CL-018 (Q21) — Naturalist's Lens biome-steer weighting. The steered biome becomes
    // dominant (weight × boost) while every other eligible biome still appears (dominant, not exclusive),
    // so the §7.3.2 three-species offer never starves. Ineligible/null chosen biome → auto-surface the
    // top non-primary eligible biome. All pure logic — no RNG.
    public class WildAreaBiomeWeightingTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private BiomeSO Biome(string id)
        {
            BiomeSO b = ScriptableObject.CreateInstance<BiomeSO>();
            b.BiomeId = id; b.DisplayName = id; b.name = id;
            _disposables.Add(b);
            return b;
        }

        // R1-style config: Meadow primary (weight 3), Cave + River secondary (weight 1 each).
        private List<BiomeWeight> R1Biomes(out BiomeSO meadow, out BiomeSO cave, out BiomeSO river)
        {
            meadow = Biome("Meadow"); cave = Biome("Cave"); river = Biome("River");
            return new List<BiomeWeight>
            {
                new BiomeWeight { Biome = meadow, Weight = 3f },
                new BiomeWeight { Biome = cave,   Weight = 1f },
                new BiomeWeight { Biome = river,  Weight = 1f },
            };
        }

        private static float WeightOf(List<(BiomeSO value, float weight)> opts, BiomeSO b)
        {
            foreach (var o in opts) if (o.value == b) return o.weight;
            return 0f;
        }

        [Test]
        public void NoSteer_ReturnsConfigWeightsUnchanged()
        {
            List<BiomeWeight> cfg = R1Biomes(out BiomeSO meadow, out BiomeSO cave, out BiomeSO river);
            var opts = WildAreaBiomeWeighting.BuildOptions(cfg, steerActive: false, chosenBiome: null, boost: 5f);

            Assert.That(opts.Count, Is.EqualTo(3));
            Assert.That(WeightOf(opts, meadow), Is.EqualTo(3f), "primary unchanged");
            Assert.That(WeightOf(opts, cave), Is.EqualTo(1f));
            Assert.That(WeightOf(opts, river), Is.EqualTo(1f));
        }

        [Test]
        public void Steer_ChosenEligible_BecomesDominant()
        {
            List<BiomeWeight> cfg = R1Biomes(out BiomeSO meadow, out BiomeSO cave, out BiomeSO river);
            var opts = WildAreaBiomeWeighting.BuildOptions(cfg, steerActive: true, chosenBiome: cave, boost: 5f);

            Assert.That(WeightOf(opts, cave), Is.EqualTo(5f), "chosen biome boosted ×5");
            Assert.That(WeightOf(opts, cave), Is.GreaterThan(WeightOf(opts, meadow)),
                "steered biome now outweighs the default primary (dominant)");
            // Dominant, NOT exclusive — secondaries still present so the 3-species offer never starves.
            Assert.That(WeightOf(opts, meadow), Is.EqualTo(3f));
            Assert.That(WeightOf(opts, river), Is.EqualTo(1f));
        }

        [Test]
        public void Steer_NullChosen_AutoSurfacesTopNonPrimary()
        {
            List<BiomeWeight> cfg = R1Biomes(out BiomeSO meadow, out BiomeSO cave, out BiomeSO river);
            // No explicit choice → auto-surface the top non-primary eligible biome (Cave, first of the 1-weights).
            var opts = WildAreaBiomeWeighting.BuildOptions(cfg, steerActive: true, chosenBiome: null, boost: 5f);

            Assert.That(WeightOf(opts, cave), Is.EqualTo(5f), "auto-surfaced Cave is boosted");
            Assert.That(WeightOf(opts, meadow), Is.EqualTo(3f), "primary not boosted");
            Assert.That(WeightOf(opts, river), Is.EqualTo(1f));
        }

        [Test]
        public void Steer_IneligibleChosen_FallsBackToAutoSurface()
        {
            List<BiomeWeight> cfg = R1Biomes(out BiomeSO meadow, out BiomeSO cave, out _);
            BiomeSO volcano = Biome("Volcano"); // not in the R1 eligible set
            var opts = WildAreaBiomeWeighting.BuildOptions(cfg, steerActive: true, chosenBiome: volcano, boost: 5f);

            // Ineligible chosen biome is ignored (the guard); falls back to top non-primary (Cave).
            Assert.That(opts.Count, Is.EqualTo(3), "ineligible biome is never injected into the eligible set");
            Assert.That(WeightOf(opts, volcano), Is.EqualTo(0f), "ineligible biome absent");
            Assert.That(WeightOf(opts, cave), Is.EqualTo(5f), "fell back to auto-surfaced Cave");
            Assert.That(WeightOf(opts, meadow), Is.EqualTo(3f));
        }

        [Test]
        public void ResolveSteerBiome_PrefersEligibleChoice_ElseRunnerUp_ElseNull()
        {
            List<BiomeWeight> cfg = R1Biomes(out BiomeSO meadow, out BiomeSO cave, out BiomeSO river);

            // Eligible explicit choice wins (even the primary itself).
            Assert.That(WildAreaBiomeWeighting.ResolveSteerBiome(cfg, river), Is.EqualTo(river));
            Assert.That(WildAreaBiomeWeighting.ResolveSteerBiome(cfg, meadow), Is.EqualTo(meadow));

            // Null choice → top non-primary (Cave, first 1-weight).
            Assert.That(WildAreaBiomeWeighting.ResolveSteerBiome(cfg, null), Is.EqualTo(cave));

            // Single-biome region → nothing to steer toward.
            List<BiomeWeight> solo = new() { new BiomeWeight { Biome = meadow, Weight = 3f } };
            Assert.That(WildAreaBiomeWeighting.ResolveSteerBiome(solo, null), Is.Null);
        }

        [Test]
        public void IneligibleBiomes_DroppedFromOptions()
        {
            BiomeSO meadow = Biome("Meadow"); BiomeSO dead = Biome("Dead");
            List<BiomeWeight> cfg = new()
            {
                new BiomeWeight { Biome = meadow, Weight = 3f },
                new BiomeWeight { Biome = dead,   Weight = 0f },   // weight 0 → ineligible
                new BiomeWeight { Biome = null,   Weight = 5f },   // null SO → ineligible
            };
            var opts = WildAreaBiomeWeighting.BuildOptions(cfg, steerActive: true, chosenBiome: null, boost: 5f);
            Assert.That(opts.Count, Is.EqualTo(1), "only the one eligible biome survives");
            Assert.That(WeightOf(opts, meadow), Is.EqualTo(3f), "single eligible biome → no runner-up to boost");
        }
    }
}
