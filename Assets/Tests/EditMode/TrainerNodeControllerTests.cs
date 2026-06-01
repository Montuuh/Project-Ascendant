using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.4 / §7.5 + Epic 9 Task 9.4 — Trainer + Elite node controllers and RewardApplier.
    public class TrainerNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_TrainerNode_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectoryOverride = _tempDir;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            SaveSystem.SaveDirectoryOverride = null;
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>();
            _disposables.Add(o);
            return o;
        }

        private PokemonSpeciesSO MakeSpecies(string id, int hp = 40)
        {
            PokemonSpeciesSO s = Make<PokemonSpeciesSO>();
            s.SpeciesId = id;
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            return s;
        }

        private TrainerArchetypeSO MakeArchetype(string id, int dollars, RelicSO relic, ConsumableSO cons)
        {
            TrainerArchetypeSO a = Make<TrainerArchetypeSO>();
            a.ArchetypeId = id;
            a.Composition = new List<TrainerPokemonSlot> { new() { Species = MakeSpecies(id + "_p"), Level = 5 } };
            a.BasePokeDollarReward = dollars;
            a.RelicLootTable = relic != null ? new List<RelicSO> { relic } : new List<RelicSO>();
            a.ConsumableLootTable = cons != null ? new List<ConsumableSO> { cons } : new List<ConsumableSO>();
            return a;
        }

        private EliteTrainerSO MakeElite(RelicSO relic, int xp, int dollars, int phaseCount = 2)
        {
            EliteTrainerSO e = Make<EliteTrainerSO>();
            e.EliteId = "elite_r1";
            e.Composition = new List<ElitePokemonSlot>
            {
                new() { Species = MakeSpecies("elite_p"), Level = 12, PhaseCount = phaseCount },
            };
            e.GuaranteedRelic = relic;
            e.TrainerXPReward = xp;
            e.PokeDollarReward = dollars;
            return e;
        }

        private RunStateSO MakeRun()
        {
            RunStateSO r = Make<RunStateSO>();
            return r;
        }

        private BattleConfigSO MakeConfig() => Make<BattleConfigSO>();

        private static MapNode TrainerNode() => new MapNode(2, 0, 0, NodeType.Trainer);
        private static MapNode EliteNode() => new MapNode(3, 0, 0, NodeType.Elite);

        // ── RewardApplier ─────────────────────────────────────────────────────

        [Test]
        public void RewardApplier_AppliesAllBundleFields()
        {
            RunStateSO run = MakeRun();
            RelicSO relic = Make<RelicSO>();
            ConsumableSO cons = Make<ConsumableSO>();
            BadgeSO badge = Make<BadgeSO>();
            TrainerRewardBundle bundle = TrainerRewardBundle.Empty;
            bundle.PokeDollars = 150;
            bundle.RelicDrops.Add(relic);
            bundle.ConsumableDrops.Add(cons);
            bundle.BadgeAwards.Add(badge);

            RewardApplier.Apply(run, bundle);

            Assert.That(run.PokeDollars, Is.EqualTo(150));
            Assert.That(run.HeldRelics, Has.Member(relic));
            Assert.That(run.Inventory, Has.Member(cons));
            Assert.That(run.EarnedBadges, Has.Member(badge));
        }

        // ── TrainerBattleNodeController ───────────────────────────────────────

        [Test]
        public void Trainer_OnEnter_PicksArchetype_Deterministic()
        {
            List<TrainerArchetypeSO> pool = new()
            {
                MakeArchetype("a", 50, null, null),
                MakeArchetype("b", 60, null, null),
            };
            TrainerBattleNodeController c1 = new(TrainerNode(), MakeRun(), pool, new PokemonInstanceFactory(), new GameRNG(7u), new GameRNG(1u));
            TrainerBattleNodeController c2 = new(TrainerNode(), MakeRun(), pool, new PokemonInstanceFactory(), new GameRNG(7u), new GameRNG(1u));
            c1.Enter();
            c2.Enter();
            Assert.That(c1.Archetype, Is.SameAs(c2.Archetype));
            Assert.That(c1.Archetype, Is.Not.Null);
        }

        [Test]
        public void Trainer_BuildCombat_PopulatesActiveBadgesFromRun()
        {
            BadgeSO badge = Make<BadgeSO>();
            RunStateSO run = MakeRun();
            run.EarnedBadges = new List<BadgeSO> { badge };
            List<TrainerArchetypeSO> pool = new() { MakeArchetype("a", 50, null, null) };

            TrainerBattleNodeController c = new(TrainerNode(), run, pool, new PokemonInstanceFactory(), new GameRNG(1u), new GameRNG(1u));
            c.Enter();
            CombatController.CombatSetup setup = c.BuildCombat(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(), FieldState.Empty, MakeConfig(), new GameRNG(1u));

            Assert.That(setup.ActiveBadges, Has.Member(badge));
            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
        }

        [Test]
        public void Trainer_ResolveCombat_Victory_AppliesReward_Cleared()
        {
            RelicSO relic = Make<RelicSO>(); // §7.4.2 — Common rarity by default
            ConsumableSO cons = Make<ConsumableSO>();
            RunStateSO run = MakeRun();
            TrainerArchetypeSO arch = MakeArchetype("a", 120, relic, cons);
            // §7.4.2 / Task 12.10.1 — single weighted drop. Force the Common-relic category for determinism.
            arch.CommonConsumableWeight = 0; arch.CommonRelicWeight = 100; arch.UncommonRelicWeight = 0;
            List<TrainerArchetypeSO> pool = new() { arch };

            TrainerBattleNodeController c = new(TrainerNode(), run, pool, new PokemonInstanceFactory(), new GameRNG(1u), new GameRNG(1u));
            c.Enter();
            c.BuildCombat(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), FieldState.Empty, MakeConfig(), new GameRNG(1u));
            TrainerRewardBundle bundle = c.ResolveCombat(CombatController.CombatOutcome.Victory);

            Assert.That(bundle.PokeDollars, Is.EqualTo(120));
            Assert.That(run.PokeDollars, Is.EqualTo(120));
            Assert.That(run.HeldRelics, Has.Member(relic), "the forced single drop is applied to the run.");
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void Trainer_ResolveCombat_Defeat_NoReward_PlayerWiped()
        {
            RelicSO relic = Make<RelicSO>();
            RunStateSO run = MakeRun();
            List<TrainerArchetypeSO> pool = new() { MakeArchetype("a", 120, relic, null) };

            TrainerBattleNodeController c = new(TrainerNode(), run, pool, new PokemonInstanceFactory(), new GameRNG(1u), new GameRNG(1u));
            c.Enter();
            c.BuildCombat(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), FieldState.Empty, MakeConfig(), new GameRNG(1u));
            c.ResolveCombat(CombatController.CombatOutcome.Defeat);

            Assert.That(run.PokeDollars, Is.EqualTo(0));
            Assert.That(run.HeldRelics ?? new List<RelicSO>(), Has.No.Member(relic));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.PlayerWiped));
        }

        [Test]
        public void Trainer_Enter_SavesRun()
        {
            List<TrainerArchetypeSO> pool = new() { MakeArchetype("a", 50, null, null) };
            TrainerBattleNodeController c = new(TrainerNode(), MakeRun(), pool, new PokemonInstanceFactory(), new GameRNG(1u), new GameRNG(1u));
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }

        // ── EliteNodeController ───────────────────────────────────────────────

        [Test]
        public void Elite_BuildCombat_CarriesPhaseCount_AndBadges()
        {
            BadgeSO badge = Make<BadgeSO>();
            RunStateSO run = MakeRun();
            run.EarnedBadges = new List<BadgeSO> { badge };
            EliteTrainerSO elite = MakeElite(Make<RelicSO>(), xp: 25, dollars: 300, phaseCount: 2);

            EliteNodeController c = new(EliteNode(), run, elite, new PokemonInstanceFactory());
            c.Enter();
            CombatController.CombatSetup setup = c.BuildCombat(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(), FieldState.Empty, MakeConfig(), new GameRNG(1u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            Assert.That(setup.EnemyTeam[0].PhaseCount, Is.EqualTo(2));
            Assert.That(setup.ActiveBadges, Has.Member(badge));
        }

        [Test]
        public void Elite_ResolveCombat_Victory_GuaranteedRelic_Cleared()
        {
            RelicSO relic = Make<RelicSO>();
            RunStateSO run = MakeRun();
            EliteTrainerSO elite = MakeElite(relic, xp: 25, dollars: 300);

            EliteNodeController c = new(EliteNode(), run, elite, new PokemonInstanceFactory());
            c.Enter();
            c.BuildCombat(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), FieldState.Empty, MakeConfig(), new GameRNG(1u));
            TrainerRewardBundle bundle = c.ResolveCombat(CombatController.CombatOutcome.Victory);

            Assert.That(bundle.PokeDollars, Is.EqualTo(300));
            Assert.That(bundle.TrainerXP, Is.EqualTo(25));
            Assert.That(run.PokeDollars, Is.EqualTo(300));
            Assert.That(run.HeldRelics, Has.Member(relic));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void Elite_ResolveCombat_Defeat_PlayerWiped_NoReward()
        {
            RelicSO relic = Make<RelicSO>();
            RunStateSO run = MakeRun();
            EliteTrainerSO elite = MakeElite(relic, xp: 25, dollars: 300);

            EliteNodeController c = new(EliteNode(), run, elite, new PokemonInstanceFactory());
            c.Enter();
            c.BuildCombat(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), FieldState.Empty, MakeConfig(), new GameRNG(1u));
            c.ResolveCombat(CombatController.CombatOutcome.Defeat);

            Assert.That(run.PokeDollars, Is.EqualTo(0));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.PlayerWiped));
        }
    }
}
