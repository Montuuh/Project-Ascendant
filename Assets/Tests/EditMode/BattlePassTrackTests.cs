using System.Collections.Generic;
using NUnit.Framework;
using ProjectAscendant.Core;

namespace ProjectAscendant.Tests
{
    // Per §6.3.5 (CL-019 — Q18) — the code-built Battle Pass reward track: every level 2–30 grants a
    // milestone, every 5th level is a Token milestone, meta-starters at L4/8/12. Structure/placement
    // guard (numbers are tunable placeholders).
    public class BattlePassTrackTests
    {
        private List<TrainerLevelMilestone> Track() => BattlePassTrack.BuildDefaultMilestones();

        [Test]
        public void Track_CoversLevels2To30_UniqueAscending()
        {
            List<TrainerLevelMilestone> t = Track();
            Assert.That(t.Count, Is.EqualTo(29), "one milestone per level 2–30.");

            HashSet<int> levels = new();
            int prev = 1;
            foreach (TrainerLevelMilestone m in t)
            {
                Assert.That(m.Level, Is.GreaterThan(prev), "levels strictly ascending.");
                Assert.That(levels.Add(m.Level), Is.True, $"duplicate level {m.Level}.");
                Assert.That(string.IsNullOrEmpty(m.Description), Is.False, $"L{m.Level} has no description.");
                prev = m.Level;
            }
            Assert.That(levels.Contains(2) && levels.Contains(30), Is.True);
        }

        [Test]
        public void Track_TokenMilestones_EveryFifthLevel_SumIs46()
        {
            int total = 0;
            List<int> tokenLevels = new();
            foreach (TrainerLevelMilestone m in Track())
                if (m.TrainerTokens > 0) { tokenLevels.Add(m.Level); total += m.TrainerTokens; }

            Assert.That(tokenLevels, Is.EquivalentTo(new[] { 5, 10, 15, 20, 25, 30 }),
                "Token milestones land on every 5th level (~20%).");
            Assert.That(total, Is.EqualTo(46), "5+5+8+8+10+10 (tunable).");
        }

        [Test]
        public void Track_MetaStarters_AtExpectedLevels()
        {
            Dictionary<int, string> starterAt = new();
            foreach (TrainerLevelMilestone m in Track())
                if (m.StarterIds != null && m.StarterIds.Count > 0) starterAt[m.Level] = m.StarterIds[0];

            Assert.That(starterAt[4], Is.EqualTo("pikachu"));
            Assert.That(starterAt[8], Is.EqualTo("eevee"));
            Assert.That(starterAt[12], Is.EqualTo("riolu"));
            Assert.That(starterAt.Count, Is.EqualTo(3), "exactly the 3 meta-starters on the track.");
        }
    }
}
