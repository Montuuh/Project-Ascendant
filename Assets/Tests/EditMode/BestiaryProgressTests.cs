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

        // ── §6.9 — Pokédex discovery: seen / recruited tracking ─────────────

        [Test]
        public void RecordSeen_MarksSpeciesSeen_AndCountsDistinct()
        {
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.IsSeen("pidgey"), Is.False);
            so.RecordSeen("pidgey");
            so.RecordSeen("pidgey"); // second sighting — still one distinct species
            so.RecordSeen("rattata");

            Assert.That(so.IsSeen("pidgey"), Is.True);
            Assert.That(so.SeenSpeciesCount(), Is.EqualTo(2));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordKill_AlsoCountsAsSeen()
        {
            // A defeated species is implicitly seen even if RecordSeen was never called.
            BestiaryProgressSO so = MakeSO();
            so.RecordKill("caterpie", RarityTier.Common);
            Assert.That(so.IsSeen("caterpie"), Is.True);
            Assert.That(so.SeenSpeciesCount(), Is.EqualTo(1));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void DefeatsForTier_ReturnsRarityScaledThresholds()
        {
            // §4.3.9.1 — Common: Fam 10 / Vet 30 / Mas 50; Rare: Fam 2 / Vet 5 / Mas 10. HUD progress.
            BestiaryProgressSO so = MakeSO();
            Assert.That(so.DefeatsForTier(BestiaryMasteryTier.Familiar, RarityTier.Common), Is.EqualTo(10));
            Assert.That(so.DefeatsForTier(BestiaryMasteryTier.Veteran, RarityTier.Common), Is.EqualTo(30));
            Assert.That(so.DefeatsForTier(BestiaryMasteryTier.Master, RarityTier.Rare), Is.EqualTo(10));
            Assert.That(so.DefeatsForTier(BestiaryMasteryTier.None, RarityTier.Common), Is.EqualTo(0));
            Object.DestroyImmediate(so);
        }

        [Test]
        public void RecordRecruit_IncrementsRecruit_AndImpliesSeen()
        {
            BestiaryProgressSO so = MakeSO();
            so.RecordRecruit("bulbasaur");
            Assert.That(so.GetOrCreate("bulbasaur").TimesRecruited, Is.EqualTo(1));
            Assert.That(so.IsSeen("bulbasaur"), Is.True);
            Object.DestroyImmediate(so);
        }

        // ── §4.3.9.1 — Bestiary Master tier → Mastery Move unlock (#9) ──────

        private MoveSO MakeMastery(string moveId)
        {
            MoveSO m = ScriptableObject.CreateInstance<MoveSO>();
            m.MoveId = moveId;
            return m;
        }

        private PokemonSpeciesSO MakeSpeciesWithMastery(string speciesId, MoveSO mastery)
        {
            PokemonSpeciesSO sp = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            sp.SpeciesId = speciesId;
            sp.MasteryMove = mastery;
            return sp;
        }

        [Test]
        public void TryUnlockMastery_BelowMasterTier_DoesNotUnlock()
        {
            // Rare thresholds: Familiar 2 / Veteran 5 / Master 10. 9 kills = Veteran (not Master).
            BestiaryProgressSO bestiary = MakeSO();
            for (int i = 0; i < 9; i++) bestiary.RecordKill("rare_mon", RarityTier.Rare);
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            PokemonSpeciesSO sp = MakeSpeciesWithMastery("rare_mon", MakeMastery("rare_mastery"));

            Assert.That(bestiary.TierFor("rare_mon"), Is.EqualTo(BestiaryMasteryTier.Veteran));
            Assert.That(BestiaryMasteryUnlock.TryUnlockMastery(bestiary, meta, sp), Is.False);
            Assert.That(meta.IsMasteryUnlocked("rare_mastery"), Is.False);

            Object.DestroyImmediate(sp); Object.DestroyImmediate(meta); Object.DestroyImmediate(bestiary);
        }

        [Test]
        public void TryUnlockMastery_AtMasterTier_UnlocksMasteryMove()
        {
            BestiaryProgressSO bestiary = MakeSO();
            for (int i = 0; i < 10; i++) bestiary.RecordKill("rare_mon", RarityTier.Rare); // → Master
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            PokemonSpeciesSO sp = MakeSpeciesWithMastery("rare_mon", MakeMastery("rare_mastery"));

            Assert.That(bestiary.TierFor("rare_mon"), Is.EqualTo(BestiaryMasteryTier.Master));
            Assert.That(BestiaryMasteryUnlock.TryUnlockMastery(bestiary, meta, sp), Is.True);
            Assert.That(meta.IsMasteryUnlocked("rare_mastery"), Is.True);

            Object.DestroyImmediate(sp); Object.DestroyImmediate(meta); Object.DestroyImmediate(bestiary);
        }

        [Test]
        public void TryUnlockMastery_AlreadyUnlocked_IsIdempotent()
        {
            BestiaryProgressSO bestiary = MakeSO();
            for (int i = 0; i < 10; i++) bestiary.RecordKill("rare_mon", RarityTier.Rare);
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            PokemonSpeciesSO sp = MakeSpeciesWithMastery("rare_mon", MakeMastery("rare_mastery"));

            Assert.That(BestiaryMasteryUnlock.TryUnlockMastery(bestiary, meta, sp), Is.True);
            Assert.That(BestiaryMasteryUnlock.TryUnlockMastery(bestiary, meta, sp), Is.False,
                "Second call must report no NEW unlock (idempotent).");
            Assert.That(meta.UnlockedMasteryMoveIds.Count, Is.EqualTo(1));

            Object.DestroyImmediate(sp); Object.DestroyImmediate(meta); Object.DestroyImmediate(bestiary);
        }

        [Test]
        public void TryUnlockMastery_SpeciesWithoutMasteryMove_NoUnlock()
        {
            BestiaryProgressSO bestiary = MakeSO();
            for (int i = 0; i < 10; i++) bestiary.RecordKill("rare_mon", RarityTier.Rare);
            MetaProgressionSO meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            PokemonSpeciesSO sp = MakeSpeciesWithMastery("rare_mon", null);

            Assert.That(BestiaryMasteryUnlock.TryUnlockMastery(bestiary, meta, sp), Is.False);

            Object.DestroyImmediate(sp); Object.DestroyImmediate(meta); Object.DestroyImmediate(bestiary);
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
