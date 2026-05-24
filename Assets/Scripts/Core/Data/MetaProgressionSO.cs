using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.5 (Topic 6 §6.10) — cross-run persistent meta-progression.
    // Serialized to meta.dat after every run end and Pokémart purchase (§9.8).
    // Bestiary detail tracked separately in BestiaryProgressSO.
    [CreateAssetMenu(menuName = "Project Ascendant/Runtime/Meta Progression")]
    public class MetaProgressionSO : ScriptableObject
    {
        [Header("Trainer Progression — §6.3")]
        // Per §6.3 — Trainer Level gates meta-unlocks. 1-based.
        public int TrainerLevel = 1;
        public int TrainerXP;
        public int TrainerXPToNextLevel;

        [Header("Unlocked Content")]
        // Per §6.5 — species unlocked as playable starters (SpeciesId strings for addressable loading).
        public List<string> UnlockedStarterIds;

        // Per §6.6.1 — which relics are in the meta-unlock pool (tracks MetaTier unlock state).
        // Stored as relic IDs; Epic 6 resolves to RelicSO refs.
        public List<string> UnlockedRelicIds;

        // Per §6.7 — Hub upgrade flags. Key = upgrade ID, Value = upgrade level.
        public List<StringIntPair> HubUpgrades;

        [Header("Run History")]
        public int TotalRunsCompleted;
        public int TotalRunsAttempted;
        public int HighScore;

        [Header("Achievements")]
        // Per §6.X — achievement completion flags by achievement ID.
        public List<string> CompletedAchievementIds;

        [Header("Currency")]
        // Per §6.3 — Trainer Tokens spent at Hub kiosks.
        public int TrainerTokens;
    }
}
