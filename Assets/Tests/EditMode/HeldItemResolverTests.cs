using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §8.4 + Epic 12 Task 12.5/12.6 — Held Item runtime: wearer type-boost (§8.4.2) + Leftovers
    // regen (§8.4.4).
    public class HeldItemResolverTests
    {
        private readonly List<Object> _disp = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disp) if (o != null) Object.DestroyImmediate(o);
            _disp.Clear();
        }

        private T Make<T>() where T : ScriptableObject { T o = ScriptableObject.CreateInstance<T>(); _disp.Add(o); return o; }

        private HeldItemSO TypeBoost(PokemonType type, float mult)
        {
            HeldItemSO h = Make<HeldItemSO>(); h.BoostsType = type; h.WearerDamageMultiplier = mult; return h;
        }

        private MoveSO Move(PokemonType type) { MoveSO m = Make<MoveSO>(); m.Type = type; return m; }

        private PokemonInstance Mon(int maxHp, int curHp, HeldItemSO held = null)
        {
            PokemonSpeciesSO sp = Make<PokemonSpeciesSO>();
            sp.BaseStats = new BaseStats { BaseHP = maxHp, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            sp.GrowthCurve = null;
            return new PokemonInstance { Species = sp, Level = 1, CurrentHP = curHp, HeldItem = held };
        }

        [Test]
        public void TypeBoost_AppliesOnlyToMatchingType()
        {
            PokemonInstance p = Mon(100, 100, TypeBoost(PokemonType.Fire, 1.20f)); // Charcoal
            Assert.That(HeldItemResolver.OutgoingDamageMultiplier(p, Move(PokemonType.Fire)), Is.EqualTo(1.20f).Within(0.001f));
            Assert.That(HeldItemResolver.OutgoingDamageMultiplier(p, Move(PokemonType.Water)), Is.EqualTo(1f), "non-matching type.");
        }

        [Test]
        public void TypeBoost_NoItem_IsOne()
        {
            Assert.That(HeldItemResolver.OutgoingDamageMultiplier(Mon(100, 100), Move(PokemonType.Fire)), Is.EqualTo(1f));
        }

        [Test]
        public void Leftovers_RegensFractionOfMax_WhenNotFull()
        {
            EconomyConfigSO econ = Make<EconomyConfigSO>();
            HeldItemSO left = Make<HeldItemSO>(); left.LeftoversRegenDivisor = 16;
            Assert.That(HeldItemResolver.LeftoversRegen(Mon(100, 50, left), econ), Is.EqualTo(6), "floor(100/16).");
            Assert.That(HeldItemResolver.LeftoversRegen(Mon(100, 100, left), econ), Is.EqualTo(0), "full → no regen.");
            Assert.That(HeldItemResolver.LeftoversRegen(Mon(100, 0, left), econ), Is.EqualTo(0), "fainted → no regen.");
            Assert.That(HeldItemResolver.LeftoversRegen(Mon(100, 50), econ), Is.EqualTo(0), "no item → 0.");
        }

        [Test]
        public void Leftovers_MinimumOne()
        {
            EconomyConfigSO econ = Make<EconomyConfigSO>();
            HeldItemSO left = Make<HeldItemSO>(); left.LeftoversRegenDivisor = 16;
            Assert.That(HeldItemResolver.LeftoversRegen(Mon(10, 5, left), econ), Is.EqualTo(1), "floor(10/16)=0 → min 1.");
        }
    }
}
