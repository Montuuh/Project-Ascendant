using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per §5.10 (approved 2026-06-02, pending Notion lock) — Learned Move Pool + active-4 configuration.
    // Tests: pool growth (dedup), in-place upgrades (reslot if active), active-4 validation (must be in
    // pool; Mastery immutable §4.3.9.2), swap, paid reconfigure gate (deduct from RunState).
    public class MoveLoadoutServiceTests
    {
        private readonly List<Object> _disposables = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private T Make<T>() where T : ScriptableObject
        {
            T o = ScriptableObject.CreateInstance<T>(); _disposables.Add(o); return o;
        }

        private MoveSO Move() => Make<MoveSO>();

        private PokemonInstance Mon(params MoveSO[] poolMoves)
        {
            PokemonInstance p = new() { Level = 5, CurrentHP = 20 };
            foreach (MoveSO m in poolMoves) p.LearnedMoves.Add(m);
            return p;
        }

        [Test]
        public void AddToPool_NewMove_Adds()
        {
            MoveSO a = Move(), b = Move();
            PokemonInstance p = Mon(a);

            MoveLoadoutService.AddToPool(p, b);

            Assert.That(p.LearnedMoves, Is.EquivalentTo(new[] { a, b }), "New move added to pool.");
        }

        [Test]
        public void AddToPool_DuplicateMove_Ignored()
        {
            MoveSO a = Move();
            PokemonInstance p = Mon(a);

            MoveLoadoutService.AddToPool(p, a);

            Assert.That(p.LearnedMoves.Count, Is.EqualTo(1), "Duplicate add ignored (§5.10.1 dedup).");
        }

        [Test]
        public void UpgradePoolMove_OldMoveNotActive_ReplacesInPool()
        {
            MoveSO a = Move(), b = Move(), aEvolved = Move();
            PokemonInstance p = Mon(a, b);
            p.CurrentMoves.AddRange(new[] { b }); // active: b only; a is benched in pool

            bool ok = MoveLoadoutService.UpgradePoolMove(p, a, aEvolved);

            Assert.That(ok, Is.True);
            Assert.That(p.LearnedMoves, Contains.Item(aEvolved), "Pool entry replaced by evolved version.");
            Assert.That(p.LearnedMoves, Has.No.Member(a), "Old version removed from pool.");
            Assert.That(p.CurrentMoves, Is.EquivalentTo(new[] { b }), "Active unchanged (old move not active).");
        }

        [Test]
        public void UpgradePoolMove_OldMoveActive_ReslotNewMove()
        {
            MoveSO a = Move(), b = Move(), aEvolved = Move();
            PokemonInstance p = Mon(a, b);
            p.CurrentMoves.AddRange(new[] { a, b }); // active: a is in slot 0

            bool ok = MoveLoadoutService.UpgradePoolMove(p, a, aEvolved);

            Assert.That(ok, Is.True);
            Assert.That(p.LearnedMoves, Contains.Item(aEvolved), "Pool entry replaced.");
            Assert.That(p.CurrentMoves[0], Is.SameAs(aEvolved), "New version took active slot (§5.10.3).");
            Assert.That(p.CurrentMoves[1], Is.SameAs(b), "Other active slot unchanged.");
        }

        [Test]
        public void UpgradePoolMove_OldMoveNotInPool_Rejected()
        {
            MoveSO a = Move(), notInPool = Move(), evolved = Move();
            PokemonInstance p = Mon(a);

            bool ok = MoveLoadoutService.UpgradePoolMove(p, notInPool, evolved);

            Assert.That(ok, Is.False, "Upgrade rejected if old move not in pool.");
            Assert.That(p.LearnedMoves, Is.EquivalentTo(new[] { a }), "Pool unchanged.");
        }

        [Test]
        public void ValidateActiveLoadout_AllInPool_Valid()
        {
            MoveSO a = Move(), b = Move(), c = Move(), d = Move();
            PokemonInstance p = Mon(a, b, c, d);

            bool ok = MoveLoadoutService.ValidateActiveLoadout(p, new List<MoveSO> { a, b, c, d });

            Assert.That(ok, Is.True, "All moves in pool → valid.");
        }

        [Test]
        public void ValidateActiveLoadout_NotInPool_Invalid()
        {
            MoveSO a = Move(), b = Move(), c = Move(), d = Move(), notInPool = Move();
            PokemonInstance p = Mon(a, b, c);

            bool ok = MoveLoadoutService.ValidateActiveLoadout(p, new List<MoveSO> { a, b, c, notInPool });

            Assert.That(ok, Is.False, "Move not in pool → invalid.");
        }

        [Test]
        public void ValidateActiveLoadout_IncludesMasteryMove_Invalid()
        {
            MoveSO a = Move(), b = Move(), c = Move(), mastery = Move();
            PokemonInstance p = Mon(a, b, c, mastery);
            p.MasteryMove = mastery;

            bool ok = MoveLoadoutService.ValidateActiveLoadout(p, new List<MoveSO> { a, b, c, mastery });

            Assert.That(ok, Is.False, "Mastery move is immutable, not part of active 4 (§4.3.9.2).");
        }

        [Test]
        public void ValidateActiveLoadout_Not4Moves_Invalid()
        {
            MoveSO a = Move(), b = Move(), c = Move();
            PokemonInstance p = Mon(a, b, c);

            Assert.That(MoveLoadoutService.ValidateActiveLoadout(p, new List<MoveSO> { a, b }), Is.False, "Not 4 moves.");
            Assert.That(MoveLoadoutService.ValidateActiveLoadout(p, new List<MoveSO> { a, b, c }), Is.False, "Not 4 moves.");
        }

        [Test]
        public void SetActiveMoves_ValidLoadout_Replaces()
        {
            MoveSO a = Move(), b = Move(), c = Move(), d = Move(), e = Move();
            PokemonInstance p = Mon(a, b, c, d, e);
            p.CurrentMoves.AddRange(new[] { a, b, c, d }); // old active

            bool ok = MoveLoadoutService.SetActiveMoves(p, new List<MoveSO> { b, c, d, e });

            Assert.That(ok, Is.True);
            Assert.That(p.CurrentMoves, Is.EquivalentTo(new[] { b, c, d, e }), "Active 4 replaced.");
        }

        [Test]
        public void SetActiveMoves_InvalidLoadout_Rejected()
        {
            MoveSO a = Move(), notInPool = Move();
            PokemonInstance p = Mon(a);
            p.CurrentMoves.Add(a);

            bool ok = MoveLoadoutService.SetActiveMoves(p, new List<MoveSO> { a, notInPool, Move(), Move() });

            Assert.That(ok, Is.False, "Invalid loadout rejected.");
            Assert.That(p.CurrentMoves, Is.EquivalentTo(new[] { a }), "CurrentMoves unchanged.");
        }

        [Test]
        public void SwapActiveMove_ValidSwap_Replaces()
        {
            MoveSO a = Move(), b = Move(), c = Move(), d = Move(), e = Move();
            PokemonInstance p = Mon(a, b, c, d, e);
            p.CurrentMoves.AddRange(new[] { a, b, c, d }); // e is benched

            bool ok = MoveLoadoutService.SwapActiveMove(p, fromPool: e, toReplaceInActive: b);

            Assert.That(ok, Is.True);
            Assert.That(p.CurrentMoves, Is.EquivalentTo(new[] { a, e, c, d }), "b → e in active.");
        }

        [Test]
        public void SwapActiveMove_MoveNotInPool_Rejected()
        {
            MoveSO a = Move(), notInPool = Move();
            PokemonInstance p = Mon(a);
            p.CurrentMoves.Add(a);

            bool ok = MoveLoadoutService.SwapActiveMove(p, fromPool: notInPool, toReplaceInActive: a);

            Assert.That(ok, Is.False, "Move not in pool → rejected.");
        }

        [Test]
        public void SwapActiveMove_MasteryMove_Rejected()
        {
            MoveSO a = Move(), mastery = Move();
            PokemonInstance p = Mon(a, mastery);
            p.CurrentMoves.Add(a);
            p.MasteryMove = mastery;

            bool ok = MoveLoadoutService.SwapActiveMove(p, fromPool: mastery, toReplaceInActive: a);

            Assert.That(ok, Is.False, "Mastery move cannot be activated (§4.3.9.2).");
        }

        [Test]
        public void DeductReconfigCost_Affordable_Deducts()
        {
            RunStateSO run = Make<RunStateSO>();
            run.PokeDollars = 100;
            EconomyConfigSO econ = Make<EconomyConfigSO>();
            econ.MoveReconfigCost = 50;

            bool ok = MoveLoadoutService.DeductReconfigCost(run, econ);

            Assert.That(ok, Is.True);
            Assert.That(run.PokeDollars, Is.EqualTo(50), "50₽ deducted (§5.10).");
        }

        [Test]
        public void DeductReconfigCost_Unaffordable_Rejected()
        {
            RunStateSO run = Make<RunStateSO>();
            run.PokeDollars = 25;
            EconomyConfigSO econ = Make<EconomyConfigSO>();
            econ.MoveReconfigCost = 50;

            bool ok = MoveLoadoutService.DeductReconfigCost(run, econ);

            Assert.That(ok, Is.False, "Insufficient Poké Dollars → rejected.");
            Assert.That(run.PokeDollars, Is.EqualTo(25), "No deduction on failure.");
        }

        [Test]
        public void IsInPool_MoveInPool_True()
        {
            MoveSO a = Move();
            PokemonInstance p = Mon(a);

            Assert.That(MoveLoadoutService.IsInPool(p, a), Is.True);
        }

        [Test]
        public void IsInPool_MoveNotInPool_False()
        {
            MoveSO a = Move(), notInPool = Move();
            PokemonInstance p = Mon(a);

            Assert.That(MoveLoadoutService.IsInPool(p, notInPool), Is.False);
        }
    }
}
