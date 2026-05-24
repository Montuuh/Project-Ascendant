using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.2.4 — boosts a stat stage for the target Pokémon this combat (X Attack, X Defense).
    [CreateAssetMenu(fileName = "New Stat Boost Effect", menuName = "Project Ascendant/Consumable Effects/Stat Boost")]
    public class StatBoostConsumableEffectSO : ConsumableEffectSO
    {
        public Stat TargetStat;

        [Tooltip("Positive stage delta. Typical: +1.")]
        public int StageChange = 1;
    }
}
