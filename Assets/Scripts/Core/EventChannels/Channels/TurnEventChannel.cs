using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "TurnEventChannel", menuName = "ProjectAscendant/Events/Turn")]
    public sealed class TurnEventChannelSO : GameEventSO<TurnContext> { }

    public sealed class TurnEventListener : GameEventListener<TurnEventChannelSO, TurnContext> { }
}
