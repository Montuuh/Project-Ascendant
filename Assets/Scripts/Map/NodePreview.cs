using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.2.3 / Task 9.2.3 — view-agnostic preview of a map node for the Map View (Task 9.9).
    // Pillar 1 (telegraphed tactics): the node TYPE is always visible up-front. DetailKey carries
    // an optional extra telegraph (e.g. a Mystery node's risk-profile key); null when none.
    // All text is a localisation KEY, never display text (UI rules).
    public readonly struct NodePreview
    {
        public readonly NodeType NodeType;
        public readonly int Layer;
        public readonly int Lane;
        public readonly string DisplayNameKey;
        public readonly string DetailKey; // optional; may be null

        public NodePreview(NodeType nodeType, int layer, int lane, string displayNameKey, string detailKey)
        {
            NodeType       = nodeType;
            Layer          = layer;
            Lane           = lane;
            DisplayNameKey = displayNameKey;
            DetailKey      = detailKey;
        }
    }
}
