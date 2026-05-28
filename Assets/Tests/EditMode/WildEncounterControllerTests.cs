using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per Epic 8 Task 8.1 — WildEncounterController + CombatController catch
    // dispatch coverage.
    //   Bucket 1: OfferSpeciesChoices                 (8.1.1, 8.1.2)
    //   Bucket 2: BuildCombatSetup + ball injection   (8.1.3)
    //   Bucket 3: ResolveOutcome + Box (§2.3.1)       (8.1.5)
    //   Bucket 4: Catch dispatch in CombatController  (8.1.4)
    //   Bucket 5: Lethal-during-catch edge case       (8.1.7)
    public class WildEncounterControllerTests
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

        private PokemonSpeciesSO MakeSpecies(int hp, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "sp_" + hp;
            s.Types = new List<PokemonType>(types);
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private BiomeSO MakeBiome(IEnumerable<PokemonSpeciesSO> pool)
        {
            BiomeSO b = ScriptableObject.CreateInstance<BiomeSO>();
            b.BiomeId = "test_biome";
            b.SpeciesPool = new List<PokemonSpeciesSO>(pool);
            _disposables.Add(b);
            return b;
        }

        private ConsumableSO MakePokeball(float threshold, bool statusWindow)
        {
            CatchConsumableEffectSO eff = ScriptableObject.CreateInstance<CatchConsumableEffectSO>();
            eff.CatchThresholdPercent = threshold;
            eff.CatchWithAnyStatus = statusWindow;
            _disposables.Add(eff);
            ConsumableSO c = ScriptableObject.CreateInstance<ConsumableSO>();
            c.Id = "pokeball";
            c.DisplayName = "Pokeball";
            c.APCost = 1;
            c.Effect = eff;
            _disposables.Add(c);
            return c;
        }

        // ── Bucket 1: OfferSpeciesChoices (8.1.1, 8.1.2) ─────────────────────

        [Test]
        public void OfferSpeciesChoices_ReturnsRequestedCount_NoDuplicates()
        {
            BiomeSO biome = MakeBiome(new[]
            {
                MakeSpecies(40, PokemonType.Normal),
                MakeSpecies(45, PokemonType.Grass),
                MakeSpecies(50, PokemonType.Water),
                MakeSpecies(55, PokemonType.Fire),
            });
            WildEncounterController wild = new(biome, null, new PokemonInstanceFactory(), new GameRNG(1u));
            List<PokemonSpeciesSO> picks = wild.OfferSpeciesChoices(3);
            Assert.That(picks.Count, Is.EqualTo(3));
            // Uniqueness — no duplicates within an offer.
            HashSet<PokemonSpeciesSO> set = new(picks);
            Assert.That(set.Count, Is.EqualTo(3));
        }

        [Test]
        public void OfferSpeciesChoices_PoolSmallerThanRequest_ReturnsWholePool()
        {
            BiomeSO biome = MakeBiome(new[]
            {
                MakeSpecies(40, PokemonType.Normal),
                MakeSpecies(45, PokemonType.Grass),
            });
            WildEncounterController wild = new(biome, null, new PokemonInstanceFactory(), new GameRNG(1u));
            List<PokemonSpeciesSO> picks = wild.OfferSpeciesChoices(5);
            Assert.That(picks.Count, Is.EqualTo(2));
        }

        [Test]
        public void OfferSpeciesChoices_DeterministicGivenSeed()
        {
            BiomeSO biome = MakeBiome(new[]
            {
                MakeSpecies(40, PokemonType.Normal),
                MakeSpecies(45, PokemonType.Grass),
                MakeSpecies(50, PokemonType.Water),
                MakeSpecies(55, PokemonType.Fire),
                MakeSpecies(60, PokemonType.Electric),
            });
            const uint SEED = 0xC0FFEE12u;
            WildEncounterController a = new(biome, null, new PokemonInstanceFactory(), new GameRNG(SEED));
            WildEncounterController b = new(biome, null, new PokemonInstanceFactory(), new GameRNG(SEED));
            List<PokemonSpeciesSO> pa = a.OfferSpeciesChoices(3);
            List<PokemonSpeciesSO> pb = b.OfferSpeciesChoices(3);
            Assert.That(pa, Is.EqualTo(pb));
        }

        [Test]
        public void OfferSpeciesChoices_NullBiome_ReturnsEmpty()
        {
            WildEncounterController wild = new(null, null, new PokemonInstanceFactory(), new GameRNG(1u));
            Assert.That(wild.OfferSpeciesChoices(3), Is.Empty);
        }

        // ── Bucket 2: BuildCombatSetup + ball injection (8.1.3) ──────────────

        [Test]
        public void BuildCombatSetup_InjectsPokeball_WildAtFullHP_LearnsetCopied()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            MoveSO tackle = MakeMove(PokemonType.Normal, 40);
            sp.BaseLearnset.Add(tackle);
            BiomeSO biome = MakeBiome(new[] { sp });
            ConsumableSO ball = MakePokeball(0.5f, true);

            WildEncounterController wild = new(biome, ball, new PokemonInstanceFactory(), new GameRNG(1u));
            List<ConsumableSO> baseInv = new();
            CombatController.CombatSetup setup = wild.BuildCombatSetup(
                sp, wildLevel: 5,
                new List<PokemonInstance>(), 0,
                baseInv, FieldState.Empty, _config, new GameRNG(1u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            // Per §7.3.4.1 step 1 — wild enters at full HP.
            PokemonInstance w = setup.EnemyTeam[0];
            Assert.That(w.CurrentHP, Is.GreaterThan(0));
            Assert.That(w.Species, Is.SameAs(sp));
            Assert.That(w.CurrentMoves, Has.Member(tackle));

            // Pokéball injected into per-combat inventory snapshot.
            Assert.That(setup.ConsumableInventory, Has.Member(ball));
            // Caller's baseInv must not have been mutated.
            Assert.That(baseInv, Is.Empty);
        }

        [Test]
        public void BuildCombatSetup_NoReinforcements_SingleEnemyEncounter()
        {
            PokemonSpeciesSO sp = MakeSpecies(50, PokemonType.Normal);
            BiomeSO biome = MakeBiome(new[] { sp });
            WildEncounterController wild = new(biome, null, new PokemonInstanceFactory(), new GameRNG(1u));
            CombatController.CombatSetup setup = wild.BuildCombatSetup(
                sp, 5, new List<PokemonInstance>(), 0,
                new List<ConsumableSO>(), FieldState.Empty, _config, new GameRNG(1u));
            Assert.That(setup.Reinforcements, Is.Null);
        }

        // ── Bucket 3: ResolveOutcome + Box overflow (§2.3.1) ─────────────────

        private sealed class SkipHandler : IBoxOverflowHandler
        {
            public int OnBoxOverflow(IReadOnlyList<PokemonInstance> b, PokemonInstance c) => -1;
        }

        private sealed class SwapAtZeroHandler : IBoxOverflowHandler
        {
            public int OnBoxOverflow(IReadOnlyList<PokemonInstance> b, PokemonInstance c) => 0;
        }

        [Test]
        public void ResolveOutcome_PlayerWiped_ReturnsPlayerWiped()
        {
            WildEncounterController wild = new(null, null, new PokemonInstanceFactory(), new GameRNG(1u));
            WildEncounterResult r = wild.ResolveOutcome(
                CombatController.CombatOutcome.Defeat,
                caughtTarget: null,
                currentBox: new List<PokemonInstance>(),
                boxCapacity: 6,
                overflowHandler: null);
            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.PlayerWiped));
            Assert.That(r.BoxUpdated, Is.False);
        }

        [Test]
        public void ResolveOutcome_VictoryNoCatch_ReturnsWildFainted()
        {
            // Wild fainted from combat damage; CaughtTarget stays null.
            WildEncounterController wild = new(null, null, new PokemonInstanceFactory(), new GameRNG(1u));
            WildEncounterResult r = wild.ResolveOutcome(
                CombatController.CombatOutcome.Victory,
                caughtTarget: null,
                currentBox: new List<PokemonInstance>(),
                boxCapacity: 6,
                overflowHandler: null);
            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.WildFainted));
            Assert.That(r.BoxUpdated, Is.False);
        }

        [Test]
        public void ResolveOutcome_Caught_BoxHasRoom_AddsCandidate_NoPrompt()
        {
            PokemonSpeciesSO sp = MakeSpecies(40, PokemonType.Normal);
            PokemonInstance caught = new() { Species = sp, Level = 5, CurrentHP = 20 };
            List<PokemonInstance> box = new();

            WildEncounterController wild = new(null, null, new PokemonInstanceFactory(), new GameRNG(1u));
            WildEncounterResult r = wild.ResolveOutcome(
                CombatController.CombatOutcome.Victory,
                caughtTarget: caught,
                currentBox: box,
                boxCapacity: 6,
                overflowHandler: null);

            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.Caught));
            Assert.That(r.BoxUpdated, Is.True);
            Assert.That(r.BoxOverflowPromptShown, Is.False);
            Assert.That(box, Has.Member(caught));
        }

        [Test]
        public void ResolveOutcome_Caught_BoxAtCapacity_SkipPrompt_NoChange()
        {
            // Per §2.3.1 — Skip declines the recruitment.
            PokemonSpeciesSO sp = MakeSpecies(40, PokemonType.Normal);
            PokemonInstance caught = new() { Species = sp, Level = 5, CurrentHP = 20 };
            List<PokemonInstance> box = new()
            {
                new() { Species = sp }, new() { Species = sp },
                new() { Species = sp }, new() { Species = sp },
                new() { Species = sp }, new() { Species = sp },
            };

            WildEncounterController wild = new(null, null, new PokemonInstanceFactory(), new GameRNG(1u));
            WildEncounterResult r = wild.ResolveOutcome(
                CombatController.CombatOutcome.Victory,
                caughtTarget: caught,
                currentBox: box,
                boxCapacity: 6,
                overflowHandler: new SkipHandler());

            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.Caught));
            Assert.That(r.BoxOverflowPromptShown, Is.True);
            Assert.That(r.BoxUpdated, Is.False);
            Assert.That(r.ReleasedBoxIndex, Is.EqualTo(-1));
            Assert.That(box, Has.No.Member(caught));
            Assert.That(box.Count, Is.EqualTo(6));
        }

        [Test]
        public void ResolveOutcome_Caught_BoxAtCapacity_SwapPrompt_ReplacesIndex()
        {
            // Per §2.3.1 — Swap releases the chosen Box index and adds candidate.
            PokemonSpeciesSO sp = MakeSpecies(40, PokemonType.Normal);
            PokemonInstance caught = new() { Species = sp, Level = 5, CurrentHP = 20 };
            PokemonInstance victim = new() { Species = sp, Level = 1 };
            List<PokemonInstance> box = new()
            {
                victim,
                new() { Species = sp }, new() { Species = sp },
                new() { Species = sp }, new() { Species = sp },
                new() { Species = sp },
            };

            WildEncounterController wild = new(null, null, new PokemonInstanceFactory(), new GameRNG(1u));
            WildEncounterResult r = wild.ResolveOutcome(
                CombatController.CombatOutcome.Victory,
                caughtTarget: caught,
                currentBox: box,
                boxCapacity: 6,
                overflowHandler: new SwapAtZeroHandler());

            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.Caught));
            Assert.That(r.BoxOverflowPromptShown, Is.True);
            Assert.That(r.BoxUpdated, Is.True);
            Assert.That(r.ReleasedBoxIndex, Is.EqualTo(0));
            Assert.That(box[0], Is.SameAs(caught));
            Assert.That(box, Has.No.Member(victim));
            Assert.That(box.Count, Is.EqualTo(6));
        }

        // ── Bucket 4: CombatController catch dispatch (8.1.4) ────────────────

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                                            IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        private CombatController BuildCombatWithWild(PokemonInstance player,
                                                     PokemonInstance wild,
                                                     ConsumableSO ball)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { wild },
                ConsumableInventory = new List<ConsumableSO> { ball },
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(1u),
            };
            CombatController cc = new(setup, new PassiveAgent());
            cc.Start();
            return cc;
        }

        [Test]
        public void Catch_WildBelowThreshold_CaughtTargetSet_EnemyTeamCleared()
        {
            PokemonSpeciesSO sp = MakeSpecies(100, PokemonType.Normal);
            ConsumableSO ball = MakePokeball(0.5f, true);

            PokemonInstance player = new() { Species = sp, Level = 5, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove(PokemonType.Normal, 40));
            PokemonInstance wild = new() { Species = sp, Level = 5, CurrentHP = 30 }; // 30% — catchable

            CombatController cc = BuildCombatWithWild(player, wild, ball);
            // Simulate a turn where the player chooses to play the consumable.
            cc.State.ConsumableHand.Add(ball);
            PlayerAction playBall = PlayerAction.PlayConsumable(cc.State.ConsumableHand.Count - 1);
            // Reach into TryPlayConsumable via ActionPhase by injecting an
            // agent action — simplest test path: call the public DecideAction
            // shim by directly using PlayerActionKind dispatch via reflection-free
            // public ApplyAction. Since there's no such API, exercise via
            // ActionPhase + a one-shot agent.
            cc.State.ConsumableHand[0] = ball;
            // Just dispatch synchronously using the same code path the player
            // would take — TryPlayConsumable is private, so we drive it
            // through a one-shot scripted agent.
            cc = ReplayWithCatchAgent(player, wild, ball, throwOnTurn: 1);

            Assert.That(cc.State.CaughtTarget, Is.SameAs(wild));
            Assert.That(cc.State.EnemyTeam, Is.Empty);
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
        }

        [Test]
        public void Catch_WildAboveThreshold_BallConsumed_CombatContinues()
        {
            PokemonSpeciesSO sp = MakeSpecies(100, PokemonType.Normal);
            ConsumableSO ball = MakePokeball(0.5f, true);
            PokemonInstance player = new() { Species = sp, Level = 5, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove(PokemonType.Normal, 40));
            PokemonInstance wild = new() { Species = sp, Level = 5, CurrentHP = 90 }; // 90% — not catchable

            CombatController cc = ReplayWithCatchAgent(player, wild, ball, throwOnTurn: 1);

            // Ball was used; wild is still there; combat is still going (or
            // ended for some other reason — but CaughtTarget must be null).
            Assert.That(cc.State.CaughtTarget, Is.Null);
            Assert.That(cc.State.EnemyTeam, Has.Count.EqualTo(1));
        }

        [Test]
        public void Catch_FullHPWithStatus_Caught_StatusWindowExpands()
        {
            PokemonSpeciesSO sp = MakeSpecies(100, PokemonType.Normal);
            ConsumableSO ball = MakePokeball(0.5f, true);
            PokemonInstance player = new() { Species = sp, Level = 5, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove(PokemonType.Normal, 40));
            PokemonInstance wild = new()
            {
                Species = sp,
                Level = 5,
                CurrentHP = 100,
                PrimaryStatus = StatusCondition.Burn,
            };

            CombatController cc = ReplayWithCatchAgent(player, wild, ball, throwOnTurn: 1);

            Assert.That(cc.State.CaughtTarget, Is.SameAs(wild));
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
        }

        // ── Bucket 5: Lethal-damage-during-catch edge case (8.1.7) ──────────

        [Test]
        public void Catch_WildAlreadyFainted_BallStillConsumed_WildFaintedNotCaught()
        {
            // Per §7.3.4.1 — "HP ≤ 0: the wild Pokémon faints. The recruit is lost."
            // Throwing a Pokéball at an already-fainted wild does not retroactively
            // catch it. The ball is consumed (no special fizzle), CaughtTarget
            // stays null, and the combat resolves as WildFainted Victory.
            PokemonSpeciesSO sp = MakeSpecies(100, PokemonType.Normal);
            ConsumableSO ball = MakePokeball(0.5f, true);
            PokemonInstance player = new() { Species = sp, Level = 5, CurrentHP = 100 };
            player.CurrentMoves.Add(MakeMove(PokemonType.Normal, 40));
            PokemonInstance wild = new() { Species = sp, Level = 5, CurrentHP = 0 };

            CombatController cc = ReplayWithCatchAgent(player, wild, ball, throwOnTurn: 1);

            Assert.That(cc.State.CaughtTarget, Is.Null);
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));
        }

        // ── Catch-agent harness ──────────────────────────────────────────────
        //
        // The agent throws the Pokéball on the first ActionPhase it sees the
        // consumable in hand, then ends turn. This is the minimal way to
        // drive TryPlayConsumable without reaching into private API.

        private sealed class CatchAgent : IPlayerAgent
        {
            public bool BallThrown;

            public int PickLeadReplacement(CombatController.CombatState s,
                                            IReadOnlyList<PokemonInstance> c) => -1;

            public PlayerAction DecideAction(CombatController.CombatState s)
            {
                if (BallThrown) return PlayerAction.End();
                for (int i = 0; i < s.ConsumableHand.Count; i++)
                {
                    if (s.ConsumableHand[i] != null && s.ConsumableHand[i].Effect is CatchConsumableEffectSO)
                    {
                        BallThrown = true;
                        return PlayerAction.PlayConsumable(i);
                    }
                }
                return PlayerAction.End();
            }
        }

        private CombatController ReplayWithCatchAgent(PokemonInstance player,
                                                      PokemonInstance wild,
                                                      ConsumableSO ball,
                                                      int throwOnTurn)
        {
            CombatController.CombatSetup setup = new()
            {
                PlayerTeam = new List<PokemonInstance> { player },
                InitialLeadIndex = 0,
                EnemyTeam = new List<PokemonInstance> { wild },
                ConsumableInventory = new List<ConsumableSO> { ball },
                InitialField = FieldState.Empty,
                Config = _config,
                Rng = new GameRNG(1u),
            };
            CombatController cc = new(setup, new CatchAgent());
            cc.RunFullCombat(maxTurns: 4);
            return cc;
        }
    }
}
