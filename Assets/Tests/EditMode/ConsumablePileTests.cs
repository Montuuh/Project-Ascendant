using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Deck;

namespace ProjectAscendant.Tests
{
    // Per §3.5 + Epic 5 Task 5.1.2 / 5.3.3 — ConsumablePile behaviour:
    // build from inventory, draw via seeded RNG, mark-used once-per-combat,
    // restore at combat end.
    public class ConsumablePileTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _disposables.Count; i++)
                if (_disposables[i] != null) Object.DestroyImmediate(_disposables[i]);
            _disposables.Clear();
        }

        private ConsumableSO MakeConsumable(string id)
        {
            ConsumableSO c = ScriptableObject.CreateInstance<ConsumableSO>();
            c.name = id;
            _disposables.Add(c);
            return c;
        }

        // ── Build / Available ────────────────────────────────────────────────

        [Test]
        public void Build_NullInventory_AvailableZero()
        {
            ConsumablePile pile = new();
            pile.Build(null);
            Assert.That(pile.AvailableCount, Is.EqualTo(0));
        }

        [Test]
        public void Build_ThreeConsumables_AvailableThree()
        {
            ConsumablePile pile = new();
            List<ConsumableSO> inv = new() { MakeConsumable("a"), MakeConsumable("b"), MakeConsumable("c") };
            pile.Build(inv);
            Assert.That(pile.AvailableCount, Is.EqualTo(3));
        }

        // ── Draw (§3.2.2 — 2 consumable cards per turn) ──────────────────────

        [Test]
        public void DrawHand_TwoFromThree_ReturnsTwo()
        {
            ConsumablePile pile = new();
            List<ConsumableSO> inv = new() { MakeConsumable("a"), MakeConsumable("b"), MakeConsumable("c") };
            pile.Build(inv);
            GameRNG rng = new(seed: 11);
            List<ConsumableSO> hand = pile.DrawHand(2, rng);
            Assert.That(hand.Count, Is.EqualTo(2));
        }

        [Test]
        public void DrawHand_RequestMoreThanAvailable_ReturnsAvailable()
        {
            ConsumablePile pile = new();
            pile.Build(new List<ConsumableSO> { MakeConsumable("a") });
            List<ConsumableSO> hand = pile.DrawHand(5, new GameRNG(seed: 11));
            Assert.That(hand.Count, Is.EqualTo(1));
        }

        [Test]
        public void DrawHand_DoesNotMutateInventory()
        {
            // Per §3.5 — drawing is non-consuming. Drawing a consumable into
            // the hand does NOT mark it as used (only playing it does, via
            // MarkUsed). So a second draw can see the same consumable.
            ConsumablePile pile = new();
            ConsumableSO only = MakeConsumable("only");
            pile.Build(new List<ConsumableSO> { only });
            GameRNG rng = new(seed: 11);
            List<ConsumableSO> h1 = pile.DrawHand(1, rng);
            List<ConsumableSO> h2 = pile.DrawHand(1, rng);
            Assert.That(h1[0], Is.SameAs(only));
            Assert.That(h2[0], Is.SameAs(only));
            Assert.That(pile.AvailableCount, Is.EqualTo(1));
        }

        [Test]
        public void DrawHand_SkipsUsedConsumables()
        {
            ConsumablePile pile = new();
            ConsumableSO a = MakeConsumable("a");
            ConsumableSO b = MakeConsumable("b");
            pile.Build(new List<ConsumableSO> { a, b });
            pile.MarkUsed(a);
            // Only b remains available — repeated draws always return b.
            for (int i = 0; i < 5; i++)
            {
                List<ConsumableSO> hand = pile.DrawHand(2, new GameRNG(seed: (uint)i + 1));
                Assert.That(hand.Count, Is.EqualTo(1));
                Assert.That(hand[0], Is.SameAs(b));
            }
        }

        [Test]
        public void DrawHand_NullEntryInInventory_Skipped()
        {
            ConsumablePile pile = new();
            pile.Build(new List<ConsumableSO> { MakeConsumable("a"), null, MakeConsumable("c") });
            List<ConsumableSO> hand = pile.DrawHand(5, new GameRNG(seed: 11));
            Assert.That(hand.Count, Is.EqualTo(2));
            Assert.That(hand, Has.No.Member(null));
        }

        // ── MarkUsed (§3.5 once-per-combat) ──────────────────────────────────

        [Test]
        public void MarkUsed_OnlyOnce_NoDuplicate()
        {
            ConsumablePile pile = new();
            ConsumableSO a = MakeConsumable("a");
            pile.Build(new List<ConsumableSO> { a });
            pile.MarkUsed(a);
            pile.MarkUsed(a);
            pile.MarkUsed(a);
            Assert.That(pile.UsedThisCombat.Count, Is.EqualTo(1));
            Assert.That(pile.IsUsed(a), Is.True);
        }

        [Test]
        public void MarkUsed_AvailableCountReflectsUsed()
        {
            ConsumablePile pile = new();
            ConsumableSO a = MakeConsumable("a");
            ConsumableSO b = MakeConsumable("b");
            pile.Build(new List<ConsumableSO> { a, b });
            pile.MarkUsed(a);
            Assert.That(pile.AvailableCount, Is.EqualTo(1));
        }

        [Test]
        public void MarkUsed_NullInput_NoThrow()
        {
            ConsumablePile pile = new();
            pile.Build(new List<ConsumableSO> { MakeConsumable("a") });
            Assert.DoesNotThrow(() => pile.MarkUsed(null));
            Assert.That(pile.UsedThisCombat.Count, Is.EqualTo(0));
        }

        // ── RestoreAll (§3.5 combat-end restoration) ─────────────────────────

        [Test]
        public void RestoreAll_ClearsUsedList_InventoryUntouched()
        {
            ConsumablePile pile = new();
            List<ConsumableSO> inv = new() { MakeConsumable("a"), MakeConsumable("b") };
            pile.Build(inv);
            pile.MarkUsed(inv[0]);
            pile.MarkUsed(inv[1]);
            Assert.That(pile.AvailableCount, Is.EqualTo(0));

            pile.RestoreAll();
            Assert.That(pile.UsedThisCombat.Count, Is.EqualTo(0));
            Assert.That(pile.AvailableCount, Is.EqualTo(2));
            // The original inventory list is untouched (still 2 items, same refs).
            Assert.That(inv.Count, Is.EqualTo(2));
        }

        // ── Determinism ──────────────────────────────────────────────────────

        [Test]
        public void DrawHand_SameSeed_SameSequence()
        {
            ConsumablePile p1 = new();
            ConsumablePile p2 = new();
            ConsumableSO a = MakeConsumable("a");
            ConsumableSO b = MakeConsumable("b");
            ConsumableSO c = MakeConsumable("c");
            ConsumableSO d = MakeConsumable("d");
            p1.Build(new List<ConsumableSO> { a, b, c, d });
            p2.Build(new List<ConsumableSO> { a, b, c, d });
            List<ConsumableSO> h1 = p1.DrawHand(3, new GameRNG(seed: 0xCAFE));
            List<ConsumableSO> h2 = p2.DrawHand(3, new GameRNG(seed: 0xCAFE));
            CollectionAssert.AreEqual(h1, h2);
        }
    }
}
