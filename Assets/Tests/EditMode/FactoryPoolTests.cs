using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §9.6 + Task 2.4.7 — Pool<T> and factory unit tests.
    public class FactoryPoolTests
    {
        // ── Pool<T> test helper ────────────────────────────────────────────────────

        private sealed class PoolTestObj
        {
            public bool WasReset;
        }

        // ── Pool<T> ───────────────────────────────────────────────────────────────

        [Test]
        public void Pool_Rent_WithPrewarm_ReturnsCachedInstance()
        {
            // Per Task 2.4.1 — Rent returns a prewarm instance (non-null).
            Pool<PoolTestObj> pool = new(initialCapacity: 2);
            PoolTestObj a = pool.Rent();
            Assert.That(a, Is.Not.Null);
        }

        [Test]
        public void Pool_Rent_ExceedsInitialCapacity_CreatesNew()
        {
            // Per Task 2.4.1 — Rent beyond prewarm creates a distinct new instance.
            Pool<PoolTestObj> pool = new(initialCapacity: 1);
            PoolTestObj a = pool.Rent(); // from prewarm
            PoolTestObj b = pool.Rent(); // fresh allocation
            Assert.That(b, Is.Not.Null);
            Assert.That(b, Is.Not.SameAs(a));
        }

        [Test]
        public void Pool_RentAfterReturn_ReusesInstance()
        {
            // Per Task 2.4.1 — object returned to pool is handed out again on next Rent.
            Pool<PoolTestObj> pool = new(initialCapacity: 0);
            PoolTestObj a = pool.Rent(); // fresh allocation (pool is empty)
            pool.Return(a);
            PoolTestObj b = pool.Rent(); // should receive the same reference
            Assert.That(b, Is.SameAs(a));
        }

        [Test]
        public void Pool_Return_IncreasesFreeCount()
        {
            // Per Task 2.4.1 — FreeCount grows after Return.
            Pool<PoolTestObj> pool = new(initialCapacity: 0);
            PoolTestObj a = pool.Rent();
            Assert.That(pool.FreeCount, Is.EqualTo(0));
            pool.Return(a);
            Assert.That(pool.FreeCount, Is.EqualTo(1));
        }

        [Test]
        public void Pool_Return_CallsResetHook()
        {
            // Per Task 2.4.1 — the resetHook fires on every Return.
            Pool<PoolTestObj> pool = new(initialCapacity: 0, resetHook: o => o.WasReset = true);
            PoolTestObj a = pool.Rent();
            a.WasReset = false;
            pool.Return(a);
            Assert.That(a.WasReset, Is.True);
        }

        [Test]
        public void Pool_Return_AtMaxCapacity_DiscardsItem()
        {
            // Per Task 2.4.1 — items returned when pool is full are discarded (FreeCount stays capped).
            Pool<PoolTestObj> pool = new(initialCapacity: 0, maxCapacity: 1);
            PoolTestObj a = pool.Rent();
            PoolTestObj b = pool.Rent();
            pool.Return(a); // accepted — pool now full
            pool.Return(b); // discarded — over maxCapacity
            Assert.That(pool.FreeCount, Is.EqualTo(1));
        }

        [Test]
        public void Pool_Return_Null_IsNoOp()
        {
            // Per Task 2.4.1 — returning null must not throw.
            Pool<PoolTestObj> pool = new(initialCapacity: 0);
            Assert.DoesNotThrow(() => pool.Return(null));
        }

        // ── PokemonInstanceFactory ────────────────────────────────────────────────

        private PokemonSpeciesSO _species;

        [SetUp]
        public void SetUp()
        {
            _species = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            _species.SpeciesId = "bulbasaur";
            _species.BaseStats = new BaseStats { BaseHP = 45 };
        }

        [TearDown]
        public void TearDown()
        {
            if (_species != null)
                Object.DestroyImmediate(_species);
        }

        [Test]
        public void PokemonInstanceFactory_Create_SetsSpeciesAndLevel()
        {
            // Per Task 2.4.2 — factory populates Species and Level.
            PokemonInstanceFactory factory = new();
            PokemonInstance instance = factory.Create(_species, level: 5);
            Assert.That(instance.Species, Is.SameAs(_species));
            Assert.That(instance.Level, Is.EqualTo(5));
            factory.Release(instance);
        }

        [Test]
        public void PokemonInstanceFactory_Create_ComputesCurrentHP()
        {
            // Per §9.3.2.6 stub formula: BaseHP(45) + Level(5)*2 = 55.
            // TODO: Epic 4 — update test when real HP formula is in BattleConfigSO.
            PokemonInstanceFactory factory = new();
            PokemonInstance instance = factory.Create(_species, level: 5);
            Assert.That(instance.CurrentHP, Is.EqualTo(55));
            factory.Release(instance);
        }

        [Test]
        public void PokemonInstanceFactory_Release_ReturnedToPool()
        {
            // Per Task 2.4.2 — Release returns instance to pool (FreeCount recovers).
            PokemonInstanceFactory factory = new();
            int before = factory.PoolFreeCount;
            PokemonInstance instance = factory.Create(_species, 1);
            Assert.That(factory.PoolFreeCount, Is.EqualTo(before - 1));
            factory.Release(instance);
            Assert.That(factory.PoolFreeCount, Is.EqualTo(before));
        }

        [Test]
        public void PokemonInstanceFactory_Release_ResetsCurrentHP()
        {
            // Per §2.4.1 — after Reset, CurrentHP is 0 (the fainted state).
            PokemonInstanceFactory factory = new();
            PokemonInstance instance = factory.Create(_species, 5);
            factory.Release(instance);
            Assert.That(instance.CurrentHP, Is.EqualTo(0));
        }

        [Test]
        public void PokemonInstanceFactory_ReleaseAndCreate_ReusesInstance()
        {
            // Per Task 2.4.7 — pool reuse: Release then Create returns the same reference.
            PokemonInstanceFactory factory = new();

            // Drain prewarm pool so the next Create() is a fresh allocation we can track.
            int prewarmed = factory.PoolFreeCount;
            List<PokemonInstance> drained = new(prewarmed);
            for (int i = 0; i < prewarmed; i++)
                drained.Add(factory.Create(_species, 1));

            PokemonInstance a = factory.Create(_species, 5); // fresh allocation (pool empty)
            factory.Release(a);                               // 'a' goes to pool top
            PokemonInstance b = factory.Create(_species, 3); // LIFO → should get 'a'

            Assert.That(b, Is.SameAs(a));

            factory.Release(b);
            foreach (PokemonInstance p in drained) factory.Release(p);
        }

        // ── IntentDataFactory ─────────────────────────────────────────────────────

        [Test]
        public void IntentDataFactory_Create_SetsTargetSlotAndActionId()
        {
            // Per §4.3.2 — intents target slot indices, not Pokémon references.
            IntentDataFactory factory = new();
            IntentData data = factory.Create(targetSlotIndex: 2, actionId: 99, priority: 1);
            Assert.That(data.TargetSlotIndex, Is.EqualTo(2));
            Assert.That(data.ActionId, Is.EqualTo(99));
            Assert.That(data.Priority, Is.EqualTo(1));
            factory.Release(data);
        }

        [Test]
        public void IntentDataFactory_Release_ReturnedToPool()
        {
            // Per Task 2.4.4 — released IntentData goes back to pool.
            IntentDataFactory factory = new();
            int before = factory.PoolFreeCount;
            IntentData data = factory.Create(0, 0);
            factory.Release(data);
            Assert.That(factory.PoolFreeCount, Is.EqualTo(before));
        }

        // ── DamageContextFactory ──────────────────────────────────────────────────

        [Test]
        public void DamageContextFactory_Rent_ReturnsCleanInstance()
        {
            // Per Task 2.4.5 — rented DamageContextData is reset to safe defaults.
            DamageContextFactory factory = new();
            DamageContextData data = factory.Rent();
            Assert.That(data.SlotIndex, Is.EqualTo(-1));
            Assert.That(data.TypeMultiplier, Is.EqualTo(1f));
            Assert.That(data.IsCrit, Is.False);
            factory.Return(data);
        }

        [Test]
        public void DamageContextFactory_Return_ReturnedToPool()
        {
            // Per Task 2.4.5 — Return increases FreeCount.
            DamageContextFactory factory = new();
            int before = factory.PoolFreeCount;
            DamageContextData data = factory.Rent();
            factory.Return(data);
            Assert.That(factory.PoolFreeCount, Is.EqualTo(before));
        }

        [Test]
        public void DamageContextData_ToEventPayload_MapsAllFields()
        {
            // Per §9.4.1.1 — ToEventPayload converts mutable context to immutable DamageContext struct.
            DamageContextData data = new()
            {
                SlotIndex = 1, BaseDamage = 30, FinalDamage = 45,
                IsCrit = true, IsStab = true, TypeMultiplier = 2f
            };
            DamageContext payload = data.ToEventPayload();
            Assert.That(payload.SlotIndex, Is.EqualTo(1));
            Assert.That(payload.BaseDamage, Is.EqualTo(30));
            Assert.That(payload.FinalDamage, Is.EqualTo(45));
            Assert.That(payload.IsCrit, Is.True);
            Assert.That(payload.IsStab, Is.True);
            Assert.That(payload.TypeMultiplier, Is.EqualTo(2f));
        }

        // ── EnemyInstanceFactory ──────────────────────────────────────────────────

        [Test]
        public void EnemyInstanceFactory_Create_SetsHPAndId()
        {
            // Per Task 2.4.6 — factory initialises EnemyInstance correctly.
            EnemyInstanceFactory factory = new();
            EnemyInstance enemy = factory.Create("geodude_wild", maxHP: 80);
            Assert.That(enemy.EnemyId, Is.EqualTo("geodude_wild"));
            Assert.That(enemy.CurrentHP, Is.EqualTo(80));
            Assert.That(enemy.MaxHP, Is.EqualTo(80));
            factory.Release(enemy);
        }

        [Test]
        public void EnemyInstanceFactory_Release_ReturnedToPool()
        {
            // Per Task 2.4.6 — released EnemyInstance returns to small pool.
            EnemyInstanceFactory factory = new();
            int before = factory.PoolFreeCount;
            EnemyInstance enemy = factory.Create("x", 10);
            factory.Release(enemy);
            Assert.That(factory.PoolFreeCount, Is.EqualTo(before));
        }

        // ── MoveCardInstanceFactory ───────────────────────────────────────────────

        [Test]
        public void MoveCardInstanceFactory_Create_SetsMove()
        {
            // Per Task 2.4.3 + §9.17 — factory creates instance with the given move (no pool for VS).
            MoveSO move = ScriptableObject.CreateInstance<MoveSO>();
            move.MoveId = "tackle";
            MoveCardInstanceFactory factory = new();
            MoveCardInstance card = factory.Create(move);
            Assert.That(card.Move, Is.SameAs(move));
            Assert.That(card.IsExhausted, Is.False);
            Object.DestroyImmediate(move);
        }
    }
}
