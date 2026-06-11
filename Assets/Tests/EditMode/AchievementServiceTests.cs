using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.7 + Epic 11 Task 11.5 — achievement detection/award: single-shot, counter goals,
    // idempotency, XP banking + Level recompute.
    public class AchievementServiceTests
    {
        private readonly List<Object> _disposables = new();
        private MetaProgressionSO _meta;
        private MetaProgressionConfigSO _cfg;

        [SetUp]
        public void SetUp()
        {
            _meta = Make<MetaProgressionSO>(); _meta.TrainerLevel = 1;
            _cfg = Make<MetaProgressionConfigSO>();
            _cfg.LevelCurveBase = 500; _cfg.LevelCurveExponent = 1.6f; _cfg.MaxTrainerLevel = 30;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _disposables) if (o != null) Object.DestroyImmediate(o);
            _disposables.Clear();
        }

        private T Make<T>() where T : ScriptableObject { T o = ScriptableObject.CreateInstance<T>(); _disposables.Add(o); return o; }

        private AchievementSO Ach(string id, AchievementTrigger t, int target = 1, int reward = 50, int tokens = 0)
        {
            AchievementSO a = Make<AchievementSO>();
            a.AchievementId = id; a.Trigger = t; a.TargetCount = target; a.TrainerXPReward = reward;
            a.TokenReward = tokens;
            return a;
        }

        [Test]
        public void Report_SingleShot_Completes_AwardsXp()
        {
            AchievementSO first = Ach("first_steps", AchievementTrigger.WinCombat, target: 1, reward: 100);
            List<AchievementSO> reg = new() { first };

            List<AchievementSO> done = AchievementService.Report(AchievementTrigger.WinCombat, 1, reg, _meta, _cfg);

            Assert.That(done, Has.Count.EqualTo(1));
            Assert.That(AchievementService.IsCompleted(_meta, "first_steps"), Is.True);
            Assert.That(_meta.TrainerXP, Is.EqualTo(100), "Reward banked into Meta XP.");
        }

        [Test]
        public void Report_Counter_CompletesOnlyAtTarget()
        {
            AchievementSO sniper = Ach("status_sniper", AchievementTrigger.ApplyStatus, target: 10, reward: 200);
            List<AchievementSO> reg = new() { sniper };

            for (int i = 0; i < 9; i++) AchievementService.Report(AchievementTrigger.ApplyStatus, 1, reg, _meta, _cfg);
            Assert.That(AchievementService.IsCompleted(_meta, "status_sniper"), Is.False);
            Assert.That(AchievementService.GetProgress(_meta, "status_sniper"), Is.EqualTo(9));

            List<AchievementSO> done = AchievementService.Report(AchievementTrigger.ApplyStatus, 1, reg, _meta, _cfg);
            Assert.That(done, Has.Count.EqualTo(1));
            Assert.That(AchievementService.IsCompleted(_meta, "status_sniper"), Is.True);
            Assert.That(_meta.TrainerXP, Is.EqualTo(200));
        }

        [Test]
        public void Report_AlreadyCompleted_NoDoubleAward()
        {
            AchievementSO a = Ach("evolver", AchievementTrigger.Evolve, reward: 150);
            List<AchievementSO> reg = new() { a };
            AchievementService.Report(AchievementTrigger.Evolve, 1, reg, _meta, _cfg);
            int xpAfterFirst = _meta.TrainerXP;

            List<AchievementSO> again = AchievementService.Report(AchievementTrigger.Evolve, 1, reg, _meta, _cfg);
            Assert.That(again, Is.Empty);
            Assert.That(_meta.TrainerXP, Is.EqualTo(xpAfterFirst), "No double award.");
        }

        [Test]
        public void Report_NonMatchingTrigger_NoChange()
        {
            AchievementSO a = Ach("recruiter", AchievementTrigger.RecruitWild);
            List<AchievementSO> reg = new() { a };
            List<AchievementSO> done = AchievementService.Report(AchievementTrigger.DefeatGym, 1, reg, _meta, _cfg);
            Assert.That(done, Is.Empty);
            Assert.That(AchievementService.IsCompleted(_meta, "recruiter"), Is.False);
        }

        [Test]
        public void Report_BigReward_RecomputesLevel()
        {
            AchievementSO a = Ach("boulder_breaker", AchievementTrigger.DefeatGym, reward: 500);
            List<AchievementSO> reg = new() { a };
            AchievementService.Report(AchievementTrigger.DefeatGym, 1, reg, _meta, _cfg);
            Assert.That(_meta.TrainerLevel, Is.EqualTo(2), "500 XP → Lv2 (threshold 500).");
        }

        // §6.7.0 (CL-020 — Q19) — Gold/Platinum achievements grant Trainer Tokens on completion.
        [Test]
        public void Report_GoldTier_GrantsTokens_OnceOnly()
        {
            AchievementSO a = Ach("one_mon_army", AchievementTrigger.WinCombatLeadOnly, reward: 300, tokens: 2);
            List<AchievementSO> reg = new() { a };

            AchievementService.Report(AchievementTrigger.WinCombatLeadOnly, 1, reg, _meta, _cfg);
            Assert.That(_meta.TrainerTokens, Is.EqualTo(2), "Gold tier grants +2 Tokens.");
            Assert.That(_meta.TrainerXP, Is.EqualTo(300));

            AchievementService.Report(AchievementTrigger.WinCombatLeadOnly, 1, reg, _meta, _cfg);
            Assert.That(_meta.TrainerTokens, Is.EqualTo(2), "Completed → no double Token grant.");
        }

        [Test]
        public void Report_BronzeTier_GrantsNoTokens()
        {
            AchievementSO a = Ach("first_steps", AchievementTrigger.WinCombat, reward: 50, tokens: 0);
            List<AchievementSO> reg = new() { a };
            AchievementService.Report(AchievementTrigger.WinCombat, 1, reg, _meta, _cfg);
            Assert.That(_meta.TrainerTokens, Is.EqualTo(0), "Bronze/Silver grant no Tokens.");
        }
    }
}
