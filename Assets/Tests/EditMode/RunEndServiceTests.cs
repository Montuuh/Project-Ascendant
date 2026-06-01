using System.IO;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.Tests
{
    // Per §2.1.7 + Epic 11 Task 11.4 — run-end flow: commit accrued Trainer XP → Meta (Level/Tokens),
    // run-failed bonus on defeat (§6.3.2), run counters, and the RunSummary tallies.
    public class RunEndServiceTests
    {
        private RunStateSO _run;
        private MetaProgressionSO _meta;
        private MetaProgressionConfigSO _cfg;

        private string _saveDir;

        [SetUp]
        public void SetUp()
        {
            // Finalize calls SaveSystem.SaveMeta — redirect to a temp dir so tests never touch the real save.
            _saveDir = Path.Combine(Path.GetTempPath(), "PA_RunEndTests_" + System.Guid.NewGuid().ToString("N"));
            SaveSystem.SaveDirectoryOverride = _saveDir;

            _run = ScriptableObject.CreateInstance<RunStateSO>();
            _meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            _meta.TrainerLevel = 1;
            _cfg = ScriptableObject.CreateInstance<MetaProgressionConfigSO>();
            _cfg.LevelCurveBase = 500; _cfg.LevelCurveExponent = 1.6f; _cfg.MaxTrainerLevel = 30;
            _cfg.TokenXPDivisor = 100; _cfg.TokensPerRunCap = 50;
            _cfg.RunFailedXPPerLayer = 50; _cfg.RunFailedXPCap = 400;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_run); Object.DestroyImmediate(_meta); Object.DestroyImmediate(_cfg);
            SaveSystem.SaveDirectoryOverride = null;
            if (_saveDir != null && Directory.Exists(_saveDir)) Directory.Delete(_saveDir, true);
        }

        // §6.7 — pre-complete every achievement so run-end awards don't perturb commit-XP assertions.
        private void SuppressAchievements()
        {
            _meta.CompletedAchievementIds = new System.Collections.Generic.List<string>();
            foreach (AchievementSO a in AchievementCatalog.All) _meta.CompletedAchievementIds.Add(a.AchievementId);
        }

        [Test]
        public void Finalize_Victory_CommitsXp_AwardsTokens_CountsCompleted()
        {
            SuppressAchievements();
            _run.TrainerXPEarnedThisRun = 550;
            _run.CombatsClearedThisRun = 6;
            _run.EvolutionsThisRun = 2;

            RunEndService.RunSummary s = RunEndService.Finalize(_run, null, _meta, _cfg, RunOutcome.Victory, 7);

            Assert.That(_meta.TrainerXP, Is.EqualTo(550));
            Assert.That(_meta.TrainerLevel, Is.EqualTo(2), "550 ≥ 500 → Lv2.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(5));
            Assert.That(_meta.TotalRunsCompleted, Is.EqualTo(1));
            Assert.That(_meta.TotalRunsAttempted, Is.EqualTo(1));
            Assert.That(s.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(s.CombatsCleared, Is.EqualTo(6));
            Assert.That(s.Evolutions, Is.EqualTo(2));
            Assert.That(s.TokensGained, Is.EqualTo(5));
            Assert.That(s.LeveledUp, Is.True);
        }

        [Test]
        public void Finalize_Defeat_AddsFailedBonus_CountsAttemptedOnly()
        {
            _run.TrainerXPEarnedThisRun = 30;

            // layers 5 × 50 = 250 bonus → total 280 banked.
            RunEndService.RunSummary s = RunEndService.Finalize(_run, null, _meta, _cfg, RunOutcome.Defeat, 5);

            Assert.That(s.RunXpEarned, Is.EqualTo(280));
            Assert.That(_meta.TrainerXP, Is.EqualTo(280));
            Assert.That(_meta.TotalRunsCompleted, Is.EqualTo(0), "Defeat is not a completed run.");
            Assert.That(_meta.TotalRunsAttempted, Is.EqualTo(1));
        }

        [Test]
        public void Finalize_Defeat_FailedBonus_Capped()
        {
            _run.TrainerXPEarnedThisRun = 0;
            RunEndService.Finalize(_run, null, _meta, _cfg, RunOutcome.Defeat, 99); // 99×50 capped at 400
            Assert.That(_meta.TrainerXP, Is.EqualTo(400));
        }

        [Test]
        public void Finalize_AppliesDifficultyXpMultiplier_BeforeCommit()
        {
            // §6.8.1 — a ×1.20 modifier boosts the whole run haul before banking.
            SuppressAchievements();
            _run.TrainerXPEarnedThisRun = 500;
            DifficultyModifierSO hard = ScriptableObject.CreateInstance<DifficultyModifierSO>();
            hard.TrainerXPMultiplier = 1.20f;
            _run.ActiveDifficultyModifiers = new System.Collections.Generic.List<DifficultyModifierSO> { hard };

            RunEndService.RunSummary s = RunEndService.Finalize(_run, null, _meta, _cfg, RunOutcome.Victory, 7);

            Assert.That(s.RunXpEarned, Is.EqualTo(600), "500 × 1.20.");
            Assert.That(_meta.TrainerXP, Is.EqualTo(600));
            Object.DestroyImmediate(hard);
        }

        [Test]
        public void Finalize_ReportsMaxTrauma_FromBox()
        {
            Box box = new(6);
            box.TryAdd(new PokemonInstance { Species = ScriptableObject.CreateInstance<PokemonSpeciesSO>(), CurrentHP = 10, TraumaStacks = 1 });
            box.TryAdd(new PokemonInstance { Species = ScriptableObject.CreateInstance<PokemonSpeciesSO>(), CurrentHP = 10, TraumaStacks = 4 });

            RunEndService.RunSummary s = RunEndService.Finalize(_run, box, _meta, _cfg, RunOutcome.Victory, 7);

            Assert.That(s.MaxTrauma, Is.EqualTo(4));
            foreach (PokemonInstance p in box.Members) Object.DestroyImmediate(p.Species);
        }

        [Test]
        public void Finalize_UnlocksRunDataAchievements()
        {
            // §6.7 — a won run with combats + an evolution + a recruit + a 5-Trauma Pokémon unlocks
            // First Steps + Evolver + Recruiter + Boulder Breaker + Trauma Survivor.
            PokemonSpeciesSO spA = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            PokemonSpeciesSO spB = ScriptableObject.CreateInstance<PokemonSpeciesSO>();
            Box box = new(6);
            box.TryAdd(new PokemonInstance { Species = spA, CurrentHP = 10, TraumaStacks = 5 });
            box.TryAdd(new PokemonInstance { Species = spB, CurrentHP = 10, TraumaStacks = 0 });
            _run.CombatsClearedThisRun = 3;
            _run.EvolutionsThisRun = 1;

            RunEndService.RunSummary s = RunEndService.Finalize(_run, box, _meta, _cfg, RunOutcome.Victory, 7);

            Assert.That(AchievementService.IsCompleted(_meta, "first_steps"), Is.True, "first_steps");
            Assert.That(AchievementService.IsCompleted(_meta, "evolver"), Is.True, "evolver");
            Assert.That(AchievementService.IsCompleted(_meta, "recruiter"), Is.True, "recruiter");
            Assert.That(AchievementService.IsCompleted(_meta, "boulder_breaker"), Is.True, "boulder_breaker");
            Assert.That(AchievementService.IsCompleted(_meta, "trauma_survivor"), Is.True, "trauma_survivor");
            Assert.That(s.AchievementsUnlocked, Is.EqualTo(5), "unlocked count");

            Object.DestroyImmediate(spA); Object.DestroyImmediate(spB);
        }

        [Test]
        public void Finalize_NullMetaOrConfig_StillBuildsSummary_NoThrow()
        {
            _run.CombatsClearedThisRun = 3;
            RunEndService.RunSummary s = RunEndService.Finalize(_run, null, null, null, RunOutcome.Victory, 4);
            Assert.That(s.CombatsCleared, Is.EqualTo(3));
            Assert.That(s.TokensGained, Is.EqualTo(0));
        }
    }
}
