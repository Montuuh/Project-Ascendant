using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.4.1.1 — GDD specifies GameEventSO<RelicSO>.
    // TODO: Epic 3 — update payload to RelicSO once data layer is defined.
    [CreateAssetMenu(fileName = "RelicAcquiredEventChannel", menuName = "ProjectAscendant/Events/Relic Acquired")]
    public sealed class RelicAcquiredEventChannelSO : GameEventSO<RelicAcquiredContext> { }

    public sealed class RelicAcquiredEventListener : GameEventListener<RelicAcquiredEventChannelSO, RelicAcquiredContext> { }
}
