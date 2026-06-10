using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.1.4 + §7.3.4.1 (CL-014) — deterministic Catchability Gauge.
    // CatchThreshold = base (ball tier) + (hasStatus ? statusBonus : 0); gauge fills linearly as HP
    // drops; catch succeeds at gauge == 100 (HP fraction ≤ threshold). Basic ball = base 0.30 +0.20
    // status → catchable at HP ≤ 30% (no status) or ≤ 50% (status). No RNG (Pillar 1).
    public class WildCatchResolverTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // Basic-ball defaults unless overridden: base 0.30, +0.20 status window (CL-014).
        private CatchConsumableEffectSO MakeEffect(float threshold = 0.30f, float statusBonus = 0.20f)
        {
            CatchConsumableEffectSO e = ScriptableObject.CreateInstance<CatchConsumableEffectSO>();
            e.CatchThresholdPercent = threshold;
            e.StatusCatchBonusPercent = statusBonus;
            _disposables.Add(e);
            return e;
        }

        private PokemonSpeciesSO MakeSpecies(int baseHP)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "wild";
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = baseHP, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private static PokemonInstance MakeWild(PokemonSpeciesSO sp, int currentHP,
                                                StatusCondition status = StatusCondition.None)
        {
            return new PokemonInstance
            {
                Species = sp,
                Level = 1,           // GrowthCurve null → MaxHP == BaseHP, so currentHP == HP%
                CurrentHP = currentHP,
                PrimaryStatus = status,
            };
        }

        // ── §7.3.4.1 (CL-014): HP > threshold, no status → gauge < 100 → FailedHighHP ──

        [Test]
        public void Evaluate_FullHP_NoStatus_FailedHighHP()
        {
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 100), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        [Test]
        public void Evaluate_AboveThreshold_NoStatus_FailedHighHP()
        {
            // HP 50% with base 0.30 → gauge 71 → not catchable.
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 50), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        // ── §7.3.4.1 (CL-014): HP ≤ threshold → gauge 100 → Caught (inclusive at the threshold) ──

        [Test]
        public void Evaluate_ExactlyThreshold_NoStatus_Caught()
        {
            // HP exactly 30% with base 0.30 → gauge 100 → catchable (≤ inclusive).
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 30), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.Caught));
        }

        [Test]
        public void Evaluate_BelowThreshold_NoStatus_Caught()
        {
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 25), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.Caught));
        }

        // ── §7.3.4.1 (CL-014): status adds +20pt (NOT catch-at-any-HP) ──

        [Test]
        public void Evaluate_Status_ExpandsWindowBy20pt()
        {
            // HP 50%: not catchable without status (gauge 71), catchable with status (threshold 0.50 → 100).
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 50, StatusCondition.None), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 50, StatusCondition.Burn), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.Caught));
        }

        [Test]
        public void Evaluate_Status_DoesNotCatchAtFullHP()
        {
            // CL-014 removes the old "status → catch at ANY HP". Full HP + status (threshold 0.50) → fail.
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 100, StatusCondition.Burn), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        [Test]
        public void Evaluate_ZeroStatusBonus_StatusIrrelevant()
        {
            // statusBonus = 0 → status grants no window.
            CatchConsumableEffectSO eff = MakeEffect(statusBonus: 0f);
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 50, StatusCondition.Sleep), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        // ── §7.3.4.1: HP ≤ 0 → FailedFainted (recruit lost) regardless of status ──

        [Test]
        public void Evaluate_FaintedWild_FailedFainted()
        {
            CatchConsumableEffectSO eff = MakeEffect();
            Assert.That(WildCatchResolver.Evaluate(MakeWild(MakeSpecies(100), 0), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedFainted));
        }

        [Test]
        public void Evaluate_FaintedWildWithStatus_StillFailedFainted()
        {
            CatchConsumableEffectSO eff = MakeEffect();
            Assert.That(WildCatchResolver.Evaluate(MakeWild(MakeSpecies(100), 0, StatusCondition.Sleep), eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedFainted));
        }

        // ── Guards ──

        [Test]
        public void Evaluate_NullWild_FailedFainted()
            => Assert.That(WildCatchResolver.Evaluate(null, MakeEffect()),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedFainted));

        [Test]
        public void Evaluate_NullEffect_FailedHighHP_DefensiveDefault()
        {
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Evaluate(MakeWild(sp, 30), null),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        // ── Catchability gauge values (the telegraph) ──

        [TestCase(100, StatusCondition.None, 0)]    // full HP
        [TestCase(50, StatusCondition.None, 71)]    // 100×0.50/0.70
        [TestCase(65, StatusCondition.None, 50)]    // 100×0.35/0.70
        [TestCase(30, StatusCondition.None, 100)]   // at threshold → READY
        [TestCase(25, StatusCondition.None, 100)]   // below threshold → clamped 100
        [TestCase(50, StatusCondition.Burn, 100)]   // status → threshold 0.50 → READY at 50%
        [TestCase(60, StatusCondition.Burn, 80)]    // 100×0.40/0.50
        public void Catchability_GaugeValues(int hp, StatusCondition status, int expected)
        {
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.Catchability(MakeWild(sp, hp, status), eff), Is.EqualTo(expected));
        }

        [Test]
        public void Catchability_FaintedOrNull_IsZero()
        {
            CatchConsumableEffectSO eff = MakeEffect();
            Assert.That(WildCatchResolver.Catchability(MakeWild(MakeSpecies(100), 0), eff), Is.EqualTo(0));
            Assert.That(WildCatchResolver.Catchability(null, eff), Is.EqualTo(0));
        }

        [Test]
        public void IsCatchable_AgreesWithEvaluate()
        {
            CatchConsumableEffectSO eff = MakeEffect();
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.IsCatchable(MakeWild(sp, 30), eff), Is.True);
            Assert.That(WildCatchResolver.IsCatchable(MakeWild(sp, 100), eff), Is.False);
            Assert.That(WildCatchResolver.IsCatchable(MakeWild(sp, 0), eff), Is.False);
        }
    }
}
