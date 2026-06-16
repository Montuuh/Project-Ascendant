using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.2.1 — a single node on the Region map. Pure data; no Unity/view dependency.
    // Content (the concrete encounter/shop/event) is resolved later by the node controllers
    // (Tasks 9.3–9.8); 9.1 only fixes topology + node TYPE.
    public sealed class MapNode
    {
        // Per §7.2.1 — position in the ladder.
        public readonly int Layer;
        // 0 for the single pre-branch lane; 0..LaneCountAfterBranch-1 after the Branch Point.
        public readonly int Lane;
        // Position within (layer, lane), used for the no-adjacent-same-type constraint.
        public readonly int IndexInLane;

        public NodeType NodeType;

        // Per §7.2 v2 — for Gym nodes, which gym from the pool (0-based index). -1 for non-Gym nodes.
        public int GymIndex = -1;

        // Per §7.5.1 (CL-024) — RNG-weighted Elite Trainer occupant (Rival / Giovanni / Specialist).
        // Resolved at map-gen via RegionMapGenerator.ResolveEliteOccupant.
        public EliteTrainerSO EliteTrainerOccupant;

        // Per §7.5.2 (CL-024) — Elite Wild occupant (Snorlax OR Marowak's Spirit in R1).
        // Resolved at map-gen for the seeded EliteWild node (≤1/Region, not guaranteed).
        public EliteWildSO EliteWildOccupant;

        // Per §7.2 v2 — transient generation flag: this node's type was force-stamped (Wild entry,
        // Elite, Center, Gym) and must not be rerolled by the no-adjacent-same-type pass. Not persisted.
        public bool Forced;

        // Per §7.2.3 — forward connections into the next layer (1..DefaultMaxBranches).
        public readonly List<MapNode> Next = new();

        public MapNode(int layer, int lane, int indexInLane, NodeType nodeType)
        {
            Layer       = layer;
            Lane        = lane;
            IndexInLane = indexInLane;
            NodeType    = nodeType;
        }

        public override string ToString() => $"L{Layer}/Lane{Lane}#{IndexInLane}:{NodeType}";
    }

    // Per §7.2 v2 — a generated Region map: a 12-layer branching tree with gym fork.
    // Deterministic for a given (RunSeed, RegionIndex). Pure data — owned by RunState, never by a view.
    public sealed class RegionMap
    {
        public readonly int RegionIndex;

        // Layers[layer] = the nodes in that layer, ordered (lane, indexInLane).
        public readonly List<List<MapNode>> Layers;

        // Per §7.2 v2 — the 2 gyms chosen from the pool for this map. Index matches MapNode.GymIndex.
        public readonly List<GymLeaderSO> ChosenGyms;

        public RegionMap(int regionIndex, List<List<MapNode>> layers, List<GymLeaderSO> chosenGyms = null)
        {
            RegionIndex = regionIndex;
            Layers      = layers;
            ChosenGyms  = chosenGyms ?? new List<GymLeaderSO>();
        }

        public int LayerCount => Layers.Count;

        // Per §7.2 v2 — L0 entry nodes (multiple possible starting points).
        public IReadOnlyList<MapNode> EntryNodes => Layers[0];

        // Per §7.2 v2 — the Gym nodes (Layer LayerCount-1), one per lane (2 total after fork).
        public IReadOnlyList<MapNode> GymNodes => Layers[LayerCount - 1];

        public IEnumerable<MapNode> AllNodes()
        {
            for (int l = 0; l < Layers.Count; l++)
                for (int n = 0; n < Layers[l].Count; n++)
                    yield return Layers[l][n];
        }

        // Returns the nodes of a layer that belong to a given lane, in IndexInLane order.
        public List<MapNode> LaneNodes(int layer, int lane)
        {
            List<MapNode> result = new();
            List<MapNode> nodes = Layers[layer];
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Lane == lane)
                    result.Add(nodes[i]);
            return result;
        }
    }
}
