using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.4 — runtime run state. Serialized to run-current.dat between nodes (§9.8).
    // TODO: Epic 3 — full schema: Box, ActiveTeamIndices, LeadIndex, HeldRelics, Inventory,
    //       PokeDollars, EarnedBadges, ActiveRegionModifiers, ActiveBoon, EventFlags, RecordedInputs.
    [CreateAssetMenu(menuName = "ProjectAscendant/RunState")]
    public class RunStateSO : ScriptableObject
    {
        // Per §9.3.2.4 — run seed for deterministic replay (§9.7.4).
        public int RunSeed;
    }
}
