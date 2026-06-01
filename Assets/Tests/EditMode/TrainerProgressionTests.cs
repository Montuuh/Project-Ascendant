using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.3 + Epic 11 Task 11.3 — Trainer Level curve (floor(500×(L-1)^1.6)), Token conversion
    // (floor(runXP/100) cap 50), and the run-end CommitRun (bank XP → recompute Level → award Tokens).
    public class TrainerProgressionTests
    {
        private MetaProgressionConfigSO _cfg;
        private MetaProgressionSO _meta;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<MetaProgressionConfigSO>();
            _cfg.LevelCurveBase = 500;
            _cfg.LevelCurveExponent = 1.6f;
            _cfg.MaxTrainerLevel = 30;
            _cfg.TokenXPDivisor = 100;
            _cfg.TokensPerRunCap = 50;

            _meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
            _meta.TrainerLevel = 1;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cfg);
            Object.DestroyImmediate(_meta);
        }

        // §6.3.3 — cumulative thresholds. Formula is canonical (table is illustrative, see config note).
        [TestCase(1, 0)]
        [TestCase(2, 500)]    // floor(500 × 1^1.6)
        [TestCase(3, 1515)]   // floor(500 × 2^1.6)
        public void CumulativeXPForLevel_MatchesFormula(int level, int expected)
        {
            Assert.That(_cfg.CumulativeXPForLevel(level), Is.EqualTo(expected));
        }

        [Test]
        public void LevelForXP_StepsAtThresholds()
        {
            Assert.That(TrainerProgression.LevelForXP(0, _cfg), Is.EqualTo(1));
            Assert.That(TrainerProgression.LevelForXP(499, _cfg), Is.EqualTo(1));
            Assert.That(TrainerProgression.LevelForXP(500, _cfg), Is.EqualTo(2));
            Assert.That(TrainerProgression.LevelForXP(1514, _cfg), Is.EqualTo(2));
            Assert.That(TrainerProgression.LevelForXP(1515, _cfg), Is.EqualTo(3));
        }

        [Test]
        public void LevelForXP_ClampsToMaxLevel()
        {
            Assert.That(TrainerProgression.LevelForXP(int.MaxValue, _cfg), Is.EqualTo(_cfg.MaxTrainerLevel));
        }

        // §6.3.4 — Token conversion: floor(runXP / 100), capped at 50.
        [TestCase(0, 0)]
        [TestCase(99, 0)]
        [TestCase(100, 1)]
        [TestCase(550, 5)]
        [TestCase(5000, 50)]    // exactly cap
        [TestCase(9999, 50)]    // over cap → clamped
        public void TokensForRun_FloorsAndCaps(int runXp, int expected)
        {
            Assert.That(_cfg.TokensForRun(runXp), Is.EqualTo(expected));
        }

        [Test]
        public void CommitRun_BanksXp_RecomputesLevel_AwardsTokens()
        {
            TrainerProgression.CommitResult r = TrainerProgression.CommitRun(_meta, 550, _cfg);

            Assert.That(_meta.TrainerXP, Is.EqualTo(550));
            Assert.That(_meta.TrainerLevel, Is.EqualTo(2), "550 ≥ 500 → Level 2.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(5), "floor(550/100).");
            Assert.That(r.LeveledUp, Is.True);
            Assert.That(r.OldLevel, Is.EqualTo(1));
            Assert.That(r.NewLevel, Is.EqualTo(2));
            Assert.That(_meta.TrainerXPToNextLevel, Is.EqualTo(_cfg.CumulativeXPForLevel(3) - 550),
                "XP-to-next reflects the Level-3 threshold.");
        }

        [Test]
        public void CommitRun_Accumulates_AcrossRuns_AndTokenCapIsPerRun()
        {
            TrainerProgression.CommitRun(_meta, 9999, _cfg); // tokens capped at 50 this run
            Assert.That(_meta.TrainerTokens, Is.EqualTo(50));
            int xpAfterFirst = _meta.TrainerXP;

            TrainerProgression.CommitRun(_meta, 9999, _cfg); // another capped run → +50 more
            Assert.That(_meta.TrainerTokens, Is.EqualTo(100), "Cap is per-run, not lifetime.");
            Assert.That(_meta.TrainerXP, Is.EqualTo(xpAfterFirst + 9999), "XP accumulates.");
        }

        [Test]
        public void CommitRun_NegativeOrZero_NoChange()
        {
            TrainerProgression.CommitRun(_meta, -50, _cfg);
            Assert.That(_meta.TrainerXP, Is.EqualTo(0));
            Assert.That(_meta.TrainerTokens, Is.EqualTo(0));
            Assert.That(_meta.TrainerLevel, Is.EqualTo(1));
        }
    }
}
