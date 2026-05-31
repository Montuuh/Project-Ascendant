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
        public int WildXP = 8;
        public int TrainerXP = 14;
        public int EliteXP = 30;
        public int GymXP = 60;

        [Header("§5.2.2 — XP cost to advance FROM `level` to `level+1` = Base + (level-1)·Slope")]
        public int LevelUpBaseXP = 18;
        public int LevelUpSlopeXP = 6;

        [Header("§5.2.4 — single-stage species growth bonus (post-VS; no single-stage species in the VS roster)")]
        public int SingleStageGrowthBonusPercent = 25;

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
