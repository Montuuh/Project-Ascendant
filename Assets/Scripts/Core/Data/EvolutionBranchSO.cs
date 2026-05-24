using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §5.3 + Epic 3.1.3 — Definition SO for one evolution branch.
    // A PokemonSpeciesSO holds 2-4 EvolutionBranchSOs per §5.3.3.
    // Each branch defines the evolved form, which moves change, and which ability is granted.
    [CreateAssetMenu(fileName = "New Evolution Branch", menuName = "Project Ascendant/Data/Evolution Branch")]
    public class EvolutionBranchSO : ScriptableObject
    {
        public string BranchId;
        public string DisplayName;

        // Per §5.3.4 — archetype drives move-kit design guidelines.
        public BranchArchetype Archetype;

        // The evolved species SO for this branch (e.g. Wartortle Vanguard SO).
        public PokemonSpeciesSO EvolvedSpecies;

        // Per §5.3.5 — which of the 4 base move slots change on taking this branch.
        // Slots not listed are retained unchanged.
        public List<MoveSlotOverride> MoveOverrides;

        // Per §5.5.1 — ability granted when this branch is chosen (nullable).
        public AbilitySO GrantedAbility;

        // Per §5.3.5 — Stage 2 sub-choices within the same archetype (e.g. A1 vs A2).
        // Empty on Stage 1 branches; populated on final-form branches with sub-selections.
        public List<EvolutionBranchSO> SubBranches;

        [Tooltip("GDD section for this branch. Per §9.15.")]
        public string GDDReference;
    }

    // Per §5.3.5 — specifies which move slot is replaced and with what on evolution.
    [Serializable]
    public struct MoveSlotOverride
    {
        [Range(0, 3)]
        [Tooltip("0-3 = base move slots. Mastery Move (slot 4) cannot be overridden per §4.3.9.2.")]
        public int SlotIndex;

        public MoveSO ReplacementMove;
    }
}
