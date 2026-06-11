using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.Tests
{
    // Per §6.6.3 + Epic 12 Task 12.11 — Starting Relic offer: N distinct, never Rare, deterministic.
    public class StartingRelicServiceTests
    {
        private readonly List<Object> _disp = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disp) if (o != null) Object.DestroyImmediate(o);
            _disp.Clear();
        }

        private RelicSO Relic(string id, RarityTier rarity)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>(); r.Id = id; r.Rarity = rarity; _disp.Add(r); return r;
        }

        private List<RelicSO> Pool()
        {
            return new List<RelicSO>
            {
                Relic("c1", RarityTier.Common), Relic("c2", RarityTier.Common), Relic("c3", RarityTier.Common),
                Relic("u1", RarityTier.Uncommon), Relic("u2", RarityTier.Uncommon),
                Relic("r1", RarityTier.Rare),            // must never be offered (§6.6.3)
                Relic("l1", RarityTier.Legendary),       // must never be offered (CL-021 — choice-only)
            };
        }

        [Test]
        public void Offer_ReturnsThreeDistinct_NeverRareOrLegendary()
        {
            List<RelicSO> offer = StartingRelicService.Offer(Pool(), new GameRNG(123u), 3);
            Assert.That(offer, Has.Count.EqualTo(3));
            HashSet<RelicSO> seen = new();
            foreach (RelicSO r in offer)
            {
                Assert.That(r.Rarity, Is.Not.EqualTo(RarityTier.Rare), "§6.6.3 — never Rare.");
                Assert.That(r.Rarity, Is.Not.EqualTo(RarityTier.Legendary), "CL-021 — never Legendary (choice-only).");
                Assert.That(seen.Add(r), Is.True, "distinct.");
            }
        }

        [Test]
        public void Offer_Deterministic_ForSameSeed()
        {
            List<RelicSO> a = StartingRelicService.Offer(Pool(), new GameRNG(7u), 3);
            List<RelicSO> b = StartingRelicService.Offer(Pool(), new GameRNG(7u), 3);
            // Same seed + same candidate ordering → same Ids.
            for (int i = 0; i < a.Count; i++) Assert.That(a[i].Id, Is.EqualTo(b[i].Id));
        }

        [Test]
        public void Offer_PoolSmallerThanCount_ReturnsAllNonRare()
        {
            List<RelicSO> small = new() { Relic("c1", RarityTier.Common), Relic("r1", RarityTier.Rare) };
            List<RelicSO> offer = StartingRelicService.Offer(small, new GameRNG(1u), 3);
            Assert.That(offer, Has.Count.EqualTo(1), "only the 1 non-Rare available.");
        }
    }
}
