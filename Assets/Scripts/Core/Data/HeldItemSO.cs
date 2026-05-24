using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.3 — Definition SO for a Held Item. Immutable at runtime.
    // TODO: Epic 3 — expand with full schema from Topic 8 §8.X (effect hooks, equip slot rules).
    [CreateAssetMenu(fileName = "New Held Item", menuName = "ProjectAscendant/Data/Held Item")]
    public class HeldItemSO : ScriptableObject
    {
        public string ItemId;
        public string DisplayName;
    }
}
