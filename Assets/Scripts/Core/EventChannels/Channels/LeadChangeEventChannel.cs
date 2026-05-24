using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "LeadChangeEventChannel", menuName = "ProjectAscendant/Events/Lead Change")]
    public sealed class LeadChangeEventChannelSO : GameEventSO<LeadChangeContext> { }

    public sealed class LeadChangeEventListener : GameEventListener<LeadChangeEventChannelSO, LeadChangeContext> { }
}
