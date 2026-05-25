using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §3.1.12 + §9.3.2.4 — stub SO for region-wide environmental modifiers.
    // Full implementation deferred to post-VS per Epic 3.1.12.
    // Included as stub so RunStateSO.ActiveRegionModifiers compiles.
    [CreateAssetMenu(fileName = "New Region Modifier", menuName = "Project Ascendant/World/Region Modifier (stub)")]
    public class RegionModifierSO : ScriptableObject
    {
        public string ModifierId;
        public string DisplayName;
        // TODO: Post-VS — full effect schema and HookBinding wiring.
    }
}
