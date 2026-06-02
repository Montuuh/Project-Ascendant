using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §4.4.4 / §7.13 + Epic 9 Task 9.8 — Gym Leader node (run-end transition).
    public class GymNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Gym_" + System.Guid.NewGuid().ToString("N"));
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

        private PokemonSpeciesSO MakeSpecies(string id, int hp = 60)
        {
            PokemonSpeciesSO s = Make<PokemonSpeciesSO>();
            s.SpeciesId = id;
            s.Types = new List<PokemonType> { PokemonType.Rock };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.BaseLearnset = new List<MoveSO>();
            return s;
        }

        private GymLeaderSO MakeGym(BadgeSO badge, RelicSO relic)
        {
            GymLeaderSO g = Make<GymLeaderSO>();
            g.GymLeaderId = "rock_gym_r1";
            g.GymType = PokemonType.Rock;
            g.Composition = new List<GymPokemonSlot>
            {
                new() { Species = MakeSpecies("geodude"), Level = 14, PhaseCount = 2 },
                new() { Species = MakeSpecies("graveler"), Level = 16, PhaseCount = 3, IsAce = true, HasSturdy = true },
            };
            g.BadgeReward = badge;
            g.GuaranteedRareRelic = relic;
            g.TrainerXPReward = 50;
            g.PokeDollarReward = 500;
            return g;
        }

        private RunStateSO MakeRun(int dollars = 0)
        {
            RunStateSO r = Make<RunStateSO>();
            r.PokeDollars = dollars;
            return r;
        }

        private BattleConfigSO MakeConfig() => Make<BattleConfigSO>();
        private static MapNode GymNode() => new MapNode(7, 0, 0, NodeType.Gym);

        // ── BuildCombat (9.8.1 / 9.8.2) ───────────────────────────────────────

        [Test]
        public void BuildCombat_SetsGymField_PhaseCount_AndBadges()
        {
            BadgeSO badge = Make<BadgeSO>();
            RunStateSO run = MakeRun();
            run.EarnedBadges = new List<BadgeSO> { badge };
            GymNodeController c = new(GymNode(), run, MakeGym(badge, Make<RelicSO>()), new PokemonInstanceFactory());
            c.Enter();

            CombatController.CombatSetup setup = c.BuildCombat(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(), MakeConfig(), new GameRNG(1u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            Assert.That(setup.EnemyTeam[0].PhaseCount, Is.EqualTo(2));
            Assert.That(setup.InitialField.HasGymField, Is.True);
            Assert.That(setup.InitialField.GymTypeField, Is.EqualTo(PokemonType.Rock));
            Assert.That(setup.ActiveBadges, Has.Member(badge));
        }

        [Test]
        public void BuildCombat_NoPreFightHeal_PlayerHpUnchanged()
        {
            // Per §9.8.2 — no pre-fight heal: the team enters at current HP.
            PokemonInstance hurt = new() { Species = MakeSpecies("ally"), Level = 10, CurrentHP = 5 };
            GymNodeController c = new(GymNode(), MakeRun(), MakeGym(Make<BadgeSO>(), Make<RelicSO>()), new PokemonInstanceFactory());
            c.Enter();
            c.BuildCombat(new List<PokemonInstance> { hurt }, 0, new List<ConsumableSO>(), MakeConfig(), new GameRNG(1u));
            Assert.That(hurt.CurrentHP, Is.EqualTo(5));
        }

        // ── ResolveCombat (9.8.3) ─────────────────────────────────────────────

        [Test]
        public void ResolveCombat_Victory_AppliesBadgeRelicReward_RunEnded()
        {
            BadgeSO badge = Make<BadgeSO>();
            RelicSO relic = Make<RelicSO>();
            RunStateSO run = MakeRun();
            GymNodeController c = new(GymNode(), run, MakeGym(badge, relic), new PokemonInstanceFactory());
            c.Enter();
            c.BuildCombat(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), MakeConfig(), new GameRNG(1u));

            TrainerRewardBundle bundle = c.ResolveCombat(CombatController.CombatOutcome.Victory, finalLeadIndex: 0);

            Assert.That(bundle.BadgeAwards, Has.Member(badge));
            Assert.That(run.EarnedBadges, Has.Member(badge));
            Assert.That(run.HeldRelics, Has.Member(relic));
            Assert.That(run.PokeDollars, Is.EqualTo(500));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.RunEnded));
            Assert.That(c.ToGameEventType(), Is.EqualTo(GameEventType.RunEnded));
        }

        [Test]
        public void ResolveCombat_Defeat_PlayerWiped_NoReward()
        {
            BadgeSO badge = Make<BadgeSO>();
            RunStateSO run = MakeRun();
            GymNodeController c = new(GymNode(), run, MakeGym(badge, Make<RelicSO>()), new PokemonInstanceFactory());
            c.Enter();
            c.BuildCombat(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), MakeConfig(), new GameRNG(1u));

            c.ResolveCombat(CombatController.CombatOutcome.Defeat, finalLeadIndex: 0);

            Assert.That(run.EarnedBadges ?? new List<BadgeSO>(), Has.No.Member(badge));
            Assert.That(run.PokeDollars, Is.EqualTo(0));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.PlayerWiped));
            Assert.That(c.ToGameEventType(), Is.EqualTo(GameEventType.GameOver));
        }

        [Test]
        public void Enter_SavesRun()
        {
            GymNodeController c = new(GymNode(), MakeRun(), MakeGym(Make<BadgeSO>(), Make<RelicSO>()), new PokemonInstanceFactory());
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }
    }
}
