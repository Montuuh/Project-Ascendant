using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.8 + Epic 11 Task 11.6 — definition SO for a run difficulty modifier. Selected at run start
    // (§6.8.1, default 1 slot) and applied globally for the run. Each modifier multiplies the run's
    // Trainer-XP reward (§6.8.1) and carries one or more mechanical effects (§6.8.2). VS ships 3:
    // Iron Will / Dense Fog / One Path.
    //
    // ⚠ SCHEMA-vs-§6.8.2 RECONCILIATION (Claude Code, 2026-06-01, designer authority): the VS uses the
    // implemented fields below; their semantics differ slightly from §6.8.2 prose and are flagged for
    // systems-designer sign-off:
    //   • Iron Will — §6.8.2 "+20% wild HP"; VS uses EnemyStatMultiplier on enemy Max HP (all combats).
    //   • Dense Fog — §6.8.2 "one Unknown intent per non-boss"; VS uses HideAllEnemyIntents (stronger).
    //   • One Path — §6.8.2 "both Gym branches same type"; VS uses MaxRouteBranchChoices (fewer routes),
    //     as per-branch Gym typing is not modelled in the R1 VS map.
    [CreateAssetMenu(fileName = "New Difficulty Modifier", menuName = "Project Ascendant/World/Difficulty Modifier")]
    public class DifficultyModifierSO : ScriptableObject
    {
        [Header("Identity")]
        public string ModifierId;
        public string DisplayName;
        public Sprite Icon;

        [TextArea(2, 4)]
        public string Description;

        [Header("Reward — §6.8.1")]
        [Tooltip("Multiplier on Trainer XP earned this run. Stacks multiplicatively across modifiers (§6.8.3). 1.0 = none.")]
        public float TrainerXPMultiplier = 1f;

        [Header("Stat Modifiers")]
        [Tooltip("Multiplier on enemy Max HP. 1.0 = default. Iron Will = 1.20 (§6.8.2).")]
        public float EnemyStatMultiplier = 1f;

        [Tooltip("Multiplier on Trauma stacks gained per faint. 1.0 = default. (Trauma Surge family.)")]
        public float TraumaStackMultiplier = 1f;

        [Header("Visibility")]
        [Tooltip("Enemy intents start Unknown (Dense Fog). Not revealable by Keen Eye while active.")]
        public bool HideAllEnemyIntents;

        [Header("Map Restrictions")]
        [Tooltip("Maximum route branches shown at each map junction. One Path = 1.")]
        [Range(1, 3)]
        public int MaxRouteBranchChoices = 3;

        [Tooltip("GDD section for this modifier. Per §9.15.")]
        public string GDDReference;
    }
}
