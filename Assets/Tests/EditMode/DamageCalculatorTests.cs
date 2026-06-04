using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.2.5 — minimum 32 cases across the full multiplier matrix.
    // Each test references the GDD section that specifies its expected behaviour.
    // All operational defaults follow the OPEN flag block on Topic 4 §4.1.1.
    public class DamageCalculatorTests
    {
        // ── Test scaffolding ──────────────────────────────────────────────────

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
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_config);
        }

        private static PokemonSpeciesSO MakeSpecies(PokemonType primary, PokemonType? secondary, int atk, int def)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = secondary.HasValue
                ? new List<PokemonType> { primary, secondary.Value }
                : new List<PokemonType> { primary };
            s.BaseStats = new BaseStats { BaseAtk = atk, BaseDef = def, BaseHP = 50, BaseSpd = 50 };
            s.GrowthCurve = null;
            return s;
        }

        private static PokemonInstance MakeInstance(PokemonSpeciesSO species, int level = 1)
        {
            PokemonInstance pi = new PokemonInstance
            {
                Species = species,
                Level = level,
                CurrentHP = species.BaseStats.BaseHP
            };
            return pi;
        }

        private static MoveSO MakeMove(PokemonType type, int power, MoveRange range = MoveRange.Melee, bool alwaysCrit = false)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.Type = power < 0 ? type : type;
            m.BasePower = power;
            m.Range = range;
            m.RangeModifierMultiplier = range == MoveRange.Ranged ? 0.75f : 1.0f;
            m.AlwaysCrit = alwaysCrit;
            return m;
        }

        // ── Bucket 1: Power scaling (5) ───────────────────────────────────────

        [Test]
        public void Compute_PowerZero_FinalZero()
        {
            // Per §4.1.1 — Power=0 should yield 0 damage regardless of multipliers.
            var atkSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Normal, 0);

            var ctx = new MoveContext(MakeInstance(atkSpec), MakeInstance(defSpec), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(0));

            UnityEngine.Object.DestroyImmediate(atkSpec);
            UnityEngine.Object.DestroyImmediate(defSpec);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_Power50_AtkDefEqual_NeutralNoStabNoCrit_Final1()
        {
            // Per §4.1.1 — minimal canonical case: P=50, Atk=Def=50, Divisor=50
            // → BaseDamage = 50*1*1/50 = 1.0 → Final = 1.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 50); // attacker Normal, move Water → no STAB
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_Power100_LinearScale_Final2()
        {
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_Power200_Final4()
        {
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 200);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(4));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_Power1_AllNeutralFlooredToZero()
        {
            // Per OPEN G4 — no min-damage clamp. Sub-1.0 result floors to 0.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 1);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(0));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 2: Attack/Defense ratio (4) ────────────────────────────────

        [Test]
        public void Compute_DoubleAttack_DoublesBaseDamage()
        {
            var atk = MakeSpecies(PokemonType.Normal, null, 100, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            // 50 * 100/50 / 50 = 2.0 → Final = 2
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_DoubleDefense_HalvesBaseDamage()
        {
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 100);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            // 100 * 50/100 / 50 = 1.0 → Final = 1
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_DefenseFlooredToOne_NoDivByZero()
        {
            // Per CombatStatResolver — Defense clamped to ≥1 to avoid divide-by-zero.
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 0);
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.DoesNotThrow(() => DamageCalculator.Compute(ctx));
            Assert.That(DamageCalculator.Compute(ctx).EffectiveDefense, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_HighAtkLowDef_LargeDamage()
        {
            var atk = MakeSpecies(PokemonType.Normal, null, 200, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 25);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            // 100 * 200/25 / 50 = 16 → Final = 16
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(16));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 3: STAB (4) ────────────────────────────────────────────────

        [Test]
        public void Compute_STAB_AppliedWhenAttackerTypeMatchesMoveType()
        {
            // Per §4.1.2 — STAB ×1.5 when card type matches one of attacker's types.
            var atk = MakeSpecies(PokemonType.Water, null, 50, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.HasStab, Is.True);
            Assert.That(br.StabMultiplier, Is.EqualTo(1.5).Within(0.001));
            // 100 * 1/1 / 50 = 2.0; × 1.5 STAB = 3.0 → 3
            Assert.That(br.Final, Is.EqualTo(3));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_STAB_NotApplied_WhenTypeMismatch()
        {
            var atk = MakeSpecies(PokemonType.Fire, null, 50, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.HasStab, Is.False);
            Assert.That(br.StabMultiplier, Is.EqualTo(1.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_STAB_AppliedOnSecondaryType()
        {
            // Per §4.1.2 — STAB triggers on either of attacker's dual types.
            var atk = MakeSpecies(PokemonType.Grass, PokemonType.Poison, 50, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Poison, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).HasStab, Is.True);
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_NoSTAB_OnDualTypeMismatch()
        {
            var atk = MakeSpecies(PokemonType.Grass, PokemonType.Poison, 50, 50);
            var def = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Fire, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).HasStab, Is.False);
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 4: Crit (4) ────────────────────────────────────────────────

        [Test]
        public void Compute_Crit_Applies1_5xMultiplier()
        {
            // Per §4.1.1 + §4.1.3 — crit ×1.5 applied to base before STAB/TypeEff.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, true);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.IsCrit, Is.True);
            Assert.That(br.CritMultiplier, Is.EqualTo(1.5).Within(0.001));
            // 100*1/50 = 2.0 × 1.5 = 3.0 → 3
            Assert.That(br.Final, Is.EqualTo(3));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_NoCrit_LeavesMultiplierAt1()
        {
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.IsCrit, Is.False);
            Assert.That(br.CritMultiplier, Is.EqualTo(1.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_AlwaysCritMove_OverridesFalseInput()
        {
            // Per §4.1.3 — AlwaysCrit is independent of any crit-chance source;
            // it forces a 100% crit on the move.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100, MoveRange.Melee, alwaysCrit: true);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).IsCrit, Is.True);
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_Crit_AppliedBeforeStabTypeEffArithmetically()
        {
            // Per §4.1.1 + OPEN G3 — ordering is presentational only; multiplication
            // is commutative with a single floor at end. We assert that the total
            // multiplier product matches the spec multipliers regardless of order.
            var atk = MakeSpecies(PokemonType.Water, null, 50, 50);
            var def = MakeSpecies(PokemonType.Fire, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, true);
            var br = DamageCalculator.Compute(ctx);
            // base = 100*1/50 = 2.0; ×1.5 crit ×1.5 STAB ×2.0 typeEff = 9.0 → 9
            Assert.That(br.Final, Is.EqualTo(9));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 5: Type effectiveness (7) ──────────────────────────────────

        [Test]
        public void Compute_TypeEff_SuperEffective_2x()
        {
            // Per §4.1.2 — Water → Fire = 2.0×.
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50); // no STAB
            var def = MakeSpecies(PokemonType.Fire, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.TypeEffectiveness, Is.EqualTo(2.0).Within(0.001));
            Assert.That(br.Final, Is.EqualTo(4)); // 2.0 base × 2.0 type = 4
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_TypeEff_NotVeryEffective_0_5x()
        {
            // Per §4.1.2 — Fire → Water = 0.5×.
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Water, null, 50, 50);
            var move = MakeMove(PokemonType.Fire, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).TypeEffectiveness, Is.EqualTo(0.5).Within(0.001));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_TypeEff_Neutral_1x()
        {
            // Per §4.1.2 — Normal vs Normal = 1.0×.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100); // Water vs Normal = 1×
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).TypeEffectiveness, Is.EqualTo(1.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_TypeEff_DualType4x()
        {
            // Per §4.1.2 worked example 2 — Electric vs Water/Flying = 2.0××2.0× = 4.0×.
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Water, PokemonType.Flying, 50, 50);
            var move = MakeMove(PokemonType.Electric, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).TypeEffectiveness, Is.EqualTo(4.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_TypeEff_DualType0_25x()
        {
            // Per §4.1.2 — Grass attacking Bug/Poison = 0.5× × 0.5× = 0.25×.
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Bug, PokemonType.Poison, 50, 50);
            var move = MakeMove(PokemonType.Grass, 200);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).TypeEffectiveness, Is.EqualTo(0.25).Within(0.001));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_TypeEff_Immunity_FinalZero()
        {
            // Per §4.1.2 worked example 4 — Normal vs Ghost = 0× → Final = 0
            // regardless of any positive multipliers stacked.
            var atk = MakeSpecies(PokemonType.Normal, null, 999, 50);
            var def = MakeSpecies(PokemonType.Ghost, null, 50, 50);
            var move = MakeMove(PokemonType.Normal, 200, MoveRange.Melee, alwaysCrit: true);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, true);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.TypeEffectiveness, Is.EqualTo(0.0).Within(0.001));
            Assert.That(br.Final, Is.EqualTo(0));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_TypeEff_DualImmunity_Zero()
        {
            // Per §4.1.2 — immunity is multiplicative (0× × anything = 0×).
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Ghost, PokemonType.Poison, 50, 50);
            var move = MakeMove(PokemonType.Normal, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).TypeEffectiveness, Is.EqualTo(0.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 6: Range modifier (3) ──────────────────────────────────────

        [Test]
        public void Compute_MeleeRange_Multiplier1x()
        {
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100, MoveRange.Melee);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).RangeModifier, Is.EqualTo(1.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_RangedMove_Multiplier0_75x()
        {
            // Per §4.1.1 — Ranged moves take ×0.75 to base damage.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100, MoveRange.Ranged);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.RangeModifier, Is.EqualTo(0.75).Within(0.001));
            // 100 * 1/1 * 0.75 / 50 = 1.5 → floor → 1
            Assert.That(br.Final, Is.EqualTo(1));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_RangeBundledIntoBaseDamage_PerOpenG2()
        {
            // Per OPEN G2 — Range is folded into BaseDamage before Crit.
            var s = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100, MoveRange.Ranged);
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(s), move, _config, false);
            var br = DamageCalculator.Compute(ctx);
            // BaseDamage = (100 * 1/1 * 0.75) / 50 = 1.5
            Assert.That(br.BaseDamage, Is.EqualTo(1.5).Within(0.001));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 7: Stat stages (4) ─────────────────────────────────────────

        [Test]
        public void Compute_AttackerStage_Plus2_AttackDoubled()
        {
            // Per §4.2.6 — stage +2 = ×2.0 multiplier on Attack.
            var atkSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var atkInst = MakeInstance(atkSpec);
            atkInst.StatStages[Stat.Attack] = 2;
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(atkInst, MakeInstance(defSpec), move, _config, false);
            // EffAtk = floor(50 * 2.0) = 100
            Assert.That(DamageCalculator.Compute(ctx).EffectiveAttack, Is.EqualTo(100));
            UnityEngine.Object.DestroyImmediate(atkSpec);
            UnityEngine.Object.DestroyImmediate(defSpec);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_DefenderStage_Minus1_DefenseReduced()
        {
            // Per §4.2.6 — stage -1 = ×0.67 multiplier on Defense.
            var atkSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defInst = MakeInstance(defSpec);
            defInst.StatStages[Stat.Defense] = -1;
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(MakeInstance(atkSpec), defInst, move, _config, false);
            // EffDef = floor(50 * 0.67) = 33
            Assert.That(DamageCalculator.Compute(ctx).EffectiveDefense, Is.EqualTo(33));
            UnityEngine.Object.DestroyImmediate(atkSpec);
            UnityEngine.Object.DestroyImmediate(defSpec);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_AttackerStage_Plus6_MaxMultiplier4x()
        {
            // Per §4.2.6 — stage +6 = ×4.0 multiplier (ladder top).
            var atkSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var atkInst = MakeInstance(atkSpec);
            atkInst.StatStages[Stat.Attack] = 6;
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(atkInst, MakeInstance(defSpec), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).EffectiveAttack, Is.EqualTo(200));
            UnityEngine.Object.DestroyImmediate(atkSpec);
            UnityEngine.Object.DestroyImmediate(defSpec);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // Per §4.4.5.1 — Normal Badge: +10% base stats, but ONLY for the side the player owns. The
        // caller (CombatController) passes the player's badges to the attacker side iff the attacker is
        // player-owned, and the target side iff the target is player-owned — so a player badge never
        // buffs an enemy's Atk/Def.
        [Test]
        public void Compute_NormalBadge_BoostsOnlyTheSideItIsPassedTo()
        {
            var atkSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(MakeInstance(atkSpec), MakeInstance(defSpec), move, _config, false);

            BadgeSO normal = ScriptableObject.CreateInstance<BadgeSO>();
            normal.BadgeId = "normal_badge";
            var badges = new System.Collections.Generic.List<BadgeSO> { normal };

            var none = DamageCalculator.Compute(ctx);
            Assert.That(none.EffectiveAttack, Is.EqualTo(50));
            Assert.That(none.EffectiveDefense, Is.EqualTo(50));

            var atkOnly = DamageCalculator.Compute(ctx, attackerBadges: badges, targetBadges: null);
            Assert.That(atkOnly.EffectiveAttack, Is.EqualTo(55), "+10% to the player attacker.");
            Assert.That(atkOnly.EffectiveDefense, Is.EqualTo(50), "Enemy target Def NOT buffed by a player badge.");

            var defOnly = DamageCalculator.Compute(ctx, attackerBadges: null, targetBadges: badges);
            Assert.That(defOnly.EffectiveAttack, Is.EqualTo(50), "Enemy attacker Atk NOT buffed by a player badge.");
            Assert.That(defOnly.EffectiveDefense, Is.EqualTo(55), "+10% to the player defender.");

            UnityEngine.Object.DestroyImmediate(atkSpec);
            UnityEngine.Object.DestroyImmediate(defSpec);
            UnityEngine.Object.DestroyImmediate(move);
            UnityEngine.Object.DestroyImmediate(normal);
        }

        [Test]
        public void Compute_AttackerStage_Plus99_ClampedToPlus6()
        {
            // Per §4.2.6 — stages clamped to [-6, +6]; values outside the ladder
            // resolve to the boundary multiplier.
            var atkSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var defSpec = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var atkInst = MakeInstance(atkSpec);
            atkInst.StatStages[Stat.Attack] = 99;
            var move = MakeMove(PokemonType.Water, 50);
            var ctx = new MoveContext(atkInst, MakeInstance(defSpec), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).EffectiveAttack, Is.EqualTo(200));
            UnityEngine.Object.DestroyImmediate(atkSpec);
            UnityEngine.Object.DestroyImmediate(defSpec);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 8: Determinism & invariants (3) ────────────────────────────

        [Test]
        public void Compute_Deterministic_IdenticalInputsYieldIdenticalOutputs()
        {
            // Per Engineering Pillar 3 — determinism is non-negotiable. No RNG in
            // the calculator; 100 calls with the same input must match exactly.
            var atk = MakeSpecies(PokemonType.Water, null, 73, 51);
            var def = MakeSpecies(PokemonType.Fire, null, 47, 63);
            var move = MakeMove(PokemonType.Water, 95);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, true);

            int first = DamageCalculator.Compute(ctx).Final;
            for (int i = 0; i < 100; i++)
                Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(first));

            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_PreviewMatchesCompute_PerTask4_2_2()
        {
            // Per Task 4.2.2 — Preview returns the same numbers as Compute.
            var atk = MakeSpecies(PokemonType.Water, null, 50, 50);
            var def = MakeSpecies(PokemonType.Fire, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);

            var live    = DamageCalculator.Compute(ctx);
            var preview = DamageCalculator.Preview(ctx);
            Assert.That(preview.Final, Is.EqualTo(live.Final));
            Assert.That(preview.BaseDamage, Is.EqualTo(live.BaseDamage));

            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_FloorAppliedOnlyAtEnd_PerTask4_2_4()
        {
            // Per Task 4.2.4 — intermediate values stay double; floor runs once.
            // A combo that would lose precision under repeated flooring still
            // resolves to the canonical product here.
            var s = MakeSpecies(PokemonType.Water, null, 50, 50);
            var def = MakeSpecies(PokemonType.Fire, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 33); // small base
            var ctx = new MoveContext(MakeInstance(s), MakeInstance(def), move, _config, true);
            // base = 33/50 = 0.66; × 1.5 crit × 1.5 STAB × 2.0 type = 2.97 → 2
            // If we floored after each step we'd get 0 ×… = 0. Single floor → 2.
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(2));
            UnityEngine.Object.DestroyImmediate(s);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        // ── Bucket 9: Full stack & worked-example replays (3) ─────────────────

        [Test]
        public void Compute_AllMultipliersStacked_ProductMatches()
        {
            // STAB × Crit × TypeEff × Range stacked. Multiplication is commutative
            // (OPEN G3) so the product is the spec-canonical value regardless of
            // internal ordering.
            var atk = MakeSpecies(PokemonType.Water, null, 100, 50);
            var def = MakeSpecies(PokemonType.Fire, null, 50, 50);
            var move = MakeMove(PokemonType.Water, 100, MoveRange.Ranged);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, true);
            // base = 100 * 100/50 * 0.75 / 50 = 3.0
            // ×1.5 crit ×1.5 stab ×2.0 type = 13.5 → 13
            Assert.That(DamageCalculator.Compute(ctx).Final, Is.EqualTo(13));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_SpecExample1_FireVsGrassPoison_2xTypeEff()
        {
            // Per §4.1.2 worked example 1 — Fire vs Grass/Poison → 2.0× type total.
            var atk = MakeSpecies(PokemonType.Normal, null, 50, 50);
            var def = MakeSpecies(PokemonType.Grass, PokemonType.Poison, 50, 50);
            var move = MakeMove(PokemonType.Fire, 100);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, false);
            Assert.That(DamageCalculator.Compute(ctx).TypeEffectiveness, Is.EqualTo(2.0).Within(0.001));
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }

        [Test]
        public void Compute_BreakdownFieldsPopulated()
        {
            // Per Task 4.2.1 — DamageBreakdown surfaces every component for the UI
            // panel and tests. Sanity check that nothing is left at default.
            var atk = MakeSpecies(PokemonType.Water, null, 60, 50);
            var def = MakeSpecies(PokemonType.Fire, null, 40, 70);
            var move = MakeMove(PokemonType.Water, 80, MoveRange.Ranged);
            var ctx = new MoveContext(MakeInstance(atk), MakeInstance(def), move, _config, true);
            var br = DamageCalculator.Compute(ctx);
            Assert.That(br.Power, Is.EqualTo(80));
            Assert.That(br.EffectiveAttack, Is.EqualTo(60));
            Assert.That(br.EffectiveDefense, Is.EqualTo(70));
            Assert.That(br.RangeModifier, Is.EqualTo(0.75).Within(0.001));
            Assert.That(br.StabMultiplier, Is.EqualTo(1.5).Within(0.001));
            Assert.That(br.CritMultiplier, Is.EqualTo(1.5).Within(0.001));
            Assert.That(br.TypeEffectiveness, Is.EqualTo(2.0).Within(0.001));
            Assert.That(br.IsCrit, Is.True);
            Assert.That(br.HasStab, Is.True);
            UnityEngine.Object.DestroyImmediate(atk);
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(move);
        }
    }
}
