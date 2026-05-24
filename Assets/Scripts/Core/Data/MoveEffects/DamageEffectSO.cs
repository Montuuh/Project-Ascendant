using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.6 — secondary damage effect (not the primary BasePower hit).
    // Use for: fixed bonus damage, percentage-of-HP secondary hits, recoil.
    [CreateAssetMenu(fileName = "New Damage Effect", menuName = "Project Ascendant/Move Effects/Damage Effect")]
    public class DamageEffectSO : MoveEffectSO
    {
        [Tooltip("Flat bonus damage added after primary damage.")]
        public int FlatBonusDamage;

        [Tooltip("0 = unused. E.g. 0.25 = deal 25% of target's current HP as bonus damage.")]
        public float PercentageOfCurrentHP;

        [Tooltip("If true, effect applies to the attacker (recoil) instead of the target.")]
        public bool IsRecoil;
    }
}
