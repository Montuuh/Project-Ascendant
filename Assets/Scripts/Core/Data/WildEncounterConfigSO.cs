using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.3.1 / §7.3.2 / §7.3.5 + Epic 9 Task 9.3 — tuning for Wild Pokémon Area nodes:
    // which biomes a Region surfaces (weighted), the species-choice composition, and the wild
    // level band. All wild-encounter balance lives here — no inline literals (PA0001).
    [CreateAssetMenu(fileName = "WildEncounterConfig",
        menuName = "Project Ascendant/Config/Wild Encounter Config")]
    public class WildEncounterConfigSO : ScriptableObject
    {
        [Header("Biome Selection — §7.3.1")]
        [Tooltip("Biomes this Region can surface, weighted (primary biome appears more often).")]
        public List<BiomeWeight> RegionBiomes;

        [Header("Choice Composition — §7.3.2")]
        [Tooltip("Number of Common-rarity species offered per Wild Area node.")]
        public int CommonChoices = 2;

        [Tooltip("Number of Uncommon-rarity species offered per Wild Area node.")]
        public int UncommonChoices = 1;

        [Tooltip("Chance to replace the Uncommon choice with a Rare species (~0.10 per §7.3.2).")]
        [Range(0f, 1f)]
        public float RareSwapChance = 0.10f;

        [Header("Wild Level Band — §7.3.5")]
        [Tooltip("Minimum wild level for this Region (R1 = 5).")]
        public int WildLevelMin = 5;

        [Tooltip("Maximum wild level for this Region (R1 = 10).")]
        public int WildLevelMax = 10;
    }

    [Serializable]
    public struct BiomeWeight
    {
        public BiomeSO Biome;

        [Tooltip("Relative weight (not a probability). Primary biome should weigh highest.")]
        public float Weight;
    }
}
