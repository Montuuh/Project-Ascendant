using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.2 — TrainerBattleController coverage.
    //   • Bucket 1: Constructor + queue intake (8.2.1)
    //   • Bucket 2: BuildCombatSetup spawn + EnemyTeam wiring (8.2.2)
    //   • Bucket 3: RequestReinforcements sequential dequeue (8.2.3)
    //   • Bucket 4: ResolveReward bundle composition (8.2.4)
    //   • Bucket 5: CombatController integration — sequential spawn + Victory
    public class TrainerBattleControllerTests
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

        private ConsumableSO MakeConsumable(string id)
        {
            ConsumableSO c = ScriptableObject.CreateInstance<ConsumableSO>();
            c.name = id;
            _disposables.Add(c);
            return c;
        }

        private TrainerArchetypeSO MakeArchetype(string id, int pokeDollars,
                                                  IEnumerable<TrainerPokemonSlot> slots,
                                                  IEnumerable<RelicSO> relics = null,
                                                  IEnumerable<ConsumableSO> consumables = null)
        {
            TrainerArchetypeSO a = ScriptableObject.CreateInstance<TrainerArchetypeSO>();
            a.ArchetypeId = id;
            a.DisplayName = id;
            a.BasePokeDollarReward = pokeDollars;
            a.Composition = new List<TrainerPokemonSlot>(slots);
            a.RelicLootTable = relics != null ? new List<RelicSO>(relics) : new List<RelicSO>();
            a.ConsumableLootTable = consumables != null
                ? new List<ConsumableSO>(consumables)
                : new List<ConsumableSO>();
            _disposables.Add(a);
            return a;
        }

        // ── Bucket 1: Constructor + queue intake ─────────────────────────────

        [Test]
        public void Constructor_NullArchetype_DoesNotThrow_EmptyQueue()
        {
            // Per Epic 8 Task 8.2.1 — defensive against missing assets.
            TrainerBattleController ctrl = new(null, new PokemonInstanceFactory(), new GameRNG(1u));
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(0));
            Assert.That(ctrl.SpawnsExecuted, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_EmptyComposition_EmptyQueue()
        {
            // Per §7.4 — composition lists 1-2 Pokémon. Empty is malformed
            // content but the controller must not crash.
            TrainerArchetypeSO arch = MakeArchetype("empty", 50,
                new List<TrainerPokemonSlot>());
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_TwoSlotComposition_QueuesBoth()
        {
            // Per §7.4 — sequential 1-2 trainer Pokémon.
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            TrainerArchetypeSO arch = MakeArchetype("two", 100, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
                new TrainerPokemonSlot { Species = sp, Level = 7 },
            });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(2));
        }

        // ── Bucket 2: BuildCombatSetup (8.2.2) ───────────────────────────────

        [Test]
        public void BuildCombatSetup_MaterialisesFirstPokemon_RemainingDecrementsByOne()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            sp.BaseLearnset.Add(tackle);

            TrainerArchetypeSO arch = MakeArchetype("first-only", 50, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
                new TrainerPokemonSlot { Species = sp, Level = 7 },
            });

            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            CombatController.CombatSetup setup = ctrl.BuildCombatSetup(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            Assert.That(setup.EnemyTeam[0].Species, Is.SameAs(sp));
            Assert.That(setup.EnemyTeam[0].Level, Is.EqualTo(5));
            // Per Task 8.2.2 — base learnset copied so AI has moves to score.
            Assert.That(setup.EnemyTeam[0].CurrentMoves, Contains.Item(tackle));
            Assert.That(ctrl.SpawnsExecuted, Is.EqualTo(1));
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(1));
            // Provider wired so reinforcements fire on enemy wipe.
            Assert.That(setup.Reinforcements, Is.SameAs(ctrl));
        }

        [Test]
        public void BuildCombatSetup_LearnsetCappedAtFour()
        {
            // Per §3.7 — only 4 active moves at any time.
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            for (int i = 0; i < 6; i++) sp.BaseLearnset.Add(MakeMove(PokemonType.Normal, 40));

            TrainerArchetypeSO arch = MakeArchetype("cap-4", 50, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            CombatController.CombatSetup setup = ctrl.BuildCombatSetup(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            Assert.That(setup.EnemyTeam[0].CurrentMoves.Count, Is.EqualTo(4));
        }

        // ── Bucket 3: RequestReinforcements (8.2.3) ──────────────────────────

        [Test]
        public void RequestReinforcements_DequeuesNextSlot_ReturnsSingleElement()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            TrainerArchetypeSO arch = MakeArchetype("reinf", 50, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
                new TrainerPokemonSlot { Species = sp, Level = 7 },
            });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));

            // First Pokémon up-front via the setup builder.
            ctrl.BuildCombatSetup(new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            List<PokemonInstance> next = ctrl.RequestReinforcements(null);
            Assert.That(next.Count, Is.EqualTo(1));
            Assert.That(next[0].Level, Is.EqualTo(7), "Second slot must be Level 7.");
            Assert.That(ctrl.SpawnsExecuted, Is.EqualTo(2));
            Assert.That(ctrl.RemainingInQueue, Is.EqualTo(0));
        }

        [Test]
        public void RequestReinforcements_QueueExhausted_ReturnsEmpty()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            TrainerArchetypeSO arch = MakeArchetype("exhaust", 50, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            ctrl.BuildCombatSetup(new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            // The only Pokémon was spawned by BuildCombatSetup; no reinforcements remain.
            List<PokemonInstance> next = ctrl.RequestReinforcements(null);
            Assert.That(next, Is.Empty);
        }

        // ── Bucket 4: ResolveReward (8.2.4) ──────────────────────────────────

        [Test]
        public void ResolveReward_OnVictory_GivesXPAndDollars()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            TrainerArchetypeSO arch = MakeArchetype("xp", 137, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Victory);

            Assert.That(bundle.TrainerXP, Is.EqualTo(5), "§7.4.2 default trainer XP.");
            Assert.That(bundle.PokeDollars, Is.EqualTo(137));
        }

        [Test]
        public void ResolveReward_OnDefeat_ReturnsEmpty()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            TrainerArchetypeSO arch = MakeArchetype("d", 200, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            }, relics: new[] { MakeRelic("r1") });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Defeat);

            Assert.That(bundle.TrainerXP, Is.EqualTo(0));
            Assert.That(bundle.PokeDollars, Is.EqualTo(0));
            Assert.That(bundle.RelicDrops, Is.Empty);
            Assert.That(bundle.ConsumableDrops, Is.Empty);
        }

        [Test]
        public void ResolveReward_EmptyLootTables_NoDrops()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            TrainerArchetypeSO arch = MakeArchetype("noloot", 50, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            });
            TrainerBattleController ctrl = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Victory);

            Assert.That(bundle.RelicDrops, Is.Empty);
            Assert.That(bundle.ConsumableDrops, Is.Empty);
        }

        [Test]
        public void ResolveReward_DeterministicGivenSeed()
        {
            // Per §9.7 — LootRNG-driven picks must replay identically.
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            RelicSO r1 = MakeRelic("alpha");
            RelicSO r2 = MakeRelic("beta");
            RelicSO r3 = MakeRelic("gamma");
            ConsumableSO c1 = MakeConsumable("potion");
            ConsumableSO c2 = MakeConsumable("antidote");

            TrainerArchetypeSO arch = MakeArchetype("det", 100, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            }, relics: new[] { r1, r2, r3 }, consumables: new[] { c1, c2 });

            const uint SEED = 0xABCD1234u;

            TrainerBattleController a = new(arch, new PokemonInstanceFactory(), new GameRNG(SEED));
            TrainerBattleController b = new(arch, new PokemonInstanceFactory(), new GameRNG(SEED));

            TrainerRewardBundle ba = a.ResolveReward(CombatController.CombatOutcome.Victory);
            TrainerRewardBundle bb = b.ResolveReward(CombatController.CombatOutcome.Victory);

            Assert.That(ba.RelicDrops, Is.EqualTo(bb.RelicDrops));
            Assert.That(ba.ConsumableDrops, Is.EqualTo(bb.ConsumableDrops));
            Assert.That(ba.RelicDrops.Count, Is.EqualTo(1));
            Assert.That(ba.ConsumableDrops.Count, Is.EqualTo(1));
        }

        // ── Bucket 5: Integration — CombatController reinforcement flow ──────

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                                            IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        [Test]
        public void Integration_TwoPokemonTrainer_FirstFaintTriggersReinforcement_OutcomeInProgress()
        {
            // Set up a player who never acts so the only enemy faint we can
            // observe must come from us manually zeroing the enemy's HP.
            // Then we drive ResolutionPhase → HandleAnyFaints to trigger the
            // reinforcement hook.
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            sp.BaseLearnset.Add(tackle);

            TrainerArchetypeSO arch = MakeArchetype("two-int", 100, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
                new TrainerPokemonSlot { Species = sp, Level = 7 },
            });

            PokemonInstance player = new()
            {
                Species = sp,
                Level = 5,
                CurrentHP = 100,
            };
            player.CurrentMoves.Add(tackle);

            TrainerBattleController trainer = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            CombatController.CombatSetup setup = trainer.BuildCombatSetup(
                new List<PokemonInstance> { player }, 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u));

            CombatController cc = new(setup, new PassiveAgent());
            cc.Start();

            // Faint the only enemy. Run a full turn — IntentPhase will see a
            // fainted slot (Unknown intent), ResolutionPhase calls
            // HandleAnyFaints, which consults the reinforcement provider.
            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase();
            cc.IntentPhase();
            cc.ActionPhase();
            cc.ResolutionPhase();

            // Reinforcement should have landed — Outcome stays InProgress,
            // second Pokémon is now slot 0, and SpawnsExecuted=2.
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.InProgress));
            Assert.That(cc.State.EnemyTeam.Count, Is.EqualTo(1));
            Assert.That(cc.State.EnemyTeam[0].Level, Is.EqualTo(7));
            Assert.That(cc.State.EnemyTeam[0].CurrentHP, Is.GreaterThan(0));
            Assert.That(trainer.SpawnsExecuted, Is.EqualTo(2));
            Assert.That(trainer.RemainingInQueue, Is.EqualTo(0));
        }

        [Test]
        public void Integration_LastPokemonFainted_NoMoreReinforcements_OutcomeVictory()
        {
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            sp.BaseLearnset.Add(tackle);

            TrainerArchetypeSO arch = MakeArchetype("one-int", 50, new[]
            {
                new TrainerPokemonSlot { Species = sp, Level = 5 },
            });

            PokemonInstance player = new()
            {
                Species = sp,
                Level = 5,
                CurrentHP = 100,
            };
            player.CurrentMoves.Add(tackle);

            TrainerBattleController trainer = new(arch, new PokemonInstanceFactory(), new GameRNG(1u));
            CombatController cc = new(trainer.BuildCombatSetup(
                new List<PokemonInstance> { player }, 0, new List<ConsumableSO>(),
                FieldState.Empty, _config, new GameRNG(1u)), new PassiveAgent());
            cc.Start();

            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase();
            cc.IntentPhase();
            cc.ActionPhase();
            cc.ResolutionPhase();

            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
        }

        [Test]
        public void Integration_NoProvider_FaintedEnemyImmediatelyVictory()
        {
            // Regression: CombatSetup without a Reinforcements provider must
            // behave exactly as it did pre-Task-8.2 (Outcome.Victory on wipe).
            PokemonSpeciesSO sp = MakeSpecies(20, 30, 30, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            PokemonInstance player = new()
            {
                Species = sp,
                Level = 5,
                CurrentHP = 100,
            };
            player.CurrentMoves.Add(tackle);
            PokemonInstance enemy = new()
            {
                Species = sp,
                Level = 5,
                CurrentHP = 100,
            };
            enemy.CurrentMoves.Add(tackle);

            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { enemy },
                ConsumableInventory = new List<ConsumableSO>(),
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(1u),
                // Reinforcements deliberately null.
            };
            CombatController cc = new(setup, new PassiveAgent());
            cc.Start();
            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase();
            cc.IntentPhase();
            cc.ActionPhase();
            cc.ResolutionPhase();
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
        }
    }
}
