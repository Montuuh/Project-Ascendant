using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per §5.2 + Epic 10 Task 10.1 — XP award + leveling foundation (XPAwarder / LevelUpResolver).
    public class ProgressionTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>(); _disposables.Add(o); return o;
        }

        private ProgressionConfigSO Config()
        {
            ProgressionConfigSO c = Make<ProgressionConfigSO>();
            c.WildXP = 10; c.TrainerXP = 20; c.EliteXP = 40; c.GymXP = 80;
            c.LevelUpBaseXP = 10; c.LevelUpSlopeXP = 0; // flat 10 XP per level for clean arithmetic
            return c;
        }

        private PokemonSpeciesSO Species(int baseHp, int hpPerLevel, int evolveLevel, bool hasBranch)
        {
            StatGrowthCurveSO curve = Make<StatGrowthCurveSO>();
            int[] g = new int[60];
            for (int i = 0; i < g.Length; i++) g[i] = hpPerLevel;
            curve.HPGrowthPerLevel = g;
            curve.AttackGrowthPerLevel = g;
            curve.DefenseGrowthPerLevel = g;

            PokemonSpeciesSO sp = Make<PokemonSpeciesSO>();
            sp.DisplayName = "Test";
            sp.BaseStats = new BaseStats { BaseHP = baseHp, BaseAtk = 20, BaseDef = 20, BaseSpd = 20 };
            sp.GrowthCurve = curve;
            sp.EvolveLevel = evolveLevel;
            sp.Branches = hasBranch ? new List<EvolutionBranchSO> { Make<EvolutionBranchSO>() } : new List<EvolutionBranchSO>();
            return sp;
        }

        private PokemonInstance Mon(PokemonSpeciesSO sp, int level)
            => new() { Species = sp, Level = level, CurrentHP = PokemonVitals.MaxHP(new PokemonInstance { Species = sp, Level = level }) };

        // ── XPAwarder ─────────────────────────────────────────────────────────

        [Test]
        // §5.2.1 — every Active-Team member is credited; nulls skipped.
        public void Award_CreditsEachActiveMember_SkipsNull()
        {
            PokemonSpeciesSO sp = Species(40, 4, 0, false);
            PokemonInstance a = Mon(sp, 5), b = Mon(sp, 5);
            List<PokemonInstance> team = new() { a, null, b };

            int credited = XPAwarder.Award(team, 15);

            Assert.That(credited, Is.EqualTo(2));
            Assert.That(a.CurrentXP, Is.EqualTo(15));
            Assert.That(b.CurrentXP, Is.EqualTo(15));
        }

        [Test]
        public void AwardForNode_UsesTierXP()
        {
            ProgressionConfigSO cfg = Config();
            PokemonInstance a = Mon(Species(40, 4, 0, false), 5);
            XPAwarder.AwardForNode(new List<PokemonInstance> { a }, NodeType.Gym, cfg);
            Assert.That(a.CurrentXP, Is.EqualTo(80));
        }

        // ── AwardToBench (CL-010) ────────────────────────────────────────────────

        [Test]
        // §5.12.5 (CL-010) — every benched Box mon earns floor(activeXp · 0.75); active members skipped.
        public void AwardToBench_CreditsBenchAt75Percent_SkipsActiveMembers()
        {
            ProgressionConfigSO cfg = Config();
            cfg.LevelUpBaseXP = 10000; // suppress level-up so CurrentXP holds the raw credit
            PokemonSpeciesSO sp = Species(40, 4, 0, false);
            PokemonInstance active = Mon(sp, 5), benchA = Mon(sp, 5), benchB = Mon(sp, 5);
            List<PokemonInstance> box = new() { active, benchA, benchB };
            List<PokemonInstance> team = new() { active };

            int credited = XPAwarder.AwardToBench(box, team, 40, cfg.BenchXpShare, cfg);

            Assert.That(credited, Is.EqualTo(2));
            Assert.That(active.CurrentXP, Is.EqualTo(0), "active member earns full XP via Award, not the bench path");
            Assert.That(benchA.CurrentXP, Is.EqualTo(30), "floor(40 · 0.75)");
            Assert.That(benchB.CurrentXP, Is.EqualTo(30));
        }

        [Test]
        // §8.3.3 (CL-010) — Exp Share lifts the bench fraction to 1.0 → benched mons earn 100%.
        public void AwardToBench_ExpShareFraction_LiftsBenchToFullXP()
        {
            ProgressionConfigSO cfg = Config();
            cfg.LevelUpBaseXP = 10000; // suppress level-up so CurrentXP holds the raw credit
            PokemonInstance bench = Mon(Species(40, 4, 0, false), 5);
            List<PokemonInstance> box = new() { bench };

            XPAwarder.AwardToBench(box, null, 40, cfg.ExpShareBoxFraction, cfg);

            Assert.That(bench.CurrentXP, Is.EqualTo(40), "Exp Share fraction is 1.0");
        }

        [Test]
        // §5.12.5 (CL-010) — benched mons level up off-screen (LevelUpResolver runs on the bench too).
        public void AwardToBench_ProcessesBenchLevelUp_OffScreen()
        {
            ProgressionConfigSO cfg = Config(); // flat 10 XP per level
            PokemonInstance bench = Mon(Species(40, 4, 0, false), 1);
            List<PokemonInstance> box = new() { bench };

            XPAwarder.AwardToBench(box, null, 40, cfg.BenchXpShare, cfg); // floor(40·0.75)=30 XP → 3 levels

            Assert.That(bench.Level, Is.EqualTo(4));
            Assert.That(bench.CurrentXP, Is.EqualTo(0));
        }

        [Test]
        // Guards: null box / non-positive XP / zero fraction are no-ops.
        public void AwardToBench_GuardsNoOp()
        {
            ProgressionConfigSO cfg = Config();
            PokemonInstance bench = Mon(Species(40, 4, 0, false), 5);
            List<PokemonInstance> box = new() { bench };

            Assert.That(XPAwarder.AwardToBench(null, null, 40, 0.75f, cfg), Is.EqualTo(0));
            Assert.That(XPAwarder.AwardToBench(box, null, 0, 0.75f, cfg), Is.EqualTo(0));
            Assert.That(XPAwarder.AwardToBench(box, null, 40, 0f, cfg), Is.EqualTo(0));
            Assert.That(bench.CurrentXP, Is.EqualTo(0));
        }

        // ── LevelUpResolver ───────────────────────────────────────────────────

        [Test]
        // §5.2.2 — cumulative XP converts to the right number of levels; remainder carries.
        public void Process_ConvertsXPToLevels_AndCarriesRemainder()
        {
            ProgressionConfigSO cfg = Config(); // 10 XP per level (flat)
            PokemonInstance p = Mon(Species(40, 4, 0, false), 5);
            p.CurrentXP = 25; // 2 level-ups (10+10), 5 carry

            LevelUpResolver.Result r = LevelUpResolver.Process(p, cfg);

            Assert.That(r.LevelsGained, Is.EqualTo(2));
            Assert.That(p.Level, Is.EqualTo(7));
            Assert.That(p.CurrentXP, Is.EqualTo(5));
        }

        [Test]
        // §5.2.3 — level-ups raise MaxHP via the curve and heal CurrentHP by the growth.
        public void Process_AppliesStatGrowth_AndHeals()
        {
            ProgressionConfigSO cfg = Config();
            PokemonInstance p = Mon(Species(40, 4, 0, false), 5); // MaxHP at Lv5 = 40 + 4*4 = 56
            int hpBefore = p.CurrentHP;
            p.CurrentXP = 10; // exactly one level-up

            LevelUpResolver.Result r = LevelUpResolver.Process(p, cfg);

            Assert.That(p.Level, Is.EqualTo(6));
            Assert.That(PokemonVitals.MaxHP(p), Is.EqualTo(60)); // 40 + 5*4
            Assert.That(r.HPGained, Is.EqualTo(4));
            Assert.That(p.CurrentHP, Is.EqualTo(hpBefore + 4));
        }

        [Test]
        // §2.4.1 — a fainted Pokémon levels (MaxHP rises) but is NOT revived by the level-up heal.
        public void Process_FaintedPokemon_LevelsButStaysFainted()
        {
            ProgressionConfigSO cfg = Config();
            PokemonInstance p = Mon(Species(40, 4, 0, false), 5);
            p.CurrentHP = 0; // fainted
            p.CurrentXP = 10;

            LevelUpResolver.Result r = LevelUpResolver.Process(p, cfg);

            Assert.That(p.Level, Is.EqualTo(6));
            Assert.That(p.CurrentHP, Is.EqualTo(0), "A level-up never revives a fainted Pokémon.");
            Assert.That(r.HPGained, Is.EqualTo(0));
        }

        // ── CL-006 level-gated learnset (§5.12.1) ─────────────────────────────

        [Test]
        // §5.12.1 — crossing a learnset threshold learns the move into the pool and auto-equips
        // it while an active slot is free.
        public void Process_CrossesLearnsetThreshold_LearnsMoveIntoPoolAndActive()
        {
            ProgressionConfigSO cfg = Config(); // flat 10 XP/level
            MoveSO m1 = Make<MoveSO>(), m2 = Make<MoveSO>(), m8 = Make<MoveSO>();
            PokemonSpeciesSO sp = Species(40, 4, 0, false);
            sp.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = m1 },
                new LevelUpEntry { Level = 1, Move = m2 },
                new LevelUpEntry { Level = 8, Move = m8 },
            };
            PokemonInstance p = Mon(sp, 5);
            p.LearnedMoves.Add(m1); p.LearnedMoves.Add(m2);   // known-at-L5 (as the factory seeds)
            p.CurrentMoves.Add(m1); p.CurrentMoves.Add(m2);
            p.CurrentXP = 30; // 5 -> 8

            LevelUpResolver.Result r = LevelUpResolver.Process(p, cfg);

            Assert.That(p.Level, Is.EqualTo(8));
            Assert.That(p.LearnedMoves, Has.Member(m8), "learned into the pool");
            Assert.That(p.CurrentMoves, Has.Member(m8), "auto-equipped (a slot was free)");
            Assert.That(r.MovesLearned, Is.Not.Null);
            Assert.That(r.MovesLearned, Has.Member(m8));
        }

        [Test]
        // §5.12.1 / §5.10.2 — when the active 4 are full, a newly learned move goes to the pool
        // only; the player picks it via the Move Manager.
        public void Process_LearnsBeyondFourActive_GoesToPoolOnly()
        {
            ProgressionConfigSO cfg = Config();
            MoveSO a = Make<MoveSO>(), b = Make<MoveSO>(), c = Make<MoveSO>(), d = Make<MoveSO>(), e = Make<MoveSO>();
            PokemonSpeciesSO sp = Species(40, 4, 0, false);
            sp.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = a }, new LevelUpEntry { Level = 1, Move = b },
                new LevelUpEntry { Level = 1, Move = c }, new LevelUpEntry { Level = 1, Move = d },
                new LevelUpEntry { Level = 8, Move = e },
            };
            PokemonInstance p = Mon(sp, 5);
            p.LearnedMoves.AddRange(new[] { a, b, c, d });
            p.CurrentMoves.AddRange(new[] { a, b, c, d }); // 4 active already
            p.CurrentXP = 30;

            LevelUpResolver.Process(p, cfg);

            Assert.That(p.LearnedMoves, Has.Member(e), "still learned into the pool");
            Assert.That(p.CurrentMoves, Has.No.Member(e), "not auto-equipped past 4");
            Assert.That(p.CurrentMoves.Count, Is.EqualTo(4));
        }

        [Test]
        // §5.3.1 — crossing EvolveLevel flags the Pokémon evolution-eligible (only if it has branches).
        public void Process_CrossingEvolveLevel_FlagsEligible()
        {
            ProgressionConfigSO cfg = Config();
            PokemonInstance p = Mon(Species(40, 4, evolveLevel: 7, hasBranch: true), 5);
            Assert.That(LevelUpResolver.IsEvolutionEligible(p), Is.False);

            p.CurrentXP = 20; // 5 → 7
            LevelUpResolver.Result r = LevelUpResolver.Process(p, cfg);

            Assert.That(p.Level, Is.EqualTo(7));
            Assert.That(r.EvolutionUnlocked, Is.True);
            Assert.That(LevelUpResolver.IsEvolutionEligible(p), Is.True);
        }

        [Test]
        // A final-form species (no branches) is never evolution-eligible even past a stray EvolveLevel.
        public void IsEvolutionEligible_NoBranches_AlwaysFalse()
        {
            PokemonInstance p = Mon(Species(40, 4, evolveLevel: 1, hasBranch: false), 20);
            Assert.That(LevelUpResolver.IsEvolutionEligible(p), Is.False);
        }

        // ── EvolutionExecutor (Task 10.4, §5.3.5) ─────────────────────────────

        [Test]
        // §5.3.5 / §5.10.3 / §4.3.9.2 / §6.2.3 — evolving upgrades active moves in place, advances the
        // species + stage, grants the branch ability, upgrades Mastery, and carries Trauma.
        public void Evolve_UpgradesMoves_AdvancesStage_NoAbilityGrant_CarriesTrauma()
        {
            MoveSO tackle = Make<MoveSO>();
            MoveSO skullBash = Make<MoveSO>();
            MoveSO waterGun = Make<MoveSO>();
            MoveSO evolvedMastery = Make<MoveSO>();
            AbilitySO torrent = Make<AbilitySO>();

            PokemonSpeciesSO evolved = Species(60, 4, 0, false); // Wartortle-ish, no further branch
            evolved.MasteryMove = evolvedMastery;

            EvolutionBranchSO branch = Make<EvolutionBranchSO>();
            branch.EvolvedSpecies = evolved;
            branch.MoveUpgrades = new List<MoveUpgradePair> { new() { OldMove = tackle, NewMove = skullBash } };
            branch.NewMoves = new List<MoveSO>();
            branch.GrantedAbility = torrent;

            PokemonInstance p = new()
            {
                Species = Species(40, 4, 12, true),
                Level = 12,
                CurrentHP = 30,
                CurrentStage = EvolutionStage.Basic,
                TraumaStacks = 2,
            };
            p.CurrentMoves.Add(tackle);
            p.CurrentMoves.Add(waterGun);

            EvolutionExecutor.Result r = EvolutionExecutor.Evolve(p, branch);

            Assert.That(r.Evolved, Is.True);
            Assert.That(p.Species, Is.SameAs(evolved));
            Assert.That(p.CurrentStage, Is.EqualTo(EvolutionStage.Stage1));
            Assert.That(p.CurrentMoves, Has.Member(skullBash), "Active Tackle upgrades in place.");
            Assert.That(p.CurrentMoves, Has.No.Member(tackle));
            Assert.That(p.CurrentMoves, Has.Member(waterGun), "Unaffected moves are retained.");
            Assert.That(p.Ability, Is.Null,
                "CL-007 (§5.12.2/§5.12.3) — evolution grants NO ability even though the branch records one; abilities come from the Dojo.");
            Assert.That(p.MasteryMove, Is.SameAs(evolvedMastery), "Mastery upgrades to evolved stage (§4.3.9.2).");
            Assert.That(p.SelectedBranch, Is.SameAs(branch), "Branch recorded (§5.12.2 — record only, no longer a path lock).");
            Assert.That(p.TraumaStacks, Is.EqualTo(2), "Trauma carries through (§6.2.3).");
        }

        // Task 10.4.5 — evolving broadcasts EvolutionTriggeredContext on the EventBus.
        [Test]
        public void Evolve_PublishesEvolutionTriggeredEvent()
        {
            EventBus.Clear();
            EvolutionTriggeredContext got = default;
            int fired = 0;
            void Handler(EvolutionTriggeredContext c) { got = c; fired++; }
            EventBus.Subscribe<EvolutionTriggeredContext>(Handler);

            PokemonSpeciesSO from = Species(40, 4, 12, true);
            PokemonSpeciesSO to = Species(60, 4, 0, false);
            EvolutionBranchSO branch = Make<EvolutionBranchSO>();
            branch.EvolvedSpecies = to;
            branch.MoveUpgrades = new List<MoveUpgradePair>();
            branch.NewMoves = new List<MoveSO>();
            PokemonInstance p = new() { Species = from, Level = 12, CurrentHP = 20, CurrentStage = EvolutionStage.Basic };

            EvolutionExecutor.Evolve(p, branch);
            EventBus.Unsubscribe<EvolutionTriggeredContext>(Handler);

            Assert.That(fired, Is.EqualTo(1), "Event fired exactly once.");
            Assert.That(got.From, Is.SameAs(from));
            Assert.That(got.To, Is.SameAs(to));
            Assert.That(got.Branch, Is.SameAs(branch));
            Assert.That(got.Pokemon, Is.SameAs(p));
        }

        // Task 10.5 — Evolution Items add item-gated branches; none ship in the VS (empty inventory).
        [Test]
        public void EvolutionOptions_LevelOnly_WhenNoItems()
        {
            PokemonSpeciesSO sp = Species(40, 4, 12, true); // one level branch
            PokemonInstance p = new() { Species = sp, Level = 12 };

            List<EvolutionOptions.Option> opts = EvolutionOptions.For(p, null);

            Assert.That(opts.Count, Is.EqualTo(1));
            Assert.That(opts[0].IsItemGated, Is.False, "Level branch is not item-gated.");
            Assert.That(opts[0].Branch, Is.SameAs(sp.Branches[0]));
        }

        [Test]
        public void EvolutionOptions_ItemGated_AddsBranch_AndConsumes()
        {
            PokemonSpeciesSO sp = Species(40, 4, 12, true); // one level branch
            EvolutionBranchSO itemBranch = Make<EvolutionBranchSO>();
            EvolutionItemSO item = Make<EvolutionItemSO>();
            item.EnabledBranch = itemBranch;
            RunStateSO run = Make<RunStateSO>();
            run.OwnedEvolutionItems = new List<EvolutionItemSO> { item };
            PokemonInstance p = new() { Species = sp, Level = 12 };

            List<EvolutionOptions.Option> opts = EvolutionOptions.For(p, run.OwnedEvolutionItems);

            Assert.That(opts.Count, Is.EqualTo(2), "Level branch + item-gated branch.");
            EvolutionOptions.Option gated = opts.Find(o => o.IsItemGated);
            Assert.That(gated.Branch, Is.SameAs(itemBranch));
            Assert.That(gated.SourceItem, Is.SameAs(item));

            EvolutionOptions.ConsumeItem(run, item);
            Assert.That(run.OwnedEvolutionItems, Has.No.Member(item), "Item consumed on use (10.5.3).");
        }

        [Test]
        public void Evolve_NullBranchOrSpecies_NoChange()
        {
            PokemonInstance p = new() { Species = Species(40, 4, 12, true), Level = 12 };
            PokemonSpeciesSO before = p.Species;
            Assert.That(EvolutionExecutor.Evolve(p, null).Evolved, Is.False);
            Assert.That(EvolutionExecutor.Evolve(null, Make<EvolutionBranchSO>()).Evolved, Is.False);
            Assert.That(p.Species, Is.SameAs(before));
        }
    }
}
