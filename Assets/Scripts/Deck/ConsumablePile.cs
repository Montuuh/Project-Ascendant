using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Deck
{
    // Per §3.5 + Epic 5 Task 5.1.2 — per-combat consumable pile.
    //
    // The player's persistent consumable inventory is held outside combat
    // (Topic 8). At combat start, this class is built FROM that inventory —
    // a reference, not a copy. Each turn, 2 cards are drawn (§3.2.2). When
    // a consumable is played, it is set aside (UsedThisCombat) and not
    // redrawn for the remainder of the combat. At combat end, RestoreAll()
    // clears the used-list — consumables are NOT expendable (§3.5).
    //
    // Tier upgrades (Potion → Super Potion → … → Max Potion) are an
    // out-of-combat progression concern (Topic 8). This class is type-blind
    // about consumable identity — it cares only about uniqueness for the
    // "once per combat" rule, keyed on the ConsumableSO reference.
    //
    // Hand storage (the per-turn drawn cards) is owned by the controller;
    // DrawHand returns a freshly-rolled hand list and does NOT mutate any
    // internal state — drawing is non-consuming. Only MarkUsed mutates the
    // used-list (called by the controller when a consumable is actually
    // played, not merely drawn).
    public sealed class ConsumablePile
    {
        private IList<ConsumableSO> _inventory;
        private readonly List<ConsumableSO> _usedThisCombat = new();

        public IReadOnlyList<ConsumableSO> UsedThisCombat => _usedThisCombat;

        // The number of consumables eligible to be drawn this turn:
        // inventory minus already-used. Null inventory → 0.
        public int AvailableCount
        {
            get
            {
                if (_inventory == null) return 0;
                int n = 0;
                for (int i = 0; i < _inventory.Count; i++)
                    if (!_usedThisCombat.Contains(_inventory[i])) n++;
                return n;
            }
        }

        // Build at combat start. Holds a reference to the player's inventory
        // (NOT a copy) so consumables added by mid-combat effects (Pickup
        // ability variants, etc.) are visible immediately. UsedThisCombat
        // is cleared — a fresh combat starts with the full inventory available.
        public void Build(IList<ConsumableSO> inventory)
        {
            _inventory = inventory;
            _usedThisCombat.Clear();
        }

        // Per §3.2.2 — draws `count` consumable cards via the seeded
        // CombatRNG. Skips anything in UsedThisCombat. Returns an empty list
        // if no inventory or nothing available. Non-consuming: drawing does
        // NOT mark anything as used; the controller calls MarkUsed when the
        // player actually plays a consumable.
        public List<ConsumableSO> DrawHand(int count, GameRNG rng)
        {
            List<ConsumableSO> hand = new();
            if (_inventory == null || count <= 0) return hand;
            List<ConsumableSO> available = new();
            for (int i = 0; i < _inventory.Count; i++)
            {
                ConsumableSO c = _inventory[i];
                if (c == null) continue;
                if (!_usedThisCombat.Contains(c)) available.Add(c);
            }
            for (int i = 0; i < count && available.Count > 0; i++)
            {
                int idx = rng.Range(0, available.Count);
                hand.Add(available[idx]);
                available.RemoveAt(idx);
            }
            return hand;
        }

        // Per §3.5 — once per combat. Idempotent: re-marking is a no-op
        // (callers may invoke this without a contains-check).
        public void MarkUsed(ConsumableSO c)
        {
            if (c == null) return;
            if (_usedThisCombat.Contains(c)) return;
            _usedThisCombat.Add(c);
        }

        public bool IsUsed(ConsumableSO c)
        {
            if (c == null) return false;
            return _usedThisCombat.Contains(c);
        }

        // Per §3.5 — at combat end, consumables are restored to the
        // persistent inventory. Since the inventory was never mutated (we
        // only tracked the used-list), restoration is just clearing the
        // used-list.
        public void RestoreAll()
        {
            _usedThisCombat.Clear();
        }
    }
}
