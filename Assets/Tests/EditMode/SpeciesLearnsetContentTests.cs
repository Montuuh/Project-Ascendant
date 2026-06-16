using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §5.12.1 (CL-006) — content validation over the REAL species assets (loaded via
    // AssetDatabase). Guards the level-gated learnset invariants so a bad cadence can't ship:
    //  • every learnset move must be reachable BEFORE the species evolves (else it is lost), and
    //  • a base form starts lean (at most 2 moves known at level 1).
    public class SpeciesLearnsetContentTests
    {
        private static IEnumerable<PokemonSpeciesSO> AllSpecies()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PokemonSpeciesSO"))
            {
                PokemonSpeciesSO so = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (so != null) yield return so;
            }
        }

        [Test]
        // §5.12.1 — a learnset move at/after EvolveLevel is never learned (evolution takes over),
        // so it would be silently lost. Every authored level must be < EvolveLevel.
        public void LearnsetMoves_AreAllReachableBeforeEvolution()
        {
            List<string> violations = new();
            foreach (PokemonSpeciesSO s in AllSpecies())
            {
                if (s.LevelUpLearnset == null || s.LevelUpLearnset.Count == 0) continue;
                if (s.EvolveLevel <= 0) continue; // final / non-evolving form: no ceiling
                foreach (LevelUpEntry e in s.LevelUpLearnset)
                    if (e.Move != null && e.Level >= s.EvolveLevel)
                        violations.Add($"{s.SpeciesId}: move at L{e.Level} >= EvolveLevel {s.EvolveLevel}");
            }
            Assert.That(violations, Is.Empty,
                "Learnset moves must be reachable before evolution (§5.12.1):\n" + string.Join("\n", violations));
        }

        [Test]
        // §5.12.1 — base forms start with 2 moves: no more than two learnset entries at L<=1.
        // Final forms (EvolveLevel==0 OR Branches empty) and boss-wilds are exempt.
        public void Learnset_StartsWithAtMostTwoMovesAtLevelOne()
        {
            List<string> violations = new();
            foreach (PokemonSpeciesSO s in AllSpecies())
            {
                if (s.LevelUpLearnset == null || s.LevelUpLearnset.Count == 0) continue;
                // Skip final forms (EvolveLevel==0) and boss-wilds (no evolution, full kit).
                if (s.EvolveLevel == 0 && (s.Branches == null || s.Branches.Count == 0)) continue;
                int atOne = 0;
                foreach (LevelUpEntry e in s.LevelUpLearnset) if (e.Level <= 1) atOne++;
                if (atOne > 2) violations.Add($"{s.SpeciesId}: {atOne} moves at L<=1 (expected <=2)");
            }
            Assert.That(violations, Is.Empty,
                "Base forms start with 2 moves (§5.12.1):\n" + string.Join("\n", violations));
        }
    }
}
