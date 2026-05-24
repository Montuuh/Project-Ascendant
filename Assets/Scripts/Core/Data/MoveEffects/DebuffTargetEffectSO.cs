using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.6 — lowers the target's stat by a number of stages.
    // Per §4.X stat stage system: stages range −6 to +6.
    [CreateAssetMenu(fileName = "New Debuff Target Effect", menuName = "Project Ascendant/Move Effects/Debuff Target Effect")]
    public class DebuffTargetEffectSO : MoveEffectSO
    {
        public Stat TargetStat;

        [Tooltip("Negative = debuff. Typical values: −1 or −2.")]
        public int StageChange = -1;
    }
}
