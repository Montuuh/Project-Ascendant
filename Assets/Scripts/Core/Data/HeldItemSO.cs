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

        // Per Epic 13 — short player-facing effect summary for inventory/shop tooltips.
        [TextArea(1, 3)]
        public string EffectDescription;

        [Header("Type-boost (wearer) — §8.4.2")]
        // Per §8.4.2 + Epic 12 Task 12.6 — the WEARER's moves of BoostsType deal ×WearerDamageMultiplier
        // (Charcoal/Mystic Water/Magnet/Miracle Seed = +20%). Multiplier 1.0 = no type boost.
        public PokemonType BoostsType;
        public float WearerDamageMultiplier = 1f;

        [Header("Sustain — §8.4.4")]
        // Per §8.4.4 Leftovers — at end of Resolution, restore floor(EffectiveMaxHP / Divisor) (min 1).
        // 0 = no regen.
        public int LeftoversRegenDivisor;

        [Header("Lead Aura — §5.5.4 / §8.4.3 (Type Plates, post-VS)")]
        // Per §5.5.4 — if true, bench moves of LeadAuraType gain +5% while wearer is Lead. The VS 5 items
        // (§8.4.2) use the wearer type-boost above; Lead Aura belongs to the §8.4.3 Type Plates (post-VS).
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
