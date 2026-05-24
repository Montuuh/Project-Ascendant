using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §5.5 — Definition SO for a passive ability. Immutable at runtime.
    // Abilities are granted at first evolution (primary) and by branch at final evolution (secondary).
    // Per §5.5.4 — Lead Aura abilities activate/deactivate when this Pokémon enters/leaves Lead.
    [CreateAssetMenu(fileName = "New Ability", menuName = "Project Ascendant/Data/Ability")]
    public class AbilitySO : ScriptableObject
    {
        public string AbilityId;
        public string DisplayName;

        [TextArea(2, 4)]
        public string Description;

        // Per §5.5.2 — category governs which subsystem this ability interacts with.
        public AbilityCategory Category;

        // Per §5.5.4 — Aura-category abilities only. Ignored if Category != Aura.
        // While this Pokémon is Lead, bench moves of LeadAuraType gain +5% damage.
        public bool GrantsLeadAura;
        public PokemonType LeadAuraType;

        // Hook wired by HookSubscriber in Epic 4 when this ability is active.
        public ScriptableHook EffectHook;

        [Tooltip("GDD section that specifies this ability. Per §9.15 bidirectional link.")]
        public string GDDReference;
    }
}
