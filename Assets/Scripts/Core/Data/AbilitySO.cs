using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.1 — Definition SO for a passive ability. Immutable at runtime.
    // TODO: Epic 3 — expand with effect hooks, trigger conditions, Lead Aura flag (§5.4).
    [CreateAssetMenu(fileName = "New Ability", menuName = "ProjectAscendant/Data/Ability")]
    public class AbilitySO : ScriptableObject
    {
        public string AbilityId;
        public string DisplayName;
    }
}
