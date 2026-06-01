using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Map;

namespace ProjectAscendant.Tests
{
    // Per §8.3.3 + Epic 12 Task 12.4 — Coin Pouch relic multiplies ₽ drops at RewardApplier.
    public class RewardApplierCoinPouchTests
    {
        private readonly List<Object> _disp = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disp) if (o != null) Object.DestroyImmediate(o);
            _disp.Clear();
        }

        private RelicSO Relic(string id) { RelicSO r = ScriptableObject.CreateInstance<RelicSO>(); r.Id = id; _disp.Add(r); return r; }

        [Test]
        public void Apply_CoinPouch_MultipliesPokeDollars()
        {
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>(); _disp.Add(run);
            run.HeldRelics = new List<RelicSO> { Relic("coin_pouch") };
            EconomyConfigSO econ = ScriptableObject.CreateInstance<EconomyConfigSO>(); econ.CoinPouchPokeDollarMultiplier = 1.25f; _disp.Add(econ);
            TrainerRewardBundle bundle = new() { PokeDollars = 100 };

            RewardApplier.Apply(run, bundle, econ);
            Assert.That(run.PokeDollars, Is.EqualTo(125), "100 × 1.25.");
        }

        [Test]
        public void Apply_NoCoinPouch_UnchangedPokeDollars()
        {
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>(); _disp.Add(run);
            EconomyConfigSO econ = ScriptableObject.CreateInstance<EconomyConfigSO>(); econ.CoinPouchPokeDollarMultiplier = 1.25f; _disp.Add(econ);
            TrainerRewardBundle bundle = new() { PokeDollars = 100 };

            RewardApplier.Apply(run, bundle, econ);
            Assert.That(run.PokeDollars, Is.EqualTo(100), "no relic → unchanged.");
        }
    }
}
