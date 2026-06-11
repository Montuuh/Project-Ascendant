using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §6.3.5 (CL-019 — Q18) — the Hybrid Battle Pass reward track, built in code (mirrors
    // AchievementCatalog / RegionModifierPool: content lives here until authored into a config asset).
    // Each Trainer Level grants its milestone on level-up; ~80% are auto-grants, every 5th level is a
    // Token milestone (the agency lane, §6.3.4).
    //
    // VS scope (CL-019 inc B): the live mechanical reward in the VS is the **Tokens** (granted via
    // TrainerLevelMilestone.TrainerTokens — see TrainerProgression.GrantLevelUnlocks). The meta-starter
    // and Hub-upgrade *grants* (and the Token sink — Tier-3 Mastery relics) are post-VS content; the
    // track records their unlock ids + Descriptions so they light up when those systems land. All values
    // are systems-designer-tunable placeholders.
    public static class BattlePassTrack
    {
        public static List<TrainerLevelMilestone> BuildDefaultMilestones()
        {
            return new List<TrainerLevelMilestone>
            {
                M(2,  "Relic pool +1 (Tier-1 signature)"),
                M(3,  "Hub: Curated Starting Relic +1 (3→4 offer)"),
                M(4,  "Meta-Starter: Pikachu", starter: "pikachu"),
                M(5,  "+5 Trainer Tokens", tokens: 5),
                M(6,  "Hub: Expanded Box (6→8 slots)"),
                M(7,  "Hub: Pokédex Insight"),
                M(8,  "Meta-Starter: Eevee", starter: "eevee"),
                M(9,  "Hub: Trauma Salve Cache"),
                M(10, "+5 Trainer Tokens — Mastery-relic lane opens", tokens: 5),
                M(11, "Hub: Apex Pokémon Reveal"),
                M(12, "Meta-Starter: Riolu", starter: "riolu"),
                M(13, "Hub: Difficulty Modifier Slot +1"),
                M(14, "New difficulty modifier unlocked"),
                M(15, "+8 Trainer Tokens", tokens: 8),
                M(16, "Relic pool +1"),
                M(17, "New difficulty modifier unlocked"),
                M(18, "Hub: Second Starter Slot (Twin Run)"),
                M(19, "Cosmetic: Trainer title / card frame"),
                M(20, "+8 Trainer Tokens", tokens: 8),
                M(21, "New difficulty modifier unlocked"),
                M(22, "Relic pool +1"),
                M(23, "Cosmetic: Pokédex frame"),
                M(24, "Relic pool +1"),
                M(25, "+10 Trainer Tokens", tokens: 10),
                M(26, "Relic pool +1"),
                M(27, "Cosmetic: prestige flair"),
                M(28, "Relic pool +1"),
                M(29, "Cosmetic: prestige flair"),
                M(30, "+10 Trainer Tokens + Prestige cap (Ascension, post-launch)", tokens: 10),
            };
        }

        private static TrainerLevelMilestone M(int level, string desc, int tokens = 0, string starter = null)
        {
            TrainerLevelMilestone m = new() { Level = level, TrainerTokens = tokens, Description = desc };
            if (!string.IsNullOrEmpty(starter)) m.StarterIds.Add(starter);
            return m;
        }
    }
}
