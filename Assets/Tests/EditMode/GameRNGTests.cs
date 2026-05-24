using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.7 + Task 2.5.4 — GameRNG and RNGStreams unit tests.
    public class GameRNGTests
    {
        // ── Determinism ───────────────────────────────────────────────────────────

        [Test]
        public void GameRNG_SameSeed_ProducesSameSequence()
        {
            // Per §9.7.3 — identical seed must yield identical output sequence.
            GameRNG a = new(42u);
            GameRNG b = new(42u);
            for (int i = 0; i < 100; i++)
                Assert.That(a.NextUInt(), Is.EqualTo(b.NextUInt()), $"Diverged at step {i}");
        }

        [Test]
        public void GameRNG_DifferentSeeds_ProduceDifferentFirstValues()
        {
            // Per §9.7.3 — different seeds must diverge immediately.
            GameRNG a = new(1u);
            GameRNG b = new(2u);
            Assert.That(a.NextUInt(), Is.Not.EqualTo(b.NextUInt()));
        }

        [Test]
        public void GameRNG_ZeroSeed_ProducesNonZeroOutput()
        {
            // Per §9.7.1 — seed 0 is clamped to 1; xorshift32 must not lock to 0.
            GameRNG rng = new(0u);
            Assert.That(rng.NextUInt(), Is.Not.EqualTo(0u));
        }

        // ── Range ─────────────────────────────────────────────────────────────────

        [Test]
        public void GameRNG_Range_StaysWithinBounds()
        {
            // Per §9.7.1 — Range(min, maxExclusive) must never exceed [min, maxExclusive).
            GameRNG rng = new(12345u);
            for (int i = 0; i < 1000; i++)
            {
                int v = rng.Range(3, 10);
                Assert.That(v, Is.GreaterThanOrEqualTo(3));
                Assert.That(v, Is.LessThan(10));
            }
        }

        [Test]
        public void GameRNG_Range_EqualMinMax_ReturnsMin()
        {
            // Edge case: maxExclusive <= min → return min.
            GameRNG rng = new(99u);
            Assert.That(rng.Range(5, 5), Is.EqualTo(5));
            Assert.That(rng.Range(5, 3), Is.EqualTo(5));
        }

        [Test]
        public void GameRNG_Range01_StaysWithin0To1()
        {
            // Per §9.7.1 — Range01() must return values in [0, 1].
            GameRNG rng = new(777u);
            for (int i = 0; i < 1000; i++)
            {
                float v = rng.Range01();
                Assert.That(v, Is.GreaterThanOrEqualTo(0f));
                Assert.That(v, Is.LessThanOrEqualTo(1f));
            }
        }

        // ── PickWeighted ──────────────────────────────────────────────────────────

        [Test]
        public void GameRNG_PickWeighted_AlwaysReturnsValidOption()
        {
            // Per §9.7.1 — every pick must be one of the provided options.
            GameRNG rng = new(555u);
            var options = new List<(string value, float weight)>
            {
                ("a", 1f), ("b", 3f), ("c", 6f)
            };
            for (int i = 0; i < 200; i++)
            {
                string pick = rng.PickWeighted<string>(options);
                Assert.That(pick, Is.EqualTo("a").Or.EqualTo("b").Or.EqualTo("c"));
            }
        }

        [Test]
        public void GameRNG_PickWeighted_DistributionSanity()
        {
            // Per Task 2.5.4 — heavily-weighted option must win the vast majority of picks.
            // weights: [1f, 9f] → "rare" ~10%, "common" ~90%.
            GameRNG rng = new(314159u);
            var options = new List<(string value, float weight)>
            {
                ("rare", 1f), ("common", 9f)
            };
            int commonCount = 0;
            const int trials = 1000;
            for (int i = 0; i < trials; i++)
                if (rng.PickWeighted<string>(options) == "common")
                    commonCount++;

            // Expect > 80% "common" (well below the 90% theoretical; tolerates xorshift bias).
            Assert.That(commonCount, Is.GreaterThan(800));
        }

        [Test]
        public void GameRNG_PickWeighted_SingleOption_AlwaysReturnsThat()
        {
            // Edge case: single-element list must always return that element.
            GameRNG rng = new(1u);
            var options = new List<(int value, float weight)> { (42, 1f) };
            for (int i = 0; i < 10; i++)
                Assert.That(rng.PickWeighted(options), Is.EqualTo(42));
        }

        // ── RNGStreams ────────────────────────────────────────────────────────────

        [Test]
        public void RNGStreams_SameSeed_ProducesSameFirstValues()
        {
            // Per §9.7.3 — same RunSeed → same stream output on identical call sequences.
            RNGStreams a = new(1000u);
            RNGStreams b = new(1000u);
            Assert.That(a.MapRNG.NextUInt(),  Is.EqualTo(b.MapRNG.NextUInt()));
            Assert.That(a.CombatRNG.NextUInt(), Is.EqualTo(b.CombatRNG.NextUInt()));
            Assert.That(a.LootRNG.NextUInt(),   Is.EqualTo(b.LootRNG.NextUInt()));
        }

        [Test]
        public void RNGStreams_StreamsAreDistinct()
        {
            // Per §9.7.2 — streams must start at different states (FNV1a guarantees distinct seeds).
            RNGStreams streams = new(9999u);
            uint mapFirst      = streams.MapRNG.NextUInt();
            uint combatFirst   = streams.CombatRNG.NextUInt();
            uint lootFirst     = streams.LootRNG.NextUInt();
            uint mysteryFirst  = streams.MysteryRNG.NextUInt();
            uint encounterFirst = streams.EncounterRNG.NextUInt();

            // All five first values must be distinct.
            var all = new[] { mapFirst, combatFirst, lootFirst, mysteryFirst, encounterFirst };
            for (int i = 0; i < all.Length; i++)
                for (int j = i + 1; j < all.Length; j++)
                    Assert.That(all[i], Is.Not.EqualTo(all[j]),
                        $"Streams {i} and {j} share first value {all[i]}");
        }

        [Test]
        public void RNGStreams_CrossStreamIndependence()
        {
            // Per §9.7.2 — advancing one stream must not affect another stream's sequence.
            const uint seed = 2718u;
            RNGStreams baseline = new(seed);
            uint baselineCombat = baseline.CombatRNG.NextUInt();

            RNGStreams withAdvancedMap = new(seed);
            // Advance MapRNG many times.
            for (int i = 0; i < 100; i++)
                withAdvancedMap.MapRNG.NextUInt();
            uint isolatedCombat = withAdvancedMap.CombatRNG.NextUInt();

            Assert.That(isolatedCombat, Is.EqualTo(baselineCombat));
        }

        [Test]
        public void RNGStreams_FNV1a_DifferentNames_ProduceDifferentHashes()
        {
            // Per §9.7.2 — each stream name must hash to a distinct seed modifier.
            string[] names = { "MapRNG", "CombatRNG", "LootRNG", "MysteryRNG", "EncounterRNG" };
            var hashes = new System.Collections.Generic.HashSet<uint>();
            foreach (string name in names)
                Assert.That(hashes.Add(RNGStreams.FNV1a(name)), Is.True,
                    $"FNV1a collision for '{name}'");
        }
    }
}
