using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.4 — grants AP to the player's pool this turn (Ether).
    [CreateAssetMenu(fileName = "New AP Grant Effect", menuName = "Project Ascendant/Consumable Effects/AP Grant")]
    public class APGrantConsumableEffectSO : ConsumableEffectSO
    {
        [Tooltip("AP added to the player's current-turn pool.")]
        public int APGranted = 2;
    }
}
