using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Tests
{
    // Per §4.4.4 + Epic 8 Task 8.5 — GymLeaderController coverage.
    //   • Bucket 1: queue intake + first spawn + persistent type field
    //   • Bucket 2: ace materialisation (PhaseCount 3 + Sturdy + evo target)
    //   • Bucket 3: ResolveReward (Badge + Rare relic + 50 XP + 500₽)
    //   • Bucket 4: full integration (sequential → Victory → reward)
    public class GymLeaderControllerTests
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
            { 0.25f,0.29f,0.33f,0.40f,0.50f,0.67f,1.00f,1.50f,2.00f,2.50f,3.00f,3.50f,4.00f };
            _config.BaseAPPerTurn = 3;
            _config.MaxAPPerTurn = 6;
            _config.BaseSkillCardsPerTurn = 5;
            _config.BaseConsumableCardsPerTurn = 2;
            _config.DefaultUtilityWeight = 50;
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

        private MoveSO Mk(int power)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.name = "mv"; m.Type = PokemonType.Rock; m.BasePower = power; m.APCost = 1;
            m.Role = MoveRole.Offensive; m.Range = MoveRange.Melee; m.RangeModifierMultiplier = 1f;
            _disposables.Add(m);
            return m;
        }

        private PokemonSpeciesSO Sp(string id, params PokemonType[] types)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = id;
            s.Types = new List<PokemonType>(types.Length == 0 ? new[] { PokemonType.Rock } : types);
            s.BaseStats = new BaseStats { BaseHP = 40, BaseAtk = 40, BaseDef = 40, BaseSpd = 40 };
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO> { Mk(40) };
            _disposables.Add(s);
            return s;
        }

        private RelicSO Rare(string id)
        {
            RelicSO r = ScriptableObject.CreateInstance<RelicSO>();
            r.name = id; r.Id = id; r.Rarity = RarityTier.Rare;
            _disposables.Add(r);
            return r;
        }

        private BadgeSO Boulder()
        {
            BadgeSO b = ScriptableObject.CreateInstance<BadgeSO>();
            b.BadgeId = "boulder_badge"; b.DisplayName = "Boulder Badge";
            b.LeadIncomingDamageReduction = 1;
            _disposables.Add(b);
            return b;
        }

        private GymLeaderSO MakeGym(PokemonSpeciesSO lead, PokemonSpeciesSO aceBase,
            PokemonSpeciesSO aceEvo, BadgeSO badge, RelicSO rare)
        {
            GymLeaderSO g = ScriptableObject.CreateInstance<GymLeaderSO>();
            g.GymLeaderId = "rock_gym_r1"; g.DisplayName = "Rock Gym Leader";
            g.GymType = PokemonType.Rock;
            g.TacticalIdentity = "Rock.";
            g.Composition = new List<GymPokemonSlot>
            {
                new GymPokemonSlot { Species = lead, Level = 14, PhaseCount = 2, IsAce = false },
                new GymPokemonSlot { Species = aceBase, Level = 16, PhaseCount = 3,
                    IsAce = true, HasSturdy = true, MidFightEvolution = aceEvo },
            };
            g.BadgeReward = badge; g.GuaranteedRareRelic = rare;
            g.TrainerXPReward = 50; g.PokeDollarReward = 500;
            _disposables.Add(g);
            return g;
        }

        private sealed class PassiveAgent : IPlayerAgent
        {
            public int PickLeadReplacement(CombatController.CombatState s,
                IReadOnlyList<PokemonInstance> c) => -1;
            public PlayerAction DecideAction(CombatController.CombatState s) => PlayerAction.End();
        }

        // ── Bucket 1: intake + first spawn + field ───────────────────────────

        [Test]
        public void BuildCombatSetup_SpawnsLeadAndSetsPersistentRockField()
        {
            GymLeaderSO gym = MakeGym(Sp("geodude"), Sp("graveler"), Sp("golem"), Boulder(), Rare("r"));
            GymLeaderController ctrl = new(gym, new PokemonInstanceFactory());
            CombatController.CombatSetup setup = ctrl.BuildCombatSetup(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(), _config, new GameRNG(1u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            Assert.That(setup.EnemyTeam[0].PhaseCount, Is.EqualTo(2), "Gym lead is 2-phase.");
            Assert.That(setup.Reinforcements, Is.SameAs(ctrl));
            // Per §4.4.4.3 — type field set at start.
            Assert.That(setup.InitialField.HasGymField, Is.True);
            Assert.That(setup.InitialField.GymTypeField, Is.EqualTo(PokemonType.Rock));
        }

        [Test]
        public void BuildCombatSetup_PassesActiveBadgesThrough()
        {
            GymLeaderSO gym = MakeGym(Sp("geodude"), Sp("graveler"), Sp("golem"), Boulder(), Rare("r"));
            GymLeaderController ctrl = new(gym, new PokemonInstanceFactory());
            List<BadgeSO> badges = new() { Boulder() };
            CombatController.CombatSetup setup = ctrl.BuildCombatSetup(
                new List<PokemonInstance>(), 0, new List<ConsumableSO>(), _config, new GameRNG(1u), badges);
            Assert.That(setup.ActiveBadges, Is.SameAs(badges));
        }

        // ── Bucket 2: ace materialisation ────────────────────────────────────

        [Test]
        public void Reinforcement_AceCarriesPhaseThreeSturdyAndEvolutionTarget()
        {
            PokemonSpeciesSO golem = Sp("golem");
            GymLeaderSO gym = MakeGym(Sp("geodude"), Sp("graveler"), golem, Boulder(), Rare("r"));
            GymLeaderController ctrl = new(gym, new PokemonInstanceFactory());
            ctrl.BuildCombatSetup(new List<PokemonInstance>(), 0, new List<ConsumableSO>(), _config, new GameRNG(1u));

            List<PokemonInstance> ace = ctrl.RequestReinforcements(null);
            Assert.That(ace.Count, Is.EqualTo(1));
            Assert.That(ace[0].PhaseCount, Is.EqualTo(3), "Ace is 3-phase (§4.4.3).");
            Assert.That(ace[0].HasSturdy, Is.True);
            Assert.That(ace[0].MidFightEvolutionTarget, Is.SameAs(golem));
            Assert.That(ace[0].Level, Is.EqualTo(16));
        }

        // ── Bucket 3: reward ─────────────────────────────────────────────────

        [Test]
        public void ResolveReward_OnVictory_AwardsBadgeRareRelicAndXP()
        {
            BadgeSO badge = Boulder();
            RelicSO rare = Rare("conquerors_stone");
            GymLeaderSO gym = MakeGym(Sp("geodude"), Sp("graveler"), Sp("golem"), badge, rare);
            GymLeaderController ctrl = new(gym, new PokemonInstanceFactory());

            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Victory);
            Assert.That(bundle.BadgeAwards, Has.Member(badge));
            Assert.That(bundle.RelicDrops, Has.Member(rare));
            Assert.That(bundle.TrainerXP, Is.EqualTo(50));
            Assert.That(bundle.PokeDollars, Is.EqualTo(500));
        }

        [Test]
        public void ResolveReward_OnDefeat_ReturnsEmpty()
        {
            GymLeaderSO gym = MakeGym(Sp("geodude"), Sp("graveler"), Sp("golem"), Boulder(), Rare("r"));
            GymLeaderController ctrl = new(gym, new PokemonInstanceFactory());
            TrainerRewardBundle bundle = ctrl.ResolveReward(CombatController.CombatOutcome.Defeat);
            Assert.That(bundle.BadgeAwards, Is.Empty);
            Assert.That(bundle.RelicDrops, Is.Empty);
            Assert.That(bundle.TrainerXP, Is.EqualTo(0));
        }

        // ── Bucket 4: integration ────────────────────────────────────────────

        [Test]
        public void Integration_TwoPokemonGym_SequentialThenVictoryAndReward()
        {
            BadgeSO badge = Boulder();
            RelicSO rare = Rare("conquerors_stone");
            PokemonSpeciesSO geodude = Sp("geodude");
            GymLeaderSO gym = MakeGym(geodude, Sp("graveler"), Sp("golem"), badge, rare);

            PokemonInstance player = new() { Species = geodude, Level = 14, CurrentHP = 200 };
            player.CurrentMoves.Add(Mk(40));

            GymLeaderController ctrl = new(gym, new PokemonInstanceFactory());
            CombatController cc = new(ctrl.BuildCombatSetup(
                new List<PokemonInstance> { player }, 0, new List<ConsumableSO>(),
                _config, new GameRNG(1u)), new PassiveAgent());
            cc.Start();

            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase(); cc.IntentPhase(); cc.ActionPhase(); cc.ResolutionPhase();
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.InProgress));
            Assert.That(cc.State.EnemyTeam[0].PhaseCount, Is.EqualTo(3), "Ace spawned.");

            cc.State.EnemyTeam[0].CurrentHP = 0;
            cc.DrawPhase(); cc.IntentPhase(); cc.ActionPhase(); cc.ResolutionPhase();
            Assert.That(cc.State.Outcome, Is.EqualTo(CombatController.CombatOutcome.Victory));

            TrainerRewardBundle reward = ctrl.ResolveReward(cc.State.Outcome);
            Assert.That(reward.BadgeAwards, Has.Member(badge));
            Assert.That(reward.RelicDrops, Has.Member(rare));
            Assert.That(reward.TrainerXP, Is.EqualTo(50));
        }
    }
}
