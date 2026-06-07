using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §5.12.1 (CL-006) — level-gated learnset. KnownMovesAtLevel returns every
    // LevelUpLearnset entry with Level <= the queried level (de-duplicated, order-preserved),
    // with a legacy fallback to BaseLearnset when no LevelUpLearnset is authored.
    // This suite covers the pure helper; deck-builder wiring (min(known, 4)) and base-kit
    // re-authoring (4 -> 2 + curve) are later CL-006 increments.
    public class PokemonSpeciesLearnsetTests
    {
        private static MoveSO Move() => ScriptableObject.CreateInstance<MoveSO>();
        private static PokemonSpeciesSO Species() => ScriptableObject.CreateInstance<PokemonSpeciesSO>();

        [Test]
        public void KnownMovesAtLevel_NoLevelUpLearnset_FallsBackToBaseLearnset()
        {
            // Per §5.12.1 — empty learnset = legacy behaviour (all BaseLearnset known).
            MoveSO a = Move(), b = Move();
            PokemonSpeciesSO s = Species();
            s.BaseLearnset = new List<MoveSO> { a, b };
            s.LevelUpLearnset = new List<LevelUpEntry>();

            Assert.That(s.KnownMovesAtLevel(1), Is.EquivalentTo(new[] { a, b }));
        }

        [Test]
        public void KnownMovesAtLevel_GatesByLevel_ReturnsOnlyLearnedAtOrBelow()
        {
            // Per §5.12.1 — start with 2, learn more at thresholds.
            MoveSO t1 = Move(), t2 = Move(), t8 = Move();
            PokemonSpeciesSO s = Species();
            s.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = t1 },
                new LevelUpEntry { Level = 1, Move = t2 },
                new LevelUpEntry { Level = 8, Move = t8 },
            };

            Assert.That(s.KnownMovesAtLevel(5), Is.EquivalentTo(new[] { t1, t2 }));
            Assert.That(s.KnownMovesAtLevel(8), Is.EquivalentTo(new[] { t1, t2, t8 }));
        }

        [Test]
        public void KnownMovesAtLevel_DuplicateMove_ReturnedOnce()
        {
            // Re-listing a move at a higher level (e.g. an upgrade alias) must not double it.
            MoveSO t1 = Move();
            PokemonSpeciesSO s = Species();
            s.LevelUpLearnset = new List<LevelUpEntry>
            {
                new LevelUpEntry { Level = 1, Move = t1 },
                new LevelUpEntry { Level = 5, Move = t1 },
            };

            Assert.That(s.KnownMovesAtLevel(10).Count, Is.EqualTo(1));
        }

        [Test]
        public void KnownMovesAtLevel_NullListsAndEntries_AreSafe()
        {
            PokemonSpeciesSO s = Species();
            s.BaseLearnset = null;
            s.LevelUpLearnset = null;
            Assert.That(s.KnownMovesAtLevel(1), Is.Empty);

            s.LevelUpLearnset = new List<LevelUpEntry> { new LevelUpEntry { Level = 1, Move = null } };
            Assert.That(s.KnownMovesAtLevel(1), Is.Empty);
        }
    }
}
