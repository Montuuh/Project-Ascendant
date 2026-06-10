using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §7.8.3.1 (CL-016) — the 16-modifier pool + RegionModifierResolver query API.
    public class RegionModifierResolverTests
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

        // Single active modifier of the given kind (the per-Region rule: 0..1 active).
        private List<RegionModifierSO> Active(RegionModifierKind kind)
        {
            foreach (RegionModifierSO m in Pool())
                if (m.Kind == kind) return new List<RegionModifierSO> { m };
            return new List<RegionModifierSO>();
        }

        [Test]
        public void Pool_Has16_UniqueKindsAndIds()
        {
            List<RegionModifierSO> pool = Pool();
            Assert.That(pool.Count, Is.EqualTo(16), "16-modifier launch pool (CL-016).");

            HashSet<RegionModifierKind> kinds = new();
            HashSet<string> ids = new();
            foreach (RegionModifierSO m in pool)
            {
                Assert.That(kinds.Add(m.Kind), Is.True, $"duplicate kind {m.Kind}");
                Assert.That(ids.Add(m.ModifierId), Is.True, $"duplicate id {m.ModifierId}");
                Assert.That(string.IsNullOrEmpty(m.EffectDescription), Is.False, $"{m.ModifierId} has no description");
            }
            // All 16 kinds represented.
            Assert.That(kinds.Count, Is.EqualTo(System.Enum.GetValues(typeof(RegionModifierKind)).Length));
        }

        [Test]
        public void FindAndHas_LocateActiveKind()
        {
            List<RegionModifierSO> active = Active(RegionModifierKind.GlassCannon);
            Assert.That(RegionModifierResolver.Has(active, RegionModifierKind.GlassCannon), Is.True);
            Assert.That(RegionModifierResolver.Has(active, RegionModifierKind.HandOfPlenty), Is.False);
            Assert.That(RegionModifierResolver.Find(active, RegionModifierKind.GlassCannon), Is.Not.Null);
        }

        [Test]
        public void NeutralValues_WhenNoActiveModifier()
        {
            List<RegionModifierSO> none = new();
            Assert.That(RegionModifierResolver.HandSizeBonus(none), Is.EqualTo(0));
            Assert.That(RegionModifierResolver.XpMultiplier(none), Is.EqualTo(1f));
            Assert.That(RegionModifierResolver.ShopPriceMultiplier(none), Is.EqualTo(1f));
            Assert.That(RegionModifierResolver.DamageDealtMultiplier(none), Is.EqualTo(1f));
            Assert.That(RegionModifierResolver.CoinMultiplier(none), Is.EqualTo(1f));
            Assert.That(RegionModifierResolver.SwapHealAmount(none), Is.EqualTo(0));
            Assert.That(RegionModifierResolver.GrantsSturdyLead(none), Is.False);
            // Null-safe.
            Assert.That(RegionModifierResolver.HandSizeBonus(null), Is.EqualTo(0));
            Assert.That(RegionModifierResolver.XpMultiplier(null), Is.EqualTo(1f));
        }

        [Test]
        public void HandOfPlenty_GivesPlusOneHand()
            => Assert.That(RegionModifierResolver.HandSizeBonus(Active(RegionModifierKind.HandOfPlenty)), Is.EqualTo(1));

        [Test]
        public void QuickStudy_Gives15PercentXp()
            => Assert.That(RegionModifierResolver.XpMultiplier(Active(RegionModifierKind.QuickStudy)), Is.EqualTo(1.15f).Within(0.0001f));

        [Test]
        public void BargainHunter_Gives20PercentDiscount()
            => Assert.That(RegionModifierResolver.ShopPriceMultiplier(Active(RegionModifierKind.BargainHunter)), Is.EqualTo(0.8f).Within(0.0001f));

        [Test]
        public void GlassCannon_Boosts20PercentBothWays()
        {
            List<RegionModifierSO> active = Active(RegionModifierKind.GlassCannon);
            Assert.That(RegionModifierResolver.DamageDealtMultiplier(active), Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(RegionModifierResolver.DamageTakenMultiplier(active), Is.EqualTo(1.2f).Within(0.0001f));
        }

        [Test]
        public void CoinPurse_Gives50PercentMore()
            => Assert.That(RegionModifierResolver.CoinMultiplier(Active(RegionModifierKind.CoinPurse)), Is.EqualTo(1.5f).Within(0.0001f));

        [Test]
        public void SwapFuel_Heals5()
            => Assert.That(RegionModifierResolver.SwapHealAmount(Active(RegionModifierKind.SwapFuel)), Is.EqualTo(5));

        [Test]
        public void IronSkin_Reduces1CleaveDamage()
            => Assert.That(RegionModifierResolver.CleaveDamageReduction(Active(RegionModifierKind.IronSkin)), Is.EqualTo(1));

        [Test]
        public void TraumaResistance_Reduces1Point()
            => Assert.That(RegionModifierResolver.TraumaPenaltyReduction(Active(RegionModifierKind.TraumaResistance)), Is.EqualTo(1));

        [Test]
        public void BooleanKinds_DetectedCorrectly()
        {
            Assert.That(RegionModifierResolver.GrantsSturdyLead(Active(RegionModifierKind.SturdyLead)), Is.True);
            Assert.That(RegionModifierResolver.StepDrawsCard(Active(RegionModifierKind.MassMobilization)), Is.True);
            Assert.That(RegionModifierResolver.RevealsFirstUnknown(Active(RegionModifierKind.PokedexWhisper)), Is.True);
            Assert.That(RegionModifierResolver.GrantsFieldChoice(Active(RegionModifierKind.FieldSurveyor)), Is.True);
        }
    }
}
