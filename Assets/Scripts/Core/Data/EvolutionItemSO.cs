using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §5.3.2 + Epic 10 Task 10.5 — an Evolution Item: a consumable that UNLOCKS an additional
    // evolution branch not reachable through level-only evolution (e.g. a Fire Stone enabling a
    // Flareon path). Standard 2-stage evolutions never require items. The item carries the branch it
    // enables; the Branch-Selection screen offers that branch when a matching item is in inventory,
    // and consumes the item on use.
    //
    // VS: no Evolution Items are shipped — this is the architecture stub (Task 10.5.4). The
    // Branch-Selection screen + RunState inventory + consume path are all in place and latent.
    [CreateAssetMenu(fileName = "New Evolution Item", menuName = "Project Ascendant/Items/Evolution Item")]
    public class EvolutionItemSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public Sprite Icon;

        // Per §5.3.2 — the branch this item makes available (in addition to the species' level branches).
        public EvolutionBranchSO EnabledBranch;

        [Tooltip("GDD section for this item. Per §9.15.")]
        public string GDDReference;
    }

    // Per §5.3 + Epic 10 Task 10.4.5 — fired on EventBus when a Pokémon evolves. Listeners (achievement
    // tracking, VFX, Pokedex) subscribe; none required for the VS, but the hook is in place.
    public readonly struct EvolutionTriggeredContext
    {
        public readonly PokemonInstance Pokemon;
        public readonly PokemonSpeciesSO From;
        public readonly PokemonSpeciesSO To;
        public readonly EvolutionBranchSO Branch;

        public EvolutionTriggeredContext(PokemonInstance pokemon, PokemonSpeciesSO from,
                                         PokemonSpeciesSO to, EvolutionBranchSO branch)
        {
            Pokemon = pokemon; From = from; To = to; Branch = branch;
        }
    }
}
