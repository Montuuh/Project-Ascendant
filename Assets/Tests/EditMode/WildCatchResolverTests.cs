using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.1.4 + §7.3.4.1 — deterministic catch evaluator.
    // Every test maps to one bullet of the LOCKED catch-success rules.
    public class WildCatchResolverTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private CatchConsumableEffectSO MakeEffect(float threshold, bool catchWithStatus)
        {
            CatchConsumableEffectSO e = ScriptableObject.CreateInstance<CatchConsumableEffectSO>();
            e.CatchThresholdPercent = threshold;
            e.CatchWithAnyStatus = catchWithStatus;
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
                Level = 1,
                CurrentHP = currentHP,
                PrimaryStatus = status,
            };
        }

        // ── §7.3.4.1 step 5a: HP ≥ threshold, no status → FailedHighHP ──

        [Test]
        public void Evaluate_FullHP_NoStatus_FailedHighHP()
        {
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 100);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        [Test]
        public void Evaluate_ExactlyThresholdHP_NoStatus_FailedHighHP_StrictLessThan()
        {
            // Per §7.3.4.1 — HP < 50% (strict). HP exactly at 50% must fail.
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 50);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        // ── §7.3.4.1 step 5b: HP < threshold, no status → Caught ────────

        [Test]
        public void Evaluate_BelowThreshold_NoStatus_Caught()
        {
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 49);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.Caught));
        }

        // ── §7.3.4.1 step 5c: any HP + any status → Caught ──────────────

        [Test]
        public void Evaluate_FullHP_WithBurn_Caught_StatusExpandsWindow()
        {
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 100, status: StatusCondition.Burn);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.Caught));
        }

        [Test]
        public void Evaluate_FullHP_WithStatus_ButEffectDisablesStatusWindow_FailedHighHP()
        {
            // CatchWithAnyStatus=false → status no longer matters.
            CatchConsumableEffectSO eff = MakeEffect(0.5f, false);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 100, status: StatusCondition.Burn);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        // ── §7.3.4.1 step 5d: HP ≤ 0 → FailedFainted (recruit lost) ─────

        [Test]
        public void Evaluate_FaintedWild_FailedFainted()
        {
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 0);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedFainted));
        }

        [Test]
        public void Evaluate_FaintedWildWithStatus_StillFailedFainted_StatusDoesNotRescue()
        {
            // Per §7.3.4.1 — HP ≤ 0 is "recruit is lost" regardless of status.
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 0, status: StatusCondition.Sleep);
            Assert.That(WildCatchResolver.Evaluate(wild, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedFainted));
        }

        // ── Guards ────────────────────────────────────────────────────────

        [Test]
        public void Evaluate_NullWild_FailedFainted()
        {
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            Assert.That(WildCatchResolver.Evaluate(null, eff),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedFainted));
        }

        [Test]
        public void Evaluate_NullEffect_FailedHighHP_DefensiveDefault()
        {
            PokemonSpeciesSO sp = MakeSpecies(100);
            PokemonInstance wild = MakeWild(sp, currentHP: 30);
            Assert.That(WildCatchResolver.Evaluate(wild, null),
                Is.EqualTo(WildCatchResolver.CatchAttempt.FailedHighHP));
        }

        [Test]
        public void IsCatchable_AgreesWithEvaluate()
        {
            CatchConsumableEffectSO eff = MakeEffect(0.5f, true);
            PokemonSpeciesSO sp = MakeSpecies(100);
            Assert.That(WildCatchResolver.IsCatchable(MakeWild(sp, 30), eff), Is.True);
            Assert.That(WildCatchResolver.IsCatchable(MakeWild(sp, 100), eff), Is.False);
            Assert.That(WildCatchResolver.IsCatchable(MakeWild(sp, 0), eff), Is.False);
        }
    }
}
