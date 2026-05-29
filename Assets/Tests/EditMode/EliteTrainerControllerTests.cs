using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §7.5 + §4.4.3 + Epic 8 Task 8.4 — Elite Trainer coverage.
    //   • Bucket 1: Constructor + queue intake
    //   • Bucket 2: BuildCombatSetup — first spawn, PhaseCount stamped, learnset
    //   • Bucket 3: RequestReinforcements — sequential 2-Pokémon spawn
    //   • Bucket 4: ResolveReward — guaranteed Uncommon relic + flat XP/₽
    //   • Bucket 5: IntentScorer phase-aggression seam (unit)
    //   • Bucket 6: CombatController phase wiring — Phase 2 floor-disable
    //   • Bucket 7: full Elite combat integration (sequential + Victory + reward)
    public class EliteTrainerControllerTests
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
            _config.DefaultUtilityWeight = 50;
            _config.LowTargetHPMultiplier = 2.0f;
            _config.LowTargetHPThreshold = 0.30f;
            _config.AggressiveSelfMultiplier = 1.5f;
            _config.LowSelfHPThreshold = 0.40f;
            _config.SetupSelfMultiplier = 1.5f;
            _config.HighSelfHPThreshold = 0.70f;
            _config.RandomnessFloorChance = 0f;
            _config.BossCounterIntelTopPenalty = 0.7f;
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

        // ── Helpers ───────────────────────────────────────────────────────────

        private MoveSO MakeMove(PokemonType type, int power, int apCost = 1)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.Type = type;
            m.BasePower = power;
            m.APCost = apCost;
            m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonSpeciesSO MakeSpecies(int hp, int atk, int def, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "test_species";
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = atk, BaseDef = def, BaseSpd = 50 };
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private RelicSO MakeRelic(string id)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>();
            r.name = id;
            _disposables.Add(r);
            return r;
        }

        private EliteTrainerSO MakeElite(IEnumerable<ElitePokemonSlot> slots,
                                         RelicSO relic, int xp, int dollars)
        {
            EliteTrainerSO e = ScriptableObject.CreateInstance<EliteTrainerSO>();
            e.EliteId = "test_elite";
            e.DisplayName = "Test Elite";
            e.TacticalIdentity = "No type lock test elite.";
            e.Composition = new List<ElitePokemonSlot>(slots);
            e.GuaranteedRelic = relic;
            e.TrainerXPReward = xp;
            e.PokeDollarReward = dollars;
            _disposables.Add(e);
            return e;
        }

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                                            IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        // ── Bucket 1: Constructor + queue intake ─────────────────────────────

        [Test]
        public void Constructor_NullElite_DoesNotThrow_EmptyQueue()
        {
            EliteTrainerController ctrl = new(null, new PokemonInstanceFactory());
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(0));
            Assert.That(ctrl.SpawnsExecuted, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_TwoSlotComposition_QueuesBoth()
        {
            // Per §7.5.1 — Elite fields 2 Pokémon, sequential.
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
                new ElitePokemonSlot { Species = sp, Level = 13, PhaseCount = 2 },
            }, MakeRelic("r"), 25, 300);
            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(2));
        }

        // ── Bucket 2: BuildCombatSetup — PhaseCount stamped ──────────────────

        [Test]
        public void BuildCombatSetup_StampsPhaseCountOnMaterialisedPokemon()
        {
            // Per §4.4.3 / §7.5.1 — each Elite Pokémon is 2-phase. The authored
            // PhaseCount must reach PokemonInstance so BossPhaseTracker escalates.
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            sp.BaseLearnset.Add(MakeMove(PokemonType.Normal, 40));
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
            }, MakeRelic("r"), 25, 300);

            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            CombatController.CombatSetup setup = ctrl.BuildCombatSetup(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            Assert.That(setup.EnemyTeam[0].PhaseCount, Is.EqualTo(2));
            Assert.That(setup.Reinforcements, Is.SameAs(ctrl));
            Assert.That(ctrl.SpawnsExecuted, Is.EqualTo(1));
        }

        [Test]
        public void BuildCombatSetup_ZeroPhaseCount_FlooredToOne()
        {
            // Defensive: an unauthored (0) PhaseCount must behave as single-phase,
            // never as "0 phases".
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 0 },
            }, MakeRelic("r"), 25, 300);
            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            CombatController.CombatSetup setup = ctrl.BuildCombatSetup(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));
            Assert.That(setup.EnemyTeam[0].PhaseCount, Is.EqualTo(1));
        }

        // ── Bucket 3: RequestReinforcements ──────────────────────────────────

        [Test]
        public void RequestReinforcements_DequeuesSecond_WithItsPhaseCount()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
                new ElitePokemonSlot { Species = sp, Level = 13, PhaseCount = 2 },
            }, MakeRelic("r"), 25, 300);
            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            ctrl.BuildCombatSetup(new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            List<PokemonInstance> next = ctrl.RequestReinforcements(null);
            Assert.That(next.Count, Is.EqualTo(1));
            Assert.That(next[0].Level, Is.EqualTo(13));
            Assert.That(next[0].PhaseCount, Is.EqualTo(2));
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(0));
        }

        // ── Bucket 4: ResolveReward — guaranteed relic, no RNG ───────────────

        [Test]
        public void ResolveReward_OnVictory_GivesGuaranteedRelicAndFixedXPDollars()
        {
            // Per §7.5.1 / §7.12 — exactly one guaranteed relic, 25 XP, 300₽.
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            RelicSO relic = MakeRelic("uncommon_relic");
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
            }, relic, 25, 300);
            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());

            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Victory);
            Assert.That(bundle.TrainerXP, Is.EqualTo(25));
            Assert.That(bundle.PokeDollars, Is.EqualTo(300));
            Assert.That(bundle.RelicDrops.Count, Is.EqualTo(1));
            Assert.That(bundle.RelicDrops[0], Is.SameAs(relic));
            Assert.That(bundle.ConsumableDrops, Is.Empty);
        }

        [Test]
        public void ResolveReward_OnDefeat_ReturnsEmpty()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
            }, MakeRelic("r"), 25, 300);
            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Defeat);

            Assert.That(bundle.TrainerXP, Is.EqualTo(0));
            Assert.That(bundle.PokeDollars, Is.EqualTo(0));
            Assert.That(bundle.RelicDrops, Is.Empty);
        }

        [Test]
        public void ResolveReward_NullGuaranteedRelic_NoRelicDrop_StillPaysXPDollars()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
            }, relic: null, xp: 25, dollars: 300);
            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Victory);

            Assert.That(bundle.RelicDrops, Is.Empty);
            Assert.That(bundle.TrainerXP, Is.EqualTo(25));
            Assert.That(bundle.PokeDollars, Is.EqualTo(300));
        }

        // ── Bucket 5: IntentScorer phase-aggression seam (unit) ──────────────

        [Test]
        public void Score_PhaseAggressive_BoostsOffensiveIntent()
        {
            // Per §4.4.3 — aggressive-phase offensive intents ×1.5.
            PokemonInstance enemy = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };
            PokemonInstance lead = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };
            MoveSO atk = MakeMove(PokemonType.Normal, 60);
            Intent intent = new() { Kind = IntentKind.Attack, Move = atk, TargetSlot = 0 };

            IntentScorer.Context calm = new()
            {
                Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config, PhaseAggressive = false
            };
            IntentScorer.Context aggressive = calm;
            aggressive.PhaseAggressive = true;

            Assert.That(IntentScorer.Score(intent, calm), Is.EqualTo(60f).Within(0.001f));
            Assert.That(IntentScorer.Score(intent, aggressive), Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void Score_PhaseAggressive_DoesNotBoostNonOffensiveIntent()
        {
            // Buff is setup, not offensive — phase aggression must leave it alone
            // (so the multiplier biases toward damage, not all moves equally).
            PokemonInstance enemy = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };
            PokemonInstance lead = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };
            MoveSO util = MakeMove(PokemonType.Normal, 0);
            Intent intent = new() { Kind = IntentKind.Buff, Move = util, BuffStat = Stat.Attack };

            IntentScorer.Context calm = new()
            {
                Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config, PhaseAggressive = false
            };
            IntentScorer.Context aggressive = calm;
            aggressive.PhaseAggressive = true;

            // Both equal (whatever the setup/HP weighting resolves to) — phase
            // aggression introduces no delta for non-offensive intents.
            Assert.That(IntentScorer.Score(intent, aggressive),
                Is.EqualTo(IntentScorer.Score(intent, calm)).Within(0.001f));
        }

        [Test]
        public void PickIntent_PhaseAggressive_DisablesRandomnessFloor_CommitsToTop()
        {
            // With the floor forced ON, a non-aggressive boss would hedge to the
            // weaker move; an aggressive-phase boss commits to the top (§4.4.3).
            _config.RandomnessFloorChance = 1.0f;
            PokemonInstance enemy = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };
            PokemonInstance lead = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };
            MoveSO weak = MakeMove(PokemonType.Normal, 10);
            MoveSO strong = MakeMove(PokemonType.Normal, 100);
            List<Intent> candidates = new()
            {
                new Intent { Kind = IntentKind.Attack, Move = weak, TargetSlot = 0 },
                new Intent { Kind = IntentKind.Attack, Move = strong, TargetSlot = 0 },
            };

            IntentScorer.Context calm = new()
            {
                Attacker = enemy, PlayerTeam = new[] { lead }, Config = _config, PhaseAggressive = false
            };
            IntentScorer.Context aggressive = calm;
            aggressive.PhaseAggressive = true;

            // Floor active (chance 1.0) → only non-top candidate is the weak move.
            Assert.That(IntentScorer.PickIntent(candidates, calm, new GameRNG(7u)).Move,
                Is.SameAs(weak), "Non-aggressive boss hedges to the non-top pick.");
            // Floor suppressed → top (strong) selected deterministically.
            Assert.That(IntentScorer.PickIntent(candidates, aggressive, new GameRNG(7u)).Move,
                Is.SameAs(strong), "Aggressive-phase boss commits to the top pick.");
        }

        // ── Bucket 6: CombatController phase wiring (floor-disable end-to-end) ─

        [Test]
        public void CombatController_TwoPhaseEnemyBelowThreshold_CommitsToTopIntent()
        {
            // Drives the full BuildIntentForEnemy → BossPhaseTracker →
            // IntentScorer chain. A 2-phase enemy under 50% HP suppresses the
            // floor and commits to its strongest move; a 1-phase enemy at the
            // same HP hedges. Proves the CombatController wiring.
            _config.RandomnessFloorChance = 1.0f;
            MoveSO weak = MakeMove(PokemonType.Normal, 10);
            MoveSO strong = MakeMove(PokemonType.Normal, 100);

            PokemonSpeciesSO enemySp = MakeSpecies(100, 30, 30, PokemonType.Normal);
            enemySp.BaseLearnset.Add(weak);   // candidate[0]
            enemySp.BaseLearnset.Add(strong); // candidate[1] (top)

            PokemonInstance MakeEnemy(int phaseCount)
            {
                PokemonInstance e = new()
                {
                    Species = enemySp, Level = 5, CurrentHP = 45, // 45% → Phase 2 (≤50), self-HP > 40 so no self-aggression
                    PhaseCount = phaseCount,
                };
                e.CurrentMoves.Add(weak);
                e.CurrentMoves.Add(strong);
                return e;
            }

            PokemonInstance lead = new() { Species = MakeSpecies(100, 30, 30, PokemonType.Normal), Level = 5, CurrentHP = 100 };

            // Single-phase enemy → floor active → hedges to weak.
            CombatController.CombatSetup calmSetup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead }, InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { MakeEnemy(1) },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty, Config = _config, Rng = new GameRNG(7u),
            };
            CombatController calm = new(calmSetup, new PassiveAgent());
            calm.Start();
            calm.IntentPhase();
            Assert.That(calm.State.EnemyIntents[0].Move, Is.SameAs(weak),
                "Single-phase enemy hedges (floor active).");

            // Two-phase enemy under threshold → aggressive → commits to strong.
            CombatController.CombatSetup aggSetup = new()
            {
                PlayerTeam = new List<PokemonInstance> { lead }, InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { MakeEnemy(2) },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty, Config = _config, Rng = new GameRNG(7u),
            };
            CombatController agg = new(aggSetup, new PassiveAgent());
            agg.Start();
            agg.IntentPhase();
            Assert.That(agg.State.EnemyIntents[0].Move, Is.SameAs(strong),
                "Phase-2 enemy commits to its strongest move (floor suppressed).");
        }

        // ── Bucket 7: full Elite combat integration ──────────────────────────

        [Test]
        public void Integration_TwoPokemonElite_SequentialSpawn_ThenVictoryAndReward()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, 30, 30, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            sp.BaseLearnset.Add(tackle);
            RelicSO relic = MakeRelic("ace_reward");

            EliteTrainerSO elite = MakeElite(new[]
            {
                new ElitePokemonSlot { Species = sp, Level = 12, PhaseCount = 2 },
                new ElitePokemonSlot { Species = sp, Level = 13, PhaseCount = 2 },
            }, relic, 25, 300);

            PokemonInstance player = new() { Species = sp, Level = 12, CurrentHP = 100 };
            player.CurrentMoves.Add(tackle);

            EliteTrainerController ctrl = new(elite, new PokemonInstanceFactory());
            CombatController cc = new(ctrl.BuildCombatSetup(
                new List<PokemonInstance> { player }, 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u)), new PassiveAgent());
            cc.Start();

            // First Pokémon faints → reinforcement → still InProgress.
            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase(); cc.IntentPhase(); cc.ActionPhase(); cc.ResolutionPhase();
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.InProgress));
            Assert.That(cc.State.EnemyTeam[0].Level, Is.EqualTo(13));
            Assert.That(cc.State.EnemyTeam[0].PhaseCount, Is.EqualTo(2));
            Assert.That(ctrl.SpawnsExecuted, Is.EqualTo(2));

            // Second Pokémon faints → no more reinforcements → Victory.
            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase(); cc.IntentPhase(); cc.ActionPhase(); cc.ResolutionPhase();
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));

            TrainerRewardBundle reward = ctrl.ResolveReward(cc.State.Outcome);
            Assert.That(reward.RelicDrops, Has.Member(relic));
            Assert.That(reward.TrainerXP, Is.EqualTo(25));
            Assert.That(reward.PokeDollars, Is.EqualTo(300));
        }
    }
}
