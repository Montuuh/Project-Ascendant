using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "FaintEventChannel", menuName = "ProjectAscendant/Events/Faint")]
    public sealed class FaintEventChannelSO : GameEventSO<FaintContext> { }

    public sealed class FaintEventListener : GameEventListener<FaintEventChannelSO, FaintContext> { }
}
