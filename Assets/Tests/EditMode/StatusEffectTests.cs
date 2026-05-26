using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.5.11 — coverage matrix for all 6 status conditions:
    // application, immunity, duration tick, per-condition effects, cures,
    // and integration with CombatStatResolver.
    public class StatusEffectTests
    {
        private const uint SEED = 0xABCDEF01;

        private BattleConfigSO _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            // Defaults are already set on the SO (see BattleConfigSO authoring),
            // but pin them here to keep tests independent of the asset values.
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
            _config.BurnDoTDivisor = 16;
            _config.BurnAttackMultiplier = 0.75f;
            _config.PoisonDoTDivisor = 16;
            _config.PoisonDefenseMultiplier = 0.85f;
            _config.ParalysisAPCostBonus = 1;
            _config.ParalysisDuration = 3;
            _config.SleepDuration = 1;
            _config.FreezeDuration = 1;
            _config.FreezeFireDamageMultiplier = 1.5f;
            _config.ConfusionDuration = 3;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PokemonSpeciesSO MakeSpecies(int hp, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            return s;
        }

        private static PokemonInstance MakeInstance(PokemonSpeciesSO species)
        {
            return new PokemonInstance
            {
                Species = species,
                Level = 1,
                CurrentHP = species.BaseStats.BaseHP
            };
        }

        private static MoveSO MakeMove(PokemonType type, int basePower = 50, int apCost = 1)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.Type = type;
            m.BasePower = basePower;
            m.APCost = apCost;
            m.RangeModifierMultiplier = 1f;
            return m;
        }

        // ── Bucket 1: Apply + type immunity (§4.2.4) (6) ──────────────────────

        [Test]
        public void TryApply_BurnOnFireType_BlockedByImmunity()
        {
            // Per §4.2.4 — Fire-type immune to Burn.
            PokemonSpeciesSO fire = MakeSpecies(64, PokemonType.Fire);
            PokemonInstance pi = MakeInstance(fire);
            bool applied = StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            Assert.That(applied, Is.False);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Object.DestroyImmediate(fire);
        }

        [Test]
        public void TryApply_FreezeOnFireOrIceType_BlockedByImmunity()
        {
            // Per §4.2.4 — Fire AND Ice immune to Freeze.
            foreach (PokemonType immune in new[] { PokemonType.Fire, PokemonType.Ice })
            {
                PokemonSpeciesSO sp = MakeSpecies(50, immune);
                PokemonInstance pi = MakeInstance(sp);
                Assert.That(StatusEffectManager.TryApply(pi, StatusCondition.Freeze, _config), Is.False);
                Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
                Object.DestroyImmediate(sp);
            }
        }

        [Test]
        public void TryApply_ParalysisOnElectricType_BlockedByImmunity()
        {
            // Per §4.2.4 — Electric immune to Paralysis.
            PokemonSpeciesSO ele = MakeSpecies(50, PokemonType.Electric);
            PokemonInstance pi = MakeInstance(ele);
            Assert.That(StatusEffectManager.TryApply(pi, StatusCondition.Paralysis, _config), Is.False);
            Object.DestroyImmediate(ele);
        }

        [Test]
        public void TryApply_PoisonOnPoisonOrSteelType_BlockedByImmunity()
        {
            foreach (PokemonType immune in new[] { PokemonType.Poison, PokemonType.Steel })
            {
                PokemonSpeciesSO sp = MakeSpecies(50, immune);
                PokemonInstance pi = MakeInstance(sp);
                Assert.That(StatusEffectManager.TryApply(pi, StatusCondition.Poison, _config), Is.False);
                Object.DestroyImmediate(sp);
            }
        }

        [Test]
        public void TryApply_NonImmuneTarget_SetsPrimaryAndDuration()
        {
            // Per §4.2.2.3 — Paralysis duration = 3 (from config). Primary slot set.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            bool applied = StatusEffectManager.TryApply(pi, StatusCondition.Paralysis, _config);
            Assert.That(applied, Is.True);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Paralysis));
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(3));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void TryApply_RespectsExplicitSpeciesImmunityList()
        {
            // Per §4.2.4 — PokemonSpeciesSO.StatusImmunities is an explicit override.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            sp.StatusImmunities = new List<StatusCondition> { StatusCondition.Sleep };
            PokemonInstance pi = MakeInstance(sp);
            Assert.That(StatusEffectManager.TryApply(pi, StatusCondition.Sleep, _config), Is.False);
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 2: Storage + replacement (§4.2.2 + §4.2.3) (3) ─────────────

        [Test]
        public void TryApply_NewPrimaryReplacesExistingPrimary()
        {
            // Per §4.2.2 — new primary status replaces the existing one.
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Poison, _config);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Poison));
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(int.MaxValue));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void TryApply_PrimaryAndSecondaryCoexist()
        {
            // Per §4.2.3 — Confusion (secondary) coexists with primary status.
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Burn));
            Assert.That(pi.SecondaryStatus, Is.EqualTo(StatusCondition.Confusion));
            Assert.That(pi.SecondaryStatusTurnsRemaining, Is.EqualTo(3));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void TryApply_ReConfusion_RefreshesDuration()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.SecondaryStatusTurnsRemaining, Is.EqualTo(2));
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config); // refresh
            Assert.That(pi.SecondaryStatusTurnsRemaining, Is.EqualTo(3));
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 3: Durations + tick (§4.2.5) (5) ───────────────────────────

        [Test]
        public void Tick_BurnIsPermanent_DoesNotDecrement()
        {
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            for (int i = 0; i < 10; i++) StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Burn));
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(int.MaxValue));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Tick_PoisonIsPermanent_DoesNotDecrement()
        {
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Poison, _config);
            for (int i = 0; i < 10; i++) StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Poison));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Tick_Paralysis3Turns_ExpiresAfter3Ticks()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Paralysis, _config);
            StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(2));
            StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(1));
            StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(0));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Tick_Sleep1Turn_ExpiresAfterOneTick()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Sleep, _config);
            StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Tick_BurnAndConfusion_IndependentDurations()
        {
            // Per §4.2.5 — Confusion ticks separately from primary status.
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            for (int i = 0; i < 3; i++) StatusEffectManager.TickAtEndOfTurn(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Burn)); // permanent
            Assert.That(pi.SecondaryStatus, Is.EqualTo(StatusCondition.None)); // 3 ticks expired
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 4: Per-condition effects (§4.2.2 / §4.2.3) (9) ─────────────

        [Test]
        public void ComputeDoT_Burn_FloorMaxHpDiv16()
        {
            // Per §4.2.2.1 — floor(MaxHP/16) min 1.
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            Assert.That(StatusEffectManager.ComputeDoTDamage(pi, _config), Is.EqualTo(4)); // 64/16
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void ComputeDoT_Burn_LowHpFloorsToOneMinimum()
        {
            // Per §4.2.2.1 — minimum 1 damage even at very low HP.
            PokemonSpeciesSO sp = MakeSpecies(10, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            Assert.That(StatusEffectManager.ComputeDoTDamage(pi, _config), Is.EqualTo(1));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void ComputeDoT_NonDoTStatus_ReturnsZero()
        {
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Paralysis, _config);
            Assert.That(StatusEffectManager.ComputeDoTDamage(pi, _config), Is.EqualTo(0));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Modifiers_Burn_AttackMultiplierIsConfigValue()
        {
            // Per §4.2.2.1 — Burn attack multiplier comes from BattleConfigSO.
            Assert.That(StatusModifiers.GetAttackMultiplier(StatusCondition.Burn, _config),
                Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void Modifiers_Poison_DefenseMultiplierIsConfigValue()
        {
            // Per §4.2.2.2 — Poison defense multiplier comes from BattleConfigSO.
            Assert.That(StatusModifiers.GetDefenseMultiplier(StatusCondition.Poison, _config),
                Is.EqualTo(0.85f).Within(0.0001f));
        }

        [Test]
        public void Modifiers_Paralysis_AddsAPCostBonus()
        {
            // Per §4.2.2.3 — +1 AP on every move.
            Assert.That(StatusModifiers.GetMoveAPCostBonus(StatusCondition.Paralysis, _config), Is.EqualTo(1));
            // Effective cost: base APCost + bonus.
            MoveSO m = MakeMove(PokemonType.Normal, 50, apCost: 2);
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Paralysis, _config);
            Assert.That(StatusModifiers.GetEffectiveAPCost(m, pi, _config), Is.EqualTo(3));
            Object.DestroyImmediate(m);
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Modifiers_SleepAndFreeze_BlockCardPlayability()
        {
            // Per §4.2.2.4 / §4.2.2.5 — Sleep + Freeze make cards unplayable.
            Assert.That(StatusModifiers.AreCardsPlayable(StatusCondition.Sleep), Is.False);
            Assert.That(StatusModifiers.AreCardsPlayable(StatusCondition.Freeze), Is.False);
            Assert.That(StatusModifiers.AreCardsPlayable(StatusCondition.Burn), Is.True);
            Assert.That(StatusModifiers.AreCardsPlayable(StatusCondition.None), Is.True);
        }

        [Test]
        public void Modifiers_OnlyFreezeLocksPosition()
        {
            // Per §4.2.2.5 — Freeze locks position; Sleep does NOT (OPEN G10).
            Assert.That(StatusModifiers.IsPositionLocked(StatusCondition.Freeze), Is.True);
            Assert.That(StatusModifiers.IsPositionLocked(StatusCondition.Sleep), Is.False);
            Assert.That(StatusModifiers.IsPositionLocked(StatusCondition.Burn), Is.False);
        }

        [Test]
        public void Modifiers_FreezeFireMultiplier_OnlyAppliesToFireMoves()
        {
            // Per §4.2.2.5 + OPEN G9 — Fire-type moves against Frozen target ×1.5.
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Freeze, _config);

            MoveSO fire = MakeMove(PokemonType.Fire);
            MoveSO water = MakeMove(PokemonType.Water);
            Assert.That(StatusModifiers.GetIncomingDamageMultiplier(pi, fire, _config),
                Is.EqualTo(1.5f).Within(0.0001f));
            Assert.That(StatusModifiers.GetIncomingDamageMultiplier(pi, water, _config),
                Is.EqualTo(1.0f).Within(0.0001f));

            Object.DestroyImmediate(fire);
            Object.DestroyImmediate(water);
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 5: Confusion discard (§4.2.3.1) (3) ────────────────────────

        [Test]
        public void ConfusionDiscard_RemovesOneCardFromDrawnHand()
        {
            // Per §4.2.3.1 — 1 random skill card discarded per Confused Pokémon.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);

            MoveSO m1 = MakeMove(PokemonType.Normal);
            MoveSO m2 = MakeMove(PokemonType.Water);
            MoveSO m3 = MakeMove(PokemonType.Fire);
            List<MoveSO> hand = new List<MoveSO> { m1, m2, m3 };

            GameRNG rng = new GameRNG(SEED);
            int idx = StatusEffectManager.ResolveConfusionDiscard(pi, hand, rng);
            Assert.That(idx, Is.InRange(0, 2));
            Assert.That(hand.Count, Is.EqualTo(2));

            Object.DestroyImmediate(m1);
            Object.DestroyImmediate(m2);
            Object.DestroyImmediate(m3);
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void ConfusionDiscard_NoConfusion_ReturnsMinusOne_HandIntact()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);

            MoveSO m1 = MakeMove(PokemonType.Normal);
            List<MoveSO> hand = new List<MoveSO> { m1 };

            GameRNG rng = new GameRNG(SEED);
            Assert.That(StatusEffectManager.ResolveConfusionDiscard(pi, hand, rng), Is.EqualTo(-1));
            Assert.That(hand.Count, Is.EqualTo(1));

            Object.DestroyImmediate(m1);
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void ConfusionDiscard_Deterministic_SameSeedSameIndex()
        {
            // Per Engineering Pillar 3 — same RNG seed → same discard index.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi1 = MakeInstance(sp);
            PokemonInstance pi2 = MakeInstance(sp);
            StatusEffectManager.TryApply(pi1, StatusCondition.Confusion, _config);
            StatusEffectManager.TryApply(pi2, StatusCondition.Confusion, _config);

            MoveSO m = MakeMove(PokemonType.Normal);
            List<MoveSO> handA = new List<MoveSO> { m, m, m, m, m };
            List<MoveSO> handB = new List<MoveSO> { m, m, m, m, m };

            GameRNG rngA = new GameRNG(SEED);
            GameRNG rngB = new GameRNG(SEED);
            int idxA = StatusEffectManager.ResolveConfusionDiscard(pi1, handA, rngA);
            int idxB = StatusEffectManager.ResolveConfusionDiscard(pi2, handB, rngB);
            Assert.That(idxA, Is.EqualTo(idxB));

            Object.DestroyImmediate(m);
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 6: Cures + combat-end (§4.2.7 + §4.2.1) (4) ────────────────

        [Test]
        public void Cure_SingleStatus_ClearsMatchingSlotOnly()
        {
            // Per §4.2.7 — Antidote cures Poison without clearing Confusion.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Poison, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            StatusEffectManager.Cure(pi, StatusCondition.Poison);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Assert.That(pi.SecondaryStatus, Is.EqualTo(StatusCondition.Confusion));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void Cure_ConfusionOnly_LeavesPrimaryUntouched()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            StatusEffectManager.Cure(pi, StatusCondition.Confusion);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.Burn));
            Assert.That(pi.SecondaryStatus, Is.EqualTo(StatusCondition.None));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void CureAll_ClearsBothSlots()
        {
            // Per §4.2.7 — Full Heal clears primary + Confusion.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            StatusEffectManager.CureAll(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Assert.That(pi.SecondaryStatus, Is.EqualTo(StatusCondition.None));
            Assert.That(pi.PrimaryStatusTurnsRemaining, Is.EqualTo(0));
            Assert.That(pi.SecondaryStatusTurnsRemaining, Is.EqualTo(0));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void ClearOnCombatEnd_AutoClearsAllStatus()
        {
            // Per §4.2.1 — combat end clears statuses automatically.
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Poison, _config);
            StatusEffectManager.TryApply(pi, StatusCondition.Confusion, _config);
            StatusEffectManager.ClearOnCombatEnd(pi);
            Assert.That(pi.PrimaryStatus, Is.EqualTo(StatusCondition.None));
            Assert.That(pi.SecondaryStatus, Is.EqualTo(StatusCondition.None));
            Object.DestroyImmediate(sp);
        }

        // ── Bucket 7: CombatStatResolver integration (§4.2.2.1/2 + G8) (2) ────

        [Test]
        public void CombatStatResolver_Burn_ReducesEffectiveAttackBy25Percent()
        {
            // Per §4.2.2.1 + OPEN G8 — stat-stage first, then status mul.
            // Base 50 × stage 1.0 × burn 0.75 = 37 (floored from 37.5).
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Burn, _config);
            int eff = CombatStatResolver.EffectiveAttack(pi, _config);
            Assert.That(eff, Is.EqualTo(37));
            Object.DestroyImmediate(sp);
        }

        [Test]
        public void CombatStatResolver_Poison_ReducesEffectiveDefenseBy15Percent()
        {
            // Per §4.2.2.2 + OPEN G8 — Base 50 × stage 1.0 × poison 0.85 = 42.
            PokemonSpeciesSO sp = MakeSpecies(64, PokemonType.Water);
            PokemonInstance pi = MakeInstance(sp);
            StatusEffectManager.TryApply(pi, StatusCondition.Poison, _config);
            int eff = CombatStatResolver.EffectiveDefense(pi, _config);
            Assert.That(eff, Is.EqualTo(42));
            Object.DestroyImmediate(sp);
        }
    }
}
