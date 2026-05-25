using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 + §8.4 — Definition SO for a Held Item. Immutable at runtime.
    // One slot per Pokémon (§8.4.1). Persists across combats until manually un-equipped.
    // Per §5.5.4 — Type Plates grant a Lead Aura while the wearer is Lead.
    [CreateAssetMenu(fileName = "New Held Item", menuName = "Project Ascendant/Items/Held Item")]
    public class HeldItemSO : ScriptableObject
    {
        [Header("Identity")]
        public string Id;
        public string DisplayName;
        public Sprite Icon;

        [Header("Lead Aura — §5.5.4")]
        // Per §5.5.4 — if true, bench moves of LeadAuraType gain +5% while wearer is Lead.
        public bool GrantsLeadAura;
        public PokemonType LeadAuraType;

        [Header("Hooks")]
        // Per §8.7 — fired once when this item is equipped to a Pokémon in the Map View.
        public ScriptableHook OnEquipHook;

        // Per §8.7 — event+hook bindings; wired by HookSubscriber in Epic 4.
        public List<HookBinding> EventHooks;

        [Tooltip("GDD section for this item. Per §9.15.")]
        public string GDDReference;
    }
}
