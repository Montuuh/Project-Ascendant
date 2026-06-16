using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §6.8 + Epic 11 Task 11.6 — difficulty-modifier aggregation: multiplicative XP stacking
    // (§6.8.3), enemy-stat product, hide-intents OR, route-branch min.
    public class DifficultyModifiersTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private DifficultyModifierSO Mod(float xp = 1f, float enemy = 1f, bool hide = false, int routes = 3)
        {
            DifficultyModifierSO m = ScriptableObject.CreateInstance<DifficultyModifierSO>();
            m.TrainerXPMultiplier = xp; m.EnemyStatMultiplier = enemy;
            m.HideAllEnemyIntents = hide; m.MaxRouteBranchChoices = routes;
            _disposables.Add(m);
            return m;
        }

        [Test]
        public void XPMultiplier_None_IsOne()
        {
            Assert.That(DifficultyModifiers.XPMultiplier(null), Is.EqualTo(1f));
            Assert.That(DifficultyModifiers.XPMultiplier(new List<DifficultyModifierSO>()), Is.EqualTo(1f));
        }

        [Test]
        public void XPMultiplier_Stacks_Multiplicatively()
        {
            // §6.8.3 — ×1.15 and ×1.20 → ×1.38.
            List<DifficultyModifierSO> active = new() { Mod(xp: 1.15f), Mod(xp: 1.20f) };
            Assert.That(DifficultyModifiers.XPMultiplier(active), Is.EqualTo(1.38f).Within(0.0001f));
        }

        [Test]
        public void ApplyXP_FloorsScaledValue()
        {
            List<DifficultyModifierSO> one = new() { Mod(xp: 1.15f) };
            Assert.That(DifficultyModifiers.ApplyXP(100, one), Is.EqualTo(115));   // 100×1.15
            List<DifficultyModifierSO> two = new() { Mod(xp: 1.15f), Mod(xp: 1.20f) };
            Assert.That(DifficultyModifiers.ApplyXP(100, two), Is.EqualTo(138));   // 100×1.38
            Assert.That(DifficultyModifiers.ApplyXP(0, two), Is.EqualTo(0));
            Assert.That(DifficultyModifiers.ApplyXP(55, null), Is.EqualTo(55));    // baseline
        }

        [Test]
        public void EnemyStat_HideIntents_RouteCap_Aggregate()
        {
            List<DifficultyModifierSO> active = new() { Mod(enemy: 1.20f), Mod(hide: true, routes: 1) };
            Assert.That(DifficultyModifiers.EnemyStatMultiplier(active), Is.EqualTo(1.20f).Within(0.0001f));
            Assert.That(DifficultyModifiers.HidesIntents(active), Is.True);
            Assert.That(DifficultyModifiers.MaxRouteBranches(active), Is.EqualTo(1), "tightest cap wins.");

            Assert.That(DifficultyModifiers.HidesIntents(null), Is.False);
            Assert.That(DifficultyModifiers.MaxRouteBranches(null), Is.EqualTo(3), "baseline.");
        }

        // ── #44 — runtime application to a CombatSetup ───────────────────────────────

        private static PokemonInstance Enemy(int baseHP)
        {
            PokemonSpeciesSO sp = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            sp.SpeciesId = "dummy";
            sp.BaseStats = new BaseStats { BaseHP = baseHP, BaseAtk = 40, BaseDef = 40, BaseSpd = 40 };
            sp.GrowthCurve = null;
            return new PokemonInstance { Species = sp, Level = 1, CurrentHP = baseHP, MaxHPMultiplier = 1f };
        }

        [Test]
        public void ApplyDifficultyModifiers_IronWill_ScalesEnemyMaxHpAndRefills()
        {
            // Per §6.8.2 (#44) — Iron Will (EnemyStatMultiplier 1.20) scales the enemy's Max HP
            // and refills CurrentHP to the new full.
            PokemonInstance enemy = Enemy(100);
            CombatController.CombatSetup setup = new()
            {
                EnemyTeam = new List<PokemonInstance> { enemy },
                HideBaselineIntents = false,
            };
            List<DifficultyModifierSO> active = new() { Mod(enemy: 1.20f) };

            setup = CombatController.ApplyDifficultyModifiers(setup, active);

            Assert.That(enemy.MaxHPMultiplier, Is.EqualTo(1.20f).Within(0.0001f));
            Assert.That(PokemonVitals.MaxHP(enemy), Is.EqualTo(120), "100 × 1.20");
            Assert.That(enemy.CurrentHP, Is.EqualTo(120), "refilled to scaled full HP");
            Assert.That(setup.HideBaselineIntents, Is.False, "Iron Will does not hide intents");
        }

        [Test]
        public void ApplyDifficultyModifiers_DenseFog_HidesBaselineIntents()
        {
            // Per §6.8.2 (#44) — Dense Fog (HideAllEnemyIntents) forces HideBaselineIntents on
            // a wild/trainer setup and leaves enemy HP unscaled.
            PokemonInstance enemy = Enemy(100);
            CombatController.CombatSetup setup = new()
            {
                EnemyTeam = new List<PokemonInstance> { enemy },
                HideBaselineIntents = false,
            };
            List<DifficultyModifierSO> active = new() { Mod(hide: true) };

            setup = CombatController.ApplyDifficultyModifiers(setup, active);

            Assert.That(setup.HideBaselineIntents, Is.True);
            Assert.That(enemy.MaxHPMultiplier, Is.EqualTo(1f).Within(0.0001f), "Dense Fog does not scale HP");
        }

        [Test]
        public void ApplyDifficultyModifiers_None_LeavesSetupUnchanged()
        {
            PokemonInstance enemy = Enemy(100);
            CombatController.CombatSetup setup = new()
            {
                EnemyTeam = new List<PokemonInstance> { enemy },
                HideBaselineIntents = false,
            };

            setup = CombatController.ApplyDifficultyModifiers(setup, null);

            Assert.That(enemy.MaxHPMultiplier, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(enemy.CurrentHP, Is.EqualTo(100));
            Assert.That(setup.HideBaselineIntents, Is.False);
        }
    }
}
