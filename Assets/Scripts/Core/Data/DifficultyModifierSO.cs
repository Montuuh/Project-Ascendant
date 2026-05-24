using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.8 + §3.1.17 — Definition SO for a run difficulty modifier.
    // VS modifiers: Iron Will (harder enemies), Dense Fog (hidden intents), One Path (fewer choices).
    // Selected at run start and applied globally for the entire run.
    [CreateAssetMenu(fileName = "New Difficulty Modifier", menuName = "Project Ascendant/Data/Difficulty Modifier")]
    public class DifficultyModifierSO : ScriptableObject
    {
        [Header("Identity")]
        public string ModifierId;
        public string DisplayName;
        public Sprite Icon;

        [TextArea(2, 4)]
        public string Description;

        [Header("Stat Modifiers")]
        [Tooltip("Multiplier on all enemy base stats. 1.0 = default. Iron Will = 1.25.")]
        public float EnemyStatMultiplier = 1f;

        [Tooltip("Multiplier on Trauma stacks gained per faint. 1.0 = default. Iron Will = 2.0.")]
        public float TraumaStackMultiplier = 1f;

        [Header("Visibility")]
        [Tooltip("All enemy intents are Unknown (Dense Fog). Cannot be revealed by Keen Eye.")]
        public bool HideAllEnemyIntents;

        [Header("Map Restrictions")]
        [Tooltip("Maximum route branches shown at each map junction. One Path = 1.")]
        [Range(1, 3)]
        public int MaxRouteBranchChoices = 3;

        [Tooltip("GDD section for this modifier. Per §9.15.")]
        public string GDDReference;
    }
}
