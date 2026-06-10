using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §7.8.3.1 (CL-016) — end-to-end Region Modifier effect wiring (economy layer).
    // Combat-layer effects (Glass Cannon, etc.) are covered in CombatControllerTests.
    public class RegionModifierEffectTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private RunStateSO RunWith(RegionModifierKind? kind)
        {
            RunStateSO r = ScriptableObject.CreateInstance<RunStateSO>();
            _disposables.Add(r);
            if (kind.HasValue)
            {
                foreach (RegionModifierSO m in RegionModifierPool.BuildAll())
                {
                    if (m.Kind == kind.Value) { _disposables.Add(m); r.SetRegionModifier(m); }
                    else Object.DestroyImmediate(m);
                }
            }
            return r;
        }

        [Test]
        public void CoinPurse_Multiplies_PokeDollarReward()
        {
            RunStateSO run = RunWith(RegionModifierKind.CoinPurse);
            TrainerRewardBundle bundle = TrainerRewardBundle.Empty;
            bundle.PokeDollars = 100;
            RewardApplier.Apply(run, bundle, null);
            Assert.That(run.PokeDollars, Is.EqualTo(150), "Coin Purse ×1.5");
        }

        [Test]
        public void NoCoinPurse_NoMultiplier()
        {
            RunStateSO run = RunWith(null);
            TrainerRewardBundle bundle = TrainerRewardBundle.Empty;
            bundle.PokeDollars = 100;
            RewardApplier.Apply(run, bundle, null);
            Assert.That(run.PokeDollars, Is.EqualTo(100));
        }

        [Test]
        public void BargainHunter_Discounts_DojoPrices()
        {
            EconomyConfigSO eco = ScriptableObject.CreateInstance<EconomyConfigSO>();
            eco.DojoMoveCost = 150; eco.DojoAbilityCost = 200;
            _disposables.Add(eco);
            RunStateSO run = RunWith(RegionModifierKind.BargainHunter);
            Box box = new(6);
            DojoNodeController dojo = new(new MapNode(5, 0, 0, NodeType.Dojo), run, box, eco);

            Assert.That(dojo.MoveCost(), Is.EqualTo(120), "150 × 0.8");
            Assert.That(dojo.AbilityCost(), Is.EqualTo(160), "200 × 0.8");
        }

        [Test]
        public void NoBargainHunter_FullDojoPrice()
        {
            EconomyConfigSO eco = ScriptableObject.CreateInstance<EconomyConfigSO>();
            eco.DojoMoveCost = 150; eco.DojoAbilityCost = 200;
            _disposables.Add(eco);
            RunStateSO run = RunWith(null);
            Box box = new(6);
            DojoNodeController dojo = new(new MapNode(5, 0, 0, NodeType.Dojo), run, box, eco);

            Assert.That(dojo.MoveCost(), Is.EqualTo(150));
            Assert.That(dojo.AbilityCost(), Is.EqualTo(200));
        }
    }
}
