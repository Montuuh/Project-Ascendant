using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §8.3.3 + Epic 12 Task 12.3/12.4 — RelicResolver combat multipliers: Brave Charm (<50% HP),
    // Soothe Bell (full HP), Berry Pouch (heal +20%).
    public class RelicResolverTests
    {
        private readonly List<Object> _disp = new();
        private BattleConfigSO _cfg;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<BattleConfigSO>();
            _cfg.BraveCharmDamageMultiplier = 1.10f;
            _cfg.SootheBellDamageMultiplier = 1.05f;
            _cfg.BerryPouchHealMultiplier = 1.20f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cfg);
            foreach (Object o in _disp) if (o != null) Object.DestroyImmediate(o);
            _disp.Clear();
        }

        private RelicSO Relic(string id)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>(); r.Id = id; _disp.Add(r); return r;
        }

        private PokemonInstance Mon(int maxHp, int currentHp)
        {
            PokemonSpeciesSO sp = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            sp.BaseStats = new BaseStats { BaseHP = maxHp, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            sp.GrowthCurve = null;
            _disp.Add(sp);
            return new PokemonInstance { Species = sp, Level = 1, CurrentHP = currentHp };
        }

        [Test]
        public void Outgoing_NoRelics_IsOne()
        {
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 100), null, _cfg), Is.EqualTo(1f));
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 40), new List<RelicSO>(), _cfg), Is.EqualTo(1f));
        }

        [Test]
        public void BraveCharm_AppliesOnlyBelowHalfHp()
        {
            List<RelicSO> r = new() { Relic("brave_charm") };
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 40), r, _cfg), Is.EqualTo(1.10f).Within(0.001f));
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 60), r, _cfg), Is.EqualTo(1f), "≥50% HP → no bonus.");
        }

        [Test]
        public void SootheBell_AppliesOnlyAtFullHp()
        {
            List<RelicSO> r = new() { Relic("soothe_bell") };
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 100), r, _cfg), Is.EqualTo(1.05f).Within(0.001f));
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 99), r, _cfg), Is.EqualTo(1f), "not full → no bonus.");
        }

        [Test]
        public void Relics_DoNotStackWhenConditionsExclusive()
        {
            List<RelicSO> both = new() { Relic("brave_charm"), Relic("soothe_bell") };
            // Brave fires at low HP, Soothe at full HP — never both at once.
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 40), both, _cfg), Is.EqualTo(1.10f).Within(0.001f));
            Assert.That(RelicResolver.OutgoingDamageMultiplier(Mon(100, 100), both, _cfg), Is.EqualTo(1.05f).Within(0.001f));
        }

        [Test]
        public void BerryPouch_BoostsHeal()
        {
            List<RelicSO> r = new() { Relic("berry_pouch") };
            Assert.That(RelicResolver.ApplyHealBonus(30, r, _cfg), Is.EqualTo(36), "30 × 1.20.");
            Assert.That(RelicResolver.ApplyHealBonus(30, new List<RelicSO>(), _cfg), Is.EqualTo(30), "no relic → unchanged.");
        }

        private MoveSO Mv(MoveRange range) { MoveSO m = ScriptableObject.CreateInstance<MoveSO>(); m.Range = range; _disp.Add(m); return m; }

        [Test]
        public void ChoiceSpecs_FirstRangedFree_SubsequentPlusOne()
        {
            List<RelicSO> r = new() { Relic("choice_specs") };
            Assert.That(RelicResolver.ApplyChoiceCost(3, Mv(MoveRange.Ranged), r, 0, 0), Is.EqualTo(0), "first ranged free.");
            Assert.That(RelicResolver.ApplyChoiceCost(3, Mv(MoveRange.Ranged), r, 1, 0), Is.EqualTo(4), "subsequent +1.");
            Assert.That(RelicResolver.ApplyChoiceCost(3, Mv(MoveRange.Melee), r, 0, 0), Is.EqualTo(3), "Specs ignores Melee.");
        }

        [Test]
        public void ChoiceBand_FirstMeleeFree()
        {
            List<RelicSO> r = new() { Relic("choice_band") };
            Assert.That(RelicResolver.ApplyChoiceCost(2, Mv(MoveRange.Melee), r, 0, 0), Is.EqualTo(0));
            Assert.That(RelicResolver.ApplyChoiceCost(2, Mv(MoveRange.Melee), r, 0, 1), Is.EqualTo(3));
        }

        [Test]
        public void QuickDraw_OnlyTurnOne()
        {
            List<RelicSO> r = new() { Relic("quick_draw") };
            Assert.That(RelicResolver.QuickDrawBonus(r, 1), Is.EqualTo(1));
            Assert.That(RelicResolver.QuickDrawBonus(r, 2), Is.EqualTo(0), "only turn 1.");
            Assert.That(RelicResolver.QuickDrawBonus(new List<RelicSO>(), 1), Is.EqualTo(0));
        }

        [Test]
        public void LuckyEgg_BoostsXp()
        {
            ProgressionConfigSO pc = ScriptableObject.CreateInstance<ProgressionConfigSO>();
            pc.LuckyEggXPMultiplier = 1.15f; _disp.Add(pc);
            List<RelicSO> r = new() { Relic("lucky_egg_token") };
            Assert.That(RelicResolver.ApplyXpMultiplier(100, r, pc), Is.EqualTo(115), "100 × 1.15.");
            Assert.That(RelicResolver.ApplyXpMultiplier(100, new List<RelicSO>(), pc), Is.EqualTo(100));
        }
    }
}
