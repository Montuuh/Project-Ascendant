using System;
using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.11 — deterministic Region map seeding. Pure C#, no Unity/view dependency.
    //
    //   seed  = RunSeed XOR RegionIndex
    //   graph = LadderGraph(layers=8, defaultWidth=3)
    //   Layer(0) = single forced Wild; Layer(4) enables branching (2 lanes)
    //   for layer 1..7: node.NodeType = WeightedSample(rng, NodeTypeDistribution(layer))
    //   ApplyConstraint: NoAdjacentSameType, EliteAtL3, CenterAtL6, GymAtL7
    //
    // Same (RunSeed, RegionIndex) => identical map (Engineering Pillar 3 / §1.3.2).
    public static class RegionMapGenerator
    {
        // Per §7.11 — convenience overload that derives the MapRNG stream per the seed rule.
        // streamSeed mirrors RNGStreams.MapRNG; regionIndex is folded in so different Regions differ.
        public static RegionMap Generate(MapGenerationConfigSO config, uint runSeed, int regionIndex = 0)
        {
            uint seed = runSeed ^ RNGStreams.FNV1a("MapRNG") ^ (uint)regionIndex;
            return Generate(config, new GameRNG(seed), regionIndex);
        }

        // Core entry. The caller supplies the (already-seeded) MapRNG stream — keeps the
        // generator deterministic and unit-testable independent of stream wiring.
        public static RegionMap Generate(MapGenerationConfigSO config, GameRNG rng, int regionIndex = 0)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (rng == null)    throw new ArgumentNullException(nameof(rng));
            if (config.LayerCount < 2)
                throw new ArgumentException("MapGenerationConfig.LayerCount must be >= 2.", nameof(config));

            Dictionary<int, MapLayerSpec> specs = IndexSpecs(config);

            // 1. Build the node skeleton with forced types stamped, then weighted-sample the rest.
            List<List<MapNode>> layers = BuildSkeleton(config, specs, rng);

            // 2. Per §7.2.3 — no two adjacent nodes in a lane share a node type.
            EnforceNoAdjacentSameType(config, layers, rng);

            // 3. Per §7.2.3 — wire forward connections (1..DefaultMaxBranches per node).
            WireConnections(config, layers, rng);

            return new RegionMap(regionIndex, layers);
        }

        // ---- Skeleton + forced types --------------------------------------------------------

        private static List<List<MapNode>> BuildSkeleton(
            MapGenerationConfigSO config, Dictionary<int, MapLayerSpec> specs, GameRNG rng)
        {
            List<List<MapNode>> layers = new();

            for (int layer = 0; layer < config.LayerCount; layer++)
            {
                if (!specs.TryGetValue(layer, out MapLayerSpec spec))
                    throw new ArgumentException($"MapGenerationConfig is missing a MapLayerSpec for layer {layer}.");

                int laneCount   = layer >= config.BranchLayerIndex ? config.LaneCountAfterBranch : 1;
                int widthPerLane = Math.Max(1, spec.WidthPerLane);

                List<MapNode> layerNodes = new();

                // Per §7.11 — weighted sample first; forced types overwrite below.
                IList<(NodeType value, float weight)> options = BuildWeightedOptions(config, layer);

                for (int lane = 0; lane < laneCount; lane++)
                {
                    for (int i = 0; i < widthPerLane; i++)
                    {
                        NodeType sampled = options.Count > 0 ? rng.PickWeighted(options) : NodeType.Trainer;
                        layerNodes.Add(new MapNode(layer, lane, i, sampled));
                    }
                }

                StampForcedTypes(spec, layerNodes, laneCount, rng);
                layers.Add(layerNodes);
            }

            return layers;
        }

        // Per §7.2.1 — stamp the layer's forced node type (L0 Wild, L3 Elite, L6 Center, L7 Gym).
        private static void StampForcedTypes(MapLayerSpec spec, List<MapNode> layerNodes, int laneCount, GameRNG rng)
        {
            switch (spec.ForceMode)
            {
                case LayerForceMode.None:
                    break;

                case LayerForceMode.AllNodes:
                    for (int i = 0; i < layerNodes.Count; i++)
                        layerNodes[i].NodeType = spec.ForcedType;
                    break;

                case LayerForceMode.OneNodeInLayer:
                {
                    // Exactly one node across the whole layer (pre-branch L3 Elite).
                    int pick = rng.Range(0, layerNodes.Count);
                    layerNodes[pick].NodeType = spec.ForcedType;
                    break;
                }

                case LayerForceMode.OneNodePerLane:
                {
                    // Exactly one node per lane (L6 Center — guaranteed pre-Gym in each branch).
                    for (int lane = 0; lane < laneCount; lane++)
                    {
                        List<MapNode> inLane = LaneSlice(layerNodes, lane);
                        if (inLane.Count == 0) continue;
                        int pick = rng.Range(0, inLane.Count);
                        inLane[pick].NodeType = spec.ForcedType;
                    }
                    break;
                }
            }
        }

        // ---- No-adjacent-same-type constraint -----------------------------------------------

        // Per §7.2.3 — within each (layer, lane), consecutive nodes must differ in type.
        // Re-rolls only non-forced nodes; bounded by ConstraintRetryCap (§7.11 fallback re-rolls).
        private static void EnforceNoAdjacentSameType(
            MapGenerationConfigSO config, List<List<MapNode>> layers, GameRNG rng)
        {
            Dictionary<int, MapLayerSpec> specs = IndexSpecs(config);

            for (int layer = 0; layer < layers.Count; layer++)
            {
                MapLayerSpec spec = specs[layer];
                int laneCount = layer >= config.BranchLayerIndex ? config.LaneCountAfterBranch : 1;
                IList<(NodeType value, float weight)> options = BuildWeightedOptions(config, layer);

                for (int lane = 0; lane < laneCount; lane++)
                {
                    List<MapNode> inLane = LaneSlice(layers[layer], lane);

                    for (int attempt = 0; attempt < config.ConstraintRetryCap; attempt++)
                    {
                        bool clean = true;
                        for (int i = 1; i < inLane.Count; i++)
                        {
                            if (inLane[i].NodeType != inLane[i - 1].NodeType) continue;
                            clean = false;

                            // Re-roll a NON-forced node in the pair (forced types are fixed).
                            MapNode reroll = !IsForced(inLane[i], spec) ? inLane[i]
                                           : !IsForced(inLane[i - 1], spec) ? inLane[i - 1]
                                           : null;
                            if (reroll == null) continue; // both forced same type — unsolvable, leave it
                            reroll.NodeType = PickDifferent(options, inLane[i - 1].NodeType,
                                                            i + 1 < inLane.Count ? inLane[i + 1].NodeType : (NodeType?)null,
                                                            rng, reroll.NodeType);
                        }
                        if (clean) break;
                    }
                }
            }
        }

        // Picks a weighted type != avoidA and (where possible) != avoidB. Falls back to current.
        private static NodeType PickDifferent(
            IList<(NodeType value, float weight)> options, NodeType avoidA, NodeType? avoidB, GameRNG rng, NodeType current)
        {
            List<(NodeType, float)> filtered = new();
            for (int i = 0; i < options.Count; i++)
            {
                NodeType t = options[i].value;
                if (t == avoidA) continue;
                if (avoidB.HasValue && t == avoidB.Value) continue;
                filtered.Add((t, options[i].weight));
            }
            // Relax the avoidB constraint if it left nothing.
            if (filtered.Count == 0)
                for (int i = 0; i < options.Count; i++)
                    if (options[i].value != avoidA)
                        filtered.Add((options[i].value, options[i].weight));

            return filtered.Count > 0 ? rng.PickWeighted(filtered) : current;
        }

        // ---- Connection wiring --------------------------------------------------------------

        private static void WireConnections(
            MapGenerationConfigSO config, List<List<MapNode>> layers, GameRNG rng)
        {
            for (int layer = 0; layer < layers.Count - 1; layer++)
            {
                bool curMulti  = layer       >= config.BranchLayerIndex;
                bool nextMulti = (layer + 1)  >= config.BranchLayerIndex;

                if (!curMulti && nextMulti)
                {
                    // Per §7.2.1 — the Branch Point: every pre-branch node connects to every
                    // lane-entry node so the player can choose either branch.
                    List<MapNode> parents  = layers[layer];
                    List<MapNode> children = layers[layer + 1];
                    for (int p = 0; p < parents.Count; p++)
                        for (int c = 0; c < children.Count; c++)
                            parents[p].Next.Add(children[c]);
                }
                else if (curMulti && nextMulti)
                {
                    // Per §7.2.1 — lanes are independent after the branch; wire lane-by-lane.
                    for (int lane = 0; lane < config.LaneCountAfterBranch; lane++)
                        ConnectGroups(LaneSlice(layers[layer], lane),
                                      LaneSlice(layers[layer + 1], lane),
                                      config.DefaultMaxBranches, rng);
                }
                else
                {
                    // Single lane → single lane.
                    ConnectGroups(layers[layer], layers[layer + 1], config.DefaultMaxBranches, rng);
                }
            }
        }

        // Deterministically connects a parent group to a child group such that every parent has
        // 1..maxBranches children and every child has >= 1 parent (connectivity to the Gym).
        private static void ConnectGroups(List<MapNode> parents, List<MapNode> children, int maxBranches, GameRNG rng)
        {
            if (parents.Count == 0 || children.Count == 0) return;
            int P = parents.Count, C = children.Count;

            // 1. Coverage: every child gets at least one parent (proportional mapping).
            for (int j = 0; j < C; j++)
            {
                int pi = P == 1 ? 0 : (int)((long)j * P / C);
                if (pi >= P) pi = P - 1;
                AddEdge(parents[pi], children[j]);
            }

            // 2. Every parent gets at least one child.
            for (int i = 0; i < P; i++)
            {
                if (parents[i].Next.Count > 0) continue;
                int cj = C == 1 ? 0 : (int)((long)i * C / P);
                if (cj >= C) cj = C - 1;
                AddEdge(parents[i], children[cj]);
            }

            // 3. Optional extra edges (up to maxBranches) to fan out the ladder.
            for (int i = 0; i < P; i++)
            {
                int baseChild = C == 1 ? 0 : (int)((long)i * C / P);
                if (baseChild >= C) baseChild = C - 1;

                int wanted = maxBranches <= 1 ? 1 : 1 + rng.Range(0, maxBranches);
                int guard = 0;
                while (parents[i].Next.Count < wanted && parents[i].Next.Count < C && guard < C * 2)
                {
                    int offset = rng.Range(-1, 2); // -1, 0, +1
                    int cj = baseChild + offset;
                    if (cj >= 0 && cj < C) AddEdge(parents[i], children[cj]);
                    guard++;
                }
            }
        }

        private static void AddEdge(MapNode parent, MapNode child)
        {
            if (!parent.Next.Contains(child)) parent.Next.Add(child);
        }

        // ---- Helpers ------------------------------------------------------------------------

        private static Dictionary<int, MapLayerSpec> IndexSpecs(MapGenerationConfigSO config)
        {
            Dictionary<int, MapLayerSpec> map = new();
            if (config.Layers != null)
                for (int i = 0; i < config.Layers.Count; i++)
                    map[config.Layers[i].Layer] = config.Layers[i];
            return map;
        }

        // Builds weighted (type, weight) options for a layer from NodeLayerWeights (weight > 0 only).
        // Elite is never weighted-sampled — it is only placed via the L3 forced-type rule.
        private static IList<(NodeType value, float weight)> BuildWeightedOptions(MapGenerationConfigSO config, int layer)
        {
            List<(NodeType, float)> options = new();
            if (config.LayerWeights == null) return options;

            for (int i = 0; i < config.LayerWeights.Count; i++)
            {
                NodeLayerWeights w = config.LayerWeights[i];
                if (w.Layer != layer) continue;
                if (w.WildWeight    > 0f) options.Add((NodeType.Wild,    w.WildWeight));
                if (w.TrainerWeight > 0f) options.Add((NodeType.Trainer, w.TrainerWeight));
                if (w.CenterWeight  > 0f) options.Add((NodeType.Center,  w.CenterWeight));
                if (w.ShopWeight    > 0f) options.Add((NodeType.Shop,    w.ShopWeight));
                if (w.MysteryWeight > 0f) options.Add((NodeType.Mystery, w.MysteryWeight));
                if (w.GymWeight     > 0f) options.Add((NodeType.Gym,     w.GymWeight));
                break;
            }
            return options;
        }

        private static List<MapNode> LaneSlice(List<MapNode> layerNodes, int lane)
        {
            List<MapNode> result = new();
            for (int i = 0; i < layerNodes.Count; i++)
                if (layerNodes[i].Lane == lane)
                    result.Add(layerNodes[i]);
            return result;
        }

        private static bool IsForced(MapNode node, MapLayerSpec spec)
        {
            switch (spec.ForceMode)
            {
                case LayerForceMode.AllNodes:       return true;
                case LayerForceMode.OneNodeInLayer:
                case LayerForceMode.OneNodePerLane: return node.NodeType == spec.ForcedType;
                default:                            return false;
            }
        }
    }
}
