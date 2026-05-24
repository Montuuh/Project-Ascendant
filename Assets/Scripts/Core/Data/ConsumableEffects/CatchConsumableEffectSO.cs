using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.5 + §7.3.4 — Pokéball catch mechanic effect.
    // Catch succeeds when target HP < CatchThresholdPercent OR target has a status condition.
    [CreateAssetMenu(fileName = "New Catch Effect", menuName = "Project Ascendant/Consumable Effects/Catch")]
    public class CatchConsumableEffectSO : ConsumableEffectSO
    {
        [Range(0f, 1f)]
        [Tooltip("Catch succeeds if target HP/MaxHP < this value. E.g. 0.5 = below 50%.")]
        public float CatchThresholdPercent = 0.5f;

        [Tooltip("If true, any primary status condition also guarantees catch regardless of HP.")]
        public bool CatchWithAnyStatus = true;
    }
}
