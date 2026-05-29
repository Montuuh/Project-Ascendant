using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §4.4.5.1 + Epic 8 Task 8.5.7 — Boulder Badge: "Your Lead Pokémon
    // reduces all incoming damage by 1 (minimum 0)." Driven through a real
    // enemy attack resolving onto the player Lead (ResolutionPhase).
    public class BoulderBadgeTests
    {
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BattleConfigSO>();
            _config.Divisor = 10; // visible multi-point damage at L1
            _config.StabMultiplier = 1.5f;
            _config.CritMultiplier = 1.5f;
            _config.MeleeModifier = 1.0f;
            _config.RangedModifier = 0.75f;
            _config.StatStageMultipliers = new float[]
            { 0.25f,0.29f,0.33f,0.40f,0.50f,0.67f,1.00f,1.50f,2.00f,2.50f,3.00f,3.50f,4.00f };
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 4;
            _config.BaseConsumableCardsPerTurn = 0;
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.AggressiveSelfMultiplier = 1.5f;
            _config.LowSelfHPThreshold = 0.40f;
            _config.SetupSelfMultiplier = 1.5f;
            _config.HighSelfHPThreshold = 0.70f;
            _config.RandomnessFloorChance = 0f;
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

        private PokemonSpeciesSO Species(int hp, int atk, int def)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "sp";
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 40 };
            s.GrowthCurve = null;
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private MoveSO Mk(int power)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "atk"; m.Type = PokemonType.Normal; m.BasePower = power; m.APCost = 1;
            m.Role = MoveRole.Offensive; m.Range = MoveRange.Melee; m.Modifier = PositionalModifier.None;
            m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private BadgeSO Badge(int reduction)
        {
            BadgeSO b = ScriptableObject.CreateInstance<BadgeSO>();
            b.BadgeId = "boulder"; b.DisplayName = "Boulder";
            b.LeadIncomingDamageReduction = reduction;
            _disposables.Add(b);
            return b;
        }

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        // Runs one enemy turn against the player Lead and returns Lead HP lost.
        private int LeadDamageTaken(int enemyMovePower, List<BadgeSO> badges, int leadDef)
        {
            PokemonSpeciesSO enemySp = Species(100, 60, 40);
            MoveSO atk = Mk(enemyMovePower);
            PokemonInstance enemy = new() { Species = enemySp, Level = 1, CurrentHP = 100 };
            enemy.CurrentMoves.Add(atk);
            PokemonInstance lead = new() { Species = Species(500, 40, leadDef), Level = 1, CurrentHP = 500 };

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(0xB0DEu),
                ActiveBadges = badges,
            };
            CombatController c = new(setup, new PassiveAgent());
            c.Start();
            c.DrawPhase();
            c.IntentPhase();
            c.ResolutionPhase(); // enemy intent (Attack on Lead slot 0) resolves
            return 500 - lead.CurrentHP;
        }

        [Test]
        public void BoulderBadge_LeadTakesOneLessDamage()
        {
            int noBadge = LeadDamageTaken(40, null, leadDef: 40);
            int withBadge = LeadDamageTaken(40, new List<BadgeSO> { Badge(1) }, leadDef: 40);
            Assert.That(noBadge, Is.GreaterThan(1), "Precondition: a multi-point hit.");
            Assert.That(withBadge, Is.EqualTo(noBadge - 1),
                "Per §4.4.5.1 — Boulder reduces incoming Lead damage by 1.");
        }

        [Test]
        public void BoulderBadge_ReducesOneDamageHitToZero()
        {
            // A 1-damage hit (non-immune floor) becomes 0 under Boulder
            // ("minimum 0" — Boulder applies AFTER the non-immune floor).
            int noBadge = LeadDamageTaken(1, null, leadDef: 400);
            int withBadge = LeadDamageTaken(1, new List<BadgeSO> { Badge(1) }, leadDef: 400);
            Assert.That(noBadge, Is.EqualTo(1), "Tiny hit floors to 1 without Boulder.");
            Assert.That(withBadge, Is.EqualTo(0), "Boulder reduces the 1-damage hit to 0.");
        }

        [Test]
        public void NoBadge_NoReduction()
        {
            int a = LeadDamageTaken(40, null, leadDef: 40);
            int b = LeadDamageTaken(40, new List<BadgeSO>(), leadDef: 40);
            Assert.That(a, Is.EqualTo(b), "Empty badge list → no reduction.");
        }

        [Test]
        public void BadgeWithZeroReduction_NoEffect()
        {
            int noBadge = LeadDamageTaken(40, null, leadDef: 40);
            int zeroBadge = LeadDamageTaken(40, new List<BadgeSO> { Badge(0) }, leadDef: 40);
            Assert.That(zeroBadge, Is.EqualTo(noBadge),
                "Only badges with LeadIncomingDamageReduction > 0 reduce damage.");
        }
    }
}
