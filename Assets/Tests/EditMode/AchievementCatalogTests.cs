using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.7.1 / §6.7.1.1 (CL-020 — Q19) — the VS achievement catalog (medal-tiered) is well-formed.
    public class AchievementCatalogTests
    {
        [Test]
        public void Catalog_HasDistinctValidAchievements()
        {
            IReadOnlyList<AchievementSO> all = AchievementCatalog.All;
            Assert.That(all, Has.Count.EqualTo(19), "VS-triggerable subset of the §6.7.1.1 catalog (CL-020).");

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

        // §6.7.0 (CL-020) — medal tier sets the reward band; Gold/Platinum grant Tokens, Bronze/Silver don't.
        [Test]
        public void Catalog_TierBands_AreConsistent()
        {
            foreach (AchievementSO a in AchievementCatalog.All)
            {
                switch (a.Tier)
                {
                    case AchievementTier.Bronze:
                        Assert.That(a.TrainerXPReward, Is.InRange(50, 100), $"{a.AchievementId} Bronze XP band");
                        Assert.That(a.TokenReward, Is.EqualTo(0), $"{a.AchievementId} Bronze grants no Tokens");
                        break;
                    case AchievementTier.Silver:
                        Assert.That(a.TrainerXPReward, Is.InRange(150, 250), $"{a.AchievementId} Silver XP band");
                        Assert.That(a.TokenReward, Is.EqualTo(0), $"{a.AchievementId} Silver grants no Tokens");
                        break;
                    case AchievementTier.Gold:
                        Assert.That(a.TrainerXPReward, Is.InRange(250, 400), $"{a.AchievementId} Gold XP band");
                        Assert.That(a.TokenReward, Is.GreaterThan(0), $"{a.AchievementId} Gold grants Tokens");
                        break;
                    case AchievementTier.Platinum:
                        Assert.That(a.TrainerXPReward, Is.InRange(400, 500), $"{a.AchievementId} Platinum XP band");
                        Assert.That(a.TokenReward, Is.GreaterThan(0), $"{a.AchievementId} Platinum grants Tokens");
                        break;
                }
            }
        }

        [Test]
        public void Catalog_HasAllFourTiers()
        {
            HashSet<AchievementTier> tiers = new();
            foreach (AchievementSO a in AchievementCatalog.All) tiers.Add(a.Tier);
            Assert.That(tiers, Is.EquivalentTo(new[]
            {
                AchievementTier.Bronze, AchievementTier.Silver, AchievementTier.Gold, AchievementTier.Platinum
            }), "the VS catalog spans all four medal tiers.");
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
