using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.7.1 / §6.7.1.1 (CL-020 — Q19) — the VS achievement set, built as runtime instances (no
    // authored .asset files yet; same default-instance pattern as the difficulty modifiers). Medal tiers
    // (§6.7.0) set the reward band; Gold/Platinum also grant Trainer Tokens. ~20% hidden (§6.7.3).
    //
    // VS scope: this is the §6.7.1.1 catalog filtered to achievements whose triggers exist in R1→Gym 1.
    // Post-VS catalogue entries (Champion/League ◆, City Gym, Region 3, Master 10, 50 species, etc.) land
    // with their content. TRIGGER WIRING: the run-data triggers (WinCombat / Evolve / RecruitWild /
    // DefeatGym / WinRunHighTrauma) fire in RunEndService; the combat/timer-context triggers (ApplyStatus,
    // WinCombat*Damage/RangedOnly/ManySwaps/LeadOnly, OverkillHit, WinRunNoRevive/UnderTime, Recruit*,
    // CatchRareWild, Evolve/FieldFinalStage*) are catalogued but await the achievement-hook follow-up
    // (BACKLOG: "achievement combat/timer trigger hooks") — the same bucket the original 10 already used.
    public static class AchievementCatalog
    {
        private static List<AchievementSO> _all;

        // Rebuild if uncached OR if the cached instances were destroyed (Unity "fake-null" — e.g. a test
        // run that destroyed loose ScriptableObjects between fixtures). Guards against stale dispatch.
        public static IReadOnlyList<AchievementSO> All
        {
            get
            {
                if (_all == null || _all.Count == 0 || _all[0] == null) _all = Build();
                return _all;
            }
        }

        private static List<AchievementSO> Build() => new()
        {
            // ── Bronze (easy, 50–100 XP) ──
            A("first_steps",      "First Blood",      "Win your first combat.",                          AchievementTrigger.WinCombat,             1, AchievementTier.Bronze,  50),
            A("recruiter",        "Gotcha!",          "Recruit your first wild Pokémon.",                AchievementTrigger.RecruitWild,           1, AchievementTier.Bronze,  50),
            A("evolver",          "Growing Up",       "Trigger your first evolution.",                   AchievementTrigger.Evolve,                1, AchievementTier.Bronze, 100),
            A("welcome_wagon",    "Welcome Wagon",    "Recruit 10 different species (lifetime).",        AchievementTrigger.RecruitSpeciesCount,  10, AchievementTier.Bronze, 100),
            A("overkill",         "Overkill",         "Land a single hit dealing 3× the target's HP.",   AchievementTrigger.OverkillHit,           1, AchievementTier.Bronze, 100, hidden: true),

            // ── Silver (medium, 150–250 XP) ──
            A("boulder_breaker",  "Badge Collector",  "Defeat the Region 1 Gym Leader.",                 AchievementTrigger.DefeatGym,             1, AchievementTier.Silver, 200),
            A("status_sniper",    "Status Sniper",    "Apply 10 status conditions across your runs.",    AchievementTrigger.ApplyStatus,          10, AchievementTier.Silver, 150),
            A("tankmaster",       "Tankmaster",       "Win a combat without your Lead taking damage.",   AchievementTrigger.WinCombatNoLeadDamage, 1, AchievementTier.Silver, 150),
            A("untouchable",      "Untouchable",      "Win a combat without taking any damage.",         AchievementTrigger.WinCombatNoDamage,     1, AchievementTier.Silver, 200),
            A("sharpshooter",     "Sharpshooter",     "Win a combat using only Ranged moves.",           AchievementTrigger.WinCombatRangedOnly,   1, AchievementTier.Silver, 200),
            A("swap_maestro",     "Swap Maestro",     "Win a combat with 5+ manual swaps.",              AchievementTrigger.WinCombatManySwaps,    5, AchievementTier.Silver, 150),
            A("catch_of_the_day", "Catch of the Day", "Catch a Rare-tier wild Pokémon.",                 AchievementTrigger.CatchRareWild,         1, AchievementTier.Silver, 200),
            A("full_house",       "Full House",       "Recruit a Pokémon while your Box is full.",       AchievementTrigger.RecruitAtFullBox,      1, AchievementTier.Silver, 150, hidden: true),
            A("full_bloom",       "Full Bloom",       "Field an all-final-stage Active Team in combat.", AchievementTrigger.FieldFinalStageTeam,   1, AchievementTier.Silver, 250),

            // ── Gold (hard, 250–400 XP, +2 Tokens) ──
            A("one_mon_army",     "One-Mon Army",     "Win a combat using only your Lead's cards.",      AchievementTrigger.WinCombatLeadOnly,     1, AchievementTier.Gold,   300, tokens: 2, hidden: true),
            A("branch_out",       "Branch Out",       "Evolve into all 3 branches of one species.",      AchievementTrigger.EvolveAllBranches,     1, AchievementTier.Gold,   300, tokens: 2),
            A("no_repeat",        "No Rest",          "Win a run without using a Revive.",               AchievementTrigger.WinRunNoRevive,        1, AchievementTier.Gold,   300, tokens: 2),
            A("trauma_survivor",  "Trauma Survivor",  "Win a run with a Pokémon at 5+ Trauma.",          AchievementTrigger.WinRunHighTrauma,      1, AchievementTier.Gold,   300, tokens: 2, hidden: true),

            // ── Platinum (very hard, 400–500 XP, +5 Tokens) ──
            A("speed_demon",      "Speedrunner",      "Win a run in under 60 minutes.",                  AchievementTrigger.WinRunUnderTime,       1, AchievementTier.Platinum, 500, tokens: 5),
        };

        private static AchievementSO A(string id, string name, string desc, AchievementTrigger t,
                                       int target, AchievementTier tier, int xp, int tokens = 0, bool hidden = false)
        {
            AchievementSO a = ScriptableObject.CreateInstance<AchievementSO>();
            a.AchievementId = id; a.DisplayName = name; a.Description = desc;
            a.Trigger = t; a.TargetCount = target; a.Tier = tier; a.TrainerXPReward = xp;
            a.TokenReward = tokens; a.Hidden = hidden;
            return a;
        }
    }
}
