using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "EvolutionEventChannel", menuName = "ProjectAscendant/Events/Evolution")]
    public sealed class EvolutionEventChannelSO : GameEventSO<EvolutionContext> { }

    public sealed class EvolutionEventListener : GameEventListener<EvolutionEventChannelSO, EvolutionContext> { }
}
