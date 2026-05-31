using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §9.5 + Epic 9 run-flow integration — RunController drives a full Region run end-to-end.
    public class RunControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();
        private readonly List<GameEventType> _events = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _events.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Run_" + System.Guid.NewGuid().ToString("N"));
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
            T o = ScriptableObject.CreateInstance<T>();
            _disposables.Add(o);
            return o;
        }

        private PokemonSpeciesSO MakeSpecies(string id, RarityTier rarity = RarityTier.Common)
        {
            PokemonSpeciesSO s = Make<PokemonSpeciesSO>();
            s.SpeciesId = id;
            s.WildRarity = rarity;
            s.Types = new List<PokemonType> { PokemonType.Normal };
            s.BaseStats = new BaseStats { BaseHP = 40, BaseAtk = 20, BaseDef = 20, BaseSpd = 20 };
            s.BaseLearnset = new List<MoveSO>();
            return s;
        }

        // ── Full in-memory RunContext fixture (every node type resolvable) ────

        private MapGenerationConfigSO MakeMapConfig()
        {
            MapGenerationConfigSO c = Make<MapGenerationConfigSO>();
            c.LayerCount = 8; c.DefaultMaxBranches = 2; c.ConstraintRetryCap = 8;
            c.Layers = new List<MapLayerSpec>
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
            c.LayerWeights = new List<NodeLayerWeights>
            {
                new() { Layer = 0, WildWeight = 1 },
                new() { Layer = 1, WildWeight = 1, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 2, WildWeight = 1, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 3, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 4, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 5, WildWeight = 1, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 6, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 7, GymWeight = 1 },
            };
            return c;
        }

        private RunContext BuildContext(uint seed, out RunStateSO run)
        {
            run = Make<RunStateSO>();
            Box box = new(6);
            EconomyConfigSO economy = Make<EconomyConfigSO>();
            economy.TraumaStackPenaltyPercent = 5; economy.TraumaStackCap = 5; economy.TherapyBaseCost = 100;

            // Wild
            PokemonSpeciesSO c1 = MakeSpecies("c1"), c2 = MakeSpecies("c2");
            BiomeSO biome = Make<BiomeSO>();
            biome.SpeciesPool = new List<PokemonSpeciesSO> { c1, c2 };
            WildEncounterConfigSO wildConfig = Make<WildEncounterConfigSO>();
            wildConfig.RegionBiomes = new List<BiomeWeight> { new() { Biome = biome, Weight = 1f } };
            wildConfig.CommonChoices = 2; wildConfig.UncommonChoices = 1; wildConfig.RareSwapChance = 0f;
            wildConfig.WildLevelMin = 5; wildConfig.WildLevelMax = 10;

            // Trainer
            TrainerArchetypeSO archetype = Make<TrainerArchetypeSO>();
            archetype.ArchetypeId = "bug_catcher";
            archetype.Composition = new List<TrainerPokemonSlot> { new() { Species = MakeSpecies("tp"), Level = 6 } };
            archetype.BasePokeDollarReward = 60;
            archetype.RelicLootTable = new List<RelicSO>();
            archetype.ConsumableLootTable = new List<ConsumableSO>();

            // Elite
            EliteTrainerSO elite = Make<EliteTrainerSO>();
            elite.EliteId = "ace_r1";
            elite.Composition = new List<ElitePokemonSlot> { new() { Species = MakeSpecies("ep"), Level = 12, PhaseCount = 2 } };
            elite.GuaranteedRelic = Make<RelicSO>(); elite.TrainerXPReward = 25; elite.PokeDollarReward = 300;

            // Shop
            RegionShopConfigSO shopConfig = Make<RegionShopConfigSO>();
            shopConfig.ConsumableSlots = 1; shopConfig.ConsumablePriceMin = 30; shopConfig.ConsumablePriceMax = 100;
            shopConfig.CommonRelicPrice = 150; shopConfig.UncommonRelicPrice = 300; shopConfig.PokeballPrice = 50;
            shopConfig.HeldItemPriceMin = 250; shopConfig.HeldItemPriceMax = 500;
            shopConfig.RerollCosts = new[] { 25, 50, 100 };
            RegionShopNodeController.ShopItemPools shopPools = new()
            {
                Consumables = new List<ConsumableSO> { Make<ConsumableSO>() },
                CommonRelics = new List<RelicSO> { Make<RelicSO>() },
                UncommonRelics = new List<RelicSO> { Make<RelicSO>() },
                Pokeball = Make<ConsumableSO>(),
                HeldItems = new List<HeldItemSO> { Make<HeldItemSO>() },
                TMs = new List<TMSO>(),
            };

            // Mystery
            MysteryEventSO berry = Make<MysteryEventSO>();
            berry.EventId = "berry_bush";
            berry.Choices = new List<MysteryChoice> { new() { ChoiceText = "a" }, new() { ChoiceText = "b" } };
            MysteryConfigSO mysteryConfig = Make<MysteryConfigSO>();
            mysteryConfig.BerryBushHealPercent = 30; mysteryConfig.BerryBushPotionCount = 3;
            mysteryConfig.WanderingTutorDeclineDollars = 100; mysteryConfig.SlotBoothWager = 100; mysteryConfig.SlotBoothWinAmount = 250;

            // Gym
            GymLeaderSO gym = Make<GymLeaderSO>();
            gym.GymLeaderId = "rock_gym_r1"; gym.GymType = PokemonType.Rock;
            gym.Composition = new List<GymPokemonSlot>
            {
                new() { Species = MakeSpecies("geodude"), Level = 14, PhaseCount = 2 },
                new() { Species = MakeSpecies("graveler"), Level = 16, PhaseCount = 3, IsAce = true },
            };
            gym.BadgeReward = Make<BadgeSO>(); gym.GuaranteedRareRelic = Make<RelicSO>();
            gym.TrainerXPReward = 50; gym.PokeDollarReward = 500;

            return new RunContext
            {
                Run = run,
                Box = box,
                Loadout = new LoadoutManager(run, box),
                PokemonFactory = new PokemonInstanceFactory(),
                Streams = new RNGStreams(seed),
                Economy = economy,
                MapConfig = MakeMapConfig(),
                WildConfig = wildConfig,
                Pokeball = Make<ConsumableSO>(),
                BoxOverflow = new AutoSkipBoxOverflowHandler(),
                ArchetypePool = new List<TrainerArchetypeSO> { archetype },
                EliteSO = elite,
                ShopConfig = shopConfig,
                ShopPools = shopPools,
                MysteryPool = new List<MysteryEventSO> { berry },
                MysteryConfig = mysteryConfig,
                MysteryItems = new MysteryEventNodeController.MysteryItemRefs { Potion = Make<ConsumableSO>() },
                GymSO = gym,
            };
        }

        private RunController MakeRunController(uint seed, out RunStateSO run, out RunContext ctx)
        {
            ctx = BuildContext(seed, out run);
            NodeControllerFactory factory = new();
            ctx.RegisterBuilders(factory);
            return new RunController(ctx, factory, evt => _events.Add(evt.Type));
        }

        // Resolves whatever node is active with a "win/proceed" policy (combat = Victory).
        private static void ResolveActive(RunController rc)
        {
            switch (rc.ActiveNode)
            {
                case WildAreaNodeController w:  w.ResolveCombat(CombatController.CombatOutcome.Victory, null); break;
                case EliteNodeController e:     e.ResolveCombat(CombatController.CombatOutcome.Victory); break;
                case TrainerBattleNodeController t: t.ResolveCombat(CombatController.CombatOutcome.Victory); break;
                case GymNodeController g:       g.ResolveCombat(CombatController.CombatOutcome.Victory); break;
                case PokemonCenterNodeController c: c.Leave(); break;
                case RegionShopNodeController s:    s.Leave(); break;
                case MysteryEventNodeController m:  m.Choose(1); break;
            }
        }

        // ── Tests ─────────────────────────────────────────────────────────────

        [Test]
        public void StartRun_GeneratesMap_DispatchesStartNewRun()
        {
            RunController rc = MakeRunController(123u, out _, out _);
            rc.StartRun();
            Assert.That(rc.Map, Is.Not.Null);
            Assert.That(rc.Map.LayerCount, Is.EqualTo(8));
            Assert.That(_events, Has.Member(GameEventType.StartNewRun));
        }

        [Test]
        public void SelectableNodes_StartsAtEntry_ThenForwardConnections()
        {
            RunController rc = MakeRunController(123u, out _, out _);
            rc.StartRun();
            IReadOnlyList<MapNode> first = rc.SelectableNodes();
            Assert.That(first.Count, Is.EqualTo(1));
            Assert.That(first[0], Is.SameAs(rc.Map.Entry));

            rc.EnterNode(rc.Map.Entry);
            ResolveActive(rc);
            rc.CompleteActiveNode();
            Assert.That(rc.SelectableNodes(), Is.EquivalentTo(rc.Map.Entry.Next));
        }

        [Test]
        public void EnterNode_LocksLoadout_SavesRun_DispatchesNodeConfirmed()
        {
            RunController rc = MakeRunController(123u, out _, out RunContext ctx);
            rc.StartRun();
            rc.EnterNode(rc.Map.Entry);

            Assert.That(ctx.Loadout.IsLocked, Is.True, "Loadout locks on node entry (§2.3).");
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True, "Save on node entry (§9.8.1).");
            Assert.That(_events, Has.Member(GameEventType.NodeConfirmed));
            Assert.That(rc.ActiveNode, Is.Not.Null);
        }

        [Test]
        public void EnterNode_Unreachable_Refused()
        {
            RunController rc = MakeRunController(123u, out _, out _);
            rc.StartRun();
            // A Layer-7 gym node is not reachable from the start.
            MapNode farGym = rc.Map.GymNodes[0];
            Assert.That(rc.EnterNode(farGym), Is.False);
            Assert.That(rc.ActiveNode, Is.Null);
        }

        [Test]
        public void CompleteActiveNode_UnlocksLoadout_DispatchesNodeComplete()
        {
            RunController rc = MakeRunController(123u, out _, out RunContext ctx);
            rc.StartRun();
            rc.EnterNode(rc.Map.Entry); // Wild
            ResolveActive(rc);
            rc.CompleteActiveNode();

            Assert.That(ctx.Loadout.IsLocked, Is.False, "Loadout unlocks back to Map View after a node.");
            Assert.That(_events, Has.Member(GameEventType.NodeComplete));
            Assert.That(rc.ActiveNode, Is.Null);
        }

        [Test]
        public void FullRun_WalksEntryToGym_EndsWithRunEnded()
        {
            RunController rc = MakeRunController(777u, out RunStateSO run, out _);
            rc.StartRun();

            int guard = 0;
            while (!rc.RunOver && guard < 32)
            {
                IReadOnlyList<MapNode> options = rc.SelectableNodes();
                Assert.That(options.Count, Is.GreaterThan(0), "A non-terminal position must offer a node.");
                Assert.That(rc.EnterNode(options[0]), Is.True);
                ResolveActive(rc);
                rc.CompleteActiveNode();
                guard++;
            }

            Assert.That(rc.RunOver, Is.True, "Walking the map must reach a run-ending node.");
            Assert.That(_events[_events.Count - 1], Is.EqualTo(GameEventType.RunEnded), "Gym victory ends the run.");
            Assert.That(rc.CurrentNode.NodeType, Is.EqualTo(NodeType.Gym));
            // Gym reward applied to the run (§7.12).
            Assert.That(run.EarnedBadges, Is.Not.Null.And.Count.GreaterThanOrEqualTo(1));
            Assert.That(run.PokeDollars, Is.GreaterThanOrEqualTo(500));
        }

        [Test]
        public void FullRun_Deterministic_SameSeedSamePath()
        {
            string PathOf(uint seed)
            {
                RunController rc = MakeRunController(seed, out _, out _);
                _events.Clear();
                rc.StartRun();
                System.Text.StringBuilder sb = new();
                int guard = 0;
                while (!rc.RunOver && guard < 32)
                {
                    MapNode n = rc.SelectableNodes()[0];
                    rc.EnterNode(n);
                    sb.Append($"{n.Layer}/{n.Lane}/{n.NodeType};");
                    ResolveActive(rc);
                    rc.CompleteActiveNode();
                    guard++;
                }
                return sb.ToString();
            }

            Assert.That(PathOf(2024u), Is.EqualTo(PathOf(2024u)));
        }
    }
}
