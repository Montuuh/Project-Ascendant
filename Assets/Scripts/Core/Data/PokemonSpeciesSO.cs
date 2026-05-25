using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.1 — Definition SO for a Pokémon species. Immutable at runtime.
    // One SO per evolution stage per branch: e.g. Squirtle, Wartortle_Vanguard, Blastoise_VanguardA1.
    [CreateAssetMenu(fileName = "New Pokemon Species", menuName = "Project Ascendant/Data/Pokemon Species")]
    public class PokemonSpeciesSO : ScriptableObject
    {
        [Header("Identity")]
        public string SpeciesId;
        public string DisplayName;

        [Header("Type")]
        // Per §9.3.2.1 — 1-2 types.
        public List<PokemonType> Types;

        [Header("Stats")]
        public BaseStats BaseStats;
        public StatGrowthCurveSO GrowthCurve;

        [Header("Evolution")]
        // Per §5.3.3 — 2-4 branches (empty list if this is a final-form SO).
        public List<EvolutionBranchSO> Branches;

        [Header("Learnset")]
        // Per §9.3.2.1 — 4 moves available at base level.
        public List<MoveSO> BaseLearnset;

        // Per §5.4.2 — moves available from Move Tutors.
        public List<MoveSO> TutorLearnset;

        // Per §5.4.1 — TM compatibility list.
        public List<TMSO> TMCompatibility;

        [Header("Ability & Mastery")]
        // Per §5.5.1 — granted at first evolution.
        public AbilitySO PrimaryAbility;

        // Per §4.3.9.2 — this stage's Mastery tier card (Lv1/Lv2/Lv3 depending on evolution stage). Unlocked via meta-progression achievements. Cannot be replaced by TM/Tutor.
        public MoveSO MasteryMove;

        [Header("Wild Encounter")]
        public List<StatusCondition> StatusImmunities;
        public RarityTier WildRarity;
        public List<Biome> SpawnBiomes;

        [Header("Presentation")]
        public Sprite Portrait;

        [Tooltip("GDD section for this species. Per §9.15.")]
        public string GDDReference;
    }
}
