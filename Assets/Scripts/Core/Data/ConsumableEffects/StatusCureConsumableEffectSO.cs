using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.3 — cures a specific status condition (Antidote, Burn Heal, etc.)
    // or all statuses (Full Heal).
    [CreateAssetMenu(fileName = "New Status Cure Effect", menuName = "Project Ascendant/Consumable Effects/Status Cure")]
    public class StatusCureConsumableEffectSO : ConsumableEffectSO
    {
        [Tooltip("The specific status this item cures. Ignored if CureAll is true.")]
        public StatusCondition CuresStatus;

        [Tooltip("If true, cures any primary status + Confusion (Full Heal behaviour).")]
        public bool CureAll;
    }
}
