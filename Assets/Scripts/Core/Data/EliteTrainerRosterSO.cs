using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §7.5.1 (CL-024) — weighted Elite Trainer roster per Region.
    // MapRNG-deterministic occupant selection: Rival-primary, Giovanni (R3), Specialist fallback.
    // Weights: R1 Rival 80 / Specialist 20 · R2 Rival 60 / Specialist 40 · R3 Rival 40 / Giovanni 30 / Specialist 30.
    [CreateAssetMenu(fileName = "EliteTrainerRoster_R", menuName = "Project Ascendant/Elite Trainer Roster", order = 161)]
    public class EliteTrainerRosterSO : ScriptableObject
    {
        [Header("Region Identity")]
        [Tooltip("Zero-indexed Region (0=R1, 1=R2, 2=R3).")]
        public int RegionIndex;

        [Header("Weighted Occupant Pool — §7.5.1 (CL-024)")]
        [Tooltip("All candidate Elite Trainers for this Region. Weights are systems-designer-tunable.")]
        public List<EliteOccupantEntry> OccupantPool;

        [Header("GDD Reference")]
        [Tooltip("§7.5.1 — Elite Trainer RNG-weighted roster.")]
        public string GDDReference = "§7.5.1";
    }

    [Serializable]
    public struct EliteOccupantEntry
    {
        [Tooltip("The Elite Trainer occupant (Rival, Giovanni, or Specialist).")]
        public EliteTrainerSO Occupant;

        [Tooltip("Relative weight (R1: Rival 80 / Specialist 20; R2: Rival 60 / Specialist 40; R3: Rival 40 / Giovanni 30 / Specialist 30).")]
        public float Weight;
    }
}
