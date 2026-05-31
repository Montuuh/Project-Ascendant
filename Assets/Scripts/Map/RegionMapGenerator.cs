using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.11 (+ gap #39 design override) — deterministic Region map seeding. Pure C#.
    //
    // Flat, variable-width layers (each layer has its own node count) wired into a partial
    // StS-style net: every node links to its proportional child in the next layer plus an optional
    // near neighbour (≤ DefaultMaxBranches), and every child is guaranteed a parent. The final layer
    // is a single Gym, so all routes converge to it — giving a "choose-your-route" web rather than a
    // fully-connected mesh. Same (RunSeed, RegionIndex) ⇒ identical map (Engineering Pillar 3).
    public static class RegionMapGenerator
    {
        public static RegionMap Generate(MapGenerationConfigSO config, uint runSeed, int regionIndex = 0)
        {
            uint seed = runSeed ^ RNGStreams.FNV1a("MapRNG") ^ (uint)regionIndex;
            return Generate(config, new GameRNG(seed), regionIndex);
        }

        public static RegionMap Generate(MapGenerationConfigSO config, GameRNG rng, int regionIndex = 0)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (config.LayerCount < 2)
                throw new ArgumentException("MapGenerationConfig.LayerCount must be >= 2.", nameof(config));

            Dictionary<int, MapLayerSpec> specs = IndexSpecs(config);
            List<List<MapNode>> layers = BuildSkeleton(config, specs, rng);
            EnforceNoAdjacentSameType(config, layers, rng);
            WireConnections(config, layers, rng);
            return new RegionMap(regionIndex, layers);
        }

        // ── Skeleton + forced types ──────────────────────────────────────────

        private static List<List<MapNode>> BuildSkeleton(
            MapGenerationConfigSO config, Dictionary<int, MapLayerSpec> specs, GameRNG rng)
        {
            List<List<MapNode>> layers = new();
            for (int layer = 0; layer < config.LayerCount; layer++)
            {
                if (!specs.TryGetValue(layer, out MapLayerSpec spec))
                    throw new ArgumentException($"MapGenerationConfig is missing a MapLayerSpec for layer {layer}.");

                int count = Math.Max(1, spec.NodesInLayer);
                IList<(NodeType value, float weight)> options = BuildWeightedOptions(config, layer);

                List<MapNode> nodes = new(count);
                for (int i = 0; i < count; i++)
                {
                    NodeType sampled = options.Count > 0 ? rng.PickWeighted(options) : NodeType.Trainer;
                    nodes.Add(new MapNode(layer, 0, i, sampled));
                }

                StampForcedTypes(spec, nodes, rng);
                layers.Add(nodes);
            }
            return layers;
        }

        private static void StampForcedTypes(MapLayerSpec spec, List<MapNode> nodes, GameRNG rng)
        {
            switch (spec.ForceMode)
            {
                case LayerForceMode.AllNodes:
                    for (int i = 0; i < nodes.Count; i++) nodes[i].NodeType = spec.ForcedType;
                    break;
                case LayerForceMode.OneNodeInLayer:
                    nodes[rng.Range(0, nodes.Count)].NodeType = spec.ForcedType;
                    break;
            }
        }

        // ── No-adjacent-same-type within a layer ─────────────────────────────

        private static void EnforceNoAdjacentSameType(
            MapGenerationConfigSO config, List<List<MapNode>> layers, GameRNG rng)
        {
            Dictionary<int, MapLayerSpec> specs = IndexSpecs(config);
            for (int layer = 0; layer < layers.Count; layer++)
            {
                MapLayerSpec spec = specs[layer];
                IList<(NodeType value, float weight)> options = BuildWeightedOptions(config, layer);
                List<MapNode> nodes = layers[layer];

                for (int attempt = 0; attempt < config.ConstraintRetryCap; attempt++)
                {
                    bool clean = true;
                    for (int i = 1; i < nodes.Count; i++)
                    {
                        if (nodes[i].NodeType != nodes[i - 1].NodeType) continue;
                        clean = false;
                        MapNode reroll = !IsForced(nodes[i], spec) ? nodes[i]
                                       : !IsForced(nodes[i - 1], spec) ? nodes[i - 1] : null;
                        if (reroll == null) continue;
                        reroll.NodeType = PickDifferent(options, nodes[i - 1].NodeType,
                            i + 1 < nodes.Count ? nodes[i + 1].NodeType : (NodeType?)null, rng, reroll.NodeType);
                    }
                    if (clean) break;
                }
            }
        }

        private static NodeType PickDifferent(
            IList<(NodeType value, float weight)> options, NodeType avoidA, NodeType? avoidB, GameRNG rng, NodeType current)
        {
            List<(NodeType, float)> filtered = new();
            for (int i = 0; i < options.Count; i++)
            {
                NodeType t = options[i].value;
                if (t == avoidA || (avoidB.HasValue && t == avoidB.Value)) continue;
                filtered.Add((t, options[i].weight));
            }
            if (filtered.Count == 0)
                for (int i = 0; i < options.Count; i++)
                    if (options[i].value != avoidA) filtered.Add((options[i].value, options[i].weight));
            return filtered.Count > 0 ? rng.PickWeighted(filtered) : current;
        }

        // ── Connection wiring (partial StS net) ──────────────────────────────

        private static void WireConnections(MapGenerationConfigSO config, List<List<MapNode>> layers, GameRNG rng)
        {
            int maxFwd = Math.Max(1, config.DefaultMaxBranches);
            for (int layer = 0; layer < layers.Count - 1; layer++)
                ConnectLayers(layers[layer], layers[layer + 1], maxFwd, rng);
        }

        // Each parent links to its proportional child + (≤ maxFwd) an optional near neighbour; then
        // every child is guaranteed a parent. Feed-forward, so every node reaches the single Gym.
        private static void ConnectLayers(List<MapNode> parents, List<MapNode> children, int maxFwd, GameRNG rng)
        {
            int P = parents.Count, C = children.Count;
            if (P == 0 || C == 0) return;

            for (int i = 0; i < P; i++)
            {
                int baseChild = Proportional(i, P, C);
                AddEdge(parents[i], children[baseChild]);

                // ~50% add a single adjacent neighbour for route variety, capped at maxFwd.
                if (maxFwd > 1 && C > 1 && parents[i].Next.Count < maxFwd && rng.Range(0, 2) == 0)
                {
                    int off = rng.Range(0, 2) == 0 ? -1 : 1;
                    int nb = Mathf.Clamp(baseChild + off, 0, C - 1);
                    AddEdge(parents[i], children[nb]);
                }
            }

            // Coverage: any child with no parent is connected from its nearest proportional parent.
            for (int j = 0; j < C; j++)
            {
                if (HasParent(parents, children[j])) continue;
                int pi = Proportional(j, C, P);
                AddEdge(parents[pi], children[j]);
            }
        }

        private static int Proportional(int index, int fromCount, int toCount)
        {
            if (toCount <= 1) return 0;
            if (fromCount <= 1) return Mathf.Clamp(Mathf.RoundToInt((toCount - 1) * 0.5f), 0, toCount - 1);
            int v = Mathf.RoundToInt((float)index * (toCount - 1) / (fromCount - 1));
            return Mathf.Clamp(v, 0, toCount - 1);
        }

        private static bool HasParent(List<MapNode> parents, MapNode child)
        {
            for (int i = 0; i < parents.Count; i++)
                if (parents[i].Next.Contains(child)) return true;
            return false;
        }

        private static void AddEdge(MapNode parent, MapNode child)
        {
            if (!parent.Next.Contains(child)) parent.Next.Add(child);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Dictionary<int, MapLayerSpec> IndexSpecs(MapGenerationConfigSO config)
        {
            Dictionary<int, MapLayerSpec> map = new();
            if (config.Layers != null)
                for (int i = 0; i < config.Layers.Count; i++)
                    map[config.Layers[i].Layer] = config.Layers[i];
            return map;
        }

        private static IList<(NodeType value, float weight)> BuildWeightedOptions(MapGenerationConfigSO config, int layer)
        {
            List<(NodeType, float)> options = new();
            if (config.LayerWeights == null) return options;
            for (int i = 0; i < config.LayerWeights.Count; i++)
            {
                NodeLayerWeights w = config.LayerWeights[i];
                if (w.Layer != layer) continue;
                if (w.WildWeight > 0f) options.Add((NodeType.Wild, w.WildWeight));
                if (w.TrainerWeight > 0f) options.Add((NodeType.Trainer, w.TrainerWeight));
                if (w.CenterWeight > 0f) options.Add((NodeType.Center, w.CenterWeight));
                if (w.ShopWeight > 0f) options.Add((NodeType.Shop, w.ShopWeight));
                if (w.MysteryWeight > 0f) options.Add((NodeType.Mystery, w.MysteryWeight));
                if (w.GymWeight > 0f) options.Add((NodeType.Gym, w.GymWeight));
                break;
            }
            return options;
        }

        private static bool IsForced(MapNode node, MapLayerSpec spec)
        {
            return spec.ForceMode switch
            {
                LayerForceMode.AllNodes => true,
                LayerForceMode.OneNodeInLayer => node.NodeType == spec.ForcedType,
                _ => false,
            };
        }
    }
}
