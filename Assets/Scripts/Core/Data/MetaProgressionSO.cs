using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.5 (Topic 6 §6.10) — cross-run persistent meta progression.
    // Serialized to meta.dat after every run end + Pokémart purchase (§9.8).
    // TODO: Epic 3 — full schema per §6.10: TrainerXP, UnlockedStarters, HubUpgrades,
    //       Bestiary, RunHistory, AchievementFlags, HighScore.
    [CreateAssetMenu(menuName = "ProjectAscendant/MetaProgression")]
    public class MetaProgressionSO : ScriptableObject
    {
        // Per §6.3 — Trainer Level gate; 1-based.
        public int TrainerLevel;
    }
}
