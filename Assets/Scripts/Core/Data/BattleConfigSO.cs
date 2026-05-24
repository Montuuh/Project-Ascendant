using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §4.1.1 + Epic 3.1.18 — global damage formula constants.
    // Damage formula: floor( Power * (Atk/Def) * TypeMult * STAB * RangeMod * Crit / Divisor )
    // All balance values live here — NEVER hardcode in combat scripts (§9.1 pillar 1).
    [CreateAssetMenu(fileName = "BattleConfig", menuName = "Project Ascendant/Config/Battle Config")]
    public class BattleConfigSO : ScriptableObject
    {
        [Header("Damage Formula — §4.1.1")]
        [Tooltip("Divisor in the damage formula. Placeholder 50; tuned in playtest per §3.3.22.")]
        public int Divisor = 50;

        [Tooltip("STAB (Same-Type Attack Bonus) multiplier. Per §4.1.")]
        public float StabMultiplier = 1.5f;

        [Tooltip("Critical hit damage multiplier.")]
        public float CritMultiplier = 1.5f;

        [Header("Range Modifiers — §9.3.2.2")]
        [Tooltip("Damage multiplier for Ranged moves.")]
        public float RangedModifier = 0.75f;

        [Tooltip("Damage multiplier for Melee moves.")]
        public float MeleeModifier = 1.0f;

        [Header("Stat Stages — §4.X")]
        // Per §4.X stat stage table: 13 entries mapping stage offset (-6 to +6) to multiplier.
        // Index 0 = stage -6, index 6 = stage 0 (1.0x), index 12 = stage +6.
        // Default Gen-style curve: -6=0.25, -5=0.29, -4=0.33, -3=0.40, -2=0.50, -1=0.67,
        //                           0=1.00, +1=1.50, +2=2.00, +3=2.50, +4=3.00, +5=3.50, +6=4.00
        public float[] StatStageMultipliers = new float[]
        {
            0.25f, 0.29f, 0.33f, 0.40f, 0.50f, 0.67f,
            1.00f,
            1.50f, 2.00f, 2.50f, 3.00f, 3.50f, 4.00f
        };

        [Header("AP Economy")]
        [Tooltip("Base AP available per player turn.")]
        public int BaseAPPerTurn = 3;

        [Tooltip("Maximum AP that can be carried into a turn (from relics/effects).")]
        public int MaxAPPerTurn = 6;

        [Header("Hand Size")]
        [Tooltip("Base skill cards drawn per turn.")]
        public int BaseSkillCardsPerTurn = 4;

        [Tooltip("Base consumable cards drawn per turn.")]
        public int BaseConsumableCardsPerTurn = 2;
    }
}
