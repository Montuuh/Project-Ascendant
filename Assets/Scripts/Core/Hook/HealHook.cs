using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — adds HP to EventContext.HealAmount; combat system applies the heal to Target.
    // Example use: Berry Pouch relic (+20% to healing consumables), Leftovers Held Item.
    [CreateAssetMenu(menuName = "ProjectAscendant/Hooks/Heal")]
    public sealed class HealHook : ScriptableHook
    {
        [SerializeField] private int _amount;

        public int Amount { get => _amount; set => _amount = value; }

        public override void OnFire(EventContext context)
        {
            context.HealAmount += _amount;
        }
    }
}
