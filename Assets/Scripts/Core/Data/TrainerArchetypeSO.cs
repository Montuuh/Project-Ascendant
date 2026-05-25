using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §3.1.15 — Definition SO for a Trainer archetype.
    // VS archetypes: Bug Catcher, Lass, Hiker, Sailor (§3.3.18).
    // Full AI behaviour wired in Epic 8 (Encounters & AI).
    [CreateAssetMenu(fileName = "New Trainer Archetype", menuName = "Project Ascendant/World/Trainer Archetype")]
    public class TrainerArchetypeSO : ScriptableObject
    {
        [Header("Identity")]
        public string ArchetypeId;
        public string DisplayName;
        public Sprite TrainerSprite;

        [Header("Team Composition")]
        // Per §3.1.15 — 1-3 Pokémon slots per trainer.
        public List<TrainerPokemonSlot> Composition;

        [Header("Tactical Identity")]
        [TextArea(1, 4)]
        [Tooltip("AI intent tendencies for this archetype. Read by Epic 8 AI system.")]
        public string TacticalIdentity;

        [Header("Loot")]
        public List<RelicSO> RelicLootTable;
        public List<ConsumableSO> ConsumableLootTable;
        public int BasePokeDollarReward;

        [Tooltip("GDD section for this archetype. Per §9.15.")]
        public string GDDReference;
    }

    [Serializable]
    public struct TrainerPokemonSlot
    {
        public PokemonSpeciesSO Species;
        public int Level;
    }
}
