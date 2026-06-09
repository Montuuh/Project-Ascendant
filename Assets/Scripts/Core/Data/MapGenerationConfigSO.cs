using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.20 + §7.11 — map topology + per-layer node-type distribution. Consumed by
    // RegionMapGenerator (Epic 9). All values here — not in code.
    //
    // ⚠ DESIGN OVERRIDE (user-directed 2026-05-31, gap #39): replaces the fixed lane-based §7.2.1
    // topology with a flexible variable-width model — each layer has its own node count, partial
    // (not fully-connected) StS-style routes, and the run converges to a single final Gym with a
    // 2-route choice just before it. See BACKLOG gap #39 / GDD §7.2 flag.
    [CreateAssetMenu(fileName = "MapGenerationConfig", menuName = "Project Ascendant/Config/Map Generation Config")]
    public class MapGenerationConfigSO : ScriptableObject
    {
        [Header("Topology")]
        [Tooltip("Number of layers, including Layer 0 (entry) and the final Gym layer.")]
        public int LayerCount = 12;

        [Tooltip("Per §7.2 v2 — layer where the map forks into 2 gym lanes. -1 to disable fork (converge to 1 gym).")]
        public int GymForkLayer = 9;

        [Tooltip("Per §7.2 v2 — maximum in-degree (parent connections) per node. Caps fan-in for readability.")]
        public int MaxInDegree = 2;

        // Per §7.2.3 — every node connects to 1..N nodes in the next layer. Keep small for a clean
        // partial net (StS-style); a node usually links to 1–2 nearby children.
        [Tooltip("Maximum forward connections per node. Overridden by DifficultyModifierSO.")]
        public int DefaultMaxBranches = 3;

        [Tooltip("Max re-roll iterations when satisfying the no-adjacent-same-type constraint.")]
        public int ConstraintRetryCap = 8;

        [Header("Per-Layer Structure")]
        // One entry per layer: how many nodes it has + any forced node type. Variable counts give the
        // map shape (e.g. 1 → 3 → 5 → 3 → 2 → 1 → 2 → 1). Forced layers: L0 Wild, an Elite, a Center,
        // the final Gym.
        public List<MapLayerSpec> Layers;

        [Header("Per-Layer Node Weights")]
        // One entry per layer. Weights are relative — not probabilities. Forced types are excluded
        // at sample time.
        public List<NodeLayerWeights> LayerWeights;
    }

    // How a forced node type is distributed within a layer.
    public enum LayerForceMode
    {
        None,           // all nodes weighted-sampled
        AllNodes,       // every node is ForcedType (L0 Wild, final Gym, single-node Center)
        OneNodeInLayer  // exactly one node in the layer is ForcedType (e.g. the Elite)
    }

    [Serializable]
    public struct MapLayerSpec
    {
        [Tooltip("Layer index this spec applies to (0-based).")]
        public int Layer;

        [Tooltip("Number of nodes in this layer.")]
        public int NodesInLayer;

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

        // Per §7.14 (CL-009) — Dojo appears ~1 per region; set a small weight on mid-trunk layers.
        [Tooltip("Dojo weight. Per §7.14 — ~1 per region; typically 0.5–1.0 on 2–3 mid-trunk layers.")]
        public float DojoWeight;
    }
}
