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

        // ── Status Conditions — §4.2 ──────────────────────────────────────────
        // Per §4.2.2 — Burn/Poison are permanent; durations below are for
        // non-permanent statuses. Multiplier values follow the spec exactly.
        [Header("Status Conditions — §4.2")]

        [Tooltip("Burn DoT = floor(MaxHP / divisor), minimum 1. Per §4.2.2.1.")]
        public int BurnDoTDivisor = 16;

        [Tooltip("Burn Attack multiplier. Per §4.2.2.1 — Attack reduced 25%.")]
        public float BurnAttackMultiplier = 0.75f;

        [Tooltip("Poison DoT = floor(MaxHP / divisor), minimum 1. Per §4.2.2.2.")]
        public int PoisonDoTDivisor = 16;

        [Tooltip("Poison Defense multiplier. Per §4.2.2.2 — Defense reduced 15%.")]
        public float PoisonDefenseMultiplier = 0.85f;

        [Tooltip("Per §4.2.2.3 — Paralysis adds this AP cost to every move " +
                 "belonging to the paralyzed Pokémon.")]
        public int ParalysisAPCostBonus = 1;

        [Tooltip("Paralysis duration in turns. Per §4.2.2.3.")]
        public int ParalysisDuration = 3;

        [Tooltip("Sleep duration in turns. Per §4.2.2.4 — exactly 1 turn after application.")]
        public int SleepDuration = 1;

        [Tooltip("Freeze duration in turns. Per §4.2.2.5.")]
        public int FreezeDuration = 1;

        [Tooltip("Incoming-Fire-damage multiplier on Frozen targets (thaw window). " +
                 "Per §4.2.2.5.")]
        public float FreezeFireDamageMultiplier = 1.5f;

        [Tooltip("Confusion duration in turns, tracked per Pokémon. Per §4.2.3.1.")]
        public int ConfusionDuration = 3;

        // ── AI Intent Scoring — §4.3.3 + Epic 4 Task 4.7 ──────────────────────
        // All multipliers and thresholds live here so they're tunable without
        // touching code. Derivation: BaseWeight defaults to MoveSO.BasePower
        // when > 0, else DefaultUtilityWeight (covers Buff/Stall/Status moves).
        [Header("AI Intent Scoring — §4.3.3")]

        [Tooltip("Fallback BaseWeight when a move has BasePower == 0. Per §4.3.3.")]
        public int DefaultUtilityWeight = 50;

        [Tooltip("Score multiplier applied to Attack intents when the target Pokémon's " +
                 "HP fraction is below LowTargetHPThreshold. Per §4.3.3 HPStateModifier.")]
        public float LowTargetHPMultiplier = 2.0f;

        [Range(0f, 1f)]
        [Tooltip("HP fraction below which target counts as wounded. Per §4.3.3.")]
        public float LowTargetHPThreshold = 0.30f;

        [Tooltip("Score multiplier applied to aggressive intents when the attacker's " +
                 "own HP fraction is below LowSelfHPThreshold. Per §4.3.3.")]
        public float AggressiveSelfMultiplier = 1.5f;

        [Range(0f, 1f)]
        [Tooltip("Attacker's HP fraction below which aggression bonus applies. Per §4.3.3.")]
        public float LowSelfHPThreshold = 0.40f;

        [Tooltip("Score multiplier applied to setup intents (Buff/Stall) when the " +
                 "attacker's own HP fraction is above HighSelfHPThreshold. Per §4.3.3.")]
        public float SetupSelfMultiplier = 1.5f;

        [Range(0f, 1f)]
        [Tooltip("Attacker's HP fraction above which setup bonus applies. Per §4.3.3.")]
        public float HighSelfHPThreshold = 0.70f;

        [Range(0f, 1f)]
        [Tooltip("Probability per turn that the AI selects a non-top-scored intent. " +
                 "Per §4.3.3 — '10–15% chance'. Disabled when BossCounterIntel active.")]
        public float RandomnessFloorChance = 0.125f;

        [Range(0f, 1f)]
        [Tooltip("Score penalty applied to the top intent when Boss Counter-Intel is " +
                 "active (full intent pool revealed). Per §4.3.5 + Epic 4.7.7.")]
        public float BossCounterIntelTopPenalty = 0.7f;

        // ── Field Effects — §4.3.8 + Epic 4 Task 4.9 ──────────────────────────
        // Weather and Terrain are independent (§4.8.2). Multipliers stack
        // multiplicatively across categories.
        [Header("Field Effects — §4.3.8")]

        [Tooltip("Sunny Day Fire-damage multiplier. Per §4.3.8.1.")]
        public float SunnyDayFireMultiplier = 1.5f;

        [Tooltip("Sunny Day Water-damage multiplier. Per §4.3.8.1.")]
        public float SunnyDayWaterMultiplier = 0.5f;

        [Tooltip("Rain Dance Water-damage multiplier. Per §4.3.8.2.")]
        public float RainDanceWaterMultiplier = 1.5f;

        [Tooltip("Rain Dance Fire-damage multiplier. Per §4.3.8.2.")]
        public float RainDanceFireMultiplier = 0.5f;

        [Tooltip("Electric Terrain Electric-damage multiplier on grounded targets. " +
                 "Per §4.3.8.3.")]
        public float ElectricTerrainElectricMultiplier = 1.3f;
    }
}
