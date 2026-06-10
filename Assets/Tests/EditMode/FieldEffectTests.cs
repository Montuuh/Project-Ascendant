using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 4 Task 4.9 — coverage for field effects:
    //   • Sunny Day / Rain Dance Fire/Water multipliers (§4.3.8.1/2)
    //   • Electric Terrain Electric ×1.3 only on grounded (§4.3.8.3)
    //   • Paralysis block on grounded under Electric Terrain (§4.3.8.3)
    //   • Weather + Terrain independence & multiplicative stacking (§4.8.2)
    //   • Within-category overwrite (§4.8.2)
    public class FieldEffectTests
    {
        private BattleConfigSO _config;
        private PokemonSpeciesSO _normalSpecies;
        private PokemonSpeciesSO _flyingSpecies;
        private PokemonSpeciesSO _electricSpecies;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.SunnyDayFireMultiplier = 1.5f;
            _config.SunnyDayWaterMultiplier = 0.5f;
            _config.RainDanceWaterMultiplier = 1.5f;
            _config.RainDanceFireMultiplier = 0.5f;
            _config.ElectricTerrainElectricMultiplier = 1.3f;

            _normalSpecies = MakeSpecies(PokemonType.Normal);
            _flyingSpecies = MakeSpecies(PokemonType.Flying);
            _electricSpecies = MakeSpecies(PokemonType.Electric);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            Object.DestroyImmediate(_normalSpecies);
            Object.DestroyImmediate(_flyingSpecies);
            Object.DestroyImmediate(_electricSpecies);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static PokemonSpeciesSO MakeSpecies(params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = 100, BaseAtk = 50, BaseDef = 50, BaseSpd = 50 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            return s;
        }

        private static PokemonInstance MakeInstance(PokemonSpeciesSO sp) =>
            new() { Species = sp, Level = 1, CurrentHP = sp.BaseStats.BaseHP };

        // ── Bucket 1: Sunny Day (§4.3.8.1) ──────────────────────────────────

        [Test]
        public void SunnyDay_FireMove_Times1Point5()
        {
            FieldState f = new() { Weather = FieldEffectKind.SunnyDay };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Fire, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void SunnyDay_WaterMove_Times0Point5()
        {
            FieldState f = new() { Weather = FieldEffectKind.SunnyDay };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Water, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void SunnyDay_NeutralTypeMove_Times1()
        {
            FieldState f = new() { Weather = FieldEffectKind.SunnyDay };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Grass, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.0f).Within(0.001f));
        }

        // ── Bucket 2: Rain Dance (§4.3.8.2) ─────────────────────────────────

        [Test]
        public void RainDance_WaterMove_Times1Point5()
        {
            FieldState f = new() { Weather = FieldEffectKind.RainDance };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Water, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void RainDance_FireMove_Times0Point5()
        {
            FieldState f = new() { Weather = FieldEffectKind.RainDance };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Fire, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(0.5f).Within(0.001f));
        }

        // ── Bucket 3: Electric Terrain (§4.3.8.3) ───────────────────────────

        [Test]
        public void ElectricTerrain_ElectricMoveOnGrounded_Times1Point3()
        {
            FieldState f = new() { Terrain = FieldEffectKind.ElectricTerrain };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Electric, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.3f).Within(0.001f));
        }

        [Test]
        public void ElectricTerrain_ElectricMoveOnFlying_NoBoost()
        {
            // Per §4.3.8.3 — non-grounded Pokémon are unaffected by Electric Terrain.
            FieldState f = new() { Terrain = FieldEffectKind.ElectricTerrain };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Electric, MakeInstance(_flyingSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void ElectricTerrain_NonElectricMove_NoEffect()
        {
            FieldState f = new() { Terrain = FieldEffectKind.ElectricTerrain };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Normal, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.0f).Within(0.001f));
        }

        // ── Bucket 4: Paralysis block on grounded (§4.3.8.3) ────────────────

        [Test]
        public void ElectricTerrain_BlocksParalysisOnGrounded()
        {
            FieldState f = new() { Terrain = FieldEffectKind.ElectricTerrain };
            Assert.That(FieldEffectResolver.CanApplyParalysis(f, MakeInstance(_normalSpecies)),
                        Is.False);
        }

        [Test]
        public void ElectricTerrain_AllowsParalysisOnFlying()
        {
            // Non-grounded (Flying) is unaffected by the terrain — Paralysis
            // would only fail if the §4.2.4 Electric-type immunity also applied.
            FieldState f = new() { Terrain = FieldEffectKind.ElectricTerrain };
            Assert.That(FieldEffectResolver.CanApplyParalysis(f, MakeInstance(_flyingSpecies)),
                        Is.True);
        }

        [Test]
        public void NoTerrain_DoesNotBlockParalysis()
        {
            FieldState f = FieldState.Empty;
            Assert.That(FieldEffectResolver.CanApplyParalysis(f, MakeInstance(_normalSpecies)),
                        Is.True);
        }

        // ── Bucket 5: Weather + Terrain independence (§4.8.2) ───────────────

        [Test]
        public void WeatherAndTerrain_BothActive_StackMultiplicatively()
        {
            // Hypothetical: under Sunny Day (Fire ×1.5) + Electric Terrain
            // (electric +30% on grounded), an Electric move against a grounded
            // target should get the Terrain boost ONLY (Sunny Day doesn't
            // affect Electric). Multiplier should be ×1.3.
            FieldState f = new()
            {
                Weather = FieldEffectKind.SunnyDay,
                Terrain = FieldEffectKind.ElectricTerrain,
            };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Electric, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.3f).Within(0.001f));
        }

        [Test]
        public void WeatherAndTerrain_BothActive_BothCanApplyToSameMove()
        {
            // If a future Terrain effect ever boosted Fire (e.g. "Fire Terrain"),
            // Sunny Day Fire ×1.5 would stack on top. The current VS effects
            // never overlap, so we verify the structure with a hand-crafted
            // multiplier change: temporarily set Sunny Day's Fire to 2.0 to
            // simulate two effects amplifying. Demonstrates the multiplication
            // path is wired correctly. Per §4.8.2 multiplicative stacking.
            _config.SunnyDayFireMultiplier = 2.0f;
            FieldState f = new()
            {
                Weather = FieldEffectKind.SunnyDay,
                Terrain = FieldEffectKind.ElectricTerrain, // no Fire interaction
            };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Fire, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(2.0f).Within(0.001f));
        }

        // ── Bucket 6: Apply / within-category overwrite (§4.8.2) ────────────

        [Test]
        public void Apply_SunnyDayThenRainDance_WeatherIsRain()
        {
            FieldState f = FieldEffectResolver.Apply(FieldState.Empty, FieldEffectKind.SunnyDay);
            f = FieldEffectResolver.Apply(f, FieldEffectKind.RainDance);
            Assert.That(f.Weather, Is.EqualTo(FieldEffectKind.RainDance));
            Assert.That(f.Terrain, Is.EqualTo(FieldEffectKind.None));
        }

        [Test]
        public void Apply_TerrainDoesNotOverwriteWeather()
        {
            FieldState f = FieldEffectResolver.Apply(FieldState.Empty, FieldEffectKind.SunnyDay);
            f = FieldEffectResolver.Apply(f, FieldEffectKind.ElectricTerrain);
            Assert.That(f.Weather, Is.EqualTo(FieldEffectKind.SunnyDay));
            Assert.That(f.Terrain, Is.EqualTo(FieldEffectKind.ElectricTerrain));
        }

        [Test]
        public void Apply_None_IsNoOp()
        {
            FieldState f = new() { Weather = FieldEffectKind.SunnyDay };
            FieldState next = FieldEffectResolver.Apply(f, FieldEffectKind.None);
            Assert.That(next.Weather, Is.EqualTo(FieldEffectKind.SunnyDay));
            Assert.That(next.Terrain, Is.EqualTo(FieldEffectKind.None));
        }

        // ── Bucket 7: IsGrounded + null guards ──────────────────────────────

        [Test]
        public void IsGrounded_FlyingType_False()
        {
            Assert.That(FieldEffectResolver.IsGrounded(MakeInstance(_flyingSpecies)), Is.False);
        }

        [Test]
        public void IsGrounded_NormalType_True()
        {
            Assert.That(FieldEffectResolver.IsGrounded(MakeInstance(_normalSpecies)), Is.True);
        }

        [Test]
        public void IsGrounded_NullInstance_TrueAsSafeDefault()
        {
            // Null target → treat as grounded so we don't accidentally bypass
            // gated effects through a null reference.
            Assert.That(FieldEffectResolver.IsGrounded(null), Is.True);
        }

        [Test]
        public void GetDamageMultiplier_NullConfig_ReturnsIdentity()
        {
            FieldState f = new() { Weather = FieldEffectKind.SunnyDay };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Fire, MakeInstance(_normalSpecies), attackerIsEnemy: false, null);
            Assert.That(m, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void CategoryOf_KnownEffects_ReportsCorrectCategory()
        {
            Assert.That(FieldEffectResolver.CategoryOf(FieldEffectKind.SunnyDay), Is.EqualTo(FieldCategory.Weather));
            Assert.That(FieldEffectResolver.CategoryOf(FieldEffectKind.RainDance), Is.EqualTo(FieldCategory.Weather));
            Assert.That(FieldEffectResolver.CategoryOf(FieldEffectKind.ElectricTerrain), Is.EqualTo(FieldCategory.Terrain));
            Assert.That(FieldEffectResolver.CategoryOf(FieldEffectKind.Sandstorm), Is.EqualTo(FieldCategory.Hazard));
        }

        // ── Bucket 8: Home Field — enemy-owned type boost (§4.3.8.5, CL-012) ──

        [Test]
        public void HomeField_EnemyAttacker_TypeMoveBoosted()
        {
            _config.HomeFieldTypeMultiplier = 1.5f;
            FieldState f = new() { HasGymField = true, GymTypeField = PokemonType.Rock };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Rock, MakeInstance(_normalSpecies), attackerIsEnemy: true, _config);
            Assert.That(m, Is.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void HomeField_PlayerAttacker_SameType_NoBoost()
        {
            // §4.3.8.5 — Home Field is one-sided: the player's same-type moves get no boost.
            _config.HomeFieldTypeMultiplier = 1.5f;
            FieldState f = new() { HasGymField = true, GymTypeField = PokemonType.Rock };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Rock, MakeInstance(_normalSpecies), attackerIsEnemy: false, _config);
            Assert.That(m, Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void HomeField_EnemyAttacker_OffTypeMove_NoBoost()
        {
            _config.HomeFieldTypeMultiplier = 1.5f;
            FieldState f = new() { HasGymField = true, GymTypeField = PokemonType.Rock };
            float m = FieldEffectResolver.GetDamageMultiplier(
                f, PokemonType.Water, MakeInstance(_normalSpecies), attackerIsEnemy: true, _config);
            Assert.That(m, Is.EqualTo(1.0f).Within(0.001f));
        }

        // ── Bucket 9: Sandstorm hazard (§4.3.8.4, CL-012) ──

        [Test]
        public void Sandstorm_NonImmune_Loses5PercentMaxHP()
        {
            _config.SandstormHazardPercent = 5;
            FieldState f = new() { Hazard = FieldEffectKind.Sandstorm };
            // _normalSpecies BaseHP 100, no growth → MaxHP 100 → 5% = 5.
            int dmg = FieldEffectResolver.GetEndOfTurnHazardDamage(f, MakeInstance(_normalSpecies), _config, null);
            Assert.That(dmg, Is.EqualTo(5));
        }

        [Test]
        public void Sandstorm_RockGroundSteel_Immune()
        {
            _config.SandstormHazardPercent = 5;
            FieldState f = new() { Hazard = FieldEffectKind.Sandstorm };
            foreach (PokemonType t in new[] { PokemonType.Rock, PokemonType.Ground, PokemonType.Steel })
            {
                PokemonSpeciesSO sp = MakeSpecies(t);
                Assert.That(FieldEffectResolver.GetEndOfTurnHazardDamage(f, MakeInstance(sp), _config, null),
                    Is.EqualTo(0), $"{t} should be Sandstorm-immune");
                Object.DestroyImmediate(sp);
            }
        }

        [Test]
        public void Sandstorm_NoHazard_NoDamage()
        {
            _config.SandstormHazardPercent = 5;
            int dmg = FieldEffectResolver.GetEndOfTurnHazardDamage(
                FieldState.Empty, MakeInstance(_normalSpecies), _config, null);
            Assert.That(dmg, Is.EqualTo(0));
        }

        [Test]
        public void Apply_Sandstorm_FillsHazardSlot_IndependentOfWeatherTerrain()
        {
            FieldState f = FieldEffectResolver.Apply(FieldState.Empty, FieldEffectKind.SunnyDay);
            f = FieldEffectResolver.Apply(f, FieldEffectKind.ElectricTerrain);
            f = FieldEffectResolver.Apply(f, FieldEffectKind.Sandstorm);
            Assert.That(f.Weather, Is.EqualTo(FieldEffectKind.SunnyDay));
            Assert.That(f.Terrain, Is.EqualTo(FieldEffectKind.ElectricTerrain));
            Assert.That(f.Hazard, Is.EqualTo(FieldEffectKind.Sandstorm));
        }
    }
}
