using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.11 (+ gap #39 override) — RegionMapGenerator determinism + constraint tests for the
    // variable-width, partial-net, single-Gym map.
    public class RegionMapGeneratorTests
    {
        // Mirrors Assets/.../MapGenerationConfig.asset: widths 1,3,5,3,2,1,2,1; Elite@L3, Center@L5, Gym@L7.
        private static MapGenerationConfigSO BuildConfig()
        {
            MapGenerationConfigSO c = ScriptableObject.CreateInstance<MapGenerationConfigSO>();
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
                new() { Layer = 2, WildWeight = 1, TrainerWeight = 2, CenterWeight = 1, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 3, WildWeight = 1, TrainerWeight = 2, MysteryWeight = 1 },
                new() { Layer = 4, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 5, CenterWeight = 1 },
                new() { Layer = 6, TrainerWeight = 2, ShopWeight = 1, MysteryWeight = 1 },
                new() { Layer = 7, GymWeight = 1 },
            };
            return c;
        }

        private static readonly int[] ExpectedWidths = { 1, 3, 5, 3, 2, 1, 2, 1 };

        private static string Serialize(RegionMap map)
        {
            Dictionary<MapNode, int> id = new();
            int n = 0;
            foreach (MapNode node in map.AllNodes()) id[node] = n++;
            StringBuilder sb = new();
            foreach (MapNode node in map.AllNodes())
            {
                sb.Append($"{node.Layer},{node.IndexInLane},{(int)node.NodeType}->");
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
            Assert.That(Serialize(RegionMapGenerator.Generate(config, new GameRNG(12345u))),
                Is.EqualTo(Serialize(RegionMapGenerator.Generate(config, new GameRNG(12345u)))));
        }

        [Test]
        public void Generate_DifferentSeeds_ProduceDifferentMaps()
        {
            MapGenerationConfigSO config = BuildConfig();
            HashSet<string> shapes = new();
            for (uint seed = 1; seed <= 8; seed++)
                shapes.Add(Serialize(RegionMapGenerator.Generate(config, new GameRNG(seed))));
            Assert.That(shapes.Count, Is.GreaterThan(1));
        }

        [Test]
        public void Generate_SeedOverload_FoldsRegionIndex()
        {
            MapGenerationConfigSO config = BuildConfig();
            Assert.That(Serialize(RegionMapGenerator.Generate(config, 999u, 0)),
                Is.Not.EqualTo(Serialize(RegionMapGenerator.Generate(config, 999u, 1))));
        }

        // ── Topology / forced constraints ───────────────────────────────────

        [Test]
        public void Generate_HasEightLayers()
            => Assert.That(RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u)).LayerCount, Is.EqualTo(8));

        [Test]
        public void Generate_LayerWidths_MatchConfig()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            for (int l = 0; l < ExpectedWidths.Length; l++)
                Assert.That(map.Layers[l].Count, Is.EqualTo(ExpectedWidths[l]), $"Layer {l} width.");
        }

        [Test]
        public void Generate_Layer0_IsSingleForcedWild()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            Assert.That(map.Layers[0].Count, Is.EqualTo(1));
            Assert.That(map.Entry.NodeType, Is.EqualTo(NodeType.Wild));
        }

        [Test]
        public void Generate_FinalLayer_IsSingleGym()
        {
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            Assert.That(map.GymNodes.Count, Is.EqualTo(1), "Routes converge to one final Gym.");
            Assert.That(map.GymNodes[0].NodeType, Is.EqualTo(NodeType.Gym));
        }

        [Test]
        public void Generate_EliteExactlyOnce_AtLayer3()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                int elite = 0;
                foreach (MapNode node in map.AllNodes())
                    if (node.NodeType == NodeType.Elite) { elite++; Assert.That(node.Layer, Is.EqualTo(3), $"seed {seed}"); }
                Assert.That(elite, Is.EqualTo(1), $"seed {seed}");
            }
        }

        [Test]
        public void Generate_Center_Guaranteed_AtLayer5()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                Assert.That(map.Layers[5][0].NodeType, Is.EqualTo(NodeType.Center), $"seed {seed}");
            }
        }

        // ── Adjacency / connectivity ─────────────────────────────────────────

        [Test]
        public void Generate_NoAdjacentSameType_WithinLayer()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                for (int layer = 0; layer < map.LayerCount; layer++)
                {
                    List<MapNode> nodes = map.Layers[layer];
                    for (int i = 1; i < nodes.Count; i++)
                        Assert.That(nodes[i].NodeType, Is.Not.EqualTo(nodes[i - 1].NodeType), $"L{layer}#{i} seed {seed}");
                }
            }
        }

        [Test]
        public void Generate_EveryNonGymNode_HasForwardConnections()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                foreach (MapNode node in map.AllNodes())
                {
                    if (node.Layer == map.LayerCount - 1) { Assert.That(node.Next.Count, Is.EqualTo(0)); continue; }
                    Assert.That(node.Next.Count, Is.GreaterThanOrEqualTo(1), $"{node} seed {seed}");
                    foreach (MapNode child in node.Next)
                        Assert.That(child.Layer, Is.EqualTo(node.Layer + 1), $"{node} seed {seed}");
                }
            }
        }

        [Test]
        public void Generate_IsPartialNet_NotFullMesh()
        {
            // The L2(5) → L3(3) wiring must not connect every parent to every child (15 edges).
            RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(7u));
            int edges = 0;
            foreach (MapNode n in map.Layers[2]) edges += n.Next.Count;
            Assert.That(edges, Is.LessThan(map.Layers[2].Count * map.Layers[3].Count), "Net should be partial, not a full mesh.");
        }

        [Test]
        public void Generate_EveryNode_ReachableFromEntry()
        {
            for (uint seed = 1; seed <= 30; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                HashSet<MapNode> visited = new() { map.Entry };
                Queue<MapNode> q = new(); q.Enqueue(map.Entry);
                while (q.Count > 0)
                    foreach (MapNode c in q.Dequeue().Next)
                        if (visited.Add(c)) q.Enqueue(c);
                int total = 0; foreach (MapNode _ in map.AllNodes()) total++;
                Assert.That(visited.Count, Is.EqualTo(total), $"orphans (seed {seed})");
            }
        }

        [Test]
        public void Generate_EveryNode_ReachesGym()
        {
            for (uint seed = 1; seed <= 20; seed++)
            {
                RegionMap map = RegionMapGenerator.Generate(BuildConfig(), new GameRNG(seed));
                foreach (MapNode start in map.AllNodes())
                {
                    MapNode cur = start; int guard = 0;
                    while (cur.Next.Count > 0 && guard < map.LayerCount + 2) { cur = cur.Next[0]; guard++; }
                    Assert.That(cur.NodeType, Is.EqualTo(NodeType.Gym), $"{start} seed {seed}");
                }
            }
        }
    }
}
