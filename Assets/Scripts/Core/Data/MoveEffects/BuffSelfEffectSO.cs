using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.6 — raises the user's stat by a number of stages when the move is played.
    // Per §4.X stat stage system: stages range −6 to +6.
    [CreateAssetMenu(fileName = "New Buff Self Effect", menuName = "Project Ascendant/Move Effects/Buff Self Effect")]
    public class BuffSelfEffectSO : MoveEffectSO
    {
        public Stat TargetStat;

        [Tooltip("Positive = buff. Typical values: +1 or +2.")]
        public int StageChange = 1;
    }
}
