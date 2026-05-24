using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — applies a flat damage bonus and/or a damage multiplier to EventContext.
    // Example use: Soft Sand relic, type-boost Held Item effects.
    [CreateAssetMenu(menuName = "ProjectAscendant/Hooks/ModifyDamage")]
    public sealed class ModifyDamageHook : ScriptableHook
    {
        [SerializeField] private int   _flatBonus;
        [SerializeField] private float _multiplier = 1f;

        public int   FlatBonus   { get => _flatBonus;   set => _flatBonus   = value; }
        public float Multiplier  { get => _multiplier;  set => _multiplier  = value; }

        public override void OnFire(EventContext context)
        {
            context.DamageMultiplier  *= _multiplier;
            context.FlatDamageBonus  += _flatBonus;
        }
    }
}
