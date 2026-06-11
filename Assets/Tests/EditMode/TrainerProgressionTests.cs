using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.3 + Epic 11 Task 11.3 — Trainer Level curve (floor(500×(L-1)^1.6)) and CommitRun (bank XP →
    // recompute Level). Per CL-019 (Q18): Tokens are granted at the Battle Pass track's milestone levels
    // (TrainerLevelMilestone.TrainerTokens), NOT per-run — CommitRun no longer grants Tokens.
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

        // ── §6.3 / §6.5.2 / §6.6.1 — Trainer-Level milestone unlocks (#9) ────

        private void AddMilestone(int level, string starterId = null, string relicId = null, int tokens = 0)
        {
            TrainerLevelMilestone m = new() { Level = level, TrainerTokens = tokens };
            if (starterId != null) m.StarterIds.Add(starterId);
            if (relicId != null) m.RelicIds.Add(relicId);
            _cfg.LevelMilestones.Add(m);
        }

        [Test]
        public void GrantLevelUnlocks_LevelReached_GrantsStarterAndRelic()
        {
            AddMilestone(2, starterId: "eevee", relicId: "lucky_egg");

            var granted = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, oldLevel: 1, newLevel: 2);

            Assert.That(granted.Count, Is.EqualTo(1));
            Assert.That(_meta.UnlockedStarterIds, Does.Contain("eevee"));
            Assert.That(_meta.UnlockedRelicIds, Does.Contain("lucky_egg"));
        }

        [Test]
        public void GrantLevelUnlocks_LevelNotYetReached_GrantsNothing()
        {
            AddMilestone(5, starterId: "eevee");

            var granted = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, oldLevel: 1, newLevel: 3);

            Assert.That(granted, Is.Empty);
            Assert.That(_meta.UnlockedStarterIds == null || _meta.UnlockedStarterIds.Count == 0, Is.True);
        }

        [Test]
        public void GrantLevelUnlocks_MultiLevelJump_GrantsAllSpannedMilestones()
        {
            AddMilestone(2, starterId: "eevee");
            AddMilestone(3, relicId: "soothe_bell");
            AddMilestone(6, starterId: "pikachu"); // beyond the jump → not granted

            var granted = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, oldLevel: 1, newLevel: 4);

            Assert.That(granted.Count, Is.EqualTo(2));
            Assert.That(_meta.UnlockedStarterIds, Does.Contain("eevee"));
            Assert.That(_meta.UnlockedRelicIds, Does.Contain("soothe_bell"));
            Assert.That(_meta.UnlockedStarterIds, Does.Not.Contain("pikachu"));
        }

        [Test]
        public void GrantLevelUnlocks_AlreadyUnlocked_IsIdempotent()
        {
            AddMilestone(2, starterId: "eevee");

            TrainerProgression.GrantLevelUnlocks(_meta, _cfg, 1, 2);
            var second = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, 1, 2);

            Assert.That(second, Is.Empty, "Re-granting the same milestone must add nothing new.");
            Assert.That(_meta.UnlockedStarterIds.FindAll(s => s == "eevee").Count, Is.EqualTo(1));
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

        // CL-019 (Q18): CommitRun banks XP + recomputes Level but no longer grants Tokens.
        [Test]
        public void CommitRun_BanksXp_RecomputesLevel_NoLongerAwardsTokens()
        {
            TrainerProgression.CommitResult r = TrainerProgression.CommitRun(_meta, 550, _cfg);

            Assert.That(_meta.TrainerXP, Is.EqualTo(550));
            Assert.That(_meta.TrainerLevel, Is.EqualTo(2), "550 ≥ 500 → Level 2.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(0), "CL-019: CommitRun grants no per-run Tokens.");
            Assert.That(r.LeveledUp, Is.True);
            Assert.That(r.OldLevel, Is.EqualTo(1));
            Assert.That(r.NewLevel, Is.EqualTo(2));
            Assert.That(_meta.TrainerXPToNextLevel, Is.EqualTo(_cfg.CumulativeXPForLevel(3) - 550),
                "XP-to-next reflects the Level-3 threshold.");
        }

        [Test]
        public void CommitRun_Accumulates_XpAcrossRuns_GrantsNoTokens()
        {
            TrainerProgression.CommitRun(_meta, 9999, _cfg);
            int xpAfterFirst = _meta.TrainerXP;

            TrainerProgression.CommitRun(_meta, 9999, _cfg);
            Assert.That(_meta.TrainerXP, Is.EqualTo(xpAfterFirst + 9999), "XP accumulates.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(0), "CL-019: no per-run Token earn.");
        }

        [Test]
        public void CommitRun_NegativeOrZero_NoChange()
        {
            TrainerProgression.CommitRun(_meta, -50, _cfg);
            Assert.That(_meta.TrainerXP, Is.EqualTo(0));
            Assert.That(_meta.TrainerTokens, Is.EqualTo(0));
            Assert.That(_meta.TrainerLevel, Is.EqualTo(1));
        }

        // §6.3.4/§6.3.5 (CL-019 — Q18) — Battle Pass Token milestones: Tokens granted once on the level cross.
        [Test]
        public void GrantLevelUnlocks_TokenMilestone_GrantsTokensOnce_AndRecords()
        {
            AddMilestone(5, tokens: 5);

            var granted = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, oldLevel: 4, newLevel: 5);

            Assert.That(granted.Count, Is.EqualTo(1), "a token-only milestone is still reported.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(5));
            Assert.That(_meta.ClaimedLevelMilestones, Does.Contain(5));
        }

        [Test]
        public void GrantLevelUnlocks_TokenMilestone_IsIdempotent()
        {
            AddMilestone(5, tokens: 5);

            TrainerProgression.GrantLevelUnlocks(_meta, _cfg, 4, 5);
            var second = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, 4, 5);

            Assert.That(second, Is.Empty, "Re-crossing the same level must not re-grant Tokens.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(5), "Tokens granted exactly once.");
        }

        [Test]
        public void GrantLevelUnlocks_MultiLevel_SumsSpannedMilestoneTokens()
        {
            AddMilestone(5, tokens: 5);
            AddMilestone(10, tokens: 5);
            AddMilestone(15, tokens: 8); // beyond the jump → not granted

            var granted = TrainerProgression.GrantLevelUnlocks(_meta, _cfg, oldLevel: 1, newLevel: 10);

            Assert.That(_meta.TrainerTokens, Is.EqualTo(10), "L5 + L10 Tokens; L15 not yet reached.");
            int sum = 0; foreach (var m in granted) sum += m.TrainerTokens;
            Assert.That(sum, Is.EqualTo(10), "granted milestones carry their Tokens for the run-summary.");
        }
    }
}
