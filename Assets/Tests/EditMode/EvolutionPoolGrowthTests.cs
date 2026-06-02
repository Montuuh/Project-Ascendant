using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Progression;

namespace ProjectAscendant.Tests
{
    // Per §5.3.5 + §5.10 (approved 2026-06-02, pending Notion lock) — evolution pool mechanics.
    // Tests: in-place upgrades (reslot if active per §5.10.3), additions (grow pool per §5.10.1),
    // branch interplay with MoveLoadoutService.
    public class EvolutionPoolGrowthTests
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
        private PokemonSpeciesSO Species() => Make<PokemonSpeciesSO>();

        private PokemonInstance Mon(PokemonSpeciesSO sp, params MoveSO[] poolMoves)
        {
            PokemonInstance p = new() { Species = sp, Level = 5, CurrentHP = 20 };
            foreach (MoveSO m in poolMoves) p.LearnedMoves.Add(m);
            return p;
        }

        private EvolutionBranchSO Branch(PokemonSpeciesSO evolved)
        {
            EvolutionBranchSO b = Make<EvolutionBranchSO>();
            b.EvolvedSpecies = evolved;
            b.MoveUpgrades = new List<MoveUpgradePair>();
            b.NewMoves = new List<MoveSO>();
            return b;
        }

        [Test]
        public void Evolve_UpgradesPoolMove_NotActive_ReplacesInPlace()
        {
            PokemonSpeciesSO base_ = Species(), evolved = Species();
            MoveSO a = Move(), b = Move(), c = Move(), aEvolved = Move();
            PokemonInstance p = Mon(base_, a, b, c);
            p.CurrentMoves.AddRange(new[] { b, c }); // a benched

            EvolutionBranchSO branch = Branch(evolved);
            branch.MoveUpgrades.Add(new MoveUpgradePair { OldMove = a, NewMove = aEvolved });

            EvolutionExecutor.Evolve(p, branch);

            Assert.That(p.LearnedMoves, Contains.Item(aEvolved), "Pool upgraded in-place (§5.10.3).");
            Assert.That(p.LearnedMoves, Has.No.Member(a), "Old version removed.");
            Assert.That(p.CurrentMoves, Is.EquivalentTo(new[] { b, c }), "Active unchanged (a not active).");
        }

        [Test]
        public void Evolve_UpgradesPoolMove_Active_Reslots()
        {
            PokemonSpeciesSO base_ = Species(), evolved = Species();
            MoveSO a = Move(), b = Move(), aEvolved = Move();
            PokemonInstance p = Mon(base_, a, b);
            p.CurrentMoves.AddRange(new[] { a, b }); // a is active

            EvolutionBranchSO branch = Branch(evolved);
            branch.MoveUpgrades.Add(new MoveUpgradePair { OldMove = a, NewMove = aEvolved });

            EvolutionExecutor.Evolve(p, branch);

            Assert.That(p.CurrentMoves, Contains.Item(aEvolved), "Upgraded move takes active slot (§5.10.3).");
            Assert.That(p.CurrentMoves, Has.No.Member(a), "Old version removed from active.");
            Assert.That(p.CurrentMoves[0], Is.SameAs(aEvolved), "Reslotted at original index.");
        }

        [Test]
        public void Evolve_AddsNewMoves_GrowsPool()
        {
            PokemonSpeciesSO base_ = Species(), evolved = Species();
            MoveSO a = Move(), b = Move(), newMove1 = Move(), newMove2 = Move();
            PokemonInstance p = Mon(base_, a, b);
            p.CurrentMoves.AddRange(new[] { a, b });

            EvolutionBranchSO branch = Branch(evolved);
            branch.NewMoves.AddRange(new[] { newMove1, newMove2 });

            EvolutionExecutor.Evolve(p, branch);

            Assert.That(p.LearnedMoves.Count, Is.EqualTo(4), "Pool grew from 2 to 4 (§5.10.1).");
            Assert.That(p.LearnedMoves, Contains.Item(newMove1));
            Assert.That(p.LearnedMoves, Contains.Item(newMove2));
            // Player will reconfigure active 4 post-evolution (UI flow); CurrentMoves unchanged here.
        }

        [Test]
        public void Evolve_AddsNewMove_Deduplicates()
        {
            PokemonSpeciesSO base_ = Species(), evolved = Species();
            MoveSO a = Move(), b = Move();
            PokemonInstance p = Mon(base_, a, b);

            EvolutionBranchSO branch = Branch(evolved);
            branch.NewMoves.Add(a); // duplicate add (already in pool)

            EvolutionExecutor.Evolve(p, branch);

            Assert.That(p.LearnedMoves.Count, Is.EqualTo(2), "Duplicate add ignored (§5.10.1 dedup).");
        }

        [Test]
        public void Evolve_MultipleUpgrades_AndAdditions_Combined()
        {
            PokemonSpeciesSO base_ = Species(), evolved = Species();
            MoveSO a = Move(), b = Move(), c = Move(), aEvolved = Move(), bEvolved = Move(), newMove = Move();
            PokemonInstance p = Mon(base_, a, b, c);
            p.CurrentMoves.AddRange(new[] { a, b, c });

            EvolutionBranchSO branch = Branch(evolved);
            branch.MoveUpgrades.Add(new MoveUpgradePair { OldMove = a, NewMove = aEvolved });
            branch.MoveUpgrades.Add(new MoveUpgradePair { OldMove = b, NewMove = bEvolved });
            branch.NewMoves.Add(newMove);

            EvolutionExecutor.Evolve(p, branch);

            Assert.That(p.LearnedMoves.Count, Is.EqualTo(4), "Pool: c (retained) + aEvolved + bEvolved + newMove.");
            Assert.That(p.CurrentMoves, Is.EquivalentTo(new[] { aEvolved, bEvolved, c }), "Active reslotted.");
        }
    }
}
