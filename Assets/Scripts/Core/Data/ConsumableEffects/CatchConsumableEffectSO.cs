using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.5 + §7.3.4 (CL-014) — Pokéball catch mechanic effect (deterministic Catchability Gauge).
    // The catch threshold (in HP fraction) is the ball tier's base; a status condition on the wild adds
    // StatusCatchBonusPercent. Catch succeeds when the wild's HP fraction ≤ the resulting threshold
    // (gauge = 100). See WildCatchResolver for the gauge math.
    [CreateAssetMenu(fileName = "New Catch Effect", menuName = "Project Ascendant/Consumable Effects/Catch")]
    public class CatchConsumableEffectSO : ConsumableEffectSO
    {
        [Range(0f, 1f)]
        [Tooltip("Base catch threshold (ball tier). Catchable at HP fraction ≤ this. §7.3.4 (CL-014): basic 0.30, Great 0.45, Ultra 0.60.")]
        public float CatchThresholdPercent = 0.30f;

        [Range(0f, 1f)]
        [Tooltip("Added to the threshold when the wild has any status condition. §7.3.4 (CL-014): 0.20 (+20pt window).")]
        public float StatusCatchBonusPercent = 0.20f;
    }
}
