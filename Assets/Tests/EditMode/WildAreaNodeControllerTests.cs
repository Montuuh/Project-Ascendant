using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.3 + Epic 9 Task 9.3 — WildAreaNodeController + Box + composition tests.
    public class WildAreaNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_WildNode_" + System.Guid.NewGuid().ToString("N"));
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

        private PokemonSpeciesSO MakeSpecies(string id, RarityTier rarity, int hp = 40)
        {
            PokemonSpeciesSO s = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            s.SpeciesId = id;
            s.WildRarity = rarity;
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = hp, BaseAtk = 30, BaseDef = 30, BaseSpd = 30 };
            s.StatusImmunities = new List<StatusCondition>();
            s.BaseLearnset = new List<MoveSO>();
            _disposables.Add(s);
            return s;
        }

        private BiomeSO MakeBiome(params PokemonSpeciesSO[] pool)
        {
            BiomeSO b = ScriptableObject.CreateInstance<BiomeSO>();
            b.BiomeId = "biome";
            b.SpeciesPool = new List<PokemonSpeciesSO>(pool);
            _disposables.Add(b);
            return b;
        }

        private WildEncounterConfigSO MakeConfig(
            BiomeSO biome, int common = 2, int uncommon = 1,
            float rareChance = 0f, int lvlMin = 5, int lvlMax = 10)
        {
            WildEncounterConfigSO c = ScriptableObject.CreateInstance<WildEncounterConfigSO>();
            c.RegionBiomes = new List<BiomeWeight> { new() { Biome = biome, Weight = 1f } };
            c.CommonChoices = common;
            c.UncommonChoices = uncommon;
            c.RareSwapChance = rareChance;
            c.WildLevelMin = lvlMin;
            c.WildLevelMax = lvlMax;
            _disposables.Add(c);
            return c;
        }

        private RunStateSO MakeRun()
        {
            RunStateSO r = ScriptableObject.CreateInstance<RunStateSO>();
            _disposables.Add(r);
            return r;
        }

        private static MapNode MakeNode() => new MapNode(0, 0, 0, NodeType.Wild);

        private BiomeSO StandardBiome() => MakeBiome(
            MakeSpecies("c1", RarityTier.Common),
            MakeSpecies("c2", RarityTier.Common),
            MakeSpecies("c3", RarityTier.Common),
            MakeSpecies("u1", RarityTier.Uncommon),
            MakeSpecies("u2", RarityTier.Uncommon),
            MakeSpecies("r1", RarityTier.Rare));

        private WildAreaNodeController Make(
            WildEncounterConfigSO config, uint seed = 1u, Box box = null)
            => new(MakeNode(), MakeRun(), config, new PokemonInstanceFactory(),
                   pokeball: null, new GameRNG(seed), box ?? new Box(6),
                   new AutoSkipBoxOverflowHandler());

        // ── Box ───────────────────────────────────────────────────────────────

        [Test]
        public void Box_TryAdd_RespectsCapacity()
        {
            Box box = new(2);
            Assert.That(box.TryAdd(new PokemonInstance()), Is.True);
            Assert.That(box.TryAdd(new PokemonInstance()), Is.True);
            Assert.That(box.IsFull, Is.True);
            Assert.That(box.TryAdd(new PokemonInstance()), Is.False, "Add past capacity must fail.");
            Assert.That(box.Count, Is.EqualTo(2));
        }

        // ── Offer composition (§7.3.2) ────────────────────────────────────────

        [Test]
        public void Offer_Is2Common1Uncommon()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()));
            c.Enter();
            Assert.That(c.Choices.Count, Is.EqualTo(3));
            int commons = 0, uncommons = 0;
            foreach (PokemonSpeciesSO s in c.Choices)
            {
                if (s.WildRarity == RarityTier.Common) commons++;
                else if (s.WildRarity == RarityTier.Uncommon) uncommons++;
            }
            Assert.That(commons, Is.EqualTo(2));
            Assert.That(uncommons, Is.EqualTo(1));
        }

        [Test]
        public void Offer_CommonsAreDistinct()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()));
            c.Enter();
            HashSet<PokemonSpeciesSO> set = new(c.Choices);
            Assert.That(set.Count, Is.EqualTo(c.Choices.Count), "Offer must not repeat a species.");
        }

        [Test]
        public void Offer_RareSwapChance1_ReplacesUncommonWithRare()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome(), rareChance: 1f));
            c.Enter();
            int rares = 0;
            foreach (PokemonSpeciesSO s in c.Choices) if (s.WildRarity == RarityTier.Rare) rares++;
            Assert.That(rares, Is.EqualTo(1), "RareSwapChance=1 must yield a Rare in place of the Uncommon.");
        }

        [Test]
        public void Offer_RareSwapChance0_NoRare()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome(), rareChance: 0f));
            c.Enter();
            foreach (PokemonSpeciesSO s in c.Choices)
                Assert.That(s.WildRarity, Is.Not.EqualTo(RarityTier.Rare));
        }

        [Test]
        public void Offer_DeterministicGivenSeed()
        {
            BiomeSO biome = StandardBiome();
            WildAreaNodeController a = Make(MakeConfig(biome), seed: 0xABCDu);
            WildAreaNodeController b = Make(MakeConfig(biome), seed: 0xABCDu);
            a.Enter();
            b.Enter();
            Assert.That(a.Choices, Is.EqualTo(b.Choices));
        }

        [Test]
        public void Offer_PublishesWildSpeciesOfferedContext()
        {
            WildSpeciesOfferedContext? seen = null;
            EventBus.Subscribe<WildSpeciesOfferedContext>(ctx => seen = ctx);
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()));
            c.Enter();
            Assert.That(seen.HasValue, Is.True);
            Assert.That(seen.Value.Choices.Count, Is.EqualTo(3));
        }

        // ── Biome selection (§7.3.1) ──────────────────────────────────────────

        [Test]
        public void PickBiome_ZeroWeightBiomeNeverChosen()
        {
            BiomeSO chosen = MakeBiome(MakeSpecies("c1", RarityTier.Common), MakeSpecies("c2", RarityTier.Common));
            BiomeSO excluded = MakeBiome(MakeSpecies("x1", RarityTier.Common));
            WildEncounterConfigSO config = ScriptableObject.CreateInstance<WildEncounterConfigSO>();
            config.RegionBiomes = new List<BiomeWeight>
            {
                new() { Biome = excluded, Weight = 0f },
                new() { Biome = chosen,   Weight = 1f },
            };
            config.CommonChoices = 2; config.UncommonChoices = 0;
            config.WildLevelMin = 5; config.WildLevelMax = 10;
            _disposables.Add(config);

            WildAreaNodeController c = Make(config);
            c.Enter();
            Assert.That(c.SelectedBiome, Is.SameAs(chosen));
        }

        // ── SelectSpecies (9.3.3) ─────────────────────────────────────────────

        [Test]
        public void SelectSpecies_BuildsCombat_WildLevelInBand()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome(), lvlMin: 5, lvlMax: 10));
            c.Enter();
            CombatController.CombatSetup setup = c.SelectSpecies(
                0, new List<PokemonInstance>(), 0, new List<ConsumableSO>(),
                FieldState.Empty, ScriptableObject.CreateInstance<BattleConfigSO>(), new GameRNG(2u));

            Assert.That(setup.EnemyTeam.Count, Is.EqualTo(1));
            int level = setup.EnemyTeam[0].Level;
            Assert.That(level, Is.InRange(5, 10));
        }

        // ── ResolveCombat → Box + NodeOutcome ─────────────────────────────────

        [Test]
        public void ResolveCombat_Caught_BoxHasRoom_AddsRecruit_Cleared()
        {
            Box box = new(6);
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()), box: box);
            c.Enter();
            PokemonInstance caught = new() { Species = c.Choices[0], Level = 5, CurrentHP = 10 };

            WildEncounterResult r = c.ResolveCombat(CombatController.CombatOutcome.Victory, caught);

            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.Caught));
            Assert.That(box.Members, Has.Member(caught));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
            Assert.That(c.IsCompleted, Is.True);
        }

        [Test]
        public void ResolveCombat_PlayerWiped_MapsToPlayerWiped()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()));
            c.Enter();
            WildEncounterResult r = c.ResolveCombat(CombatController.CombatOutcome.Defeat, null);
            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.PlayerWiped));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.PlayerWiped));
        }

        [Test]
        public void ResolveCombat_WildFainted_MapsToCleared()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()));
            c.Enter();
            WildEncounterResult r = c.ResolveCombat(CombatController.CombatOutcome.Victory, caughtTarget: null);
            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.WildFainted));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void ResolveCombat_Caught_BoxFull_AutoSkip_NoRecruit()
        {
            // Per §2.3.1 — default handler Skips: recruit declined, Box unchanged.
            Box box = new(1);
            box.TryAdd(new PokemonInstance());
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()), box: box);
            c.Enter();
            PokemonInstance caught = new() { Species = c.Choices[0], Level = 5, CurrentHP = 10 };

            WildEncounterResult r = c.ResolveCombat(CombatController.CombatOutcome.Victory, caught);

            Assert.That(r.Outcome, Is.EqualTo(WildEncounterResult.WildOutcome.Caught));
            Assert.That(r.BoxOverflowPromptShown, Is.True);
            Assert.That(r.BoxUpdated, Is.False);
            Assert.That(box.Count, Is.EqualTo(1));
            Assert.That(box.Members, Has.No.Member(caught));
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        // ── Save-on-entry (inherited base behaviour, §9.8.1) ──────────────────

        [Test]
        public void Enter_SavesRun()
        {
            WildAreaNodeController c = Make(MakeConfig(StandardBiome()));
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }
    }
}
