using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per Epic 3.1.6 — heals a target Pokémon as part of a move.
    // Target is Source if HealSelf is true, otherwise Target.
    [CreateAssetMenu(fileName = "New Heal Effect", menuName = "Project Ascendant/Move Effects/Heal Effect")]
    public class HealEffectSO : MoveEffectSO
    {
        [Tooltip("Flat HP restored.")]
        public int FlatHealAmount;

        [Tooltip("0 = unused. E.g. 0.5 = restore 50% of EffectiveMaxHP. Applied as floor().")]
        public float PercentageOfMaxHP;

        [Tooltip("If true, heals the user; if false, heals the target slot.")]
        public bool HealSelf = true;

        [Tooltip("Number of turns the regen persists. 0 = instant, 1+ = ongoing regen.")]
        public int DurationTurns;
    }
}
