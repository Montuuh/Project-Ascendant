using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 + §8.2 — Definition SO for a consumable item.
    // Consumables restore at combat end (§3.5) — they are NOT expendable.
    // Exception: Revive has 1 charge per inventory copy (§8.2.2).
    // Upgrade chains (e.g. Potion → Super Potion) use the UpgradeTo reference.
    [CreateAssetMenu(fileName = "New Consumable", menuName = "Project Ascendant/Data/Consumable")]
    public class ConsumableSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        public Sprite Icon;

        [Header("Combat Use")]
        [Range(0, 4)]
        public int APCost;

        // Per §8.7 — polymorphic effect; concrete subclass determines behaviour.
        public ConsumableEffectSO Effect;

        [Header("Upgrade Chain")]
        [Tooltip("Tier within the upgrade chain. 1 = base.")]
        public int Tier = 1;

        [Tooltip("Higher-tier replacement when upgraded at City Shop. Null if no upgrade.")]
        public ConsumableSO UpgradeTo;

        [Tooltip("GDD section for this item. Per §9.15.")]
        public string GDDReference;
    }
}
