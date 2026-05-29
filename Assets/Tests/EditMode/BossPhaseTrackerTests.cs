using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §4.4.3 + Epic 8 Task 8.4 — BossPhaseTracker phase derivation.
    //   • Bucket 1: PhaseCount gating (1 = never escalates)
    //   • Bucket 2: 2-phase boundaries (Elite, §7.5.1)
    //   • Bucket 3: 3-phase boundaries (ace seam for Task 8.5)
    //   • Bucket 4: statelessness (heal back un-escalates) + null safety
    public class BossPhaseTrackerTests
    {
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.BossPhase2HPThreshold = 0.5f;
            _config.BossPhase3HPThreshold = 0.2f;
            _config.BossPhaseAggressionMultiplier = 1.5f;
            _disposables.Add(_config);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // MaxHP = BaseHP when no GrowthCurve, so CurrentHP doubles as the
        // percentage knob (BaseHP = 100 → CurrentHP == HP%).
        private PokemonInstance MakeMon(int currentHP, int phaseCount)
        {
            PokemonSpeciesSO sp = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            sp.SpeciesId = "phase_test";
            sp.Types = new List<PokemonType> { PokemonType.Normal };
            sp.BaseStats = new BaseStats { BaseHP = 100, BaseAtk = 30, BaseDef = 30, BaseSpd = 50 };
            _disposables.Add(sp);
            return new PokemonInstance
            {
                Species = sp,
                Level = 5,
                CurrentHP = currentHP,
                PhaseCount = phaseCount,
            };
        }

        // ── Bucket 1: PhaseCount gating ──────────────────────────────────────

        [Test]
        public void CurrentPhase_PhaseCountOne_AlwaysPhaseOne_RegardlessOfHP()
        {
            // Per §4.4.3 — ordinary wild/trainer Pokémon (PhaseCount 1) never
            // escalate, even at 1 HP.
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(1, 1), _config), Is.EqualTo(1));
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(100, 1), _config), Is.EqualTo(1));
            Assert.That(BossPhaseTracker.IsAggressivePhase(MakeMon(1, 1), _config), Is.False);
        }

        // ── Bucket 2: 2-phase boundaries (Elite §7.5.1) ──────────────────────

        [Test]
        public void CurrentPhase_TwoPhase_AboveThreshold_IsPhaseOne()
        {
            // 51% > 50% threshold → Phase 1.
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(51, 2), _config), Is.EqualTo(1));
        }

        [Test]
        public void CurrentPhase_TwoPhase_AtThreshold_IsPhaseTwo()
        {
            // Per §4.4.3 — Phase 2 trigger is HP <= 50% (inclusive).
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(50, 2), _config), Is.EqualTo(2));
        }

        [Test]
        public void CurrentPhase_TwoPhase_BelowThreshold_IsPhaseTwo()
        {
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(20, 2), _config), Is.EqualTo(2));
            Assert.That(BossPhaseTracker.IsAggressivePhase(MakeMon(20, 2), _config), Is.True);
        }

        [Test]
        public void CurrentPhase_TwoPhase_NeverReachesPhaseThree()
        {
            // A 2-phase Elite at 10% HP is still Phase 2 — Phase 3 requires
            // PhaseCount >= 3.
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(10, 2), _config), Is.EqualTo(2));
        }

        // ── Bucket 3: 3-phase boundaries (ace seam, Task 8.5) ────────────────

        [Test]
        public void CurrentPhase_ThreePhase_MidHP_IsPhaseTwo()
        {
            // 30% → below P2 (50%), above P3 (20%) → Phase 2.
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(30, 3), _config), Is.EqualTo(2));
        }

        [Test]
        public void CurrentPhase_ThreePhase_AtP3Threshold_IsPhaseThree()
        {
            // Per §4.4.3 three-phase template — Phase 3 trigger is HP <= 20%.
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(20, 3), _config), Is.EqualTo(3));
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(5, 3), _config), Is.EqualTo(3));
        }

        // ── Bucket 4: statelessness + null safety ────────────────────────────

        [Test]
        public void CurrentPhase_IsStateless_HealBackAboveThreshold_ReturnsToPhaseOne()
        {
            // Phase is a pure function of live HP — healing back above 50%
            // returns the boss to Phase 1 (no latched state). Determinism.
            PokemonInstance mon = MakeMon(40, 2);
            Assert.That(BossPhaseTracker.CurrentPhase(mon, _config), Is.EqualTo(2));
            mon.CurrentHP = 80; // healed back above threshold
            Assert.That(BossPhaseTracker.CurrentPhase(mon, _config), Is.EqualTo(1));
        }

        [Test]
        public void CurrentPhase_NullInputs_ReturnPhaseOne()
        {
            Assert.That(BossPhaseTracker.CurrentPhase(null, _config), Is.EqualTo(1));
            Assert.That(BossPhaseTracker.CurrentPhase(MakeMon(10, 2), null), Is.EqualTo(1));
        }
    }
}
