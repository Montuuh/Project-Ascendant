using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.2 / §10.2 + Epic 9 Task 9.2.3 — Map View presentation data per node type
    // (icon, localised label, accent colour key). Read-only data for the view layer (UI rules:
    // UI reads SOs, never owns state; all display text is localisation-key-bound).
    // Sprite refs stay null until placeholder node icons are authored at Task 9.9.
    [CreateAssetMenu(fileName = "NodePresentationConfig",
        menuName = "Project Ascendant/Config/Node Presentation Config")]
    public class NodePresentationConfigSO : ScriptableObject
    {
        // One entry per NodeType the map can surface (Wild/Trainer/Center/Shop/Mystery/Gym/Elite).
        public List<NodePresentationEntry> Entries;

        // Returns the presentation entry for a node type, or false if unmapped.
        public bool TryGet(NodeType type, out NodePresentationEntry entry)
        {
            if (Entries != null)
                for (int i = 0; i < Entries.Count; i++)
                    if (Entries[i].NodeType == type)
                    {
                        entry = Entries[i];
                        return true;
                    }
            entry = default;
            return false;
        }
    }

    [Serializable]
    public struct NodePresentationEntry
    {
        public NodeType NodeType;

        [Tooltip("Localisation key for the node's display name (§10.6 — no hardcoded display text).")]
        public string DisplayNameKey;

        [Tooltip("Map View icon. Null until placeholder art is authored at Task 9.9.")]
        public Sprite Icon;

        [Tooltip("Palette key for the node's accent colour (§10.1.3 type/UI palette).")]
        public string AccentColorKey;
    }
}
