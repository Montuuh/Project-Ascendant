using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §9.2 + Epic 9 runtime wiring — RunBootstrapper builds a working run from a catalog.
    public class RunBootstrapperTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();
        private readonly List<GameEventType> _events = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear(); _events.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Boot_" + System.Guid.NewGuid().ToString("N"));
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

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>(); _disposables.Add(o); return o;
        }

        private PokemonSpeciesSO MakeSpecies(string id, RarityTier rarity = RarityTier.Common)
        {
            PokemonSpeciesSO s = Make<PokemonSpeciesSO>();
            s.SpeciesId = id; s.DisplayName = id; s.WildRarity = rarity;
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = 40, BaseAtk = 20, BaseDef = 20, BaseSpd = 20 };
            s.BaseLearnset = new List<MoveSO>();
            return s;
        }

        private RunContentCatalogSO MakeCatalog()
        {
            RunContentCatalogSO cat = Make<RunContentCatalogSO>();

            MapGenerationConfigSO map = Make<MapGenerationConfigSO>();
            map.LayerCount = 8; map.DefaultMaxBranches = 2; map.ConstraintRetryCap = 8;
            map.Layers = new List<MapLayerSpec>
            {
                new() { Layer = 0, NodesInLayer = 1, ForceMode = LayerForceMode.AllNodes, ForcedType = NodeType.Wild },
                new() { Layer = 1, NodesInLayer = 3, ForceMode = LayerForceMode.None },
                new() { Layer = 2, NodesInLayer = 5, ForceMode = LayerForceMode.None },
                new() { Layer = 3, NodesInLayer = 3, ForceMode = LayerForceMode.OneNodeInLayer, ForcedType = NodeType.Elite },
                new() { Layer = 4, NodesInLayer = 2, ForceMode = LayerForceMode.None },
                new() { Layer = 5, NodesInLayer = 1, ForceMode = LayerForceMode.AllNodes, ForcedType = NodeType.Center },
                new() { Layer = 6, NodesInLayer = 2, ForceMode = LayerForceMode.None },
                new() { Layer = 7, NodesInLayer = 1, ForceMode = LayerForceMode.AllNodes, ForcedType = NodeType.Gym },
            };
            map.LayerWeights = new List<NodeLayerWeights>
            {
                new() { Layer = 0, WildWeight = 1 },
                new() { Layer = 1, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 2, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 3, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 4, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 5, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 6, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 7, GymWeight = 1 },
            };
            cat.MapConfig = map;

            EconomyConfigSO econ = Make<EconomyConfigSO>();
            econ.BoxCapacity = 6; econ.TraumaStackPenaltyPercent = 5; econ.TraumaStackCap = 5; econ.TherapyBaseCost = 100;
            cat.Economy = econ;

            BiomeSO biome = Make<BiomeSO>();
            biome.SpeciesPool = new List<PokemonSpeciesSO> { MakeSpecies("c1"), MakeSpecies("c2") };
            WildEncounterConfigSO wild = Make<WildEncounterConfigSO>();
            wild.RegionBiomes = new List<BiomeWeight> { new() { Biome = biome, Weight = 1f } };
            wild.CommonChoices = 2; wild.UncommonChoices = 1; wild.RareSwapChance = 0f;
            wild.WildLevelMin = 5; wild.WildLevelMax = 10;
            cat.WildConfig = wild;

            RegionShopConfigSO shop = Make<RegionShopConfigSO>();
            shop.ConsumableSlots = 1; shop.ConsumablePriceMin = 30; shop.ConsumablePriceMax = 100;
            shop.CommonRelicPrice = 150; shop.UncommonRelicPrice = 300; shop.PokeballPrice = 50;
            shop.HeldItemPriceMin = 250; shop.HeldItemPriceMax = 500; shop.RerollCosts = new[] { 25, 50, 100 };
            cat.ShopConfig = shop;

            MysteryConfigSO mys = Make<MysteryConfigSO>();
            mys.BerryBushHealPercent = 30; mys.BerryBushPotionCount = 3;
            mys.WanderingTutorDeclineDollars = 100; mys.SlotBoothWager = 100; mys.SlotBoothWinAmount = 250;
            cat.MysteryConfig = mys;

            cat.Pokeball = Make<ConsumableSO>();
            cat.Potion = Make<ConsumableSO>();

            TrainerArchetypeSO arch = Make<TrainerArchetypeSO>();
            arch.ArchetypeId = "bug"; arch.DisplayName = "Bug Catcher";
            arch.Composition = new List<TrainerPokemonSlot> { new() { Species = MakeSpecies("tp"), Level = 6 } };
            arch.BasePokeDollarReward = 60; arch.RelicLootTable = new List<RelicSO>(); arch.ConsumableLootTable = new List<ConsumableSO>();
            cat.Archetypes = new List<TrainerArchetypeSO> { arch };

            EliteTrainerSO elite = Make<EliteTrainerSO>();
            elite.EliteId = "ace"; elite.DisplayName = "Ace";
            elite.Composition = new List<ElitePokemonSlot> { new() { Species = MakeSpecies("ep"), Level = 12, PhaseCount = 2 } };
            elite.GuaranteedRelic = Make<RelicSO>(); elite.TrainerXPReward = 25; elite.PokeDollarReward = 300;
            cat.Elite = elite;

            GymLeaderSO gym = Make<GymLeaderSO>();
            gym.GymLeaderId = "rock"; gym.DisplayName = "Brock"; gym.GymType = PokemonType.Rock;
            gym.Composition = new List<GymPokemonSlot>
            {
                new() { Species = MakeSpecies("geo"), Level = 14, PhaseCount = 2 },
                new() { Species = MakeSpecies("grav"), Level = 16, PhaseCount = 3, IsAce = true },
            };
            gym.BadgeReward = Make<BadgeSO>(); gym.GuaranteedRareRelic = Make<RelicSO>();
            gym.TrainerXPReward = 50; gym.PokeDollarReward = 500;
            cat.Gym = gym;

            MysteryEventSO berry = Make<MysteryEventSO>();
            berry.EventId = "berry_bush";
            berry.Choices = new List<MysteryChoice> { new() { ChoiceText = "a" }, new() { ChoiceText = "b" } };
            cat.MysteryEvents = new List<MysteryEventSO> { berry };

            RelicSO common = Make<RelicSO>(); common.Rarity = RarityTier.Common;
            RelicSO uncommon = Make<RelicSO>(); uncommon.Rarity = RarityTier.Uncommon;
            cat.Relics = new List<RelicSO> { common, uncommon };
            cat.Consumables = new List<ConsumableSO> { Make<ConsumableSO>() };
            cat.HeldItems = new List<HeldItemSO> { Make<HeldItemSO>() };
            cat.TMs = new List<TMSO>();
            cat.Starters = new List<PokemonSpeciesSO> { MakeSpecies("Squirtle") };
            return cat;
        }

        [Test]
        public void CreateRunController_BuildsContext_FromCatalog()
        {
            RunContentCatalogSO cat = MakeCatalog();
            RunStateSO run = Make<RunStateSO>();
            RunController rc = RunBootstrapper.CreateRunController(
                cat, run, new PokemonInstanceFactory(), new RNGStreams(1u), e => _events.Add(e.Type), out RunContext ctx);

            Assert.That(rc, Is.Not.Null);
            Assert.That(ctx.Box.Capacity, Is.EqualTo(6));
            Assert.That(ctx.ShopPools.CommonRelics, Has.Count.EqualTo(1));
            Assert.That(ctx.ShopPools.UncommonRelics, Has.Count.EqualTo(1));
            Assert.That(ctx.ArchetypePool, Has.Count.EqualTo(1));
        }

        [Test]
        public void SeedStarter_AddsToBox_AndCommitsActiveTeam()
        {
            RunContentCatalogSO cat = MakeCatalog();
            RunStateSO run = Make<RunStateSO>();
            RunBootstrapper.CreateRunController(cat, run, new PokemonInstanceFactory(), new RNGStreams(1u), _ => { }, out RunContext ctx);

            RunBootstrapper.SeedStarter(ctx, cat.Starters[0], 5);
            Assert.That(ctx.Box.Count, Is.EqualTo(1));
            Assert.That(run.ActiveTeamIndices, Is.EqualTo(new List<int> { 0 }));
        }

        [Test]
        public void FullRun_FromCatalog_ReachesRunEnded()
        {
            RunContentCatalogSO cat = MakeCatalog();
            RunStateSO run = Make<RunStateSO>();
            RunController rc = RunBootstrapper.CreateRunController(
                cat, run, new PokemonInstanceFactory(), new RNGStreams(777u), e => _events.Add(e.Type), out RunContext ctx);
            RunBootstrapper.SeedStarter(ctx, cat.Starters[0], 5);

            rc.StartRun();
            RunAutoPilot.WalkToEnd(rc, null);

            Assert.That(rc.RunOver, Is.True);
            Assert.That(_events[_events.Count - 1], Is.EqualTo(GameEventType.RunEnded));
            Assert.That(rc.CurrentNode.NodeType, Is.EqualTo(NodeType.Gym));
            Assert.That(run.EarnedBadges, Is.Not.Null.And.Count.GreaterThanOrEqualTo(1));
            Assert.That(run.PokeDollars, Is.GreaterThanOrEqualTo(500));
        }
    }
}
