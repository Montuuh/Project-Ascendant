using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.14 (CL-009) + §5.12.3 (CL-008) — Dojo node: teaches moves and abilities for ₽.
    public class DojoNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Dojo_" + System.Guid.NewGuid().ToString("N"));
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

        private AbilitySO MakeAbility(string id)
        {
            AbilitySO a = ScriptableObject.CreateInstance<AbilitySO>();
            a.AbilityId = id;
            _disposables.Add(a);
            return a;
        }

        private PokemonSpeciesSO MakeSpecies(params MoveSO[] tutorMoves)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = "sp_" + System.Guid.NewGuid().ToString("N")[..6];
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = 100, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.BaseLearnset = new List<MoveSO>();
            s.TutorLearnset = new List<MoveSO>(tutorMoves);
            s.AvailableAbilities = new List<AbilitySO>();
            _disposables.Add(s);
            return s;
        }

        private EconomyConfigSO MakeEconomy(int moveCost = 150, int abilityCost = 200)
        {
            EconomyConfigSO e = ScriptableObject.CreateInstance<EconomyConfigSO>();
            e.DojoMoveCost = moveCost;
            e.DojoAbilityCost = abilityCost;
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

        private static MapNode DojoNode() => new MapNode(5, 0, 0, NodeType.Dojo);

        private DojoNodeController Make(Box box, EconomyConfigSO eco, RunStateSO run)
            => new(DojoNode(), run, box, eco);

        private PokemonInstance MakeInstance(PokemonSpeciesSO species, int hp = 100)
        {
            PokemonInstance p = new() { Species = species, Level = 5, CurrentHP = hp };
            return p;
        }

        // ── Pricing ───────────────────────────────────────────────────────────

        [Test]
        public void MoveCost_ReturnsEconomyValue()
        {
            // Per §7.14 — cost comes from EconomyConfigSO, not an inline literal.
            DojoNodeController dojo = Make(new Box(6), MakeEconomy(moveCost: 120), MakeRun());
            Assert.That(dojo.MoveCost(), Is.EqualTo(120));
        }

        [Test]
        public void AbilityCost_ReturnsEconomyValue()
        {
            // Per §7.14 — cost comes from EconomyConfigSO.
            DojoNodeController dojo = Make(new Box(6), MakeEconomy(abilityCost: 250), MakeRun());
            Assert.That(dojo.AbilityCost(), Is.EqualTo(250));
        }

        // ── OfferMoves ────────────────────────────────────────────────────────

        [Test]
        public void OfferMoves_ReturnsFullTutorPool_ExcludesLearned()
        {
            // Per §7.14 — no cap (unlike old Center which capped at 3); full pool minus already learned.
            MoveSO m1 = MakeMove("iron_tail"), m2 = MakeMove("blizzard"), m3 = MakeMove("zen_headbutt");
            PokemonSpeciesSO sp = MakeSpecies(m1, m2, m3);
            PokemonInstance p = MakeInstance(sp);
            p.LearnedMoves.Add(m1); // already learned

            List<MoveSO> offer = Make(new Box(6), MakeEconomy(), MakeRun()).OfferMoves(p);
            Assert.That(offer.Count, Is.EqualTo(2));
            Assert.That(offer, Has.No.Member(m1));
            Assert.That(offer, Has.Member(m2));
            Assert.That(offer, Has.Member(m3));
        }

        [Test]
        public void OfferMoves_EmptySpecies_ReturnsEmpty()
        {
            // Per §7.14 — null/empty TutorLearnset returns empty offer gracefully.
            PokemonSpeciesSO sp = MakeSpecies(); // no tutor moves
            Assert.That(Make(new Box(6), MakeEconomy(), MakeRun()).OfferMoves(MakeInstance(sp)), Is.Empty);
        }

        // ── OfferAbilities ────────────────────────────────────────────────────

        [Test]
        public void OfferAbilities_ReturnsAvailableAbilities()
        {
            // Per §5.12.3 (CL-008) — abilities come from species.AvailableAbilities pool.
            AbilitySO a1 = MakeAbility("overgrow"), a2 = MakeAbility("chlorophyll");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { a1, a2 };
            PokemonInstance p = MakeInstance(sp);

            List<AbilitySO> offer = Make(new Box(6), MakeEconomy(), MakeRun()).OfferAbilities(p);
            Assert.That(offer, Is.EquivalentTo(new[] { a1, a2 }));
        }

        [Test]
        public void OfferAbilities_IncludesCurrentAbility_SoSwapIsPossible()
        {
            // Per §7.14 — already-equipped ability still listed (player can swap via another ability).
            // TeachAbility rejects same-ability re-purchase, but the offer includes it so UI can show it.
            AbilitySO a1 = MakeAbility("overgrow"), a2 = MakeAbility("chlorophyll");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { a1, a2 };
            PokemonInstance p = MakeInstance(sp);
            p.Ability = a1; // already equipped

            List<AbilitySO> offer = Make(new Box(6), MakeEconomy(), MakeRun()).OfferAbilities(p);
            Assert.That(offer, Has.Member(a1), "Already-equipped ability still appears so the UI can show it.");
        }

        // ── TeachMove ─────────────────────────────────────────────────────────

        [Test]
        public void TeachMove_AddsToLearnedPool_DeductsCost()
        {
            // Per §7.14 / §5.10.1 — move is added to the pool; ₽ is deducted.
            MoveSO iron = MakeMove("iron_tail");
            PokemonSpeciesSO sp = MakeSpecies(iron);
            PokemonInstance p = MakeInstance(sp);
            RunStateSO run = MakeRun(dollars: 500);
            DojoNodeController dojo = Make(new Box(6), MakeEconomy(moveCost: 150), run);

            bool ok = dojo.TeachMove(p, iron);

            Assert.That(ok, Is.True);
            Assert.That(p.LearnedMoves, Contains.Item(iron), "Move added to Learned Move Pool (§5.10.1).");
            Assert.That(run.PokeDollars, Is.EqualTo(350), "Cost deducted.");
        }

        [Test]
        public void TeachMove_InsufficientFunds_ReturnsFalse_NoChange()
        {
            // Per §7.14 — if the player cannot afford it, no change.
            MoveSO iron = MakeMove("iron_tail");
            PokemonSpeciesSO sp = MakeSpecies(iron);
            PokemonInstance p = MakeInstance(sp);
            RunStateSO run = MakeRun(dollars: 50); // less than DojoMoveCost 150

            bool ok = Make(new Box(6), MakeEconomy(moveCost: 150), run).TeachMove(p, iron);

            Assert.That(ok, Is.False);
            Assert.That(p.LearnedMoves, Has.No.Member(iron));
            Assert.That(run.PokeDollars, Is.EqualTo(50));
        }

        [Test]
        public void TeachMove_AlreadyLearned_ReturnsFalse()
        {
            // Per §5.10.1 — duplicates are rejected.
            MoveSO iron = MakeMove("iron_tail");
            PokemonSpeciesSO sp = MakeSpecies(iron);
            PokemonInstance p = MakeInstance(sp);
            p.LearnedMoves.Add(iron); // already in pool
            RunStateSO run = MakeRun(dollars: 500);

            bool ok = Make(new Box(6), MakeEconomy(), run).TeachMove(p, iron);

            Assert.That(ok, Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(500), "No charge for rejected teach.");
        }

        [Test]
        public void TeachMove_NotInTutorPool_ReturnsFalse()
        {
            // Per §7.14 — only moves in TutorLearnset may be taught.
            MoveSO iron = MakeMove("iron_tail");
            MoveSO blizzard = MakeMove("blizzard"); // not in tutor pool
            PokemonSpeciesSO sp = MakeSpecies(iron); // pool = only iron
            PokemonInstance p = MakeInstance(sp);
            RunStateSO run = MakeRun(dollars: 500);

            bool ok = Make(new Box(6), MakeEconomy(), run).TeachMove(p, blizzard);

            Assert.That(ok, Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(500));
        }

        // ── TeachAbility ──────────────────────────────────────────────────────

        [Test]
        public void TeachAbility_SetsAbilitySlot_DeductsCost()
        {
            // Per §5.12.3 (CL-008) / §7.14 — ability slot set; ₽ deducted.
            AbilitySO overgrow = MakeAbility("overgrow");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { overgrow };
            PokemonInstance p = MakeInstance(sp);
            RunStateSO run = MakeRun(dollars: 500);

            bool ok = Make(new Box(6), MakeEconomy(abilityCost: 200), run).TeachAbility(p, overgrow);

            Assert.That(ok, Is.True);
            Assert.That(p.Ability, Is.SameAs(overgrow), "Ability slot set to the taught ability (§5.12.3).");
            Assert.That(run.PokeDollars, Is.EqualTo(300), "Cost deducted.");
        }

        [Test]
        public void TeachAbility_SwapsExisting_DeductsCost()
        {
            // Per §7.14 — a second ability can replace the first (Pokémon has one passive slot, §5.5).
            AbilitySO overgrow = MakeAbility("overgrow"), chloro = MakeAbility("chlorophyll");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { overgrow, chloro };
            PokemonInstance p = MakeInstance(sp);
            p.Ability = overgrow; // currently equipped
            RunStateSO run = MakeRun(dollars: 500);

            bool ok = Make(new Box(6), MakeEconomy(abilityCost: 200), run).TeachAbility(p, chloro);

            Assert.That(ok, Is.True);
            Assert.That(p.Ability, Is.SameAs(chloro), "Ability swapped.");
            Assert.That(run.PokeDollars, Is.EqualTo(300));
        }

        [Test]
        public void TeachAbility_AlreadyEquipped_ReturnsFalse()
        {
            // Per §7.14 — teaching the same ability that is already equipped is a no-op.
            AbilitySO overgrow = MakeAbility("overgrow");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { overgrow };
            PokemonInstance p = MakeInstance(sp);
            p.Ability = overgrow;
            RunStateSO run = MakeRun(dollars: 500);

            bool ok = Make(new Box(6), MakeEconomy(), run).TeachAbility(p, overgrow);

            Assert.That(ok, Is.False, "Re-teaching the same ability rejected.");
            Assert.That(run.PokeDollars, Is.EqualTo(500), "No charge for no-op.");
        }

        [Test]
        public void TeachAbility_InsufficientFunds_ReturnsFalse_NoChange()
        {
            // Per §7.14 — player cannot afford it; no change.
            AbilitySO overgrow = MakeAbility("overgrow");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { overgrow };
            PokemonInstance p = MakeInstance(sp);
            RunStateSO run = MakeRun(dollars: 50); // less than 200

            bool ok = Make(new Box(6), MakeEconomy(abilityCost: 200), run).TeachAbility(p, overgrow);

            Assert.That(ok, Is.False);
            Assert.That(p.Ability, Is.Null, "Ability slot unchanged.");
            Assert.That(run.PokeDollars, Is.EqualTo(50));
        }

        [Test]
        public void TeachAbility_NotInPool_ReturnsFalse()
        {
            // Per §5.12.3 — only abilities in species.AvailableAbilities may be taught.
            AbilitySO overgrow = MakeAbility("overgrow"), other = MakeAbility("blaze");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.AvailableAbilities = new List<AbilitySO> { overgrow }; // only overgrow
            PokemonInstance p = MakeInstance(sp);
            RunStateSO run = MakeRun(dollars: 500);

            bool ok = Make(new Box(6), MakeEconomy(), run).TeachAbility(p, other);

            Assert.That(ok, Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(500));
        }

        // ── CL-008 — factory no longer auto-assigns ability ───────────────────

        [Test]
        public void Factory_Create_AbilityIsNull_UnderCL008()
        {
            // Per §5.12.3 (CL-008) — PokemonInstanceFactory.Create() must NOT auto-assign
            // PrimaryAbility; abilities are earned via the Dojo, not granted at creation.
            AbilitySO overgrow = MakeAbility("overgrow");
            PokemonSpeciesSO sp = MakeSpecies();
            sp.PrimaryAbility = overgrow; // legacy field set — must NOT be auto-assigned
            sp.Types = new List<PokemonType> { PokemonType.Grass };

            PokemonInstanceFactory factory = new();
            PokemonInstance p = factory.Create(sp, level: 5);

            Assert.That(p.Ability, Is.Null, "CL-008: ability must be null on a freshly-created instance (§5.12.3).");
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [Test]
        public void Leave_CompletesCleared()
        {
            DojoNodeController dojo = Make(new Box(6), MakeEconomy(), MakeRun());
            dojo.Enter();
            dojo.Leave();
            Assert.That(dojo.IsCompleted, Is.True);
            Assert.That(dojo.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void Enter_SavesRun()
        {
            DojoNodeController dojo = Make(new Box(6), MakeEconomy(), MakeRun());
            dojo.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }
    }
}
