using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.20 + §7.2.1 / §7.11 — map topology, per-layer structure, and node-type
    // distribution weights. Consumed by RegionMapGenerator (Epic 9). All values here — not in code.
    [CreateAssetMenu(fileName = "MapGenerationConfig", menuName = "Project Ascendant/Config/Map Generation Config")]
    public class MapGenerationConfigSO : ScriptableObject
    {
        [Header("Topology")]
        // Per §7.2.1 — 8 layers per Region (0-indexed 0..7). Layer 0 = Starter, Layer 7 = Gym.
        [Tooltip("Number of map layers, including Layer 0 (Starter) and the final Gym layer.")]
        public int LayerCount = 8;

        // Per §7.2.1 — from this layer onward the ladder splits into two independent lanes
        // ("2 lanes diverging to different Gyms"). Layers below this index are a single lane.
        [Tooltip("Layer index where the map branches into 2 lanes (the Branch Point Layer).")]
        public int BranchLayerIndex = 4;

        [Tooltip("Number of independent lanes after the branch layer.")]
        public int LaneCountAfterBranch = 2;

        // Per §7.2.3 — every node connects to 1..N nodes in the next layer.
        [Tooltip("Maximum forward connections per node. Overridden by DifficultyModifierSO.")]
        public int DefaultMaxBranches = 3;

        // Per §7.11 — constraints are applied iteratively with fallback re-rolls.
        [Tooltip("Max re-roll iterations when satisfying the no-adjacent-same-type constraint.")]
        public int ConstraintRetryCap = 8;

        [Header("Per-Layer Structure")]
        // Per §7.2.1 — one entry per layer describing its per-lane width and any forced node type.
        // Forced layers (L0 Wild, L3 Elite, L6 Center, L7 Gym) are stamped before weighted sampling.
        public List<MapLayerSpec> Layers;

        [Header("Per-Layer Node Weights")]
        // One entry per layer (0 = first layer after start, LayerCount-1 = pre-Gym layer).
        // Weights are relative — not probabilities. Forced node types are excluded at sample time.
        public List<NodeLayerWeights> LayerWeights;
    }

    // Per §7.2.1 — how a forced node type is distributed within a layer.
    public enum LayerForceMode
    {
        None,           // no forced node; all nodes weighted-sampled
        AllNodes,       // every node in the layer is ForcedType (L0 Wild, L7 Gym)
        OneNodeInLayer, // exactly one node across the whole layer is ForcedType (L3 Elite — pre-branch)
        OneNodePerLane  // exactly one node per lane is ForcedType (L6 Center — guaranteed per branch)
    }

    [Serializable]
    public struct MapLayerSpec
    {
        [Tooltip("Layer index this spec applies to (0-based).")]
        public int Layer;

        [Tooltip("Number of nodes per lane in this layer.")]
        public int WidthPerLane;

        [Tooltip("How (if at all) ForcedType is stamped onto this layer.")]
        public LayerForceMode ForceMode;

        [Tooltip("The forced node type. Ignored when ForceMode == None.")]
        public NodeType ForcedType;
    }

    [Serializable]
    public struct NodeLayerWeights
    {
        [Tooltip("Layer index this weight set applies to (0-based).")]
        public int Layer;

        public float WildWeight;
        public float TrainerWeight;
        public float CenterWeight;
        public float ShopWeight;
        public float MysteryWeight;

        [Tooltip("Gym weight — typically 0 except the final layer.")]
        public float GymWeight;
    }
}
