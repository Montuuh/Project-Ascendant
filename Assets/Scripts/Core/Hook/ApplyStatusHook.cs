using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — sets EventContext.StatusToApply; combat system applies the status to Target.
    // Example use: Wide Lens relic (status rider application), status-applying abilities.
    [CreateAssetMenu(menuName = "ProjectAscendant/Hooks/ApplyStatus")]
    public sealed class ApplyStatusHook : ScriptableHook
    {
        [SerializeField] private StatusCondition _status;

        public StatusCondition Status { get => _status; set => _status = value; }

        public override void OnFire(EventContext context)
        {
            context.StatusToApply = _status;
        }
    }
}
