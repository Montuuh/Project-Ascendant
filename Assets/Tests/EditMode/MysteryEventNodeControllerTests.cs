using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.9 + Epic 9 Task 9.7 — Mystery Event node: selection, repeatability, outcomes, risk.
    public class MysteryEventNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Mystery_" + System.Guid.NewGuid().ToString("N"));
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

        private MysteryEventSO MakeEvent(string id, MysteryRiskProfile risk = MysteryRiskProfile.Safe)
        {
            MysteryEventSO e = Make<MysteryEventSO>();
            e.EventId = id;
            e.RiskProfile = risk;
            e.Choices = new List<MysteryChoice> { new() { ChoiceText = "a" }, new() { ChoiceText = "b" } };
            return e;
        }

        private MysteryConfigSO MakeConfig()
        {
            MysteryConfigSO c = Make<MysteryConfigSO>();
            c.BerryBushHealPercent = 30;
            c.BerryBushPotionCount = 3;
            c.WanderingTutorDeclineDollars = 100;
            c.SlotBoothWager = 100;
            c.SlotBoothWinAmount = 250;
            return c;
        }

        private PokemonSpeciesSO MakeSpecies(int hp)
        {
            PokemonSpeciesSO s = Make<PokemonSpeciesSO>();
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 10, BaseDef = 10, BaseSpd = 10 };
            return s;
        }

        private RunStateSO MakeRun(int dollars = 0)
        {
            RunStateSO r = Make<RunStateSO>();
            r.PokeDollars = dollars;
            return r;
        }

        private EconomyConfigSO MakeEconomy()
        {
            EconomyConfigSO e = Make<EconomyConfigSO>();
            e.TraumaStackPenaltyPercent = 5; e.TraumaStackCap = 5;
            return e;
        }

        private MysteryEventNodeController.MysteryItemRefs Items(RelicSO relic = null, ConsumableSO potion = null, ConsumableSO placeholder = null)
            => new()
            {
                StoneRelicPool = relic != null ? new List<RelicSO> { relic } : new List<RelicSO>(),
                Potion = potion,
                TutorPlaceholder = placeholder,
            };

        private static MapNode MysteryNode() => new MapNode(2, 0, 0, NodeType.Mystery);

        private MysteryEventNodeController Make(
            List<MysteryEventSO> pool, RunStateSO run,
            MysteryEventNodeController.MysteryItemRefs items = null,
            Box box = null, uint seed = 1u)
            => new(MysteryNode(), run, pool, MakeConfig(), new GameRNG(seed),
                   items ?? Items(), box, MakeEconomy());

        // ── Selection + repeatability (§7.9.1 / §7.9.4) ───────────────────────

        [Test]
        public void OnEnter_SelectsAnEventFromPool()
        {
            var pool = new List<MysteryEventSO> { MakeEvent("berry_bush") };
            MysteryEventNodeController c = Make(pool, MakeRun());
            c.Enter();
            Assert.That(c.SelectedEvent, Is.Not.Null);
        }

        [Test]
        public void Repeatability_FiredEventNotReselected()
        {
            // Per §7.9.4 — a fired event is removed from the pool for the run.
            RunStateSO run = MakeRun();
            var pool = new List<MysteryEventSO> { MakeEvent("berry_bush"), MakeEvent("slot_booth") };

            MysteryEventNodeController first = Make(pool, run, seed: 3u);
            first.Enter();
            MysteryEventSO firstEvent = first.SelectedEvent;
            first.Choose(1); // resolve + mark fired

            MysteryEventNodeController second = Make(pool, run, seed: 9u);
            second.Enter();
            Assert.That(second.SelectedEvent, Is.Not.Null);
            Assert.That(second.SelectedEvent.EventId, Is.Not.EqualTo(firstEvent.EventId));
        }

        [Test]
        public void OnEnter_PublishesRiskProfile()
        {
            MysteryEventOfferedContext? seen = null;
            EventBus.Subscribe<MysteryEventOfferedContext>(ctx => seen = ctx);
            var pool = new List<MysteryEventSO> { MakeEvent("slot_booth", MysteryRiskProfile.Gamble) };
            Make(pool, MakeRun()).Enter();
            Assert.That(seen.HasValue, Is.True);
            Assert.That(seen.Value.RiskProfile, Is.EqualTo(MysteryRiskProfile.Gamble));
        }

        // ── Berry Bush (§7.9.2) ───────────────────────────────────────────────

        [Test]
        public void BerryBush_EatNow_HealsAllBox30Percent()
        {
            Box box = new(6);
            PokemonInstance p = new() { Species = MakeSpecies(100), Level = 5, CurrentHP = 20 };
            box.Members.Add(p);
            var pool = new List<MysteryEventSO> { MakeEvent("berry_bush") };

            MysteryEventNodeController c = Make(pool, MakeRun(), box: box);
            c.Enter();
            c.Choose(0); // eat now → +30% of 100 = +30 → 50

            Assert.That(p.CurrentHP, Is.EqualTo(50));
        }

        [Test]
        public void BerryBush_TakeBerries_Grants3Potions()
        {
            ConsumableSO potion = Make<ConsumableSO>();
            RunStateSO run = MakeRun();
            var pool = new List<MysteryEventSO> { MakeEvent("berry_bush") };

            MysteryEventNodeController c = Make(pool, run, items: Items(potion: potion));
            c.Enter();
            c.Choose(1);

            Assert.That(run.Inventory, Has.Count.EqualTo(3));
        }

        // ── Wandering Tutor (§7.9.2) ──────────────────────────────────────────

        [Test]
        public void WanderingTutor_Decline_Grants100Dollars()
        {
            RunStateSO run = MakeRun(dollars: 0);
            var pool = new List<MysteryEventSO> { MakeEvent("wandering_tutor") };
            MysteryEventNodeController c = Make(pool, run);
            c.Enter();
            c.Choose(1);
            Assert.That(run.PokeDollars, Is.EqualTo(100));
        }

        [Test]
        public void WanderingTutor_Accept_GrantsPlaceholderConsumable()
        {
            ConsumableSO placeholder = Make<ConsumableSO>();
            RunStateSO run = MakeRun();
            var pool = new List<MysteryEventSO> { MakeEvent("wandering_tutor") };
            MysteryEventNodeController c = Make(pool, run, items: Items(placeholder: placeholder));
            c.Enter();
            c.Choose(0);
            Assert.That(run.Inventory, Has.Member(placeholder));
        }

        // ── Slot Booth (§7.9.2, Gamble) ───────────────────────────────────────

        [Test]
        public void SlotBooth_Wager_NetMatchesOutcome()
        {
            RunStateSO run = MakeRun(dollars: 500);
            var pool = new List<MysteryEventSO> { MakeEvent("slot_booth", MysteryRiskProfile.Gamble) };
            MysteryEventNodeController c = Make(pool, run, seed: 42u);
            c.Enter();
            c.Choose(0);
            // Stake always paid; payout only on win. Invariant holds for any seed.
            int expected = 500 - 100 + (c.LastWagerWon ? 250 : 0);
            Assert.That(run.PokeDollars, Is.EqualTo(expected));
        }

        [Test]
        public void SlotBooth_Unaffordable_NoChange()
        {
            RunStateSO run = MakeRun(dollars: 50); // < 100 wager
            var pool = new List<MysteryEventSO> { MakeEvent("slot_booth") };
            MysteryEventNodeController c = Make(pool, run);
            c.Enter();
            c.Choose(0);
            Assert.That(run.PokeDollars, Is.EqualTo(50));
            Assert.That(c.LastWagerWon, Is.False);
        }

        // ── Mysterious Stone (§7.9.2) ─────────────────────────────────────────

        [Test]
        public void MysteriousStone_Take_GrantsRandomRelic()
        {
            RelicSO relic = Make<RelicSO>();
            RunStateSO run = MakeRun();
            var pool = new List<MysteryEventSO> { MakeEvent("mysterious_stone", MysteryRiskProfile.Tradeoff) };
            MysteryEventNodeController c = Make(pool, run, items: Items(relic: relic));
            c.Enter();
            c.Choose(0);
            Assert.That(run.HeldRelics, Has.Member(relic));
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [Test]
        public void Choose_CompletesCleared_AndMarksFired()
        {
            RunStateSO run = MakeRun();
            var pool = new List<MysteryEventSO> { MakeEvent("berry_bush") };
            MysteryEventNodeController c = Make(pool, run);
            c.Enter();
            c.Choose(1);
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
            Assert.That(run.EventFlags, Is.Not.Null);
            Assert.That(run.EventFlags.Count, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void Enter_SavesRun()
        {
            var pool = new List<MysteryEventSO> { MakeEvent("berry_bush") };
            MysteryEventNodeController c = Make(pool, MakeRun());
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }
    }
}
