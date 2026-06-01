using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.7 + Epic 11 Task 11.5 — what fires an achievement's progress. The wiring layer decides
    // WHEN to report a trigger (e.g. only report WinCombatNoLeadDamage if the Lead took no damage);
    // the AchievementSO just declares which trigger it listens for and how many it needs.
    public enum AchievementTrigger
    {
        None = 0,
        WinCombat,             // any combat won
        WinCombatNoLeadDamage, // combat won with the Lead untouched (Tankmaster)
        WinCombatLeadOnly,     // combat won using only the Lead's cards (Lead Specialist)
        RecruitWild,           // a wild Pokémon recruited
        Evolve,                // an evolution triggered
        DefeatGym,             // a Gym Leader defeated
        ApplyStatus,           // a status condition applied (counter: Status Sniper = 10)
        WinRunNoRevive,        // run won without using a Revive consumable
        WinRunUnderTime,       // run won under a time threshold (Speed Demon)
        WinRunHighTrauma,      // run won with a Pokémon at 5+ Trauma (Trauma Survivor)
    }

    // Per §6.7.1 — an achievement: a challenge goal that grants Trainer XP (§6.3.2 source) and serves
    // as an unlock signal. Data-driven; the condition's "when" lives in the wiring layer, the "what it
    // listens for + reward" lives here. Counter-based goals use TargetCount > 1.
    [CreateAssetMenu(fileName = "New Achievement", menuName = "Project Ascendant/Meta/Achievement")]
    public class AchievementSO : ScriptableObject
    {
        [Header("Identity")]
        public string AchievementId;
        public string DisplayName;

        [TextArea(2, 3)]
        public string Description;

        [Tooltip("§6.7.3 — hidden achievements show '???' until completed (≈20% of the catalog).")]
        public bool Hidden;

        [Header("Condition")]
        public AchievementTrigger Trigger = AchievementTrigger.None;

        [Tooltip("Progress needed to complete. 1 = single-shot; >1 = counter (e.g. Status Sniper = 10).")]
        [Min(1)]
        public int TargetCount = 1;

        [Header("Reward — §6.3.2")]
        [Tooltip("Trainer XP granted once on completion (50–500 per §6.7).")]
        public int TrainerXPReward = 50;

        [Tooltip("GDD section. Per §9.15.")]
        public string GDDReference = "§6.7";
    }
}
