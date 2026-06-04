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

        // Per §4.3.9.2 — Mastery Moves unlocked via meta-progression (MoveId strings). A Pokémon's
        // Mastery card is only added to the Skill Deck once its MoveId is unlocked here. Permanent +
        // cross-run (persisted in meta.dat). VS unlock path: evolving a Pokémon unlocks its form's Mastery.
        public List<string> UnlockedMasteryMoveIds;

        // Per §4.3.9.1 (Veteran tier) — species (SpeciesId strings) whose Shiny variant is unlocked.
        // Retroactive + species-wide: once unlocked, every owned Pokémon of that species shows Shiny.
        // Permanent + cross-run.
        public List<string> ShinyUnlockedSpeciesIds;

        // Per §6.7 — Hub upgrade flags. Key = upgrade ID, Value = upgrade level.
        public List<StringIntPair> HubUpgrades;

        // Per §6.9 — Pokédex completion milestones already claimed (by PercentThreshold), so each
        // grants its reward exactly once across all runs.
        public List<int> ClaimedPokedexMilestones;

        [Header("Run History")]
        public int TotalRunsCompleted;
        public int TotalRunsAttempted;
        public int HighScore;

        [Header("Achievements")]
        // Per §6.7 — achievement completion flags by achievement ID (one-shot record).
        public List<string> CompletedAchievementIds;
        // Per §6.7 — in-progress counters for multi-step achievements (Key = AchievementId, Value = count).
        // Unity can't serialize Dictionary, so a flat pair list (mirrors HubUpgrades).
        public List<StringIntPair> AchievementProgress;

        [Header("Currency")]
        // Per §6.3 — Trainer Tokens spent at Hub kiosks.
        public int TrainerTokens;

        // Per §4.3.9.2 — is this Mastery Move (by MoveId) unlocked for the Skill Deck?
        public bool IsMasteryUnlocked(string moveId)
            => !string.IsNullOrEmpty(moveId) && UnlockedMasteryMoveIds != null && UnlockedMasteryMoveIds.Contains(moveId);

        // Per §4.3.9.2 — permanently unlock a Mastery Move. Returns true if newly unlocked (caller
        // should persist via SaveSystem.SaveMeta). Idempotent.
        public bool UnlockMastery(string moveId)
        {
            if (string.IsNullOrEmpty(moveId)) return false;
            UnlockedMasteryMoveIds ??= new List<string>();
            if (UnlockedMasteryMoveIds.Contains(moveId)) return false;
            UnlockedMasteryMoveIds.Add(moveId);
            return true;
        }

        // Per §4.3.9.1 — is this species' Shiny variant unlocked (Veteran tier reward)?
        public bool IsShinyUnlocked(string speciesId)
            => !string.IsNullOrEmpty(speciesId) && ShinyUnlockedSpeciesIds != null && ShinyUnlockedSpeciesIds.Contains(speciesId);

        // Per §4.3.9.1 — permanently unlock a species' Shiny variant. Returns true if newly unlocked
        // (caller should persist via SaveSystem.SaveMeta). Idempotent.
        public bool UnlockShiny(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return false;
            ShinyUnlockedSpeciesIds ??= new List<string>();
            if (ShinyUnlockedSpeciesIds.Contains(speciesId)) return false;
            ShinyUnlockedSpeciesIds.Add(speciesId);
            return true;
        }
    }
}
