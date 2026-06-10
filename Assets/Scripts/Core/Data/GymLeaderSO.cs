using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §4.4.4 + Epic 8 Task 8.5 — definition SO for a Gym Leader boss
    // (the Region climax). Distinct from standard trainers (§7.4) and Elites
    // (§7.5):
    //   • Composition: 2 Pokémon, sequential. The 2nd is the ACE — 3-phase
    //     (§4.4.3) with a Phase-3 Sturdy last-stand. Per CL-013 (§4.4.4.3) Gym
    //     aces NO LONGER evolve mid-fight — mid-evolution is reserved for the
    //     rival/Champion (the engine mechanic stays in CombatController for them).
    //   • Type identity (§4.4.4.2): the whole team + a persistent field effect
    //     reflect GymType (R1 default: Rock). The field is a VS placeholder
    //     (set + persists; no damage multiplier — see ⚠ OPEN gap #33).
    //   • Reward (§7.12 / §4.4.5): a Badge + a guaranteed Rare relic + 50 XP +
    //     500₽. The Badge is permanently active to run-end (§4.4.5).
    [CreateAssetMenu(fileName = "New Gym Leader", menuName = "Project Ascendant/World/Gym Leader")]
    public class GymLeaderSO : ScriptableObject
    {
        [Header("Identity")]
        public string GymLeaderId;
        public string DisplayName;

        [Tooltip("Single-type identity (§4.4.4.2). R1 default: Rock. Sets the " +
                 "persistent field effect at encounter start (§4.4.4.3).")]
        public PokemonType GymType;

        public Sprite TrainerSprite;

        [TextArea(1, 4)]
        public string TacticalIdentity;

        [Header("Team — §4.4.4.3: 2 Pokémon, sequential; 2nd is the 3-phase ace")]
        public List<GymPokemonSlot> Composition;

        [Header("Reward — §7.12 / §4.4.5: Badge + Rare relic + 50 XP + 500₽")]
        public BadgeSO BadgeReward;

        [Tooltip("Guaranteed Rare-tier relic drop (§7.12).")]
        public RelicSO GuaranteedRareRelic;

        [Tooltip("Trainer XP on victory. §7.12 — Gym = 50.")]
        public int TrainerXPReward;

        [Tooltip("PokéDollar windfall on victory. §7.12 — Gym = 500.")]
        public int PokeDollarReward;

        public string GDDReference;
    }

    [Serializable]
    public struct GymPokemonSlot
    {
        public PokemonSpeciesSO Species;
        public int Level;

        [Tooltip("Phase depth (§4.4.3). Gym lead = 2; ace = 3.")]
        public int PhaseCount;

        [Tooltip("True for the ace (the 2nd, climactic Pokémon).")]
        public bool IsAce;

        [Tooltip("Per §4.4.3 Phase 3 — ace survives the first lethal hit at 1 HP.")]
        public bool HasSturdy;

        // Per CL-013 (§4.4.4.3) — Gym aces no longer evolve mid-fight; the
        // MidFightEvolution slot field was removed. The engine mechanic
        // (PokemonInstance.MidFightEvolutionTarget) remains for rival/Champion.
    }
}
