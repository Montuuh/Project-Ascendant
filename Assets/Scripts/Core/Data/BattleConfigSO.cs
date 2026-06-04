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
        // Gentle ±10%/stage curve (playtest 2026-06-04 — Gen-style was too swingy): a stage is a nudge,
        // not a shutdown. -6=0.40 … -1=0.90, 0=1.00, +1=1.10 … +6=1.60.
        public float[] StatStageMultipliers = new float[]
        {
            0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f,
            1.00f,
            1.10f, 1.20f, 1.30f, 1.40f, 1.50f, 1.60f
        };

        [Header("AP Economy")]
        [Tooltip("Base AP available per player turn.")]
        public int BaseAPPerTurn = 3;

        [Tooltip("Maximum AP that can be carried into a turn (from relics/effects).")]
        public int MaxAPPerTurn = 6;

        [Tooltip("Per §3.2.6 (OPEN) — bonus AP granted when reinforcements arrive mid-combat (the 'Breather' beat). Vertical Slice default: 1.")]
        public int BreatherBonusAP = 1;

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

        // ── Boss / Elite Phase Structure — §4.4.3 + Epic 8 Tasks 8.4 / 8.5 ────
        // HP-fraction thresholds at which a multi-phase boss escalates. These
        // are universal across all boss-tier Pokémon (§4.4.3 standard template);
        // a Pokémon participates only if its PokemonInstance.PhaseCount > 1.
        [Header("Boss Phase Structure — §4.4.3")]

        [Range(0f, 1f)]
        [Tooltip("HP fraction at/under which a 2+ phase boss enters Phase 2 " +
                 "(aggressive). Per §4.4.3 standard two-phase template.")]
        public float BossPhase2HPThreshold = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("HP fraction at/under which a 3-phase ace enters Phase 3 " +
                 "(last-stand). Per §4.4.3 three-phase template. Consumed by " +
                 "the Gym ace in Task 8.5; harmless for 2-phase Elites.")]
        public float BossPhase3HPThreshold = 0.2f;

        [Tooltip("Score multiplier applied to a boss's offensive intents while " +
                 "in an aggressive phase (Phase 2+). Per §4.4.3 — 'plays " +
                 "urgently and aggressively.' Mirrors AggressiveSelfMultiplier.")]
        public float BossPhaseAggressionMultiplier = 1.5f;

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

        // ── Lead Aura — §5.5.4 + Epic 6 Task 6.6 ──────────────────────────────
        [Header("Lead Aura — §5.5.4")]

        [Tooltip("Damage bonus added per Lead Aura source whose type matches " +
                 "the attacker's move type. Sources stack additively (e.g. " +
                 "Ability + Held Item both granting Fire aura → +0.10). " +
                 "Per §5.5.4 + Epic 6 Task 6.6.")]
        public float LeadAuraMatchingTypeBonus = 0.05f;

        // Per §5.5.3.4 / §5.8 — Overgrow / Blaze / Torrent: while this Pokémon's HP is below the
        // threshold, its matching-type moves deal ×Multiplier damage.
        public float AbilityLowHpBoostMultiplier = 1.2f;   // +20%
        public float AbilityLowHpThreshold = 0.30f;        // when HP < 30%

        // Per §8.3.3 + Epic 12 Task 12.4 — Trainer Relic combat multipliers (RelicResolver).
        public float BraveCharmDamageMultiplier = 1.10f;   // Brave Charm — HP < 50% → +10% damage
        public float SootheBellDamageMultiplier = 1.05f;   // Soothe Bell — at full HP → +5% damage
        public float BerryPouchHealMultiplier = 1.20f;     // Berry Pouch — healing consumables +20%
        public float SmokeBallDamageMultiplier = 0.80f;    // Smoke Ball — first enemy attack −20% (VS: per-combat)
        public int MoveEchoMoveThreshold = 3;              // Move Echo — distinct moves from one mon in a turn
        public int MoveEchoBonusAP = 2;                    // Move Echo — AP granted next turn

        // Per §5.5.3 / §5.8 — Shell Armor: flat incoming-damage reduction while this Pokémon is Lead
        // (stacks with the Boulder Badge reduction, §4.4.5.1).
        public int ShellArmorFlatReduction = 2;

        // Per §5.8 — Static: chance to Paralyse the target when dealing damage with an Electric move.
        public int StaticParalysisChancePercent = 25;

        // Per §5.5.3 / §5.8 — Swift Swim: extra skill cards drawn on turn 1 of a Rain-active combat.
        public int SwiftSwimDrawBonus = 1;
    }
}
