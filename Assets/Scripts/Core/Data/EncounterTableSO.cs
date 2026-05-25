using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §3.1.16 — per-region encounter weight table for wild Pokémon.
    // Rolled via EncounterRNG stream (§9.7.2) using PickWeighted.
    [CreateAssetMenu(fileName = "New Encounter Table", menuName = "Project Ascendant/World/Encounter Table")]
    public class EncounterTableSO : ScriptableObject
    {
        public string TableId;

        [Tooltip("Region this table applies to. Used for multi-region lookups in Epic 9.")]
        public int RegionIndex;

        public List<EncounterWeight> Encounters;

        [Tooltip("GDD section for this table. Per §9.15.")]
        public string GDDReference;
    }

    [Serializable]
    public struct EncounterWeight
    {
        public PokemonSpeciesSO Species;

        [Tooltip("Relative encounter weight (not a probability). Higher = more common.")]
        public float Weight;

        public int MinLevel;
        public int MaxLevel;
    }
}
