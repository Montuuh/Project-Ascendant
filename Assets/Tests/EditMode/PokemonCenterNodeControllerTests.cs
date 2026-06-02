using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Combat;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.6 / §2.4.2 / §6.2.4 + Epic 9 Task 9.5 — Pokémon Center node + PokemonVitals.
    public class PokemonCenterNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Center_" + System.Guid.NewGuid().ToString("N"));
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

        private MoveSO MakeMove(string id)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.MoveId = id;
            _disposables.Add(m);
            return m;
        }

        private PokemonSpeciesSO MakeSpecies(int baseHp, params MoveSO[] tutor)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "sp";
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = baseHp, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.BaseLearnset = new List<MoveSO>();
            s.TutorLearnset = new List<MoveSO>(tutor);
            _disposables.Add(s);
            return s;
        }

        private PokemonInstance MakeInstance(PokemonSpeciesSO species, int hp, int trauma = 0, int level = 5)
        {
            PokemonInstance p = new() { Species = species, Level = level, CurrentHP = hp, TraumaStacks = trauma };
            return p;
        }

        private EconomyConfigSO MakeEconomy()
        {
            EconomyConfigSO e = ScriptableObject.CreateInstance<EconomyConfigSO>();
            e.TraumaStackPenaltyPercent = 5;
            e.TraumaStackCap = 5;
            e.TherapyBaseCost = 100;
            e.BoxCapacity = 6;
            _disposables.Add(e);
            return e;
        }

        private RunStateSO MakeRun(int dollars = 0)
        {
            RunStateSO r = ScriptableObject.CreateInstance<RunStateSO>();
            r.PokeDollars = dollars;
            _disposables.Add(r);
            return r;
        }

        private static MapNode CenterNode() => new MapNode(6, 0, 0, NodeType.Center);

        private PokemonCenterNodeController Make(Box box, EconomyConfigSO eco, RunStateSO run)
            => new(CenterNode(), run, box, eco);

        // ── PokemonVitals ─────────────────────────────────────────────────────

        [Test]
        public void Vitals_MaxHP_NoGrowth_IsBaseHP()
        {
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 50);
            Assert.That(PokemonVitals.MaxHP(p), Is.EqualTo(100));
        }

        [Test]
        public void Vitals_EffectiveMaxHP_NoTrauma_IsMaxHP()
        {
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 50, trauma: 0);
            Assert.That(PokemonVitals.EffectiveMaxHP(p, MakeEconomy()), Is.EqualTo(100));
        }

        [Test]
        public void Vitals_EffectiveMaxHP_TwoStacks_Minus10Percent()
        {
            // Per §2.4.2 — 2 stacks × 5% = 10% → 100 × 0.90 = 90.
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 50, trauma: 2);
            Assert.That(PokemonVitals.EffectiveMaxHP(p, MakeEconomy()), Is.EqualTo(90));
        }

        [Test]
        public void Vitals_EffectiveMaxHP_SoftCapAt5Stacks()
        {
            // Per §2.6 — 6 stacks clamp to the 5-stack cap (−25%) → 100 × 0.75 = 75.
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 50, trauma: 6);
            Assert.That(PokemonVitals.EffectiveMaxHP(p, MakeEconomy()), Is.EqualTo(75));
        }

        // ── Heal (9.5.2) ──────────────────────────────────────────────────────

        [Test]
        public void Heal_RevivesFainted_AndRestoresAll_ToEffectiveMaxHP()
        {
            EconomyConfigSO eco = MakeEconomy();
            Box box = new(6);
            PokemonInstance fainted = MakeInstance(MakeSpecies(100), hp: 0);          // fainted
            PokemonInstance hurtTrauma = MakeInstance(MakeSpecies(100), hp: 20, trauma: 2);
            box.Members.Add(fainted);
            box.Members.Add(hurtTrauma);

            Make(box, eco, MakeRun()).Heal();

            Assert.That(fainted.CurrentHP, Is.EqualTo(100), "Fainted Pokémon revived to EffectiveMaxHP (§2.4.2).");
            Assert.That(hurtTrauma.CurrentHP, Is.EqualTo(90), "Trauma-adjusted heal: 2 stacks → 90.");
        }

        [Test]
        public void Heal_CuresStatus_OnAllBoxMembers()
        {
            // Per §7.6.1 — "Full restore" = HP + status cure. Bug R2-3: a Box Pokémon with
            // Poison persisted after Heal(). This regression test locks the fix.
            EconomyConfigSO eco = MakeEconomy();
            Box box = new(6);
            PokemonInstance p1 = MakeInstance(MakeSpecies(100), hp: 50);
            PokemonInstance p2 = MakeInstance(MakeSpecies(100), hp: 30);
            p1.PrimaryStatus = StatusCondition.Poison; // poisoned
            p1.PrimaryStatusTurnsRemaining = int.MaxValue;
            p2.PrimaryStatus = StatusCondition.Burn;   // burned
            p2.PrimaryStatusTurnsRemaining = int.MaxValue;
            p2.SecondaryStatus = StatusCondition.Confusion; // also confused
            p2.SecondaryStatusTurnsRemaining = 3;
            box.Members.Add(p1);
            box.Members.Add(p2);

            Make(box, eco, MakeRun()).Heal();

            Assert.That(p1.CurrentHP, Is.EqualTo(100), "HP restored.");
            Assert.That(p1.PrimaryStatus, Is.EqualTo(StatusCondition.None), "Poison cured (Bug R2-3 fix).");
            Assert.That(p1.PrimaryStatusTurnsRemaining, Is.EqualTo(0));
            Assert.That(p2.CurrentHP, Is.EqualTo(100), "HP restored.");
            Assert.That(p2.PrimaryStatus, Is.EqualTo(StatusCondition.None), "Burn cured.");
            Assert.That(p2.SecondaryStatus, Is.EqualTo(StatusCondition.None), "Confusion cured.");
            Assert.That(p2.SecondaryStatusTurnsRemaining, Is.EqualTo(0));
        }

        // ── Move Tutor (9.5.3) ────────────────────────────────────────────────

        [Test]
        public void Tutor_OfferMoves_Returns3_ExcludesKnown()
        {
            MoveSO m1 = MakeMove("t1"), m2 = MakeMove("t2"), m3 = MakeMove("t3"), m4 = MakeMove("t4");
            PokemonSpeciesSO sp = MakeSpecies(100, m1, m2, m3, m4);
            PokemonInstance p = MakeInstance(sp, hp: 100);
            p.CurrentMoves.Add(m1); // already known → excluded

            List<MoveSO> offer = Make(new Box(6), MakeEconomy(), MakeRun()).OfferTutorMoves(p);
            Assert.That(offer.Count, Is.EqualTo(3));
            Assert.That(offer, Has.No.Member(m1));
        }

        [Test]
        public void Tutor_LearnMove_ReplacesSlot_OncePerVisit()
        {
            MoveSO known = MakeMove("k"), tutored = MakeMove("new");
            PokemonSpeciesSO sp = MakeSpecies(100, tutored);
            PokemonInstance p = MakeInstance(sp, hp: 100);
            p.CurrentMoves.Add(MakeMove("a"));
            p.CurrentMoves.Add(known);
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), MakeRun());

            bool first = c.LearnMove(p, tutored, slotIndex: 1);
            Assert.That(first, Is.True);
            Assert.That(p.CurrentMoves[1], Is.SameAs(tutored));
            Assert.That(c.TutorUsed, Is.True);

            // Second learn this visit is refused.
            bool second = c.LearnMove(p, MakeMove("other"), slotIndex: 0);
            Assert.That(second, Is.False);
        }

        [Test]
        // §5.7.1 / Task 10.8 — TutorLearnset is stage-aware: evolving (Species swap) recomputes the
        // offer. A Wartortle-stage species offers a broader pool than the Squirtle-stage one.
        public void Tutor_Offer_RecomputesOnEvolution()
        {
            MoveSO a = MakeMove("a"), b = MakeMove("b"), c = MakeMove("c"), d = MakeMove("d");
            PokemonSpeciesSO squirtleStage = MakeSpecies(100, a, b);       // narrow base pool
            PokemonSpeciesSO wartortleStage = MakeSpecies(120, a, b, c, d); // broader evolved pool
            PokemonInstance p = MakeInstance(squirtleStage, hp: 50);
            PokemonCenterNodeController ctrl = Make(new Box(6), MakeEconomy(), MakeRun());

            Assert.That(ctrl.OfferTutorMoves(p), Has.Count.EqualTo(2));

            p.Species = wartortleStage; // EvolutionExecutor swaps Species in-run
            List<MoveSO> after = ctrl.OfferTutorMoves(p);
            Assert.That(after, Has.Count.EqualTo(3), "Offer caps at MOVE_TUTOR_OFFER but is now larger.");
            Assert.That(after, Has.Member(c), "An evolved-stage tutor move is now offered.");
        }

        [Test]
        public void Tutor_LearnMove_InvalidSlot_False()
        {
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 100);
            p.CurrentMoves.Add(MakeMove("a"));
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), MakeRun());
            Assert.That(c.LearnMove(p, MakeMove("x"), slotIndex: 5), Is.False);
        }

        // ── Therapy (9.5.4) ───────────────────────────────────────────────────

        [Test]
        public void Therapy_Cost_Escalates_WithStacks()
        {
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), MakeRun());
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 100, trauma: 2);
            Assert.That(c.TherapyCost(p), Is.EqualTo(300)); // 100 × (1 + 2)
        }

        [Test]
        public void Therapy_RemovesOneStack_SpendsDollars()
        {
            RunStateSO run = MakeRun(dollars: 500);
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), run);
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 100, trauma: 2);

            bool ok = c.Therapy(p);
            Assert.That(ok, Is.True);
            Assert.That(p.TraumaStacks, Is.EqualTo(1));
            Assert.That(run.PokeDollars, Is.EqualTo(200)); // 500 − 300
        }

        [Test]
        public void Therapy_Unaffordable_NoChange()
        {
            RunStateSO run = MakeRun(dollars: 100);
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), run);
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 100, trauma: 2); // cost 300

            Assert.That(c.Therapy(p), Is.False);
            Assert.That(p.TraumaStacks, Is.EqualTo(2));
            Assert.That(run.PokeDollars, Is.EqualTo(100));
        }

        [Test]
        public void Therapy_NoStacks_False()
        {
            RunStateSO run = MakeRun(dollars: 500);
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), run);
            PokemonInstance p = MakeInstance(MakeSpecies(100), hp: 100, trauma: 0);
            Assert.That(c.Therapy(p), Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(500));
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [Test]
        public void Leave_CompletesCleared()
        {
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), MakeRun());
            c.Enter();
            c.Leave();
            Assert.That(c.IsCompleted, Is.True);
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void Enter_SavesRun()
        {
            PokemonCenterNodeController c = Make(new Box(6), MakeEconomy(), MakeRun());
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }
    }
}
