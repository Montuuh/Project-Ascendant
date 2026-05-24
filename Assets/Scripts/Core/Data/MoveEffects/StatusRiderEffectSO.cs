using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.6 — applies a status condition as a rider on a move.
    // ApplicationChance: deterministic per §9.7.3 (seeded CombatRNG).
    [CreateAssetMenu(fileName = "New Status Rider Effect", menuName = "Project Ascendant/Move Effects/Status Rider Effect")]
    public class StatusRiderEffectSO : MoveEffectSO
    {
        public StatusCondition StatusToApply;

        [Range(0f, 1f)]
        [Tooltip("Probability of application. 1.0 = always. Rolled via seeded CombatRNG.")]
        public float ApplicationChance = 1f;

        [Tooltip("If true, the status targets the attacker (self-inflict, e.g. Confusion on recoil).")]
        public bool ApplyToSelf;
    }
}
