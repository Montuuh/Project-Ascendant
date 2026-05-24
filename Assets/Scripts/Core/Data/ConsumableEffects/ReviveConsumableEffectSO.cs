using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.2 — Revive: restores a fainted Pokémon to a % of EffectiveMaxHP.
    // Per §2.4.3 exception: the ONLY consumable that grants in-combat revival.
    [CreateAssetMenu(fileName = "New Revive Effect", menuName = "Project Ascendant/Consumable Effects/Revive")]
    public class ReviveConsumableEffectSO : ConsumableEffectSO
    {
        [Range(0f, 1f)]
        [Tooltip("HP percentage restored on revival. Default 0.5 = 50% Effective Max HP.")]
        public float RevivePercentage = 0.5f;
    }
}
