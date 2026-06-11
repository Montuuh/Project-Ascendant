using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §5.2 (Topic 5) + Epic 10 Task 10.1 — per-Pokémon XP + leveling tuning. Distinct from
    // meta-progression Trainer XP (Topic 6, credited at run end). All values are data-driven so the
    // systems-designer can recalibrate without code changes.
    //
    // ⚠ INTERIM VALUES (BACKLOG gap #41) — seeded to make XP/leveling visibly work in the VS; the
    // real XP-per-tier amounts + level curve are a systems-designer calibration pass.
    [CreateAssetMenu(fileName = "ProgressionConfig", menuName = "Project Ascendant/Config/Progression Config")]
    public sealed class ProgressionConfigSO : ScriptableObject
    {
        [Header("§5.2.1 — XP per defeated-encounter tier (awarded to the whole Active Team)")]
        public int WildXP = 30;
        public int TrainerXP = 45;
        public int EliteXP = 80;
        public int GymXP = 140;

        [Header("§5.2.2 — XP cost to advance FROM `level` to `level+1` = Base + (level-1)·Slope")]
        public int LevelUpBaseXP = 12;
        public int LevelUpSlopeXP = 4;

        [Header("§5.2.4 — single-stage species growth bonus (post-VS; no single-stage species in the VS roster)")]
        public int SingleStageGrowthBonusPercent = 25;

        [Header("§5.12.5 (CL-010) — Box XP share: every benched Pokémon earns this fraction of Active XP")]
        public float BenchXpShare = 0.75f;           // baseline — all Box (non-active) mons earn 75% of Active XP

        [Header("§8.3.3 — XP-economy relic multipliers (Epic 12)")]
        public float LuckyEggXPMultiplier = 1.15f;  // Lucky Egg Token — all in-run XP ×1.15
        public float LivingLegendXPMultiplier = 1.3f; // §8.3.7 (CL-021) Living Legend Legendary — XP ×1.3
        public float ExpShareBoxFraction = 1.0f;     // Exp Share (CL-010) — lifts benched Pokémon to 100% of Active XP

        // XP required to advance from `level` to the next level. Monotonic in level.
        public int XPToNext(int level) => LevelUpBaseXP + Mathf.Max(0, level - 1) * LevelUpSlopeXP;

        // §5.2.1 — XP granted for clearing a node of the given type. Non-combat nodes grant 0.
        public int XPForNode(NodeType node) => node switch
        {
            NodeType.Wild => WildXP,
            NodeType.Trainer => TrainerXP,
            NodeType.Elite => EliteXP,
            NodeType.Gym => GymXP,
            _ => 0,
        };
    }
}
