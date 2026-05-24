using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "RunEndedEventChannel", menuName = "ProjectAscendant/Events/Run Ended")]
    public sealed class RunEndedEventChannelSO : GameEventSO<RunEndedContext> { }

    public sealed class RunEndedEventListener : GameEventListener<RunEndedEventChannelSO, RunEndedContext> { }
}
