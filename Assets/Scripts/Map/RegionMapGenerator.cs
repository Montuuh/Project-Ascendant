using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.2 v2 — deterministic Region map seeding with 12-layer gym-fork topology. Pure C#.
    //
    // V2 structure:
    //   L0:     ~3 entry nodes (varied types, guarantee ≥1 Wild within L1-L2)
    //   L1-L8:  trunk (branching tree, 1-3 fwd edges, in-degree ≤2, 1 Elite in late trunk)
    //   L9:     gym fork — splits into 2 independent sub-lanes
    //   L10:    per-lane; guaranteed Pokemon Center in each sub-lane
    //   L11:    terminal Gym nodes (2 total, one per sub-lane with distinct gym assignments)
    //
    // Same (RunSeed, RegionIndex) ⇒ identical map (Engineering Pillar 3).
    public static class RegionMapGenerator
    {
        public static RegionMap Generate(MapGenerationConfigSO config, uint runSeed, int regionIndex,
            IReadOnlyList<GymLeaderSO> gymPool = null, IReadOnlyList<EliteTrainerRosterSO> eliteRosters = null,
            IReadOnlyList<EliteWildSO> eliteWilds = null)
        {
            uint seed = runSeed ^ RNGStreams.FNV1a("MapRNG") ^ (uint)regionIndex;
            return Generate(config, new GameRNG(seed), regionIndex, gymPool, eliteRosters, eliteWilds);
        }

        public static RegionMap Generate(MapGenerationConfigSO config, GameRNG rng, int regionIndex,
            IReadOnlyList<GymLeaderSO> gymPool = null, IReadOnlyList<EliteTrainerRosterSO> eliteRosters = null,
            IReadOnlyList<EliteWildSO> eliteWilds = null)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (rng == null) throw new ArgumentNullException(nameof(rng));
            if (config.LayerCount < 2)
                throw new ArgumentException("MapGenerationConfig.LayerCount must be >= 2.", nameof(config));

            Dictionary<int, MapLayerSpec> specs = IndexSpecs(config);
            List<List<MapNode>> layers = BuildSkeleton(config, specs, rng);
            EnforceNoAdjacentSameType(config, layers, rng);
            WireConnections(config, layers, rng);

            // Per §7.5.1 (CL-024) — resolve Elite Trainer occupants from weighted roster.
            ResolveEliteOccupants(layers, regionIndex, eliteRosters, rng);

            // Per §7.5.2 (CL-024) — seeded Elite Wild placement (≤1/Region, Apex-node model §4.5.1.2).
            TryPlaceEliteWild(layers, config, eliteWilds, rng);

            List<GymLeaderSO> chosenGyms = AssignGyms(layers, gymPool, rng);
            return new RegionMap(regionIndex, layers, chosenGyms);
        }

        // ── Skeleton + forced types ──────────────────────────────────────────

        private static List<List<MapNode>> BuildSkeleton(
            MapGenerationConfigSO config, Dictionary<int, MapLayerSpec> specs, GameRNG rng)
        {
            List<List<MapNode>> layers = new();
            int forkLayer = config.GymForkLayer >= 0 ? config.GymForkLayer : int.MaxValue;

            for (int layer = 0; layer < config.LayerCount; layer++)
            {
                if (!specs.TryGetValue(layer, out MapLayerSpec spec))
                    throw new ArgumentException($"MapGenerationConfig is missing a MapLayerSpec for layer {layer}.");

                int count = Math.Max(1, spec.NodesInLayer);
                IList<(NodeType value, float weight)> options = BuildWeightedOptions(config, layer);

                // Per §7.2 v2 — after fork, nodes belong to lanes. Pre-fork = lane 0.
                bool postFork = layer >= forkLayer;
                int laneCount = postFork ? 2 : 1;

                List<MapNode> nodes = new(count);
                for (int i = 0; i < count; i++)
                {
                    NodeType sampled = options.Count > 0 ? rng.PickWeighted(options) : NodeType.Trainer;
                    int lane = postFork ? (i % laneCount) : 0; // distribute evenly across lanes
                    int indexInLane = postFork ? (i / laneCount) : i;
                    nodes.Add(new MapNode(layer, lane, indexInLane, sampled));
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
                    for (int i = 0; i < nodes.Count; i++) { nodes[i].NodeType = spec.ForcedType; nodes[i].Forced = true; }
                    break;
                case LayerForceMode.OneNodeInLayer:
                    MapNode chosen = nodes[rng.Range(0, nodes.Count)];
                    chosen.NodeType = spec.ForcedType;
                    chosen.Forced = true;
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

                // AllNodes-forced layers (e.g. L10 Centers, L11 Gyms) are intentionally same-type — skip.
                if (spec.ForceMode == LayerForceMode.AllNodes) continue;

                // Per §7.2 v2 — check adjacency within each lane separately. A node is locked only if it
                // is THE force-stamped node (node.Forced) — not merely sharing the forced type.
                for (int lane = 0; lane < 2; lane++)
                {
                    List<MapNode> laneNodes = GetLaneNodes(nodes, lane);
                    if (laneNodes.Count < 2) continue;

                    for (int attempt = 0; attempt < config.ConstraintRetryCap; attempt++)
                    {
                        bool clean = true;
                        for (int i = 1; i < laneNodes.Count; i++)
                        {
                            if (laneNodes[i].NodeType != laneNodes[i - 1].NodeType) continue;
                            clean = false;
                            MapNode reroll = !laneNodes[i].Forced ? laneNodes[i]
                                           : !laneNodes[i - 1].Forced ? laneNodes[i - 1] : null;
                            if (reroll == null) continue; // both forced same type (rare) — accept
                            reroll.NodeType = PickDifferent(options, laneNodes[i - 1].NodeType,
                                i + 1 < laneNodes.Count ? laneNodes[i + 1].NodeType : (NodeType?)null, rng, reroll.NodeType);
                        }
                        if (clean) break;
                    }
                }
            }
        }

        private static List<MapNode> GetLaneNodes(List<MapNode> nodes, int lane)
        {
            List<MapNode> result = new();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Lane == lane) result.Add(nodes[i]);
            // Sort by IndexInLane for adjacency checking.
            result.Sort((a, b) => a.IndexInLane.CompareTo(b.IndexInLane));
            return result;
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

        // ── Connection wiring (v2: branching tree + fork) ────────────────────

        private static void WireConnections(MapGenerationConfigSO config, List<List<MapNode>> layers, GameRNG rng)
        {
            int maxFwd = Math.Max(1, config.DefaultMaxBranches);
            int maxInDegree = Math.Max(1, config.MaxInDegree);
            int forkLayer = config.GymForkLayer >= 0 ? config.GymForkLayer : int.MaxValue;

            for (int layer = 0; layer < layers.Count - 1; layer++)
            {
                bool isFork = layer + 1 == forkLayer;
                if (isFork)
                {
                    // Fork: every trunk node connects to both lane gateways (the Gym choice).
                    ConnectFork(layers[layer], layers[layer + 1]);
                }
                else if (layer + 1 > forkLayer)
                {
                    // Post-fork: wire within each lane independently.
                    ConnectPostFork(layers[layer], layers[layer + 1], maxFwd, maxInDegree, rng);
                }
                else
                {
                    // Pre-fork trunk: branching tree.
                    ConnectLayersBranching(layers[layer], layers[layer + 1], maxFwd, maxInDegree, rng);
                }
            }
        }

        // Pre-fork / per-lane: branching tree. GUARANTEES (the load-bearing connectivity invariants):
        //   • every parent gets ≥1 forward edge (no dead ends → every node reaches the gym),
        //   • every child gets ≥1 parent (no orphans → every node reachable from an entry),
        //   • in-degree ≤ maxInDegree (branchy, not a convergent mesh).
        // The authored layer widths keep total capacity (C × maxInDegree) ≥ P, so the caps never block
        // a mandatory edge.
        private static void ConnectLayersBranching(List<MapNode> parents, List<MapNode> children, int maxFwd, int maxInDegree, GameRNG rng)
        {
            int P = parents.Count, C = children.Count;
            if (P == 0 || C == 0) return;

            Dictionary<MapNode, int> inDegree = new();
            for (int i = 0; i < C; i++) inDegree[children[i]] = 0;

            // Pass A — every parent gets ≥1 forward edge. Prefer the proportional child; if it is at the
            // in-degree cap, take the nearest child with spare capacity.
            for (int i = 0; i < P; i++)
            {
                int baseChild = Proportional(i, P, C);
                int t = NearestChildWithSpare(children, inDegree, baseChild, maxInDegree);
                ConnectChild(parents[i], children[t < 0 ? baseChild : t], inDegree);
            }

            // Pass B — a little extra branching for route variety (respect both caps).
            for (int i = 0; i < P; i++)
            {
                if (rng.Range(0, 2) != 0) continue; // ~50% of parents get a second edge
                if (parents[i].Next.Count >= maxFwd) continue;
                int baseChild = Proportional(i, P, C);
                int off = rng.Range(0, 2) == 0 ? -1 : 1;
                int nb = Mathf.Clamp(baseChild + off, 0, C - 1);
                if (inDegree[children[nb]] < maxInDegree && !parents[i].Next.Contains(children[nb]))
                    ConnectChild(parents[i], children[nb], inDegree);
            }

            // Pass C — coverage: every child gets ≥1 parent (in-degree 0 → 1, within cap).
            for (int j = 0; j < C; j++)
            {
                if (inDegree[children[j]] > 0) continue;
                int pref = Proportional(j, C, P);
                int pi = NearestParentWithRoom(parents, pref, maxFwd, children[j]);
                ConnectChild(parents[pi < 0 ? pref : pi], children[j], inDegree);
            }
        }

        // Nearest child index to 'start' (expanding ring) whose in-degree < cap; -1 if none has spare.
        private static int NearestChildWithSpare(List<MapNode> children, Dictionary<MapNode, int> inDegree, int start, int cap)
        {
            int C = children.Count;
            for (int d = 0; d < C; d++)
            {
                int lo = start - d, hi = start + d;
                if (lo >= 0 && inDegree[children[lo]] < cap) return lo;
                if (hi < C && hi != lo && inDegree[children[hi]] < cap) return hi;
            }
            return -1;
        }

        // Nearest parent index to 'start' with out-degree < maxFwd not already linked to 'child'; -1 none.
        private static int NearestParentWithRoom(List<MapNode> parents, int start, int maxFwd, MapNode child)
        {
            int P = parents.Count;
            for (int d = 0; d < P; d++)
            {
                int lo = start - d, hi = start + d;
                if (lo >= 0 && parents[lo].Next.Count < maxFwd && !parents[lo].Next.Contains(child)) return lo;
                if (hi < P && hi != lo && parents[hi].Next.Count < maxFwd && !parents[hi].Next.Contains(child)) return hi;
            }
            return -1;
        }

        private static void ConnectChild(MapNode parent, MapNode child, Dictionary<MapNode, int> inDegree)
        {
            if (parent.Next.Contains(child)) return;
            parent.Next.Add(child);
            inDegree[child] = inDegree.TryGetValue(child, out int d) ? d + 1 : 1;
        }

        // Fork (last trunk layer → fork layer): EVERY trunk node connects to one gateway per lane, so
        // wherever the player stands they get the Gym choice at the fork. The fork gateways are the one
        // intentional convergence point (in-degree = trunk width) and are exempt from the in-degree cap.
        private static void ConnectFork(List<MapNode> parents, List<MapNode> gateways)
        {
            if (parents.Count == 0 || gateways.Count == 0) return;
            List<MapNode> lane0 = GetLaneNodes(gateways, 0);
            List<MapNode> lane1 = GetLaneNodes(gateways, 1);
            for (int i = 0; i < parents.Count; i++)
            {
                if (lane0.Count > 0) AddEdge(parents[i], lane0[0]);
                if (lane1.Count > 0) AddEdge(parents[i], lane1[0]);
            }
        }

        // Post-fork: wire within each lane independently (per-lane branching tree, in-degree ≤ cap).
        private static void ConnectPostFork(List<MapNode> parents, List<MapNode> children, int maxFwd, int maxInDegree, GameRNG rng)
        {
            for (int lane = 0; lane < 2; lane++)
            {
                List<MapNode> laneParents = GetLaneNodes(parents, lane);
                List<MapNode> laneChildren = GetLaneNodes(children, lane);
                ConnectLayersBranching(laneParents, laneChildren, maxFwd, maxInDegree, rng);
            }
        }

        private static int Proportional(int index, int fromCount, int toCount)
        {
            if (toCount <= 1) return 0;
            if (fromCount <= 1) return Mathf.Clamp(Mathf.RoundToInt((toCount - 1) * 0.5f), 0, toCount - 1);
            int v = Mathf.RoundToInt((float)index * (toCount - 1) / (fromCount - 1));
            return Mathf.Clamp(v, 0, toCount - 1);
        }

        private static void AddEdge(MapNode parent, MapNode child)
        {
            if (!parent.Next.Contains(child)) parent.Next.Add(child);
        }

        // ── Gym assignment ────────────────────────────────────────────────────

        // Per §7.2 v2 — pick 2 distinct gyms from the pool and assign to the terminal Gym nodes.
        private static List<GymLeaderSO> AssignGyms(List<List<MapNode>> layers, IReadOnlyList<GymLeaderSO> gymPool, GameRNG rng)
        {
            List<GymLeaderSO> chosenGyms = new();
            if (layers.Count == 0) return chosenGyms;

            List<MapNode> gymNodes = layers[layers.Count - 1];
            List<MapNode> gyms = new();
            for (int i = 0; i < gymNodes.Count; i++)
                if (gymNodes[i].NodeType == NodeType.Gym) gyms.Add(gymNodes[i]);

            if (gyms.Count == 0) return chosenGyms;

            // Pick 2 distinct gyms from the pool (or fallback to single/duplicate if pool insufficient).
            if (gymPool == null || gymPool.Count == 0)
            {
                // No pool: leave gym indices as -1 (caller must handle fallback).
                return chosenGyms;
            }

            List<int> indices = new();
            for (int i = 0; i < gymPool.Count; i++) indices.Add(i);

            // Fisher-Yates shuffle.
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = rng.Range(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            int gym0Idx = indices[0];
            int gym1Idx = indices.Count > 1 ? indices[1] : indices[0];

            chosenGyms.Add(gymPool[gym0Idx]);
            if (gym1Idx != gym0Idx || gymPool.Count == 1)
                chosenGyms.Add(gymPool[gym1Idx]);
            else if (chosenGyms.Count < 2)
                chosenGyms.Add(gymPool[gym0Idx]); // duplicate if only 1 gym in pool

            // Assign gym indices to nodes (sorted by lane to match fork structure).
            gyms.Sort((a, b) => a.Lane != b.Lane ? a.Lane.CompareTo(b.Lane) : a.IndexInLane.CompareTo(b.IndexInLane));
            for (int i = 0; i < gyms.Count && i < 2; i++)
                gyms[i].GymIndex = i;

            return chosenGyms;
        }

        // ── Elite Trainer occupant resolution ────────────────────────────────

        // Per §7.5.1 (CL-024) — weighted pick from EliteTrainerRosterSO for each Elite node.
        // Deterministic: same (seed, regionIndex) → same occupant.
        private static void ResolveEliteOccupants(List<List<MapNode>> layers, int regionIndex,
            IReadOnlyList<EliteTrainerRosterSO> eliteRosters, GameRNG rng)
        {
            if (eliteRosters == null || eliteRosters.Count == 0) return;

            // Find the roster for this region.
            EliteTrainerRosterSO roster = null;
            foreach (var r in eliteRosters)
                if (r != null && r.RegionIndex == regionIndex)
                {
                    roster = r;
                    break;
                }

            if (roster == null || roster.OccupantPool == null || roster.OccupantPool.Count == 0)
                return;

            // Build weighted options from the roster.
            List<(EliteTrainerSO occupant, float weight)> options = new();
            foreach (var entry in roster.OccupantPool)
                if (entry.Occupant != null && entry.Weight > 0f)
                    options.Add((entry.Occupant, entry.Weight));

            if (options.Count == 0) return;

            // Walk all layers, find Elite nodes, assign occupant.
            foreach (var layer in layers)
                foreach (var node in layer)
                    if (node.NodeType == NodeType.Elite)
                        node.EliteTrainerOccupant = rng.PickWeighted(options);
        }

        // ── Elite Wild special-node placement ────────────────────────────────

        // Per §7.5.2 (CL-024) — seeded Elite Wild placement (≤1/Region, not guaranteed).
        // Modelled on Apex node (§4.5.1.2): roll EliteWildPlacementChance; if hit, stamp
        // ONE mid/late-trunk non-forced node as NodeType.EliteWild, weighted-pick occupant.
        // Deterministic: same seed → same placement + occupant (or none).
        private static void TryPlaceEliteWild(List<List<MapNode>> layers, MapGenerationConfigSO config,
            IReadOnlyList<EliteWildSO> eliteWilds, GameRNG rng)
        {
            if (eliteWilds == null || eliteWilds.Count == 0) return;
            if (config.EliteWildPlacementChance <= 0f) return;

            // Roll placement.
            if (rng.Range01() > config.EliteWildPlacementChance) return;

            // Find eligible nodes: mid/late trunk (L4–L8 for a 12-layer map), non-forced, not Gym/Elite.
            List<MapNode> candidates = new();
            int midLayer = layers.Count / 3; // ≈L4 for 12-layer
            int lateLayer = layers.Count - 4; // ≈L8 for 12-layer
            for (int l = midLayer; l <= lateLayer && l < layers.Count; l++)
            {
                foreach (var node in layers[l])
                {
                    if (node.Forced) continue;
                    if (node.NodeType == NodeType.Gym || node.NodeType == NodeType.Elite) continue;
                    candidates.Add(node);
                }
            }

            if (candidates.Count == 0) return;

            // Pick one candidate.
            MapNode chosen = candidates[rng.Range(0, candidates.Count)];
            chosen.NodeType = NodeType.EliteWild;

            // Weighted-pick occupant from the EliteWilds pool.
            // All EliteWildSO carry equal implicit weight 1.0 for the VS (R1 pool: Snorlax OR Marowak's Spirit).
            List<(EliteWildSO wild, float weight)> options = new();
            foreach (var wild in eliteWilds)
                if (wild != null)
                    options.Add((wild, 1.0f));

            if (options.Count > 0)
                chosen.EliteWildOccupant = rng.PickWeighted(options);
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
                // Per §7.14 (CL-009) — Dojo node, ~1 per region.
                if (w.DojoWeight > 0f) options.Add((NodeType.Dojo, w.DojoWeight));
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
