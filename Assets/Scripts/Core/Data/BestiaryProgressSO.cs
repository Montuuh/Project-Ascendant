using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2 + Epic 3.2.3 — per-species kill counts and Bestiary mastery tiers.
    // Serialized as part of meta.dat via SaveSystem. Epic 6 owns Bestiary unlock rewards.
    [CreateAssetMenu(menuName = "Project Ascendant/Runtime/Bestiary Progress")]
    public class BestiaryProgressSO : ScriptableObject
    {
        // Per-species record. Keyed by SpeciesId for addressable compatibility.
        public List<BestiaryEntry> Entries;

        public BestiaryEntry GetOrCreate(string speciesId)
        {
            foreach (BestiaryEntry entry in Entries)
                if (entry.SpeciesId == speciesId)
                    return entry;

            BestiaryEntry newEntry = new() { SpeciesId = speciesId };
            Entries.Add(newEntry);
            return newEntry;
        }
    }

    // Per §4.3.9 (Bestiary mastery) — three tiers of mastery per species.
    public enum BestiaryMasteryTier { Witnessed, Studied, Mastered }

    [Serializable]
    public class BestiaryEntry
    {
        public string SpeciesId;

        [Tooltip("How many times this species has been seen in any form.")]
        public int TimesEncountered;

        [Tooltip("How many times this species has been defeated.")]
        public int TimesDefeated;

        [Tooltip("How many times this species has been recruited.")]
        public int TimesRecruited;

        // Per §4.3.9 — mastery tier unlocks move-specific information in the Bestiary.
        public BestiaryMasteryTier MasteryTier;

        [Tooltip("Whether the full type chart entry has been revealed for this species.")]
        public bool TypeChartRevealed;
    }
}
