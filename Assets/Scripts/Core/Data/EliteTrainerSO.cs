using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.5 + Epic 8 Task 8.4 — definition SO for an Elite Trainer node.
    // One Elite per Region (always Layer 3). Distinct from standard trainers
    // (§7.4 / TrainerArchetypeSO) and from Gym Leaders (§4.4.4):
    //   • Composition: 2 Pokémon, sequential, each with a 2-phase design
    //     (Phase 1 / Phase 2 standard per §4.4.3). PhaseCount is authored
    //     per slot so the Gym ace (Task 8.5) can reuse the same shape at 3.
    //   • NO type lock (§7.5.1) — that single-type identity is reserved for
    //     Gym Leaders. The Elite tests adaptability across mixed types.
    //   • Reward: ONE guaranteed Uncommon relic + flat Trainer XP + a
    //     PokéDollar windfall (§7.5.1 / §7.12) — not a random loot-table roll
    //     like standard trainers.
    //
    // R1 identity note: §7.5.1 sources the Elite from the "Ace Trainer pool,"
    // but Ace Trainer is R3-only (§7.4.3) and out of VS scope (§7.13). The VS
    // ships one bespoke R1 Ace Trainer authored against the VS roster. See the
    // ⚠ OPEN flag on §7.5.1 and BACKLOG gap #31.
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

        [Header("Reward — §7.5.1 / §7.12: guaranteed Uncommon relic + XP + ₽")]
        [Tooltip("The single guaranteed relic drop. Must be Uncommon-tier (§7.5.1).")]
        public RelicSO GuaranteedRelic;

        [Tooltip("Flat Trainer XP awarded on victory. §7.12 — Elite = 25.")]
        public int TrainerXPReward;

        [Tooltip("PokéDollar windfall on victory. §7.12 — Elite = 300.")]
        public int PokeDollarReward;

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
}
