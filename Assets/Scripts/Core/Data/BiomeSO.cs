using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §3.1.13 — Definition SO for a biome. Used in encounter tables and map generation.
    // VS biomes: Meadow, Cave, River (§3.3.16).
    [CreateAssetMenu(fileName = "New Biome", menuName = "Project Ascendant/World/Biome")]
    public class BiomeSO : ScriptableObject
    {
        [Header("Identity")]
        public string BiomeId;
        public string DisplayName;
        public Biome BiomeType;

        [Header("Presentation")]
        public Color PaletteTint = Color.white;
        public Sprite BackgroundSprite;

        [Header("Encounter Pool")]
        // Species that can be encountered in this biome (weights in EncounterTableSO).
        public List<PokemonSpeciesSO> SpeciesPool;

        [Tooltip("GDD section for this biome. Per §9.15.")]
        public string GDDReference;
    }
}
