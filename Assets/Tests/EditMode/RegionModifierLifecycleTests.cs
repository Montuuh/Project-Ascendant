using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §7.8.3.1 (CL-016) — per-Region single-active lifecycle, the seeded 3-of-16 offer, and the
    // save round-trip (ActiveRegionModifier resolves by ID via the registry).
    public class RegionModifierLifecycleTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private List<RegionModifierSO> Pool()
        {
            List<RegionModifierSO> all = RegionModifierPool.BuildAll();
            foreach (RegionModifierSO m in all) _disposables.Add(m);
            return all;
        }

        private RunStateSO NewRun()
        {
            RunStateSO r = ScriptableObject.CreateInstance<RunStateSO>();
            _disposables.Add(r);
            return r;
        }

        [Test]
        public void SetRegionModifier_IsSingleActive_ReplacesPrevious()
        {
            List<RegionModifierSO> pool = Pool();
            RunStateSO run = NewRun();

            run.SetRegionModifier(pool[0]);
            Assert.That(run.ActiveRegionModifier, Is.SameAs(pool[0]));
            Assert.That(run.ActiveRegionModifiers.Count, Is.EqualTo(1));

            // Re-pick (next Region) replaces — never accumulates.
            run.SetRegionModifier(pool[5]);
            Assert.That(run.ActiveRegionModifier, Is.SameAs(pool[5]));
            Assert.That(run.ActiveRegionModifiers.Count, Is.EqualTo(1), "per-Region: single-active, non-accumulating");
        }

        [Test]
        public void SetRegionModifier_Null_Clears()
        {
            RunStateSO run = NewRun();
            run.SetRegionModifier(Pool()[0]);
            run.SetRegionModifier(null);
            Assert.That(run.ActiveRegionModifier, Is.Null);
            Assert.That(run.ActiveRegionModifiers.Count, Is.EqualTo(0));
        }

        [Test]
        public void ActiveRegionModifier_NullWhenNonePicked()
            => Assert.That(NewRun().ActiveRegionModifier, Is.Null);

        [Test]
        public void BuildOffer_ReturnsRequestedDistinctCount()
        {
            List<RegionModifierSO> pool = Pool();
            List<RegionModifierSO> offer = RegionModifierPool.BuildOffer(pool, new GameRNG(123u), 3);
            Assert.That(offer.Count, Is.EqualTo(3));
            Assert.That(new HashSet<RegionModifierSO>(offer).Count, Is.EqualTo(3), "no duplicates in an offer");
        }

        [Test]
        public void BuildOffer_IsDeterministicForSameSeed()
        {
            List<RegionModifierSO> pool = Pool();
            List<RegionModifierSO> a = RegionModifierPool.BuildOffer(pool, new GameRNG(77u), 3);
            List<RegionModifierSO> b = RegionModifierPool.BuildOffer(pool, new GameRNG(77u), 3);
            CollectionAssert.AreEqual(a, b, "same seed → same offer (Engineering Pillar 3)");
        }

        [Test]
        public void BuildOffer_ClampsToPoolSize()
        {
            List<RegionModifierSO> pool = Pool();
            List<RegionModifierSO> offer = RegionModifierPool.BuildOffer(pool, new GameRNG(1u), 999);
            Assert.That(offer.Count, Is.EqualTo(pool.Count));
        }

        [Test]
        public void SaveRoundTrip_ResolvesActiveModifierById()
        {
            List<RegionModifierSO> pool = Pool();
            RegionModifierSO chosen = pool.Find(m => m.ModifierId == "glass_cannon");

            RunStateSO run = NewRun();
            run.RunSeed = 42;
            run.SetRegionModifier(chosen);

            RunStateDTO dto = RunStateDTO.Capture(run);

            RunContentRegistry registry = new();
            registry.RegisterRegionModifiers(pool);
            RunStateSO restored = NewRun();
            dto.ApplyTo(restored, registry);

            Assert.That(restored.ActiveRegionModifier, Is.Not.Null);
            Assert.That(restored.ActiveRegionModifier.ModifierId, Is.EqualTo("glass_cannon"));
        }
    }
}
