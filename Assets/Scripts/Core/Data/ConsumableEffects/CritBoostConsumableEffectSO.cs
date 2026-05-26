using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §4.1.3 + §8.2 — temporary per-combat crit-chance boost (e.g. Sharp Lens).
    // Effect duration: the entire current combat encounter; cleared at combat end (§3.5).
    // Stacks additively with the attacker's permanent evolution-branch crit bonus,
    // per §4.1.3 stacking rule. Independent of AlwaysCrit (move-level).
    //
    // The numeric CritChanceBoost value is authored in the inspector by the seeder
    // (not hardcoded here) so the PA0001 balance-literal analyzer stays quiet.
    [CreateAssetMenu(fileName = "New Crit Boost Effect", menuName = "Project Ascendant/Consumable Effects/Crit Boost")]
    public class CritBoostConsumableEffectSO : ConsumableEffectSO
    {
        [Tooltip("Crit-chance bonus added while this consumable's effect is active this combat. " +
                 "Range 0-1 (e.g. 0.25 = +25%). Additive with permanent passive bonus, clamped to [0, 1].")]
        [Range(0f, 1f)]
        public float CritChanceBoost;
    }
}
