using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.2.1 / §7.11 + Epic 9 Task 9.1 — RegionMapGenerator determinism + constraint tests.
    public class RegionMapGeneratorTests
    {
        // Builds a config mirroring Assets/.../MapGenerationConfig.asset (the §7.2.1 8-layer spec).
        private static MapGenerationConfigSO BuildConfig()
        {
            MapGenerationConfigSO c = ScriptableObject.CreateInstance<MapGenerationConfigSO>();
            c.LayerCount           = 8;
            c.BranchLayerIndex     = 4;
            c.LaneCountAfterBranch = 2;
            c.DefaultMaxBranches   = 3;
            c.ConstraintRetryCap   = 8;

            c.Layers = new List<MapLayerSpec>
            {
                new() { Layer = 0, WidthPerLane = 1, ForceMode = LayerForceMode.AllNodes,       ForcedType = NodeType.Wild },
                new() { Layer = 1, WidthPerLane = 3, ForceMode = LayerForceMode.None },
                new() { Layer = 2, WidthPerLane = 3, ForceMode = LayerForceMode.None },
                new() { Layer = 3, WidthPerLane = 3, ForceMode = LayerForceMode.OneNodeInLayer, ForcedType = NodeType.Elite },
                new() { Layer = 4, WidthPerLane = 1, ForceMode = LayerForceMode.None },
                new() { Layer = 5, WidthPerLane = 2, ForceMode = LayerForceMode.None },
                new() { Layer = 6, WidthPerLane = 2, ForceMode = LayerForceMode.OneNodePerLane, ForcedType = NodeType.Center },
                new() { Layer = 7, WidthPerLane = 1, ForceMode = LayerForceMode.AllNodes,       ForcedType = NodeType.Gym },
            };

            c.LayerWeights = new List<NodeLayerWeights>
            {
                new() { Layer = 0, WildWeight = 1 },
                new() { Layer = 1, WildWeight = 2, TrainerWeight = 3, MysteryWeight = 1 },
                new() { Layer = 2, WildWeight = 1, TrainerWeight = 3, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 3, WildWeight = 1, TrainerWeight = 3, MysteryWeight = 1 },
                new() { Layer = 4, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 2 },
                new() { Layer = 5, WildWeight = 1, TrainerWeight = 3, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 6, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 7, GymWeight = 1 },
            };
            return c;
        }

        // Stable serialization of a map for determinism comparison: each node + its forward edges.
        private static string Serialize(RegionMap map)
        {
            // Index nodes for stable edge identity.
            Dictionary<MapNode, int> id = new();
            int n = 0;
            foreach (MapNode node in map.AllNodes()) id[node] = n++;

            StringBuilder sb = new();
            foreach (MapNode node in map.AllNodes())
            {
                sb.Append($"{node.Layer},{node.Lane},{node.IndexInLane},{(int)node.NodeType}->");
                List<int> nextIds = new();
                for (int i = 0; i < node.Next.Count; i++) nextIds.Add(id[node.Next[i]]);
                nextIds.Sort();
                sb.Append(string.Join(",", nextIds));
                sb.Append(';');
            }
            return sb.ToString();
        }

        // ── Determinism ───────────────────────────────────────────────────────────

        [Test]
        public void Generate_SameSeed_ProducesIdenticalMap()
        {
            // Per §1.3.2 / §7.2.2 — same seed = same map every time.
            MapGenerationConfigSO config = BuildConfig();
            RegionMap a = RegionMapGenerator.Generate(config, new GameRNG(12345u));
            RegionMap b = RegionMapGenerator.Generate(config, new GameRNG(12345u));
            Assert.That(Serialize(a), Is.EqualTo(Serialize(b)));
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentMaps()
        {
            // Per §7.11 — distinct seeds should yield distinct topologies (over a small sample).
            MapGenerationConfigSO config = BuildConfig();
            HashSet<string> shapes = new();
            for (uint seed = 1; seed <= 8; seed++)
                shapes.Add(Serialize(RegionMapGenerator.Generate(config, new GameRNG(seed))));
            Assert.That(shapes.Count, Is.GreaterThan(1), "All sampled seeds produced the same map.");
        }

        [Test]
        public void Generate_SeedOverload_FoldsRegionIndex()
        {
            // Per §7.11 — seed = RunSeed XOR RegionIndex: different regions differ for the same run.
            MapGenerationConfigSO config = BuildConfig();
            RegionMap r0 = RegionMapGenerator.Generate(config, 999u, regionIndex: 0);
            RegionMap r1 = RegionMapGenerator.Generate(config, 999u, regionIndex: 1);
            Assert.That(Serialize(r0), Is.Not.EqualTo(Serialize(r1)));
        }

        // ── Topology / forced constraints ───────────────────────────────────────────

        [Test]
        public void Generate_HasEightLayers()
        {
            // Per §7.2.1 — 8 layers per Region (0-indexed).
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            Assert.That(map.LayerCount, Is.EqualTo(8));
        }

        [Test]
        public void Generate_Layer0_IsSingleForcedWild()
        {
            // Per §7.2.1 — Layer 0 is a single forced Wild Pokémon Area (early recruitment).
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            Assert.That(map.Layers[0].Count, Is.EqualTo(1));
            Assert.That(map.Entry.NodeType, Is.EqualTo(NodeType.Wild));
        }

        [Test]
        public void Generate_Layer7_AllGym_OnePerLane()
        {
            // Per §7.2.1 — Gym Layer: single node per branch, always Gym.
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            List<MapNode> gymLayer = map.Layers[7];
            Assert.That(gymLayer.Count, Is.EqualTo(2), "Expected one Gym node per lane.");
            foreach (MapNode node in gymLayer)
                Assert.That(node.NodeType, Is.EqualTo(NodeType.Gym));
        }

        [Test]
        public void Generate_EliteExactlyOnce_AtLayer3()
        {
            // Per §7.2.1 — Elite always present at Layer 3, exactly one per Region.
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                int eliteCount = 0;
                foreach (MapNode node in map.AllNodes())
                    if (node.NodeType == NodeType.Elite)
                    {
                        eliteCount++;
                        Assert.That(node.Layer, Is.EqualTo(3), $"Elite off Layer 3 (seed {seed}).");
                    }
                Assert.That(eliteCount, Is.EqualTo(1), $"Expected exactly one Elite (seed {seed}).");
            }
        }

        [Test]
        public void Generate_CenterGuaranteed_PerLane_AtLayer6()
        {
            // Per §7.2.1 — guaranteed Pokémon Center at Layer 6 in each branch (pre-Gym pit stop).
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                for (int lane = 0; lane < 2; lane++)
                {
                    List<MapNode> laneNodes = map.LaneNodes(6, lane);
                    int centers = 0;
                    foreach (MapNode node in laneNodes)
                        if (node.NodeType == NodeType.Center) centers++;
                    Assert.That(centers, Is.GreaterThanOrEqualTo(1),
                        $"Lane {lane} has no Center at Layer 6 (seed {seed}).");
                }
            }
        }

        [Test]
        public void Generate_BranchesAtLayer4_IntoTwoLanes()
        {
            // Per §7.2.1 — Branch Point Layer: layers >= 4 split into 2 lanes.
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            for (int layer = 0; layer < 4; layer++)
                foreach (MapNode node in map.Layers[layer])
                    Assert.That(node.Lane, Is.EqualTo(0), $"Pre-branch layer {layer} should be single-lane.");

            for (int layer = 4; layer < 8; layer++)
            {
                HashSet<int> lanes = new();
                foreach (MapNode node in map.Layers[layer]) lanes.Add(node.Lane);
                Assert.That(lanes, Is.EquivalentTo(new[] { 0, 1 }), $"Layer {layer} should span 2 lanes.");
            }
        }

        [Test]
        public void Generate_BothBranches_AreSymmetric()
        {
            // Per §7.2.3 — both branches lead to the Gym symmetrically (equal node-count per lane).
            for (uint seed = 1; seed <= 20; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                for (int layer = 4; layer < 8; layer++)
                    Assert.That(map.LaneNodes(layer, 0).Count, Is.EqualTo(map.LaneNodes(layer, 1).Count),
                        $"Lane asymmetry at layer {layer} (seed {seed}).");
            }
        }

        // ── Adjacency + connectivity ────────────────────────────────────────────────

        [Test]
        public void Generate_NoAdjacentSameType_WithinLane()
        {
            // Per §7.2.3 — within a lane, no two consecutive nodes share a node type.
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                for (int layer = 0; layer < map.LayerCount; layer++)
                    for (int lane = 0; lane < 2; lane++)
                    {
                        List<MapNode> laneNodes = map.LaneNodes(layer, lane);
                        for (int i = 1; i < laneNodes.Count; i++)
                            Assert.That(laneNodes[i].NodeType, Is.Not.EqualTo(laneNodes[i - 1].NodeType),
                                $"Adjacent same-type at L{layer}/lane{lane}#{i} (seed {seed}).");
                    }
            }
        }

        [Test]
        public void Generate_EveryNonGymNode_HasForwardConnections()
        {
            // Per §7.2.3 — every node connects to 1..DefaultMaxBranches nodes in the next layer.
            MapGenerationConfigSO config = BuildConfig();
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(config, new GameRNG(seed));
                foreach (MapNode node in map.AllNodes())
                {
                    if (node.Layer == map.LayerCount - 1)
                    {
                        Assert.That(node.Next.Count, Is.EqualTo(0), "Gym node should be terminal.");
                        continue;
                    }
                    Assert.That(node.Next.Count, Is.GreaterThanOrEqualTo(1),
                        $"{node} has no forward edge (seed {seed}).");
                    Assert.That(node.Next.Count, Is.LessThanOrEqualTo(config.DefaultMaxBranches),
                        $"{node} exceeds DefaultMaxBranches (seed {seed}).");
                    foreach (MapNode child in node.Next)
                        Assert.That(child.Layer, Is.EqualTo(node.Layer + 1),
                            $"{node} connects across non-adjacent layer (seed {seed}).");
                }
            }
        }

        [Test]
        public void Generate_EveryNode_ReachableFromEntry()
        {
            // Connectivity: BFS from Entry must visit every node (no orphans).
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                HashSet<MapNode> visited = new();
                Queue<MapNode> queue = new();
                queue.Enqueue(map.Entry);
                visited.Add(map.Entry);
                while (queue.Count > 0)
                {
                    MapNode cur = queue.Dequeue();
                    foreach (MapNode child in cur.Next)
                        if (visited.Add(child)) queue.Enqueue(child);
                }

                int total = 0;
                foreach (MapNode _ in map.AllNodes()) total++;
                Assert.That(visited.Count, Is.EqualTo(total),
                    $"{total - visited.Count} orphan node(s) unreachable from Entry (seed {seed}).");
            }
        }

        [Test]
        public void Generate_EveryNode_ReachesAGym()
        {
            // Connectivity: from every node, following Next must terminate at a Gym node.
            for (uint seed = 1; seed <= 20; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                foreach (MapNode start in map.AllNodes())
                {
                    MapNode cur = start;
                    int guard = 0;
                    while (cur.Next.Count > 0 && guard < map.LayerCount + 2)
                    {
                        cur = cur.Next[0];
                        guard++;
                    }
                    Assert.That(cur.NodeType, Is.EqualTo(NodeType.Gym),
                        $"Path from {start} did not reach a Gym (seed {seed}).");
                }
            }
        }
    }
}
