using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §3.1.12 + §9.3.2.4 — stub SO for league boons (meta-unlocked passive benefits).
    // Full implementation deferred to post-VS per Epic 3.1.12.
    // Included as stub so RunStateSO.ActiveBoon compiles.
    [CreateAssetMenu(fileName = "New League Boon", menuName = "Project Ascendant/Data/League Boon (stub)")]
    public class LeagueBoonSO : ScriptableObject
    {
        public string BoonId;
        public string DisplayName;
        // TODO: Post-VS — full effect schema.
    }
}
