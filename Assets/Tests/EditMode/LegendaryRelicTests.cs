using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §8.3.7 (CL-021 — Q10) — the 10-relic Legendary pool + the choice-only pick logic (1-of-N,
    // exclude held, max 2/run).
    public class LegendaryRelicTests
    {
        private readonly List<Object> _disp = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disp) if (o != null) Object.DestroyImmediate(o);
            _disp.Clear();
        }

        private List<RelicSO> Pool()
        {
            List<RelicSO> all = LegendaryRelicCatalog.BuildAll();
            foreach (RelicSO r in all) _disp.Add(r);
            return all;
        }

        private RelicSO Relic(string id, RarityTier rarity)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>(); r.Id = id; r.Rarity = rarity; _disp.Add(r); return r;
        }

        // ── Catalog ──

        [Test]
        public void Catalog_HasTenDistinctLegendaries_AllCategories()
        {
            List<RelicSO> pool = Pool();
            Assert.That(pool.Count, Is.EqualTo(10), "§8.3.7 — 10 Legendaries.");

            HashSet<string> ids = new();
            HashSet<SynergyCategory> cats = new();
            foreach (RelicSO r in pool)
            {
                Assert.That(ids.Add(r.Id), Is.True, $"duplicate id {r.Id}");
                Assert.That(r.Rarity, Is.EqualTo(RarityTier.Legendary), $"{r.Id} is Legendary");
                Assert.That(r.MetaTier, Is.EqualTo(1), $"{r.Id} available run 1");
                Assert.That(string.IsNullOrEmpty(r.EffectDescription), Is.False, $"{r.Id} has a description");
                if (r.Categories != null) foreach (SynergyCategory c in r.Categories) cats.Add(c);
            }
            Assert.That(cats, Is.EquivalentTo(new[]
            {
                SynergyCategory.Combat, SynergyCategory.LeadEconomy, SynergyCategory.CardEconomy,
                SynergyCategory.MetaAcquisition, SynergyCategory.Status
            }), "spread across all 5 synergy categories.");
        }

        // ── Pick service ──

        [Test]
        public void HeldCount_CountsOnlyLegendaries()
        {
            List<RelicSO> held = new()
            {
                Relic("c1", RarityTier.Common), Relic("leg1", RarityTier.Legendary), Relic("u1", RarityTier.Uncommon),
            };
            Assert.That(LegendaryPickService.HeldCount(held), Is.EqualTo(1));
            Assert.That(LegendaryPickService.HeldCount(null), Is.EqualTo(0));
        }

        [Test]
        public void CanPick_FalseAtCap()
        {
            Assert.That(LegendaryPickService.CanPick(new List<RelicSO>()), Is.True, "0 held → can pick.");
            List<RelicSO> oneHeld = new() { Relic("l1", RarityTier.Legendary) };
            Assert.That(LegendaryPickService.CanPick(oneHeld), Is.True, "1 held → can still pick.");
            List<RelicSO> capped = new() { Relic("l1", RarityTier.Legendary), Relic("l2", RarityTier.Legendary) };
            Assert.That(LegendaryPickService.CanPick(capped), Is.False, "2 held → at the cap.");
        }

        [Test]
        public void BuildOffer_ReturnsThreeDistinctLegendaries_NoneHeld()
        {
            List<RelicSO> offer = LegendaryPickService.BuildOffer(Pool(), new List<RelicSO>(), new GameRNG(42u), 3);
            Assert.That(offer.Count, Is.EqualTo(3));
            HashSet<RelicSO> seen = new();
            foreach (RelicSO r in offer)
            {
                Assert.That(r.Rarity, Is.EqualTo(RarityTier.Legendary));
                Assert.That(seen.Add(r), Is.True, "distinct in an offer.");
            }
        }

        [Test]
        public void BuildOffer_ExcludesAlreadyHeld()
        {
            List<RelicSO> pool = Pool();
            List<RelicSO> held = new() { pool[0] }; // already hold the first Legendary
            List<RelicSO> offer = LegendaryPickService.BuildOffer(pool, held, new GameRNG(7u), 3);
            Assert.That(offer.Contains(pool[0]), Is.False, "a held Legendary is never re-offered.");
        }

        [Test]
        public void BuildOffer_EmptyAtCap()
        {
            List<RelicSO> pool = Pool();
            List<RelicSO> capped = new() { pool[0], pool[1] }; // 2 Legendaries held → at cap
            List<RelicSO> offer = LegendaryPickService.BuildOffer(pool, capped, new GameRNG(1u), 3);
            Assert.That(offer, Is.Empty, "no Legendary offered once the 2-cap is reached.");
        }

        [Test]
        public void BuildOffer_Deterministic_ForSameSeed()
        {
            List<RelicSO> pool = Pool();
            List<RelicSO> a = LegendaryPickService.BuildOffer(pool, new List<RelicSO>(), new GameRNG(99u), 3);
            List<RelicSO> b = LegendaryPickService.BuildOffer(pool, new List<RelicSO>(), new GameRNG(99u), 3);
            Assert.That(a.Count, Is.EqualTo(b.Count));
            for (int i = 0; i < a.Count; i++) Assert.That(a[i].Id, Is.EqualTo(b[i].Id));
        }
    }
}
