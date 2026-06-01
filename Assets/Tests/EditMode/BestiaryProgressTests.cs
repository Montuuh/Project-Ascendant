using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per Epic 7 Task 7.9 + §4.3.9 — Bestiary progression scaffolding tests.
    // Runtime wiring (combat-end faint hook → BestiaryProgressSO.RecordKill)
    // is deferred to Epic 8 / 11; this suite covers the helper logic + the
    // VS species-registration discipline that 7.9.1 calls out.
    public class BestiaryProgressTests
    {
        private BestiaryProgressSO MakeSO()
        {
            var so = ScriptableObject.CreateInstance<BestiaryProgressSO>();
            so.Entries = new List<BestiaryEntry>();
            return so;
        }

        // ── RecordKill ───────────────────────────────────────────────────────

        [Test]
        public void RecordKill_FirstTime_CreatesEntryWithCountOne()
        {
            BestiaryProgressSO so = MakeSO();
            so.RecordKill("pidgey", RarityTier.Common);

            Assert.That(so.Entries.Count, Is.EqualTo(1));
            Assert.That(so.Entries[0].SpeciesId, Is.EqualTo("pidgey"));
            Assert.That(so.Entries[0].TimesDefeated, Is.EqualTo(1));
            Assert.That(so.Entries[0].MasteryTier, Is.EqualTo(BestiaryMasteryTier.None));

            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordKill_Repeated_IncrementsExistingEntry()
        {
            BestiaryProgressSO so = MakeSO();
            for (int i = 0; i < 5; i++)
                so.RecordKill("caterpie", RarityTier.Common);

            Assert.That(so.Entries.Count, Is.EqualTo(1),
                "Repeated RecordKill must not duplicate the entry.");
            Assert.That(so.Entries[0].TimesDefeated, Is.EqualTo(5));

            Object.DestroyImmediate(so);
        }

        // ── EvaluateTier — §4.3.9.1 thresholds ───────────────────────────────

        [Test]
        public void EvaluateTier_CommonWildAt10Defeats_ReturnsFamiliar()
        {
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.EvaluateTier(10, RarityTier.Common),
                Is.EqualTo(BestiaryMasteryTier.Familiar));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EvaluateTier_CommonWildAt30Defeats_ReturnsVeteran()
        {
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.EvaluateTier(30, RarityTier.Common),
                Is.EqualTo(BestiaryMasteryTier.Veteran));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EvaluateTier_CommonWildAt50Defeats_ReturnsMaster()
        {
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.EvaluateTier(50, RarityTier.Common),
                Is.EqualTo(BestiaryMasteryTier.Master));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EvaluateTier_UncommonWildAt5Defeats_ReturnsFamiliar()
        {
            // §4.3.9.1 — Uncommon Familiar threshold = 5 (vs Common = 10).
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.EvaluateTier(5, RarityTier.Uncommon),
                Is.EqualTo(BestiaryMasteryTier.Familiar));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EvaluateTier_RareAt2Defeats_ReturnsFamiliar()
        {
            // §4.3.9.1 — Rare Familiar threshold = 2.
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.EvaluateTier(2, RarityTier.Rare),
                Is.EqualTo(BestiaryMasteryTier.Familiar));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void EvaluateTier_BelowAllThresholds_ReturnsNone()
        {
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.EvaluateTier(0, RarityTier.Common),
                Is.EqualTo(BestiaryMasteryTier.None));
            Assert.That(so.EvaluateTier(9, RarityTier.Common),
                Is.EqualTo(BestiaryMasteryTier.None));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordKill_AcrossThreshold_PromotesMasteryTier()
        {
            BestiaryProgressSO so = MakeSO();
            for (int i = 0; i < 9; i++)
                so.RecordKill("caterpie", RarityTier.Common);
            Assert.That(so.Entries[0].MasteryTier, Is.EqualTo(BestiaryMasteryTier.None));

            so.RecordKill("caterpie", RarityTier.Common);  // 10th defeat
            Assert.That(so.Entries[0].MasteryTier, Is.EqualTo(BestiaryMasteryTier.Familiar),
                "Familiar tier reveal must engage exactly at the 10th Common defeat.");

            Object.DestroyImmediate(so);
        }

        // ── Task 11.8.3 — Familiar-tier reveal + TierFor query ──────────────

        [Test]
        public void RecordKill_FamiliarTier_RevealsTypeChart()
        {
            // §4.3.9.1 / Task 11.8.3 — the species' type-chart entry reveals at Familiar tier.
            BestiaryProgressSO so = MakeSO();
            so.RecordKill("geodude", RarityTier.Common); // 1 defeat → None
            Assert.That(so.GetOrCreate("geodude").TypeChartRevealed, Is.False);

            for (int i = 0; i < 9; i++) so.RecordKill("geodude", RarityTier.Common); // total 10 → Familiar
            Assert.That(so.GetOrCreate("geodude").TypeChartRevealed, Is.True);
            Object.DestroyImmediate(so);
        }

        [Test]
        public void TierFor_ReturnsEntryTierOrNoneWhenUnseen()
        {
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.TierFor("mewtwo"), Is.EqualTo(BestiaryMasteryTier.None));
            for (int i = 0; i < 10; i++) so.RecordKill("pidgey", RarityTier.Common);
            Assert.That(so.TierFor("pidgey"), Is.EqualTo(BestiaryMasteryTier.Familiar));
            Object.DestroyImmediate(so);
        }

        // ── 7.9.1 — Wild species registered in BestiaryProgressSO ───────────

        [Test]
        public void AllVSWildSpecies_HaveNonEmptySpeciesId()
        {
            // Discoverability via SpeciesId is how BestiaryProgressSO keys entries.
            // No need to pre-populate; GetOrCreate is lazy.
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO",
                new[] { "Assets/ScriptableObjects/VS/Species/Wild" });

            Assert.That(guids.Length, Is.GreaterThanOrEqualTo(9),
                "Expected 9 wild VS species (3 lines × 3 stages); found " + guids.Length);

            var bad = new List<string>();
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp = AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(path);
                if (sp != null && string.IsNullOrWhiteSpace(sp.SpeciesId))
                    bad.Add(path);
            }
            Assert.That(bad, Is.Empty,
                "Every wild VS species must have a non-empty SpeciesId.\n"
                + "Offenders:\n  " + string.Join("\n  ", bad));
        }
    }
}
