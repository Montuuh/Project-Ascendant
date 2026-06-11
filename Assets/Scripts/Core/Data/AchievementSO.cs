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

        // CL-020 (Q19) — VS-triggerable additions (R1→Gym 1):
        RecruitSpeciesCount,   // distinct species recruited lifetime (counter)
        RecruitAtFullBox,      // recruited while the Box was full
        CatchRareWild,         // caught a Rare-tier wild Pokémon
        WinCombatNoDamage,     // combat won with the whole team untouched
        WinCombatRangedOnly,   // combat won using only Ranged moves
        WinCombatManySwaps,    // combat won with 5+ manual swaps (counter target)
        OverkillHit,           // a single hit ≥3× the target's remaining HP
        EvolveAllBranches,     // evolved into all 3 branches of one species (lifetime)
        FieldFinalStageTeam,   // fielded an all-final-stage Active Team in one combat
    }

    // Per §6.7.0 (CL-020 — Q19) — medal tier sets the reward band: Bronze 50–100 XP / Silver 150–250 /
    // Gold 250–400 +2 Tokens / Platinum 400–500 +5 Tokens. The per-achievement XP/Token values are
    // authored within the tier's band (the tier is the readable classification + the UI medal).
    public enum AchievementTier { Bronze, Silver, Gold, Platinum }

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

        [Header("Reward — §6.7.0 medal tiers (CL-020)")]
        [Tooltip("§6.7.0 — medal tier; sets the reward band + the UI medal.")]
        public AchievementTier Tier = AchievementTier.Bronze;

        [Tooltip("Trainer XP granted once on completion (§6.7.0 band by tier; 50–500).")]
        public int TrainerXPReward = 50;

        [Tooltip("§6.7.0 — Trainer Tokens granted once on completion. Gold +2 / Platinum +5; " +
                 "Bronze/Silver grant 0. The agency currency (CL-019), spent on Tier-3 Mastery relics.")]
        public int TokenReward;

        [Tooltip("GDD section. Per §9.15.")]
        public string GDDReference = "§6.7";
    }
}
