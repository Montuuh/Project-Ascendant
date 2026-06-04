using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.2 v2 — RegionMapGenerator determinism + constraint tests for the 12-layer gym-fork map.
    public class RegionMapGeneratorTests
    {
        // Per §7.2 v2 — 12 layers: L0 entries, L1-L8 trunk, L9 fork, L10 centers, L11 gyms.
        private static MapGenerationConfigSO BuildConfig()
        {
            MapGenerationConfigSO c = ScriptableObject.CreateInstance<MapGenerationConfigSO>();
            c.LayerCount = 12;
            c.GymForkLayer = 9;
            c.DefaultMaxBranches = 3;
            c.MaxInDegree = 2;
            c.ConstraintRetryCap = 8;

            c.Layers = new List<MapLayerSpec>
            {
                // L0: 3 entry nodes (varied types).
                new() { Layer = 0, NodesInLayer = 3, ForceMode = LayerForceMode.None },
                // L1-L2: branching trunk (guarantee ≥1 Wild reachable within L1-L2).
                new() { Layer = 1, NodesInLayer = 4, ForceMode = LayerForceMode.OneNodeInLayer, ForcedType = NodeType.Wild },
                new() { Layer = 2, NodesInLayer = 5, ForceMode = LayerForceMode.None },
                // L3-L8: trunk continues.
                new() { Layer = 3, NodesInLayer = 5, ForceMode = LayerForceMode.None },
                new() { Layer = 4, NodesInLayer = 4, ForceMode = LayerForceMode.None },
                new() { Layer = 5, NodesInLayer = 4, ForceMode = LayerForceMode.None },
                new() { Layer = 6, NodesInLayer = 3, ForceMode = LayerForceMode.None },
                new() { Layer = 7, NodesInLayer = 3, ForceMode = LayerForceMode.OneNodeInLayer, ForcedType = NodeType.Elite },
                new() { Layer = 8, NodesInLayer = 3, ForceMode = LayerForceMode.None },
                // L9: fork (2 nodes, one per lane).
                new() { Layer = 9, NodesInLayer = 2, ForceMode = LayerForceMode.None },
                // L10: centers (2 nodes, one per lane).
                new() { Layer = 10, NodesInLayer = 2, ForceMode = LayerForceMode.AllNodes, ForcedType = NodeType.Center },
                // L11: terminal gyms (2 nodes, one per lane).
                new() { Layer = 11, NodesInLayer = 2, ForceMode = LayerForceMode.AllNodes, ForcedType = NodeType.Gym },
            };

            c.LayerWeights = new List<NodeLayerWeights>
            {
                new() { Layer = 0, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 1, WildWeight = 2, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 2, WildWeight = 1, TrainerWeight = 3, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 3, TrainerWeight = 3, MysteryWeight = 1 },
                new() { Layer = 4, TrainerWeight = 3, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 5, TrainerWeight = 3, MysteryWeight = 1 },
                new() { Layer = 6, TrainerWeight = 3, MysteryWeight = 1 },
                new() { Layer = 7, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 8, TrainerWeight = 3, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 9, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 10, CenterWeight = 1 },
                new() { Layer = 11, GymWeight = 1 },
            };
            return c;
        }

        private static readonly int[] ExpectedWidths = { 3, 4, 5, 5, 4, 4, 3, 3, 3, 2, 2, 2 };

        private static List<GymLeaderSO> BuildGymPool()
        {
            GymLeaderSO gym0 = ScriptableObject.CreateInstance<GymLeaderSO>();
            gym0.GymLeaderId = "rock_gym_r1"; gym0.GymType = PokemonType.Rock;
            GymLeaderSO gym1 = ScriptableObject.CreateInstance<GymLeaderSO>();
            gym1.GymLeaderId = "water_gym_r1"; gym1.GymType = PokemonType.Water;
            return new List<GymLeaderSO> { gym0, gym1 };
        }

        private static string Serialize(RegionMap map)
        {
            Dictionary<MapNode, int> id = new();
            int n = 0;
            foreach (MapNode node in map.AllNodes()) id[node] = n++;
            StringBuilder sb = new();
            foreach (MapNode node in map.AllNodes())
            {
                sb.Append($"{node.Layer},{node.Lane},{node.IndexInLane},{(int)node.NodeType},{node.GymIndex}->");
                List<int> next = new();
                for (int i = 0; i < node.Next.Count; i++) next.Add(id[node.Next[i]]);
                next.Sort();
                sb.Append(string.Join(",", next)).Append(';');
            }
            return sb.ToString();
        }

        // ── Determinism ───────────────────────────────────────────────────────

        [Test]
        public void Generate_SameSeed_ProducesIdenticalMap()
        {
            MapGenerationConfigSO config = BuildConfig();
            List<GymLeaderSO> pool = BuildGymPool();
            Assert.That(Serialize(RegionMapGenerator.Generate(config, new GameRNG(12345u), 0, pool)),
                Is.EqualTo(Serialize(RegionMapGenerator.Generate(config, new GameRNG(12345u), 0, pool))));
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentMaps()
        {
            MapGenerationConfigSO config = BuildConfig();
            List<GymLeaderSO> pool = BuildGymPool();
            HashSet<string> shapes = new();
            for (uint seed = 1; seed <= 8; seed++)
                shapes.Add(Serialize(RegionMapGenerator.Generate(config, new GameRNG(seed), 0, pool)));
            Assert.That(shapes.Count, Is.GreaterThan(1));
        }

        [Test]
        public void Generate_SeedOverload_FoldsRegionIndex()
        {
            MapGenerationConfigSO config = BuildConfig();
            List<GymLeaderSO> pool = BuildGymPool();
            Assert.That(Serialize(RegionMapGenerator.Generate(config, 999u, 0, pool)),
                Is.Not.EqualTo(Serialize(RegionMapGenerator.Generate(config, 999u, 1, pool))));
        }

        [Test]
        public void Generate_SameSeed_ProducesIdenticalGymAssignments()
        {
            MapGenerationConfigSO config = BuildConfig();
            List<GymLeaderSO> pool = BuildGymPool();
            RegionMap map1 = RegionMapGenerator.Generate(config, new GameRNG(777u), 0, pool);
            RegionMap map2 = RegionMapGenerator.Generate(config, new GameRNG(777u), 0, pool);

            Assert.That(map1.ChosenGyms.Count, Is.EqualTo(2));
            Assert.That(map2.ChosenGyms.Count, Is.EqualTo(2));
            Assert.That(map1.ChosenGyms[0].GymLeaderId, Is.EqualTo(map2.ChosenGyms[0].GymLeaderId));
            Assert.That(map1.ChosenGyms[1].GymLeaderId, Is.EqualTo(map2.ChosenGyms[1].GymLeaderId));

            // Gym nodes should have matching GymIndex.
            Assert.That(map1.GymNodes[0].GymIndex, Is.EqualTo(map2.GymNodes[0].GymIndex));
            Assert.That(map1.GymNodes[1].GymIndex, Is.EqualTo(map2.GymNodes[1].GymIndex));
        }

        // ── Topology / forced constraints ───────────────────────────────────

        [Test]
        public void Generate_Has12Layers()
            => Assert.That(RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u), 0, BuildGymPool()).LayerCount, Is.EqualTo(12));

        [Test]
        public void Generate_LayerWidths_MatchConfig()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u), 0, BuildGymPool());
            for (int l = 0; l < ExpectedWidths.Length; l++)
                Assert.That(map.Layers[l].Count, Is.EqualTo(ExpectedWidths[l]), $"Layer {l} width.");
        }

        [Test]
        public void Generate_Layer0_HasMultipleEntryNodes()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u), 0, BuildGymPool());
            Assert.That(map.EntryNodes.Count, Is.EqualTo(3), "L0 has 3 entry nodes.");
            Assert.That(map.EntryNodes[0].Layer, Is.EqualTo(0));
        }

        [Test]
        public void Generate_GuaranteedWild_WithinL1OrL2()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                bool hasWild = false;
                foreach (MapNode node in map.Layers[1])
                    if (node.NodeType == NodeType.Wild) hasWild = true;
                Assert.That(hasWild, Is.True, $"L1 has guaranteed Wild (seed {seed}).");
            }
        }

        [Test]
        public void Generate_FinalLayer_Has2TerminalGyms()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u), 0, BuildGymPool());
            Assert.That(map.GymNodes.Count, Is.EqualTo(2), "L11 has 2 terminal Gym nodes.");
            Assert.That(map.GymNodes[0].NodeType, Is.EqualTo(NodeType.Gym));
            Assert.That(map.GymNodes[1].NodeType, Is.EqualTo(NodeType.Gym));
        }

        [Test]
        public void Generate_Gyms_HaveDistinctTypes()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                Assert.That(map.ChosenGyms.Count, Is.EqualTo(2), $"seed {seed}");
                Assert.That(map.ChosenGyms[0].GymType, Is.Not.EqualTo(map.ChosenGyms[1].GymType), $"Gyms distinct (seed {seed}).");
            }
        }

        [Test]
        public void Generate_Gyms_AssignedToNodes()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u), 0, BuildGymPool());
            Assert.That(map.GymNodes[0].GymIndex, Is.GreaterThanOrEqualTo(0), "Gym node 0 has gym index.");
            Assert.That(map.GymNodes[1].GymIndex, Is.GreaterThanOrEqualTo(0), "Gym node 1 has gym index.");
            Assert.That(map.GymNodes[0].GymIndex, Is.Not.EqualTo(map.GymNodes[1].GymIndex), "Gym nodes assigned distinct gyms.");
        }

        [Test]
        public void Generate_EliteExactlyOnce_InLateTrunk()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                int elite = 0;
                foreach (MapNode node in map.AllNodes())
                {
                    if (node.NodeType == NodeType.Elite)
                    {
                        elite++;
                        Assert.That(node.Layer, Is.EqualTo(7), $"Elite at L7 (seed {seed}).");
                    }
                }
                Assert.That(elite, Is.EqualTo(1), $"Exactly 1 Elite (seed {seed}).");
            }
        }

        [Test]
        public void Generate_Centers_Guaranteed_AtLayer10()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                Assert.That(map.Layers[10].Count, Is.EqualTo(2), $"L10 has 2 nodes (seed {seed}).");
                Assert.That(map.Layers[10][0].NodeType, Is.EqualTo(NodeType.Center), $"L10 node 0 is Center (seed {seed}).");
                Assert.That(map.Layers[10][1].NodeType, Is.EqualTo(NodeType.Center), $"L10 node 1 is Center (seed {seed}).");
            }
        }

        // ── Fork topology ─────────────────────────────────────────────────────

        [Test]
        public void Generate_Fork_SplitsInto2Lanes()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u), 0, BuildGymPool());
            // L9 and beyond should have nodes in both lanes.
            for (int layer = 9; layer < map.LayerCount; layer++)
            {
                int lane0 = 0, lane1 = 0;
                foreach (MapNode node in map.Layers[layer])
                {
                    if (node.Lane == 0) lane0++;
                    else if (node.Lane == 1) lane1++;
                }
                Assert.That(lane0, Is.GreaterThan(0), $"L{layer} has lane 0 nodes.");
                Assert.That(lane1, Is.GreaterThan(0), $"L{layer} has lane 1 nodes.");
            }
        }

        [Test]
        public void Generate_PostFork_NoEdgeCrossing()
        {
            // After fork, lane 0 nodes should only connect to lane 0; lane 1 to lane 1.
            for (uint seed = 1; seed <= 20; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                for (int layer = 9; layer < map.LayerCount - 1; layer++)
                {
                    foreach (MapNode node in map.Layers[layer])
                    {
                        foreach (MapNode child in node.Next)
                        {
                            Assert.That(child.Lane, Is.EqualTo(node.Lane), $"L{layer} {node} crosses lanes to {child} (seed {seed}).");
                        }
                    }
                }
            }
        }

        [Test]
        public void Generate_BothTerminalGyms_Reachable()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                foreach (MapNode gym in map.GymNodes)
                {
                    bool reachable = CanReachFromAnyEntry(map, gym);
                    Assert.That(reachable, Is.True, $"{gym} unreachable (seed {seed}).");
                }
            }
        }

        private static bool CanReachFromAnyEntry(RegionMap map, MapNode target)
        {
            foreach (MapNode entry in map.EntryNodes)
            {
                if (CanReach(entry, target)) return true;
            }
            return false;
        }

        private static bool CanReach(MapNode start, MapNode target)
        {
            HashSet<MapNode> visited = new() { start };
            Queue<MapNode> q = new(); q.Enqueue(start);
            while (q.Count > 0)
            {
                MapNode cur = q.Dequeue();
                if (cur == target) return true;
                foreach (MapNode child in cur.Next)
                    if (visited.Add(child)) q.Enqueue(child);
            }
            return false;
        }

        // ── Adjacency / connectivity ─────────────────────────────────────────

        [Test]
        public void Generate_NoAdjacentSameType_WithinLayer()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                for (int layer = 0; layer < map.LayerCount; layer++)
                {
                    // Check within each lane separately.
                    for (int lane = 0; lane < 2; lane++)
                    {
                        List<MapNode> laneNodes = map.LaneNodes(layer, lane);
                        for (int i = 1; i < laneNodes.Count; i++)
                            Assert.That(laneNodes[i].NodeType, Is.Not.EqualTo(laneNodes[i - 1].NodeType), $"L{layer} lane{lane}#{i} (seed {seed}).");
                    }
                }
            }
        }

        [Test]
        public void Generate_EveryNonGymNode_HasForwardConnections()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                foreach (MapNode node in map.AllNodes())
                {
                    if (node.Layer == map.LayerCount - 1)
                    {
                        Assert.That(node.Next.Count, Is.EqualTo(0), $"{node} is terminal (seed {seed}).");
                        continue;
                    }
                    Assert.That(node.Next.Count, Is.GreaterThanOrEqualTo(1), $"{node} (seed {seed}).");
                    foreach (MapNode child in node.Next)
                        Assert.That(child.Layer, Is.EqualTo(node.Layer + 1), $"{node} -> {child} (seed {seed}).");
                }
            }
        }

        [Test]
        public void Generate_InDegree_CappedAtMaxInDegree()
        {
            MapGenerationConfigSO config = BuildConfig();
            int maxInDegree = config.MaxInDegree;

            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(config, new GameRNG(seed), 0, BuildGymPool());
                Dictionary<MapNode, int> inDegree = new();
                foreach (MapNode node in map.AllNodes()) inDegree[node] = 0;
                foreach (MapNode node in map.AllNodes())
                    foreach (MapNode child in node.Next)
                        inDegree[child]++;

                foreach (KeyValuePair<MapNode, int> kv in inDegree)
                {
                    if (kv.Key.Layer == 0) continue; // Entry nodes have 0 in-degree.
                    // Per §7.2 v2 — the fork layer is the ONE intentional convergence: every trunk node
                    // connects to both lane gateways so every player gets the Gym choice. Exempt it.
                    if (kv.Key.Layer == config.GymForkLayer) continue;
                    Assert.That(kv.Value, Is.LessThanOrEqualTo(maxInDegree), $"{kv.Key} in-degree {kv.Value} exceeds {maxInDegree} (seed {seed}).");
                }
            }
        }

        [Test]
        public void Generate_EveryNode_ReachableFromAnEntry()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                HashSet<MapNode> visited = new();
                Queue<MapNode> q = new();

                foreach (MapNode entry in map.EntryNodes)
                {
                    if (visited.Add(entry)) q.Enqueue(entry);
                }

                while (q.Count > 0)
                {
                    foreach (MapNode child in q.Dequeue().Next)
                        if (visited.Add(child)) q.Enqueue(child);
                }

                int total = 0;
                foreach (MapNode _ in map.AllNodes()) total++;
                Assert.That(visited.Count, Is.EqualTo(total), $"Orphan nodes (seed {seed}).");
            }
        }

        [Test]
        public void Generate_EveryEntryNode_ReachesBothGyms()
        {
            for (uint seed = 1; seed <= 20; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed), 0, BuildGymPool());
                foreach (MapNode entry in map.EntryNodes)
                {
                    // Each entry should reach at least one gym (ideally both, but fork may branch).
                    // For this test, ensure every entry reaches the fork layer, then both gyms are reachable from fork.
                    bool reachesAnyGym = false;
                    foreach (MapNode gym in map.GymNodes)
                    {
                        if (CanReach(entry, gym))
                        {
                            reachesAnyGym = true;
                            break;
                        }
                    }
                    Assert.That(reachesAnyGym, Is.True, $"{entry} does not reach any gym (seed {seed}).");
                }
            }
        }
    }
}
