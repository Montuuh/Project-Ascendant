using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.7.1 / §7.7.2 + Epic 9 Task 9.6 — Region Shop slot layout, pricing, and re-roll costs.
    // All shop balance values here (no inline literals). Consumable/Held-Item prices are seeded
    // within [min, max]; relic/Pokéball prices are flat; TMs use their own TMSO.ShopPrice.
    [CreateAssetMenu(fileName = "RegionShopConfig",
        menuName = "Project Ascendant/Config/Region Shop Config")]
    public class RegionShopConfigSO : ScriptableObject
    {
        [Header("Consumable Slots — §7.7.1 (3 slots, 30–100₽)")]
        public int ConsumableSlots = 3;
        public int ConsumablePriceMin = 30;
        public int ConsumablePriceMax = 100;

        [Header("Relic Slots — §7.7.1")]
        public int CommonRelicPrice = 150;
        public int UncommonRelicPrice = 300;

        [Header("Pokéball Slot — §7.7.1 (50₽)")]
        public int PokeballPrice = 50;

        [Header("Special Slot — §7.7.1 (Held Item OR TM, 250–500₽)")]
        // Held Item price is seeded in this band; a TM in the special slot uses its TMSO.ShopPrice.
        public int HeldItemPriceMin = 250;
        public int HeldItemPriceMax = 500;

        [Header("Re-roll — §7.7.2 (25 → 50 → 100₽, max 3)")]
        public int[] RerollCosts = { 25, 50, 100 };
    }
}
