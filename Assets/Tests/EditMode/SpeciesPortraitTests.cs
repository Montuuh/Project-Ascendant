using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.8 — every VS species must have a (placeholder) Portrait
    // assigned. Regression guard so the seeder output stays bound to species SOs.
    public class SpeciesPortraitTests
    {
        [Test]
        public void AllSpecies_HaveNonNullPortrait()
        {
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO",
                new[] { "Assets/ScriptableObjects" });

            var missing = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(path);
                if (sp == null) continue;
                if (sp.Portrait == null) missing.Add(path);
            }
            Assert.That(missing, Is.Empty,
                "Every species must have a Portrait (placeholder OK).\n"
                + "Run Project Ascendant → Generate Placeholder Portraits.\n"
                + "Missing:\n  " + string.Join("\n  ", missing));
        }
    }
}
