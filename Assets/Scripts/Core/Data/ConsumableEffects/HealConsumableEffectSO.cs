using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.2 — Potion / Super Potion / Hyper Potion / Max Potion healing effect.
    [CreateAssetMenu(fileName = "New Heal Consumable Effect", menuName = "Project Ascendant/Consumable Effects/Heal")]
    public class HealConsumableEffectSO : ConsumableEffectSO
    {
        [Tooltip("Flat HP restored. E.g. 30 for Potion, 60 for Super Potion.")]
        public int FlatHealAmount;

        [Tooltip("If true, restores to full EffectiveMaxHP (Max Potion). Overrides FlatHealAmount.")]
        public bool RestoreToFull;
    }
}
