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
        public void Evolve_UpgradesMoves_AdvancesStage_GrantsAbility_CarriesTrauma()
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
            Assert.That(p.Ability, Is.SameAs(torrent), "Branch ability granted (§5.5.1).");
            Assert.That(p.MasteryMove, Is.SameAs(evolvedMastery), "Mastery upgrades to evolved stage (§4.3.9.2).");
            Assert.That(p.SelectedBranch, Is.SameAs(branch), "Archetype path locked (§5.3.5).");
            Assert.That(p.TraumaStacks, Is.EqualTo(2), "Trauma carries through (§6.2.3).");
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
