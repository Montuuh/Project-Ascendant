using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §4.4.4.4 (CL-013) — the ace's Phase-2 signature. Each Gym TYPE maps to exactly one
    // archetype (telegraphed, learnable). Drives the Phase-2 behaviour in CombatController.
    public enum Phase2Archetype
    {
        None,           // not a per-type Gym ace (default)
        Entrenchment,   // Rock, Ground — +Def stages on Phase-2 entry (race the wall)
        StatusSiege,    // Poison, Grass, Bug — Phase 2 forces Status/Debuff intents
        Onslaught,      // Fire, Fighting, Normal — Phase 2 forces offensive intents (+ Home Field ×1.5)
        TempoControl,   // Electric, Psychic, Ice, Water — Phase 2 taxes player AP each turn
    }

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

        [Header("Phase-2 signature — §4.4.4.4 (CL-013)")]
        [Tooltip("The ace's Phase-2 archetype. Leave None to derive it from GymType " +
                 "(Phase2ArchetypeForType). Set explicitly to override.")]
        public Phase2Archetype AcePhase2Archetype;

        public string GDDReference;

        // Per §4.4.4.4 (CL-013) — the canonical Gym-type → Phase-2 archetype mapping. Used when a
        // Gym's AcePhase2Archetype is left None, so existing/auto-authored Gyms get the right signature.
        public static Phase2Archetype Phase2ArchetypeForType(PokemonType type) => type switch
        {
            PokemonType.Rock or PokemonType.Ground => Phase2Archetype.Entrenchment,
            PokemonType.Poison or PokemonType.Grass or PokemonType.Bug => Phase2Archetype.StatusSiege,
            PokemonType.Fire or PokemonType.Fighting or PokemonType.Normal => Phase2Archetype.Onslaught,
            PokemonType.Electric or PokemonType.Psychic or PokemonType.Ice or PokemonType.Water => Phase2Archetype.TempoControl,
            _ => Phase2Archetype.None,
        };

        // The ace's effective Phase-2 archetype: the explicit field if set, else derived from GymType.
        public Phase2Archetype ResolvedAcePhase2Archetype =>
            AcePhase2Archetype != Phase2Archetype.None ? AcePhase2Archetype : Phase2ArchetypeForType(GymType);
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
