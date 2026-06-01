using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.7.1 + Epic 11 Task 11.5.2 — the VS launch achievement set (10), built as runtime instances
    // (no authored .asset files yet; same default-instance pattern as the difficulty modifiers). ~20%
    // hidden per §6.7.3. Cached static list — read-only, shared by the detection wiring + the PC Terminal.
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
            A("first_steps",     "First Steps",     "Win your first combat.",                        AchievementTrigger.WinCombat,             1,  50),
            A("recruiter",       "Recruiter",       "Recruit your first wild Pokémon.",              AchievementTrigger.RecruitWild,           1,  50),
            A("evolver",         "Evolver",         "Trigger your first evolution.",                 AchievementTrigger.Evolve,                1, 100),
            A("boulder_breaker", "Boulder Breaker", "Defeat the Region 1 Gym Leader.",               AchievementTrigger.DefeatGym,             1, 200),
            A("status_sniper",   "Status Sniper",   "Apply 10 status conditions across your runs.",  AchievementTrigger.ApplyStatus,          10, 150),
            A("tankmaster",      "Tankmaster",      "Win a combat without your Lead taking damage.", AchievementTrigger.WinCombatNoLeadDamage, 1, 150),
            A("no_repeat",       "No-Repeat",       "Win a run without using a Revive.",             AchievementTrigger.WinRunNoRevive,        1, 150),
            A("speed_demon",     "Speed Demon",     "Win a run in under 60 minutes.",                AchievementTrigger.WinRunUnderTime,       1, 200),
            A("trauma_survivor", "Trauma Survivor", "Win a run with a Pokémon at 5+ Trauma.",        AchievementTrigger.WinRunHighTrauma,      1, 200, hidden: true),
            A("lead_specialist", "Lead Specialist", "Win a combat using only your Lead's cards.",    AchievementTrigger.WinCombatLeadOnly,     1, 150, hidden: true),
        };

        private static AchievementSO A(string id, string name, string desc, AchievementTrigger t,
                                       int target, int reward, bool hidden = false)
        {
            AchievementSO a = ScriptableObject.CreateInstance<AchievementSO>();
            a.AchievementId = id; a.DisplayName = name; a.Description = desc;
            a.Trigger = t; a.TargetCount = target; a.TrainerXPReward = reward; a.Hidden = hidden;
            return a;
        }
    }
}
