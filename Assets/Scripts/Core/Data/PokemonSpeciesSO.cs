using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.1 — Definition SO for a Pokémon species. Immutable at runtime.
    // This is a stub for Epic 2 factory infrastructure.
    // TODO: Epic 3 — expand with full schema: Types, Branches, BaseLearnset, TutorLearnset,
    //       TMCompatibility, StatusImmunities, Portrait, WildRarity, SpawnBiomes, StatGrowthCurve.
    [CreateAssetMenu(fileName = "New Pokemon Species", menuName = "ProjectAscendant/Data/Pokemon Species")]
    public class PokemonSpeciesSO : ScriptableObject
    {
        public string SpeciesId;
        public string DisplayName;
        public BaseStats BaseStats;
        public AbilitySO PrimaryAbility;   // granted at first evolution
        public MoveSO MasteryMoveBase;     // §4.3.9.2 — 5th slot; immutable
    }
}
