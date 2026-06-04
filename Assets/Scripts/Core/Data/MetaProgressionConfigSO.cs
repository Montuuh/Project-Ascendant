using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §6.3 / §6.5.2 / §6.6.1 — what reaching a Trainer Level grants. Data-driven so content
    // (which starters/relics unlock at which level) is authored in the config asset, not code.
    // Unlocked ids accumulate into MetaProgressionSO.UnlockedStarterIds / UnlockedRelicIds at run-end.
    [Serializable]
    public sealed class TrainerLevelMilestone
    {
        [Tooltip("Trainer Level at which these unlocks are granted (1-based).")]
        public int Level = 2;

        [Tooltip("§6.5.2 — meta-starter SpeciesIds unlocked as playable starters at this level.")]
        public List<string> StarterIds = new();

        [Tooltip("§6.6.1 — relic ids added to the run-pool meta-unlock set at this level.")]
        public List<string> RelicIds = new();

        [Tooltip("Player-facing description of what this milestone grants (Hub / run-summary).")]
        public string Description;
    }

    // Per §6.3 (Topic 6) + Epic 11 Task 11.3 — persistent Trainer-XP / Level / Token tuning. Distinct
    // from in-run ProgressionConfigSO (§5.2, per-Pokémon). All values data-driven (systems-designer
    // recalibrates without code). Trainer XP is META — accrued during a run, committed at run-end (§6.3.4).
    //
    // ⚠ NOTE (gap): §6.3.3 gives BOTH a formula `floor(500 × N^1.6)` AND an illustrative table whose
    // values drift from the formula past Level 3 (e.g. table L5 = 5000 vs formula 4595). The FORMULA is
    // treated as canonical here; the table is pacing guidance. Flagged for systems-designer reconciliation.
    [CreateAssetMenu(fileName = "MetaProgressionConfig", menuName = "Project Ascendant/Config/Meta Progression Config")]
    public sealed class MetaProgressionConfigSO : ScriptableObject
    {
        [Header("§6.3.2 — Trainer XP awards (accrued during a run, committed at run-end)")]
        public int CombatClearedXP = 5;     // Wild / Trainer / Elite — flat
        public int RecruitmentXP = 10;      // one-time per species per run
        public int EvolutionXP = 15;        // one-time per Pokémon per run
        public int GymLeaderXP = 50;        // per Gym Leader defeated

        [Header("§6.3.2 — Run-failed bonus = floor(LayersCleared × coeff), capped")]
        public int RunFailedXPPerLayer = 50;
        public int RunFailedXPCap = 400;

        [Header("§6.3.3 — Trainer Level curve: cumulative XP to reach Level L = floor(Base × (L-1)^Exp)")]
        public int LevelCurveBase = 500;
        public float LevelCurveExponent = 1.6f;
        public int MaxTrainerLevel = 30;    // §6.3.3 prestige cap

        [Header("§6.3 / §6.5.2 / §6.6.1 — Trainer-Level milestone unlocks (authored content)")]
        [Tooltip("Reaching a milestone's Level grants its starter/relic ids (committed at run-end). " +
                 "Authored per save in the inspector; empty = no level-gated unlocks yet.")]
        public List<TrainerLevelMilestone> LevelMilestones = new();

        [Header("§6.3.4 — Trainer Token conversion (per run)")]
        public int TokenXPDivisor = 100;    // 1 Token per 100 Trainer XP earned this run
        public int TokensPerRunCap = 50;

        // §6.3.3 — cumulative Trainer XP required to REACH Level L. Level 1 = 0 (run-1 baseline).
        // floor(Base × (L-1)^Exp). Clamped at MaxTrainerLevel.
        public int CumulativeXPForLevel(int level)
        {
            if (level <= 1) return 0;
            int n = level - 1;
            return Mathf.FloorToInt(LevelCurveBase * Mathf.Pow(n, LevelCurveExponent));
        }

        // §6.3.4 — Tokens earned from a run's Trainer-XP haul: floor(runXP / divisor), capped.
        public int TokensForRun(int runXpEarned)
        {
            if (runXpEarned <= 0 || TokenXPDivisor <= 0) return 0;
            int tokens = runXpEarned / TokenXPDivisor;
            return tokens > TokensPerRunCap ? TokensPerRunCap : tokens;
        }
    }
}
