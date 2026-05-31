using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per §5.4.1 + Epic 10 Task 10.6 — TM application (compatibility, slot-replace, consume, Mastery exempt).
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
            return p;
        }

        [Test]
        public void Apply_Compatible_ReplacesSlot_ConsumesTM_MasteryUntouched()
        {
            PokemonSpeciesSO sp = Species();
            MoveSO a = Move(), b = Move(), mastery = Move();
            (TMSO tm, MoveSO taught) = Tm(sp);
            PokemonInstance p = Mon(sp, a, b);
            p.MasteryMove = mastery;
            RunStateSO run = Make<RunStateSO>();
            run.OwnedTMs = new List<TMSO> { tm };

            bool ok = TMApplicator.Apply(run, tm, p, slotIndex: 1);

            Assert.That(ok, Is.True);
            Assert.That(p.CurrentMoves[1], Is.SameAs(taught), "Slot replaced by the TM move.");
            Assert.That(p.CurrentMoves[0], Is.SameAs(a), "Other slot retained.");
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
            Assert.That(TMApplicator.Apply(run, tm, p, 0), Is.False);
            Assert.That(run.OwnedTMs, Has.Member(tm), "Failed apply does not consume the TM.");
        }

        [Test]
        public void Apply_BadSlotOrDuplicate_Rejected()
        {
            PokemonSpeciesSO sp = Species();
            (TMSO tm, MoveSO taught) = Tm(sp);
            PokemonInstance p = Mon(sp, taught, Move()); // already knows the taught move
            RunStateSO run = Make<RunStateSO>();
            run.OwnedTMs = new List<TMSO> { tm };

            Assert.That(TMApplicator.Apply(run, tm, p, slotIndex: 9), Is.False, "Out-of-range slot.");
            Assert.That(TMApplicator.Apply(run, tm, p, slotIndex: 1), Is.False, "Already-known move.");
        }
    }
}
