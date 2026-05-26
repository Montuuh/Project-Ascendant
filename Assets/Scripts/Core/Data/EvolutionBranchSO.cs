using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §5.3 + Epic 3.1.3 — Definition SO for one evolution branch.
    // A PokemonSpeciesSO holds 2-4 EvolutionBranchSOs per §5.3.3.
    // Each branch defines the evolved form, which pool moves upgrade, and which ability is granted.
    [CreateAssetMenu(fileName = "New Evolution Branch", menuName = "Project Ascendant/Pokemon/Evolution Branch")]
    public class EvolutionBranchSO : ScriptableObject
    {
        public string BranchId;
        public string DisplayName;

        // Per §5.3.4 — archetype drives move-kit design guidelines.
        public BranchArchetype Archetype;

        // The evolved species SO for this branch (e.g. Wartortle Vanguard SO).
        public PokemonSpeciesSO EvolvedSpecies;

        // Per §5.3.5 + §5.10 — in-place pool upgrades: each pair replaces OldMove with NewMove.
        // Keyed by old move reference, not slot index.
        public List<MoveUpgradePair> MoveUpgrades;

        // Per §5.10 — brand-new moves added to the pool on taking this branch.
        public List<MoveSO> NewMoves;

        // Per §5.5.1 — ability granted when this branch is chosen (nullable).
        public AbilitySO GrantedAbility;

        // Per §4.1.3 — permanent crit-chance bonus granted by this branch. Typically
        // non-zero only on offensive (Vanguard/Specialist) branches; left at 0 for
        // Support/utility branches. Additive with consumable temp boost, clamped [0, 1].
        // Authored in the inspector by the seeder (kept at 0 here so the PA0001
        // balance-literal analyzer stays quiet).
        [Range(0f, 1f)]
        public float CritChanceBonus;

        // Per §5.3.5 — Stage 2 sub-choices within the same archetype (e.g. A1 vs A2).
        // Empty on Stage 1 branches; populated on final-form branches with sub-selections.
        public List<EvolutionBranchSO> SubBranches;

        [Tooltip("GDD section for this branch. Per §9.15.")]
        public string GDDReference;
    }

    // Per §5.3.5 + §5.10 — in-place pool upgrade: OldMove is replaced by NewMove in the pool.
    [Serializable]
    public struct MoveUpgradePair
    {
        public MoveSO OldMove;
        public MoveSO NewMove;
    }
}
