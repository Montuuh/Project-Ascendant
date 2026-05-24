using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "NodeEnteredEventChannel", menuName = "ProjectAscendant/Events/Node Entered")]
    public sealed class NodeEnteredEventChannelSO : GameEventSO<NodeEnteredContext> { }

    public sealed class NodeEnteredEventListener : GameEventListener<NodeEnteredEventChannelSO, NodeEnteredContext> { }
}
