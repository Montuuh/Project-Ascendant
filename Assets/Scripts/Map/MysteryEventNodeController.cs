using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.9 + Epic 9 Task 9.7 — the Mystery Event node. Utility node: usually no combat.
    //
    //   • OnEnter (9.7.1): pick an unfired event from the pool (§7.9.4 repeatability lock via
    //     RunState.EventFlags), seeded by MysteryRNG; publish it + its risk badge (9.7.2/9.7.5).
    //   • Choose (9.7.3): resolve the chosen outcome (code resolver keyed by EventId; numeric values
    //     from MysteryConfigSO), mark the event fired, then Complete.
    //
    // Two VS choices substitute for unbuilt systems (flagged): Mysterious Stone (a) grants a random
    // relic instead of an Evolution Item; Wandering Tutor (a) grants a placeholder consumable instead
    // of a free Move Tutor (§5.10 gap #36).
    public sealed class MysteryEventNodeController : NodeController
    {
        // Item references the run layer supplies for substitute/grant outcomes.
        public sealed class MysteryItemRefs
        {
            public List<RelicSO> StoneRelicPool; // Mysterious Stone (a) substitute
            public ConsumableSO Potion;          // Berry Bush (b)
            public ConsumableSO TutorPlaceholder; // Wandering Tutor (a) substitute
        }

        private const string FLAG_PREFIX = "mystery."; // §7.9.4 repeatability key

        private readonly IReadOnlyList<MysteryEventSO> _pool;
        private readonly MysteryConfigSO _config;
        private readonly GameRNG _mysteryRng;
        private readonly MysteryItemRefs _items;
        private readonly Box _box;
        private readonly EconomyConfigSO _economy;

        public MysteryEventSO SelectedEvent { get; private set; }
        // True after a Slot Booth wager that paid out (for tests / UI feedback).
        public bool LastWagerWon { get; private set; }

        public MysteryEventNodeController(
            MapNode node, RunStateSO runState,
            IReadOnlyList<MysteryEventSO> eventPool, MysteryConfigSO config, GameRNG mysteryRng,
            MysteryItemRefs items, Box box, EconomyConfigSO economy)
            : base(node, runState)
        {
            _pool       = eventPool ?? throw new ArgumentNullException(nameof(eventPool));
            _config     = config ?? throw new ArgumentNullException(nameof(config));
            _mysteryRng = mysteryRng ?? throw new ArgumentNullException(nameof(mysteryRng));
            _items      = items ?? new MysteryItemRefs();
            _box        = box;
            _economy    = economy;
        }

        protected override void OnEnter()
        {
            SelectedEvent = SelectUnfiredEvent();
            if (SelectedEvent != null)
                EventBus.Publish(new MysteryEventOfferedContext(
                    Node.Layer, Node.Lane, SelectedEvent.EventId, SelectedEvent.RiskProfile));
        }

        // 9.7.3 — resolve the chosen outcome, lock the event for the run (§7.9.4), then Complete.
        public bool Choose(int choiceIndex)
        {
            if (SelectedEvent == null) { Complete(NodeOutcome.Cleared); return false; }

            ResolveOutcome(SelectedEvent.EventId, choiceIndex);
            MarkFired(SelectedEvent.EventId);
            Complete(NodeOutcome.Cleared);
            return true;
        }

        // ── Event selection + repeatability (§7.9.1 / §7.9.4) ─────────────────

        private MysteryEventSO SelectUnfiredEvent()
        {
            List<(MysteryEventSO value, float weight)> opts = new();
            for (int i = 0; i < _pool.Count; i++)
            {
                MysteryEventSO e = _pool[i];
                if (e != null && !IsFired(e.EventId)) opts.Add((e, 1f));
            }
            return opts.Count > 0 ? _mysteryRng.PickWeighted(opts) : null;
        }

        private bool IsFired(string eventId)
        {
            if (RunState.EventFlags == null) return false;
            string key = FLAG_PREFIX + eventId;
            for (int i = 0; i < RunState.EventFlags.Count; i++)
                if (RunState.EventFlags[i].Key == key && RunState.EventFlags[i].Value != 0)
                    return true;
            return false;
        }

        private void MarkFired(string eventId)
        {
            string key = FLAG_PREFIX + eventId;
            RunState.EventFlags ??= new List<StringIntPair>();
            for (int i = 0; i < RunState.EventFlags.Count; i++)
                if (RunState.EventFlags[i].Key == key)
                {
                    RunState.EventFlags[i] = new StringIntPair { Key = key, Value = 1 };
                    return;
                }
            RunState.EventFlags.Add(new StringIntPair { Key = key, Value = 1 });
        }

        // ── Outcome resolution (§7.9.2) — code resolver keyed by EventId ──────

        private void ResolveOutcome(string eventId, int choiceIndex)
        {
            switch (eventId)
            {
                case "mysterious_stone":
                    // (a) random Evolution Item → SUBSTITUTE: random relic (gap-flagged). (b) leave.
                    if (choiceIndex == 0) GrantRandomRelic();
                    break;

                case "berry_bush":
                    if (choiceIndex == 0) HealAllBox(_config.BerryBushHealPercent);              // eat now
                    else if (choiceIndex == 1) GrantConsumables(_items.Potion, _config.BerryBushPotionCount);
                    break;

                case "wandering_tutor":
                    // (a) free Move Tutor → SUBSTITUTE: placeholder consumable (gap-flagged).
                    if (choiceIndex == 0) GrantConsumables(_items.TutorPlaceholder, 1);
                    else if (choiceIndex == 1) RunState.PokeDollars += _config.WanderingTutorDeclineDollars;
                    break;

                case "slot_booth":
                    if (choiceIndex == 0) ResolveWager();
                    break;
            }
        }

        // §7.9.2 — eat-now: restore HealPercent of EffectiveMaxHP to every Box Pokémon (caps at max).
        private void HealAllBox(int healPercent)
        {
            if (_box == null) return;
            for (int i = 0; i < _box.Members.Count; i++)
            {
                PokemonInstance p = _box.Members[i];
                if (p == null) continue;
                int eff = PokemonVitals.EffectiveMaxHP(p, _economy);
                int restored = p.CurrentHP + eff * healPercent / 100;
                p.CurrentHP = restored < eff ? restored : eff;
            }
        }

        private void GrantConsumables(ConsumableSO consumable, int count)
        {
            if (consumable == null || count <= 0) return;
            RunState.Inventory ??= new List<ConsumableSO>();
            for (int i = 0; i < count; i++) RunState.Inventory.Add(consumable);
        }

        private void GrantRandomRelic()
        {
            if (_items.StoneRelicPool == null || _items.StoneRelicPool.Count == 0) return;
            List<(RelicSO value, float weight)> opts = new();
            for (int i = 0; i < _items.StoneRelicPool.Count; i++)
                if (_items.StoneRelicPool[i] != null) opts.Add((_items.StoneRelicPool[i], 1f));
            if (opts.Count == 0) return;
            RelicSO relic = _mysteryRng.PickWeighted(opts);
            (RunState.HeldRelics ??= new List<RelicSO>()).Add(relic);
        }

        // §7.9.2 Slot Booth — wager: pay the stake, 50% (MysteryRNG) to win the payout.
        private void ResolveWager()
        {
            LastWagerWon = false;
            if (RunState.PokeDollars < _config.SlotBoothWager) return; // can't afford to play
            RunState.PokeDollars -= _config.SlotBoothWager;
            if (_mysteryRng.Range(0, 2) == 0)
            {
                RunState.PokeDollars += _config.SlotBoothWinAmount;
                LastWagerWon = true;
            }
        }
    }
}
