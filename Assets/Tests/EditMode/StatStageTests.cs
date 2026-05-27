using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.6 — coverage for the ±6 stat-stage ladder:
    // cap clamping, multiplier accuracy, combat-end reset, boss-phase
    // persistence (§4.4.3.1), and end-to-end EffectiveAttack/Defense
    // through CombatStatResolver.
    public class StatStageTests
    {
        private BattleConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.Divisor = 50;
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.StatStageMultipliers = new float[]
            {
                0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
                1.00f,
                1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
            };
            _config.BurnAttackMultiplier = 0.75f;
            _config.PoisonDefenseMultiplier = 0.85f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PokemonSpeciesSO MakeSpecies(int atk = 50, int def = 50, int hp = 60)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            return s;
        }

        private static PokemonInstance MakeInstance(PokemonSpeciesSO species)
        {
            return new PokemonInstance { Species = species, Level = 1, CurrentHP = species.BaseStats.BaseHP };
        }

        // ── Bucket 1: cap & clamping (§4.2.6) ────────────────────────────────

        [Test]
        public void Modify_PositiveDeltaBeyondCap_ClampsToPlus6()
        {
            // Per §4.2.6 — stages clamp to [-6, +6].
            PokemonSpeciesSO sp = MakeSpecies();
            PokemonInstance pi = MakeInstance(sp);
            int result = StatStageManager.Modify(pi, Stat.Attack, +10);
            Assert.That(result, Is.EqualTo(+6));
            Assert.That(StatStageManager.GetStage(pi, Stat.Attack), Is.EqualTo(+6));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Modify_NegativeDeltaBeyondCap_ClampsToMinus6()
        {
            PokemonSpeciesSO sp = MakeSpecies();
            PokemonInstance pi = MakeInstance(sp);
            int result = StatStageManager.Modify(pi, Stat.Defense, -10);
            Assert.That(result, Is.EqualTo(-6));
            Assert.That(StatStageManager.GetStage(pi, Stat.Defense), Is.EqualTo(-6));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Modify_AccumulatesAcrossCalls()
        {
            // Three +2 calls: 0 → +2 → +4 → +6 (clamp).
            PokemonSpeciesSO sp = MakeSpecies();
            PokemonInstance pi = MakeInstance(sp);
            Assert.That(StatStageManager.Modify(pi, Stat.Attack, +2), Is.EqualTo(+2));
            Assert.That(StatStageManager.Modify(pi, Stat.Attack, +2), Is.EqualTo(+4));
            Assert.That(StatStageManager.Modify(pi, Stat.Attack, +2), Is.EqualTo(+6));
            // Further +1 stays clamped.
            Assert.That(StatStageManager.Modify(pi, Stat.Attack, +1), Is.EqualTo(+6));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Modify_OneStatDoesNotAffectAnother()
        {
            PokemonSpeciesSO sp = MakeSpecies();
            PokemonInstance pi = MakeInstance(sp);
            StatStageManager.Modify(pi, Stat.Attack, +3);
            Assert.That(StatStageManager.GetStage(pi, Stat.Attack), Is.EqualTo(+3));
            Assert.That(StatStageManager.GetStage(pi, Stat.Defense), Is.EqualTo(0));
            Assert.That(StatStageManager.GetStage(pi, Stat.Speed), Is.EqualTo(0));
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 2: multiplier lookup (§4.2.6 ladder) ──────────────────────

        [Test]
        public void GetMultiplier_StageZero_ReturnsOne()
        {
            Assert.That(StatStageManager.GetMultiplier(0, _config), Is.EqualTo(1.00f).Within(0.001f));
        }

        [Test]
        public void GetMultiplier_PlusSix_ReturnsConfigTop()
        {
            Assert.That(StatStageManager.GetMultiplier(+6, _config), Is.EqualTo(4.00f).Within(0.001f));
        }

        [Test]
        public void GetMultiplier_MinusSix_ReturnsConfigFloor()
        {
            Assert.That(StatStageManager.GetMultiplier(-6, _config), Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void GetMultiplier_OutOfBoundsStage_ClampsBeforeLookup()
        {
            // Defensive: a stale stage value of +100 must clamp, not crash.
            Assert.That(StatStageManager.GetMultiplier(+100, _config), Is.EqualTo(4.00f).Within(0.001f));
            Assert.That(StatStageManager.GetMultiplier(-100, _config), Is.EqualTo(0.25f).Within(0.001f));
        }

        [Test]
        public void GetMultiplier_NullConfig_ReturnsIdentity()
        {
            Assert.That(StatStageManager.GetMultiplier(+3, null), Is.EqualTo(1.0f).Within(0.001f));
        }

        // ── Bucket 3: combat-end reset (§4.2.6) ──────────────────────────────

        [Test]
        public void ResetAll_ClearsEveryStat()
        {
            PokemonSpeciesSO sp = MakeSpecies();
            PokemonInstance pi = MakeInstance(sp);
            StatStageManager.Modify(pi, Stat.Attack, +3);
            StatStageManager.Modify(pi, Stat.Defense, -2);
            StatStageManager.Modify(pi, Stat.Speed, +1);

            StatStageManager.ResetAll(pi);

            Assert.That(StatStageManager.GetStage(pi, Stat.Attack), Is.EqualTo(0));
            Assert.That(StatStageManager.GetStage(pi, Stat.Defense), Is.EqualTo(0));
            Assert.That(StatStageManager.GetStage(pi, Stat.Speed), Is.EqualTo(0));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void ResetAll_NullInstance_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => StatStageManager.ResetAll(null));
        }

        // ── Bucket 4: boss phase persistence (§4.4.3.1) ──────────────────────

        [Test]
        public void StatStages_PersistAcrossBossPhaseTransition()
        {
            // Per §4.4.3.1 — stat stages MUST persist when a boss transitions
            // from phase N to phase N+1. The persistence is achieved by NOT
            // calling ResetAll on phase transition. This test documents that
            // contract: a hypothetical "phase change" that only mutates HP /
            // current move set must leave StatStages intact.
            PokemonSpeciesSO sp = MakeSpecies();
            PokemonInstance boss = MakeInstance(sp);
            StatStageManager.Modify(boss, Stat.Attack, +2);
            StatStageManager.Modify(boss, Stat.Defense, -1);

            // Simulate phase transition: HP refill + status clear only.
            // (No ResetAll on stages — per §4.4.3.1.)
            boss.CurrentHP = sp.BaseStats.BaseHP;
            boss.PrimaryStatus = StatusCondition.None;

            Assert.That(StatStageManager.GetStage(boss, Stat.Attack), Is.EqualTo(+2));
            Assert.That(StatStageManager.GetStage(boss, Stat.Defense), Is.EqualTo(-1));
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 5: integration with CombatStatResolver (§4.2.6 + §4.1.1) ─

        [Test]
        public void EffectiveAttack_AtPlus2_DoublesBase()
        {
            // Base Atk 50 × stage(+2)=2.0× = 100. No status modifier.
            PokemonSpeciesSO sp = MakeSpecies(atk: 50);
            PokemonInstance pi = MakeInstance(sp);
            StatStageManager.Modify(pi, Stat.Attack, +2);

            int eff = CombatStatResolver.EffectiveAttack(pi, _config);
            Assert.That(eff, Is.EqualTo(100));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void EffectiveAttack_AtMinus6_ClampsToFloorOne()
        {
            // Base 1 × 0.25 = 0.25 → floor → max(1, 0) = 1.
            // Use a tiny base to drive past the floor.
            PokemonSpeciesSO sp = MakeSpecies(atk: 1);
            PokemonInstance pi = MakeInstance(sp);
            StatStageManager.Modify(pi, Stat.Attack, -6);

            int eff = CombatStatResolver.EffectiveAttack(pi, _config);
            // Floor of 1 prevents divide-by-zero (per CombatStatResolver header comment).
            Assert.That(eff, Is.GreaterThanOrEqualTo(1));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void EffectiveDefense_StatStageAndPoisonStack()
        {
            // Per OPEN G8 — stat-stage first, then status modifier.
            // Base Def 50 × stage(+1)=1.5 × poison(0.85) = 63.75 → floor 63.
            PokemonSpeciesSO sp = MakeSpecies(def: 50);
            PokemonInstance pi = MakeInstance(sp);
            StatStageManager.Modify(pi, Stat.Defense, +1);
            pi.PrimaryStatus = StatusCondition.Poison;

            int eff = CombatStatResolver.EffectiveDefense(pi, _config);
            Assert.That(eff, Is.EqualTo(63));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void EffectiveAttack_StatStageAndBurnStack()
        {
            // Base Atk 100 × stage(+1)=1.5 × burn(0.75) = 112.5 → floor 112.
            PokemonSpeciesSO sp = MakeSpecies(atk: 100);
            PokemonInstance pi = MakeInstance(sp);
            StatStageManager.Modify(pi, Stat.Attack, +1);
            pi.PrimaryStatus = StatusCondition.Burn;

            int eff = CombatStatResolver.EffectiveAttack(pi, _config);
            Assert.That(eff, Is.EqualTo(112));
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 6: null + edge guards ─────────────────────────────────────

        [Test]
        public void Modify_NullInstance_DoesNotThrow_ReturnsZero()
        {
            Assert.That(StatStageManager.Modify(null, Stat.Attack, +3), Is.EqualTo(0));
        }

        [Test]
        public void GetStage_NullInstance_ReturnsZero()
        {
            Assert.That(StatStageManager.GetStage(null, Stat.Attack), Is.EqualTo(0));
        }

        [Test]
        public void GetMultiplier_BadConfigArrayLength_ReturnsIdentity()
        {
            BattleConfigSO bad = ScriptableObject.CreateInstance<BattleConfigSO>();
            bad.StatStageMultipliers = new float[] { 1f, 2f, 3f }; // wrong length
            Assert.That(StatStageManager.GetMultiplier(+3, bad), Is.EqualTo(1f).Within(0.001f));
            Object.DestroyImmediate(bad);
        }
    }
}
