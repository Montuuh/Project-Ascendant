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

    // Per §7.2.1 / §7.11 — a generated Region map: an 8-layer branching ladder.
    // Deterministic for a given (RunSeed, RegionIndex). Pure data — owned by RunState, never by a view.
    public sealed class RegionMap
    {
        public readonly int RegionIndex;

        // Layers[layer] = the nodes in that layer, ordered (lane, indexInLane).
        public readonly List<List<MapNode>> Layers;

        public RegionMap(int regionIndex, List<List<MapNode>> layers)
        {
            RegionIndex = regionIndex;
            Layers      = layers;
        }

        public int LayerCount => Layers.Count;

        // The single forced Starter node (Layer 0). Per §7.2.1 always a Wild Pokémon Area.
        public MapNode Entry => Layers[0][0];

        // Per §7.2.1 — the Gym nodes (Layer LayerCount-1), one per lane.
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
