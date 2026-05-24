using UnityEngine;

namespace ProjectAscendant.Core
{
    [CreateAssetMenu(fileName = "DamageEventChannel", menuName = "ProjectAscendant/Events/Damage")]
    public sealed class DamageEventChannelSO : GameEventSO<DamageContext> { }

    public sealed class DamageEventListener : GameEventListener<DamageEventChannelSO, DamageContext> { }
}
