using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.5 + Epic 8 Task 8.4 + CL-024 — definition SO for an Elite Trainer node.
    // One Elite per Region (late trunk ≈L7 per §7.2 v2). Distinct from standard
    // trainers (§7.4 / TrainerArchetypeSO) and from Gym Leaders (§4.4.4):
    //   • Composition: 2 Pokémon, sequential, 2-phase (Phase 1 / Phase 2 standard
    //     per §4.4.3). Rival / Giovanni ace may be 3-phase + mid-fight evolution
    //     (§4.3.7 / §7.5.1 — scales by Region band).
    //   • NO type lock (§7.5.1) — that single-type identity is reserved for
    //     Gym Leaders. The Elite tests adaptability across mixed types.
    //   • Reward (CL-024): Rare-relic choice (1 of 3) + flat Trainer XP + a
    //     PokéDollar windfall (§7.5.1 / §7.12 — up from Uncommon to Rare).
    //   • Occupant (CL-024): RNG-weighted roster per Region (§7.5.1) — R1 80%
    //     Rival / 20% Specialist · R2 60% Rival / 40% Specialist · R3 40% Rival
    //     / 30% Giovanni / 30% Specialist.
    //
    // Rival scaling (§7.5.1 + §4.3.7): R1 = 2 Pokémon, 2-phase → R3 = up to 3
    // + ace mid-fight evolution. Giovanni (R3+) can appear here AND as Viridian
    // Ground Gym Leader — both encounters canon.
    [CreateAssetMenu(fileName = "New Elite Trainer", menuName = "Project Ascendant/World/Elite Trainer")]
    public class EliteTrainerSO : ScriptableObject
    {
        [Header("Identity")]
        public string EliteId;
        public string DisplayName;
        public Sprite TrainerSprite;

        [Header("Tactical Identity")]
        [TextArea(1, 4)]
        [Tooltip("AI intent tendencies. No single-type lock (§7.5.1).")]
        public string TacticalIdentity;

        [Header("Team Composition — §7.5.1: 2 Pokémon, sequential, each 2-phase")]
        public List<ElitePokemonSlot> Composition;

        [Header("Reward — §7.5.1 / §7.12 (CL-024): Rare-relic choice (1 of 3) + XP + ₽")]
        [Tooltip("3 Rare-tier relics — player picks 1 of 3 (mirrors Victory Road Gauntlet §4.5.1.1).")]
        public List<RelicSO> RareRelicChoices;

        [Tooltip("Flat Trainer XP awarded on victory. §7.12 — Elite = 25.")]
        public int TrainerXPReward;

        [Tooltip("PokéDollar windfall on victory. §7.12 — Elite = 300.")]
        public int PokeDollarReward;

        [Header("Rival Identity (CL-024 — §7.5.1 + §4.3.7)")]
        [Tooltip("Is this the recurring Rival? (Blue in Gen I).")]
        public bool IsRival;

        [Tooltip("Rival ace evolution branch (e.g., Wartortle → Blastoise at 50% HP in R3). Per CL-024, uses EvolutionBranchSO so moves upgrade properly.")]
        public EvolutionBranchSO RivalEvoBranch;

        [Header("Region Scaling (Rival / Giovanni — CL-024)")]
        [Tooltip("Per-Region power scaling (R1 = 2 Pokémon/2-phase → R3 = 3 Pokémon/ace 3-phase + evo).")]
        public List<EliteRegionScaling> RegionScaling;

        [Tooltip("GDD section for this Elite. Per §9.15.")]
        public string GDDReference;
    }

    [Serializable]
    public struct ElitePokemonSlot
    {
        public PokemonSpeciesSO Species;
        public int Level;

        [Tooltip("Boss phase depth for this Pokémon (§4.4.3). Elite = 2; a Gym " +
                 "ace = 3. Copied to PokemonInstance.PhaseCount at materialise.")]
        public int PhaseCount;
    }

    // Per §7.5.1 (CL-024) — per-Region scaling for Rival / Giovanni Elite encounters.
    // R1 = 2 Pokémon, 2-phase → R3 = up to 3 + ace mid-fight evolution (§4.3.7).
    [Serializable]
    public struct EliteRegionScaling
    {
        [Tooltip("Zero-indexed Region (0=R1, 1=R2, 2=R3).")]
        public int RegionIndex;

        [Tooltip("Number of Pokémon in this Region (2 for R1, 3 for R3).")]
        public int PokemonCount;

        [Tooltip("Ace phase count (2 for R1/R2, 3 for R3 — last-stand §4.4.3).")]
        public int AcePhaseCount;
    }
}
