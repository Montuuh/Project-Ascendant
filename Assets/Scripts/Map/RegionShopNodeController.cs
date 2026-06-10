using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.7 + Epic 9 Task 9.6 — the Region Shop node (Layer 2 or 5). Utility node: no combat.
    // Seeds a curated inventory (LootRNG, §9.7.2), supports buying with Poké Dollars, and offers
    // up to 3 escalating re-rolls (§7.7.2). Leave() completes the node.
    //
    // Slots (§7.7.1): 3 Consumables (30–100₽) · 1 Common relic (150₽) · 1 Uncommon relic (300₽)
    //               · 1 Pokéball (50₽) · 1 special Held Item (250–500₽) OR TM (TMSO.ShopPrice).
    public sealed class RegionShopNodeController : NodeController
    {
        public enum ShopSlotKind { Consumable, CommonRelic, UncommonRelic, Pokeball, HeldItem, TM }

        public sealed class ShopSlot
        {
            public ShopSlotKind Kind;
            public ScriptableObject Item;
            public int Price;
            public bool Purchased;
        }

        // Item pools the run layer supplies (the VS authored catalogs).
        public sealed class ShopItemPools
        {
            public List<ConsumableSO> Consumables;
            public List<RelicSO> CommonRelics;
            public List<RelicSO> UncommonRelics;
            public ConsumableSO Pokeball;
            public List<HeldItemSO> HeldItems;
            public List<TMSO> TMs;
        }

        private readonly RegionShopConfigSO _config;
        private readonly GameRNG _lootRng;
        private readonly ShopItemPools _pools;
        private readonly List<ShopSlot> _slots = new();

        public IReadOnlyList<ShopSlot> Slots => _slots;
        public int RerollCount { get; private set; }

        public RegionShopNodeController(
            MapNode node, RunStateSO runState, RegionShopConfigSO config,
            GameRNG lootRng, ShopItemPools pools)
            : base(node, runState)
        {
            _config  = config ?? throw new ArgumentNullException(nameof(config));
            _lootRng = lootRng ?? throw new ArgumentNullException(nameof(lootRng));
            _pools   = pools ?? throw new ArgumentNullException(nameof(pools));
        }

        protected override void OnEnter() => RollInventory();

        // ── Inventory seeding (9.6.1) ─────────────────────────────────────────

        private void RollInventory()
        {
            _slots.Clear();

            // 3 distinct consumables, each priced in the configured band.
            List<ConsumableSO> consumables = PickDistinct(_pools.Consumables, _config.ConsumableSlots);
            for (int i = 0; i < consumables.Count; i++)
                AddSlot(ShopSlotKind.Consumable, consumables[i],
                        _lootRng.Range(_config.ConsumablePriceMin, _config.ConsumablePriceMax + 1));

            RelicSO common = PickOne(_pools.CommonRelics);
            if (common != null) AddSlot(ShopSlotKind.CommonRelic, common, _config.CommonRelicPrice);

            RelicSO uncommon = PickOne(_pools.UncommonRelics);
            if (uncommon != null) AddSlot(ShopSlotKind.UncommonRelic, uncommon, _config.UncommonRelicPrice);

            if (_pools.Pokeball != null)
                AddSlot(ShopSlotKind.Pokeball, _pools.Pokeball, _config.PokeballPrice);

            AddSpecialSlot();
        }

        // §7.7.1 — special slot is a Held Item OR a TM (50/50, falling back if one pool is empty).
        private void AddSpecialSlot()
        {
            bool haveHeld = _pools.HeldItems != null && _pools.HeldItems.Count > 0;
            bool haveTM = _pools.TMs != null && _pools.TMs.Count > 0;
            if (!haveHeld && !haveTM) return;

            bool chooseHeld = haveHeld && (!haveTM || _lootRng.Range(0, 2) == 0);
            if (chooseHeld)
            {
                HeldItemSO held = PickOne(_pools.HeldItems);
                AddSlot(ShopSlotKind.HeldItem, held,
                        _lootRng.Range(_config.HeldItemPriceMin, _config.HeldItemPriceMax + 1));
            }
            else
            {
                TMSO tm = PickOne(_pools.TMs);
                AddSlot(ShopSlotKind.TM, tm, tm != null ? tm.ShopPrice : 0);
            }
        }

        private void AddSlot(ShopSlotKind kind, ScriptableObject item, int price)
        {
            if (item == null) return;
            // §7.8.3.1 (CL-016) Bargain Hunter — Shop prices discounted this Region (baked into the
            // listed price so the displayed cost matches what's charged at Buy()).
            int listed = UnityEngine.Mathf.FloorToInt(
                price * RegionModifierResolver.ShopPriceMultiplier(RunState?.ActiveRegionModifiers));
            _slots.Add(new ShopSlot { Kind = kind, Item = item, Price = listed, Purchased = false });
        }

        // ── Buy (9.6.2) ───────────────────────────────────────────────────────

        // Buys slot[index] if it exists, is unpurchased, and is affordable. Routes the item into
        // the matching RunState collection. Returns false (no change) otherwise.
        public bool Buy(int index)
        {
            if (index < 0 || index >= _slots.Count) return false;
            ShopSlot slot = _slots[index];
            if (slot.Purchased) return false;
            if (RunState.PokeDollars < slot.Price) return false;

            RunState.PokeDollars -= slot.Price;
            RouteToRunState(slot);
            slot.Purchased = true;
            return true;
        }

        private void RouteToRunState(ShopSlot slot)
        {
            switch (slot.Kind)
            {
                case ShopSlotKind.Consumable:
                    (RunState.Inventory ??= new List<ConsumableSO>()).Add((ConsumableSO)slot.Item);
                    break;
                case ShopSlotKind.Pokeball:
                    // §7.3.4 (Option 1) — a bought Pokéball adds to the counted resource, not the
                    // (non-expendable) consumable inventory; it is spent on a catch attempt.
                    RunState.PokeballCount++;
                    break;
                case ShopSlotKind.CommonRelic:
                case ShopSlotKind.UncommonRelic:
                    (RunState.HeldRelics ??= new List<RelicSO>()).Add((RelicSO)slot.Item);
                    break;
                case ShopSlotKind.HeldItem:
                    (RunState.OwnedHeldItems ??= new List<HeldItemSO>()).Add((HeldItemSO)slot.Item);
                    break;
                case ShopSlotKind.TM:
                    (RunState.OwnedTMs ??= new List<TMSO>()).Add((TMSO)slot.Item);
                    break;
            }
        }

        // ── Re-roll (9.6.3) ───────────────────────────────────────────────────

        // Cost of the next re-roll, or -1 when the per-visit limit (§7.7.2, 3) is reached.
        public int NextRerollCost =>
            RerollCount < _config.RerollCosts.Length ? _config.RerollCosts[RerollCount] : -1;

        // §7.7.2 — re-rolls the whole inventory for an escalating cost (25 → 50 → 100), max 3.
        public bool TryReroll()
        {
            int cost = NextRerollCost;
            if (cost < 0 || RunState.PokeDollars < cost) return false;

            RunState.PokeDollars -= cost;
            RerollCount++;
            RollInventory();
            return true;
        }

        // Player leaves the shop → node resolved.
        public void Leave() => Complete(NodeOutcome.Cleared);

        // ── Pick helpers (LootRNG, uniform) ───────────────────────────────────

        private T PickOne<T>(List<T> pool) where T : ScriptableObject
        {
            if (pool == null || pool.Count == 0) return null;
            List<(T value, float weight)> opts = new(pool.Count);
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null) opts.Add((pool[i], 1f));
            return opts.Count > 0 ? _lootRng.PickWeighted(opts) : null;
        }

        private List<T> PickDistinct<T>(List<T> pool, int count) where T : ScriptableObject
        {
            List<T> picks = new();
            if (pool == null || count <= 0) return picks;
            List<T> remaining = new();
            for (int i = 0; i < pool.Count; i++) if (pool[i] != null) remaining.Add(pool[i]);

            int target = count < remaining.Count ? count : remaining.Count;
            for (int i = 0; i < target; i++)
            {
                List<(T value, float weight)> opts = new(remaining.Count);
                for (int j = 0; j < remaining.Count; j++) opts.Add((remaining[j], 1f));
                T picked = _lootRng.PickWeighted(opts);
                picks.Add(picked);
                remaining.Remove(picked);
            }
            return picks;
        }
    }
}
