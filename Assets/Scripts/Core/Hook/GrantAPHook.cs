using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — adds AP to EventContext.APGranted; combat system deposits to player's AP pool.
    // Example use: Cycle Cell relic (+1 AP on deck reshuffle), Move Echo chain reward.
    [CreateAssetMenu(menuName = "ProjectAscendant/Hooks/GrantAP")]
    public sealed class GrantAPHook : ScriptableHook
    {
        [SerializeField] private int _amount;

        public int Amount { get => _amount; set => _amount = value; }

        public override void OnFire(EventContext context)
        {
            context.APGranted += _amount;
        }
    }
}
