using NUnit.Framework;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.9 — Pokédex completion-% milestone rewards: reaching a seen-% threshold grants
    // tokens / relics / starters once across runs (claimed set on MetaProgressionSO).
    public class PokedexRewardServiceTests
    {
        private MetaProgressionConfigSO _cfg;
        private MetaProgressionSO _meta;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<MetaProgressionConfigSO>();
            _meta = ScriptableObject.CreateInstance<MetaProgressionSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cfg);
            Object.DestroyImmediate(_meta);
        }

        private void AddMilestone(int pct, int tokens = 0, string relicId = null, string starterId = null)
        {
            PokedexCompletionMilestone m = new() { PercentThreshold = pct, TrainerTokens = tokens };
            if (relicId != null) m.RelicIds.Add(relicId);
            if (starterId != null) m.StarterIds.Add(starterId);
            _cfg.PokedexMilestones.Add(m);
        }

        [Test]
        public void Grant_ThresholdMet_AwardsTokensRelicStarter()
        {
            AddMilestone(50, tokens: 10, relicId: "lucky_egg", starterId: "eevee");

            var granted = PokedexRewardService.GrantCompletionMilestones(_meta, _cfg, seenCount: 6, totalSpecies: 10); // 60%

            Assert.That(granted.Count, Is.EqualTo(1));
            Assert.That(_meta.TrainerTokens, Is.EqualTo(10));
            Assert.That(_meta.UnlockedRelicIds, Does.Contain("lucky_egg"));
            Assert.That(_meta.UnlockedStarterIds, Does.Contain("eevee"));
        }

        [Test]
        public void Grant_BelowThreshold_AwardsNothing()
        {
            AddMilestone(75, tokens: 10);

            var granted = PokedexRewardService.GrantCompletionMilestones(_meta, _cfg, seenCount: 6, totalSpecies: 10); // 60%

            Assert.That(granted, Is.Empty);
            Assert.That(_meta.TrainerTokens, Is.EqualTo(0));
        }

        [Test]
        public void Grant_AlreadyClaimed_IsIdempotentAcrossRuns()
        {
            AddMilestone(50, tokens: 10);

            PokedexRewardService.GrantCompletionMilestones(_meta, _cfg, 6, 10);  // run 1 → claims 50%
            var second = PokedexRewardService.GrantCompletionMilestones(_meta, _cfg, 8, 10); // run 2 still ≥50%

            Assert.That(second, Is.Empty, "A claimed milestone must not re-grant on a later run.");
            Assert.That(_meta.TrainerTokens, Is.EqualTo(10), "Tokens granted exactly once.");
        }

        [Test]
        public void Grant_MultipleThresholdsCrossedAtOnce_AwardsAll()
        {
            AddMilestone(25, tokens: 5);
            AddMilestone(50, tokens: 10);
            AddMilestone(100, tokens: 50); // not reached

            var granted = PokedexRewardService.GrantCompletionMilestones(_meta, _cfg, 7, 10); // 70%

            Assert.That(granted.Count, Is.EqualTo(2));
            Assert.That(_meta.TrainerTokens, Is.EqualTo(15));
        }

        [Test]
        public void Grant_ZeroTotal_NoDivideByZero()
        {
            AddMilestone(0, tokens: 5);
            Assert.DoesNotThrow(() => PokedexRewardService.GrantCompletionMilestones(_meta, _cfg, 0, 0));
        }
    }
}
