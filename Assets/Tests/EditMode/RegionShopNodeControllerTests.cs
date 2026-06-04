using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using ProjectAscendant.Core;
using ProjectAscendant.Map;
using UnityEngine;

namespace ProjectAscendant.Tests
{
    // Per §7.7 + Epic 9 Task 9.6 — Region Shop node: seeding, buy, re-roll.
    public class RegionShopNodeControllerTests
    {
        private string _tempDir;
        private readonly List<Object> _disposables = new();

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            _tempDir = Path.Combine(Path.GetTempPath(), "PA_Shop_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectoryOverride = _tempDir;
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            SaveSystem.SaveDirectoryOverride = null;
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, recursive: true);
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>();
            _disposables.Add(o);
            return o;
        }

        private RegionShopConfigSO MakeConfig()
        {
            RegionShopConfigSO c = Make<RegionShopConfigSO>();
            c.ConsumableSlots = 3;
            c.ConsumablePriceMin = 30; c.ConsumablePriceMax = 100;
            c.CommonRelicPrice = 150; c.UncommonRelicPrice = 300;
            c.PokeballPrice = 50;
            c.HeldItemPriceMin = 250; c.HeldItemPriceMax = 500;
            c.RerollCosts = new[] { 25, 50, 100 };
            return c;
        }

        private RunStateSO MakeRun(int dollars)
        {
            RunStateSO r = Make<RunStateSO>();
            r.PokeDollars = dollars;
            return r;
        }

        private RegionShopNodeController.ShopItemPools FullPools()
        {
            return new RegionShopNodeController.ShopItemPools
            {
                Consumables = new List<ConsumableSO> { Make<ConsumableSO>(), Make<ConsumableSO>(), Make<ConsumableSO>(), Make<ConsumableSO>() },
                CommonRelics = new List<RelicSO> { Make<RelicSO>() },
                UncommonRelics = new List<RelicSO> { Make<RelicSO>() },
                Pokeball = Make<ConsumableSO>(),
                HeldItems = new List<HeldItemSO> { Make<HeldItemSO>() },
                TMs = new List<TMSO> { },
            };
        }

        private static MapNode ShopNode() => new MapNode(2, 0, 0, NodeType.Shop);

        private RegionShopNodeController Make(RunStateSO run, RegionShopNodeController.ShopItemPools pools, uint seed = 1u)
            => new(ShopNode(), run, MakeConfig(), new GameRNG(seed), pools);

        private static int CountKind(RegionShopNodeController c, RegionShopNodeController.ShopSlotKind kind)
        {
            int n = 0;
            foreach (var s in c.Slots) if (s.Kind == kind) n++;
            return n;
        }

        // ── Seeding (9.6.1) ───────────────────────────────────────────────────

        [Test]
        public void Enter_SeedsAllSlots_WithCorrectKindsAndPrices()
        {
            RegionShopNodeController c = Make(MakeRun(0), FullPools());
            c.Enter();

            Assert.That(CountKind(c, RegionShopNodeController.ShopSlotKind.Consumable), Is.EqualTo(3));
            Assert.That(CountKind(c, RegionShopNodeController.ShopSlotKind.CommonRelic), Is.EqualTo(1));
            Assert.That(CountKind(c, RegionShopNodeController.ShopSlotKind.UncommonRelic), Is.EqualTo(1));
            Assert.That(CountKind(c, RegionShopNodeController.ShopSlotKind.Pokeball), Is.EqualTo(1));
            // Special slot is HeldItem here (TMs pool empty).
            Assert.That(CountKind(c, RegionShopNodeController.ShopSlotKind.HeldItem), Is.EqualTo(1));

            foreach (var s in c.Slots)
            {
                switch (s.Kind)
                {
                    case RegionShopNodeController.ShopSlotKind.Consumable: Assert.That(s.Price, Is.InRange(30, 100)); break;
                    case RegionShopNodeController.ShopSlotKind.CommonRelic: Assert.That(s.Price, Is.EqualTo(150)); break;
                    case RegionShopNodeController.ShopSlotKind.UncommonRelic: Assert.That(s.Price, Is.EqualTo(300)); break;
                    case RegionShopNodeController.ShopSlotKind.Pokeball: Assert.That(s.Price, Is.EqualTo(50)); break;
                    case RegionShopNodeController.ShopSlotKind.HeldItem: Assert.That(s.Price, Is.InRange(250, 500)); break;
                }
            }
        }

        [Test]
        public void Enter_DeterministicGivenSeed()
        {
            var pools = FullPools();
            RegionShopNodeController a = Make(MakeRun(0), pools, seed: 0xBEEFu);
            RegionShopNodeController b = Make(MakeRun(0), pools, seed: 0xBEEFu);
            a.Enter(); b.Enter();
            Assert.That(Serialize(a), Is.EqualTo(Serialize(b)));
        }

        private static string Serialize(RegionShopNodeController c)
        {
            StringBuilder sb = new();
            foreach (var s in c.Slots) sb.Append($"{s.Kind}:{s.Item.GetInstanceID()}:{s.Price};");
            return sb.ToString();
        }

        [Test]
        public void SpecialSlot_TMWhenOnlyTMsAvailable_UsesShopPrice()
        {
            var pools = FullPools();
            pools.HeldItems = new List<HeldItemSO>();
            TMSO tm = Make<TMSO>(); tm.ShopPrice = 400;
            pools.TMs = new List<TMSO> { tm };

            RegionShopNodeController c = Make(MakeRun(0), pools);
            c.Enter();
            Assert.That(CountKind(c, RegionShopNodeController.ShopSlotKind.TM), Is.EqualTo(1));
            foreach (var s in c.Slots)
                if (s.Kind == RegionShopNodeController.ShopSlotKind.TM)
                    Assert.That(s.Price, Is.EqualTo(400));
        }

        // ── Buy (9.6.2) ───────────────────────────────────────────────────────

        [Test]
        public void Buy_Affordable_SpendsDollars_RoutesItem_MarksPurchased()
        {
            RunStateSO run = MakeRun(1000);
            RegionShopNodeController c = Make(run, FullPools());
            c.Enter();

            // Buy the Common relic slot.
            int idx = -1;
            for (int i = 0; i < c.Slots.Count; i++)
                if (c.Slots[i].Kind == RegionShopNodeController.ShopSlotKind.CommonRelic) idx = i;

            int before = run.PokeDollars;
            bool ok = c.Buy(idx);
            Assert.That(ok, Is.True);
            Assert.That(run.PokeDollars, Is.EqualTo(before - 150));
            Assert.That(run.HeldRelics, Has.Count.EqualTo(1));
            Assert.That(c.Slots[idx].Purchased, Is.True);

            // Second buy of same slot fails.
            Assert.That(c.Buy(idx), Is.False);
        }

        [Test]
        public void Buy_HeldItem_RoutesToOwnedHeldItems()
        {
            RunStateSO run = MakeRun(1000);
            RegionShopNodeController c = Make(run, FullPools());
            c.Enter();
            int idx = -1;
            for (int i = 0; i < c.Slots.Count; i++)
                if (c.Slots[i].Kind == RegionShopNodeController.ShopSlotKind.HeldItem) idx = i;

            Assert.That(c.Buy(idx), Is.True);
            Assert.That(run.OwnedHeldItems, Has.Count.EqualTo(1));
        }

        [Test]
        public void Buy_Pokeball_IncrementsPokeballCount_NotInventory()
        {
            // §7.3.4 (Option 1) — a bought Pokéball adds to the counted resource, not the inventory.
            RunStateSO run = MakeRun(1000);
            RegionShopNodeController c = Make(run, FullPools());
            c.Enter();
            int idx = -1;
            for (int i = 0; i < c.Slots.Count; i++)
                if (c.Slots[i].Kind == RegionShopNodeController.ShopSlotKind.Pokeball) idx = i;

            Assert.That(c.Buy(idx), Is.True);
            Assert.That(run.PokeballCount, Is.EqualTo(1));
            Assert.That(run.Inventory == null || run.Inventory.Count == 0, Is.True,
                "Pokéball must not land in the non-expendable consumable inventory.");
        }

        [Test]
        public void Buy_Unaffordable_NoChange()
        {
            RunStateSO run = MakeRun(10); // can't afford anything but maybe a cheap consumable? consumables >=30
            RegionShopNodeController c = Make(run, FullPools());
            c.Enter();
            int idx = -1;
            for (int i = 0; i < c.Slots.Count; i++)
                if (c.Slots[i].Kind == RegionShopNodeController.ShopSlotKind.UncommonRelic) idx = i; // 300
            Assert.That(c.Buy(idx), Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(10));
            Assert.That(c.Slots[idx].Purchased, Is.False);
        }

        // ── Re-roll (9.6.3) ───────────────────────────────────────────────────

        [Test]
        public void Reroll_EscalatingCost_MaxThree()
        {
            RunStateSO run = MakeRun(1000);
            RegionShopNodeController c = Make(run, FullPools());
            c.Enter();

            Assert.That(c.NextRerollCost, Is.EqualTo(25));
            Assert.That(c.TryReroll(), Is.True);
            Assert.That(run.PokeDollars, Is.EqualTo(975));

            Assert.That(c.NextRerollCost, Is.EqualTo(50));
            Assert.That(c.TryReroll(), Is.True);
            Assert.That(run.PokeDollars, Is.EqualTo(925));

            Assert.That(c.NextRerollCost, Is.EqualTo(100));
            Assert.That(c.TryReroll(), Is.True);
            Assert.That(run.PokeDollars, Is.EqualTo(825));

            // Fourth re-roll refused (max 3, §7.7.2).
            Assert.That(c.NextRerollCost, Is.EqualTo(-1));
            Assert.That(c.TryReroll(), Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(825));
        }

        [Test]
        public void Reroll_Unaffordable_NoChange()
        {
            RunStateSO run = MakeRun(10);
            RegionShopNodeController c = Make(run, FullPools());
            c.Enter();
            Assert.That(c.TryReroll(), Is.False);
            Assert.That(run.PokeDollars, Is.EqualTo(10));
            Assert.That(c.RerollCount, Is.EqualTo(0));
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        [Test]
        public void Leave_CompletesCleared()
        {
            RegionShopNodeController c = Make(MakeRun(0), FullPools());
            c.Enter();
            c.Leave();
            Assert.That(c.Outcome, Is.EqualTo(NodeOutcome.Cleared));
        }

        [Test]
        public void Enter_SavesRun()
        {
            RegionShopNodeController c = Make(MakeRun(0), FullPools());
            c.Enter();
            Assert.That(File.Exists(Path.Combine(_tempDir, "run-current.dat")), Is.True);
        }
    }
}
