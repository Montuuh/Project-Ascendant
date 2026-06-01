using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.7.1 + Epic 11 Task 11.5.2 — the 10 VS launch achievements are well-formed.
    public class AchievementCatalogTests
    {
        [Test]
        public void Catalog_HasTenDistinctValidAchievements()
        {
            IReadOnlyList<AchievementSO> all = AchievementCatalog.All;
            Assert.That(all, Has.Count.EqualTo(10));

            HashSet<string> ids = new();
            foreach (AchievementSO a in all)
            {
                Assert.That(string.IsNullOrEmpty(a.AchievementId), Is.False, "id set");
                Assert.That(ids.Add(a.AchievementId), Is.True, $"id '{a.AchievementId}' is unique");
                Assert.That(a.Trigger, Is.Not.EqualTo(AchievementTrigger.None), $"{a.AchievementId} has a trigger");
                Assert.That(a.TargetCount, Is.GreaterThanOrEqualTo(1));
                Assert.That(a.TrainerXPReward, Is.InRange(50, 500), "§6.7 reward band");
            }
        }

        [Test]
        public void Catalog_StatusSniper_IsCounterOfTen()
        {
            AchievementSO sniper = null;
            foreach (AchievementSO a in AchievementCatalog.All) if (a.AchievementId == "status_sniper") sniper = a;
            Assert.That(sniper, Is.Not.Null);
            Assert.That(sniper.TargetCount, Is.EqualTo(10));
        }

        [Test]
        public void Catalog_HasHiddenAchievements()
        {
            int hidden = 0;
            foreach (AchievementSO a in AchievementCatalog.All) if (a.Hidden) hidden++;
            Assert.That(hidden, Is.GreaterThan(0), "§6.7.3 — some achievements are hidden.");
        }
    }
}
