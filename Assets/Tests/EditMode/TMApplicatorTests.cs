using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per §5.4.1 + Epic 10 Task 10.6 — TM application (compatibility, pool-add, consume, Mastery exempt).
    // Updated for §5.10 (approved 2026-06-02, pending Notion lock): TMs add to the Learned Move Pool.
    public class TMApplicatorTests
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

        private PokemonSpeciesSO Species() => Make<PokemonSpeciesSO>();
        private MoveSO Move() => Make<MoveSO>();

        private (TMSO tm, MoveSO taught) Tm(params PokemonSpeciesSO[] compatible)
        {
            MoveSO taught = Move();
            TMSO tm = Make<TMSO>();
            tm.MoveTeach = taught;
            tm.CompatibleSpecies = new List<PokemonSpeciesSO>(compatible);
            return (tm, taught);
        }

        private PokemonInstance Mon(PokemonSpeciesSO sp, params MoveSO[] moves)
        {
            PokemonInstance p = new() { Species = sp, Level = 5, CurrentHP = 20 };
            p.CurrentMoves.AddRange(moves);
            // §5.10 — seed the pool with the same moves (mimics factory seeding from BaseLearnset)
            p.LearnedMoves.AddRange(moves);
            return p;
        }

        [Test]
        public void Apply_Compatible_AddsToPool_ConsumesTM_MasteryUntouched()
        {
            PokemonSpeciesSO sp = Species();
            MoveSO a = Move(), b = Move(), mastery = Move();
            (TMSO tm, MoveSO taught) = Tm(sp);
            PokemonInstance p = Mon(sp, a, b);
            p.MasteryMove = mastery;
            RunStateSO run = Make<RunStateSO>();
            run.OwnedTMs = new List<TMSO> { tm };

            bool ok = TMApplicator.Apply(run, tm, p);

            Assert.That(ok, Is.True);
            Assert.That(p.LearnedMoves, Contains.Item(taught), "TM move added to pool (§5.10.1).");
            Assert.That(p.LearnedMoves.Count, Is.EqualTo(3), "Pool grew by 1 (2 → 3).");
            Assert.That(p.MasteryMove, Is.SameAs(mastery), "Mastery slot exempt (§4.3.9.2).");
            Assert.That(run.OwnedTMs, Has.No.Member(tm), "TM consumed (single-use).");
        }

        [Test]
        public void Apply_Incompatible_NoChange()
        {
            PokemonSpeciesSO other = Species();
            (TMSO tm, _) = Tm(Species()); // compatible with a different species
            PokemonInstance p = Mon(other, Move(), Move());
            RunStateSO run = Make<RunStateSO>();
            run.OwnedTMs = new List<TMSO> { tm };

            Assert.That(TMApplicator.IsCompatible(tm, p), Is.False);
            Assert.That(TMApplicator.Apply(run, tm, p), Is.False);
            Assert.That(run.OwnedTMs, Has.Member(tm), "Failed apply does not consume the TM.");
        }

        [Test]
        public void Apply_Duplicate_Rejected()
        {
            PokemonSpeciesSO sp = Species();
            (TMSO tm, MoveSO taught) = Tm(sp);
            PokemonInstance p = Mon(sp, taught, Move()); // already knows the taught move (in pool)
            RunStateSO run = Make<RunStateSO>();
            run.OwnedTMs = new List<TMSO> { tm };

            Assert.That(TMApplicator.Apply(run, tm, p), Is.False, "Already-known move (§5.10.1 dedup).");
            Assert.That(p.LearnedMoves.Count, Is.EqualTo(2), "Pool unchanged.");
            Assert.That(run.OwnedTMs, Has.Member(tm), "Failed apply does not consume the TM.");
        }
    }
}
