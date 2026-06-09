using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §4.3.5 (CL-011 / Option B) — intent-hiding behaviour.
    //
    // Coverage:
    //   Wild / Trainer (HideBaselineIntents false): intents always Witnessed from turn 1.
    //   Elite / Gym   (HideBaselineIntents true):  first intent per enemy is Hidden.
    //   After fire: enemy added to WitnessedEnemies; subsequent intents Witnessed.
    //   Dense Fog extension: same flag set on Wild setup → Wild intent Hidden.
    //   Keen Eye ability: TeamRevealsIntents overrides Hidden → Witnessed.
    public class IntentHidingTests
    {
        private BattleConfigSO _config;
        private readonly List<Object> _disposables = new();

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
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 5;
            _config.BaseConsumableCardsPerTurn = 2;
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
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.AggressiveSelfMultiplier = 1.5f;
            _config.LowSelfHPThreshold = 0.40f;
            _config.SetupSelfMultiplier = 1.5f;
            _config.HighSelfHPThreshold = 0.70f;
            _config.RandomnessFloorChance = 0f;
            _config.BossCounterIntelTopPenalty = 0.7f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private PokemonSpeciesSO MakeSpecies(int hp = 100, int atk = 50, int def = 50)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 50 };
            s.StatusImmunities = new List<StatusCondition>();
            _disposables.Add(s);
            return s;
        }

        private MoveSO MakeAttackMove(int power = 40)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.Type = PokemonType.Normal;
            m.BasePower = power;
            m.APCost = 1;
            m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private AbilitySO MakeAbility(string abilityId)
        {
            AbilitySO a = ScriptableObject.CreateInstance<AbilitySO>();
            a.AbilityId = abilityId;
            _disposables.Add(a);
            return a;
        }

        private PokemonInstance MakeMon(PokemonSpeciesSO sp, MoveSO move, AbilitySO ability = null)
        {
            PokemonInstance p = new()
            {
                Species = sp,
                Level = 1,
                CurrentHP = sp.BaseStats.BaseHP,
                Ability = ability,
            };
            p.CurrentMoves.Add(move);
            return p;
        }

        private CombatController BuildController(
            PokemonInstance player,
            PokemonInstance enemy,
            bool hideBaselineIntents)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                Config = _config,
                HideBaselineIntents = hideBaselineIntents,
            };
            return new CombatController(setup, new PassAgent(), null);
        }

        // Passive agent: never plays cards, always ends turn.
        private sealed class PassAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s, IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        // Per §4.3.5 (CL-011/Option B) — Wild/Trainer baseline: HideBaselineIntents=false.
        // All enemy intents must be Witnessed from turn 1.
        [Test]
        public void IntentPhase_HideBaselineFalse_IntentIsWitnessed()
        {
            PokemonInstance player = MakeMon(MakeSpecies(hp: 500), MakeAttackMove());
            PokemonInstance enemy  = MakeMon(MakeSpecies(), MakeAttackMove());

            CombatController ctrl = BuildController(player, enemy, hideBaselineIntents: false);
            ctrl.Start();
            ctrl.IntentPhase();

            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.Not.EqualTo(IntentReveal.Hidden),
                "Wild/Trainer encounters must not start with Hidden intents.");
        }

        // Per §4.3.5 (CL-011/Option B) — Elite/Gym baseline: HideBaselineIntents=true.
        // Enemy's first intent must be Hidden (❓).
        [Test]
        public void IntentPhase_HideBaselineTrue_FirstIntentIsHidden()
        {
            PokemonInstance player = MakeMon(MakeSpecies(hp: 500), MakeAttackMove());
            PokemonInstance enemy  = MakeMon(MakeSpecies(), MakeAttackMove());

            CombatController ctrl = BuildController(player, enemy, hideBaselineIntents: true);
            ctrl.Start();
            ctrl.IntentPhase();

            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.EqualTo(IntentReveal.Hidden),
                "Elite/Gym encounters must start with Hidden intents.");
        }

        // Per §4.3.5 (CL-011) — Witnessed tier: once the enemy is in WitnessedEnemies
        // (i.e., has fired at least once), their next intent is Witnessed.
        [Test]
        public void IntentPhase_EnemyWitnessed_IntentBecomesWitnessed()
        {
            PokemonInstance player = MakeMon(MakeSpecies(hp: 500), MakeAttackMove());
            PokemonInstance enemy  = MakeMon(MakeSpecies(), MakeAttackMove());

            CombatController ctrl = BuildController(player, enemy, hideBaselineIntents: true);
            ctrl.Start();
            ctrl.IntentPhase();

            // Simulate the enemy having fired (as ExecuteEnemyIntent would do).
            ctrl.State.WitnessedEnemies.Add(ctrl.State.EnemyTeam[0]);
            ctrl.IntentPhase();

            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.Not.EqualTo(IntentReveal.Hidden),
                "After a Witnessed enemy fires, their subsequent intents must not be Hidden.");
        }

        // Per §4.3.5 (CL-011) — Dense Fog extension: the run layer sets
        // HideBaselineIntents=true even on Wild encounters when Dense Fog is active.
        // Behaviour is identical to Elite/Gym — Hidden until Witnessed.
        [Test]
        public void IntentPhase_DenseFogOnWild_IntentIsHidden()
        {
            PokemonInstance player = MakeMon(MakeSpecies(hp: 500), MakeAttackMove());
            PokemonInstance enemy  = MakeMon(MakeSpecies(), MakeAttackMove());

            // Caller sets flag based on DifficultyModifiers.HidesIntents(); we test the effect directly.
            CombatController ctrl = BuildController(player, enemy, hideBaselineIntents: true);
            ctrl.Start();
            ctrl.IntentPhase();

            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.EqualTo(IntentReveal.Hidden),
                "Dense Fog (HideBaselineIntents=true on Wild) must produce Hidden intents.");
        }

        // Per §5.5.3.1 (CL-011) — Keen Eye overrides Hidden: even with HideBaselineIntents=true,
        // a team member holding Keen Eye reveals all Hidden intents to Witnessed.
        [Test]
        public void IntentPhase_KeenEyeHolder_HiddenIntentRevealedToWitnessed()
        {
            AbilitySO keenEye = MakeAbility("keen_eye");
            PokemonInstance player = MakeMon(MakeSpecies(hp: 500), MakeAttackMove(), ability: keenEye);
            PokemonInstance enemy  = MakeMon(MakeSpecies(), MakeAttackMove());

            CombatController ctrl = BuildController(player, enemy, hideBaselineIntents: true);
            ctrl.Start();
            ctrl.IntentPhase();

            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.Not.EqualTo(IntentReveal.Hidden),
                "Keen Eye must override Hidden intents to Witnessed even in Elite/Gym/Dense Fog.");
        }

        // Per §4.3.5 (CL-011) — WitnessedEnemies is enemy-specific: when there are
        // two enemies and only one has fired, only that one's next intent is Witnessed.
        [Test]
        public void IntentPhase_TwoEnemies_OnlyWitnessedOneRevealed()
        {
            PokemonInstance player  = MakeMon(MakeSpecies(hp: 500), MakeAttackMove());
            PokemonInstance enemy0  = MakeMon(MakeSpecies(), MakeAttackMove());
            PokemonInstance enemy1  = MakeMon(MakeSpecies(), MakeAttackMove());

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy0, enemy1 },
                ConsumableInventory = new List<ConsumableSO>(),
                Config = _config,
                HideBaselineIntents = true,
            };
            CombatController ctrl = new(setup, new PassAgent(), null);
            ctrl.Start();
            ctrl.IntentPhase();

            // Both start Hidden.
            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.EqualTo(IntentReveal.Hidden));
            Assert.That(ctrl.State.EnemyIntents[1].Reveal, Is.EqualTo(IntentReveal.Hidden));

            // Only enemy0 fires.
            ctrl.State.WitnessedEnemies.Add(ctrl.State.EnemyTeam[0]);
            ctrl.IntentPhase();

            Assert.That(ctrl.State.EnemyIntents[0].Reveal, Is.Not.EqualTo(IntentReveal.Hidden),
                "enemy0 (Witnessed) must have revealed intent.");
            Assert.That(ctrl.State.EnemyIntents[1].Reveal, Is.EqualTo(IntentReveal.Hidden),
                "enemy1 (not yet Witnessed) must still be Hidden.");
        }
    }
}
