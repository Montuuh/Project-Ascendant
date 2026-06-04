using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2 + Epic 3.2.3 — per-species kill counts and Bestiary mastery tiers.
    // Serialized as part of meta.dat via SaveSystem. Epic 6 owns Bestiary unlock rewards.
    // §4.3.9 is explicitly post-vertical-slice; VS only needs the data shape +
    // RecordKill/EvaluateTier helpers wired so combat-end can later increment.
    [CreateAssetMenu(menuName = "Project Ascendant/Runtime/Bestiary Progress")]
    public class BestiaryProgressSO : ScriptableObject
    {
        // Per-species record. Keyed by SpeciesId for addressable compatibility.
        public List<BestiaryEntry> Entries;

        // Per §4.3.9.1 — thresholds scale by species rarity.
        // Index order matches RarityTier: Common(0), Uncommon(1), Rare(2), Legendary(3).
        // Defaults match the locked spec; tunable per save via the inspector.
        [Header("§4.3.9.1 — Familiar tier thresholds (defeats by rarity)")]
        [SerializeField] private int[] _familiarThresholdsByRarity = { 10, 5, 2, 2 };

        [Header("§4.3.9.1 — Veteran tier thresholds (defeats by rarity)")]
        [SerializeField] private int[] _veteranThresholdsByRarity = { 30, 15, 5, 5 };

        [Header("§4.3.9.1 — Master tier thresholds (defeats by rarity)")]
        [SerializeField] private int[] _masterThresholdsByRarity = { 50, 25, 10, 10 };

        public BestiaryEntry GetOrCreate(string speciesId)
        {
            Entries ??= new List<BestiaryEntry>(); // robust for a fresh / JSON-empty instance
            foreach (BestiaryEntry entry in Entries)
                if (entry.SpeciesId == speciesId)
                    return entry;

            BestiaryEntry newEntry = new() { SpeciesId = speciesId };
            Entries.Add(newEntry);
            return newEntry;
        }

        // Per Epic 7 Task 7.9.2 — combat-end faint hook calls this once per
        // defeated enemy. Re-evaluates MasteryTier so reveals stay current.
        // §4.3.9.2 / §5.11 Mastery Move unlocks are tracked separately on
        // MetaProgressionSO and are NOT a side-effect of RecordKill.
        // Per §6.9 / §4.3.9.1 — Pokédex discovery: the species was encountered (seen) this combat.
        // Idempotent for "seen at all" (TimesEncountered just counts sightings). Safe to call per combat.
        public void RecordSeen(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return;
            GetOrCreate(speciesId).TimesEncountered++;
        }

        // Per §6.9 / §7.3 — the species was recruited (captured) this run.
        public void RecordRecruit(string speciesId)
        {
            if (string.IsNullOrEmpty(speciesId)) return;
            BestiaryEntry e = GetOrCreate(speciesId);
            e.TimesRecruited++;
            if (e.TimesEncountered == 0) e.TimesEncountered = 1; // a recruit implies a sighting
        }

        // Per §6.9 — has this species ever been seen (encountered, defeated, or recruited)?
        // Drives the Pokédex silhouette/discovery state.
        public bool IsSeen(string speciesId)
        {
            if (Entries == null || string.IsNullOrEmpty(speciesId)) return false;
            foreach (BestiaryEntry e in Entries)
                if (e.SpeciesId == speciesId)
                    return e.TimesEncountered > 0 || e.TimesDefeated > 0 || e.TimesRecruited > 0;
            return false;
        }

        // Per §6.9 — count of distinct species seen at least once (Pokédex completion numerator).
        public int SeenSpeciesCount()
        {
            if (Entries == null) return 0;
            int n = 0;
            foreach (BestiaryEntry e in Entries)
                if (e.TimesEncountered > 0 || e.TimesDefeated > 0 || e.TimesRecruited > 0) n++;
            return n;
        }

        public void RecordKill(string speciesId, RarityTier rarity)
        {
            BestiaryEntry entry = GetOrCreate(speciesId);
            entry.TimesDefeated++;
            entry.MasteryTier = EvaluateTier(entry.TimesDefeated, rarity);
            // Per §4.3.9.1 / §6.9 + Task 11.8.3 — reaching Familiar reveals the species' type-chart entry
            // in the Bestiary. (The §4.3.9 in-combat intent reveal-by-tier is post-VS: the VS shows all
            // intents per the Telegraphed-Tactics pillar, so the VS-active reveal is this Bestiary unlock.)
            if (entry.MasteryTier >= BestiaryMasteryTier.Familiar) entry.TypeChartRevealed = true;
        }

        // Per §4.3.9.1 / Task 11.8.3 — the current Bestiary tier for a species (None if unseen).
        public BestiaryMasteryTier TierFor(string speciesId)
        {
            if (Entries == null) return BestiaryMasteryTier.None;
            foreach (BestiaryEntry e in Entries)
                if (e.SpeciesId == speciesId) return e.MasteryTier;
            return BestiaryMasteryTier.None;
        }

        // Per §4.3.9.1 — pure function so it can be unit-tested without state.
        public BestiaryMasteryTier EvaluateTier(int defeats, RarityTier rarity)
        {
            int idx = (int)rarity;
            if (idx < 0 || idx >= _masterThresholdsByRarity.Length)
                return BestiaryMasteryTier.None;

            if (defeats >= _masterThresholdsByRarity[idx])
                return BestiaryMasteryTier.Master;
            if (defeats >= _veteranThresholdsByRarity[idx])
                return BestiaryMasteryTier.Veteran;
            if (defeats >= _familiarThresholdsByRarity[idx])
                return BestiaryMasteryTier.Familiar;
            return BestiaryMasteryTier.None;
        }
    }

    // Per §4.3.9.1 — Bestiary reveal tiers (separate from §5.11 Mastery Move
    // achievement tiers; those gate the active card, these gate UI reveals).
    public enum BestiaryMasteryTier { None, Familiar, Veteran, Master }

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

        // Per §4.3.9.1 — mastery tier unlocks reveals in the Bestiary UI.
        public BestiaryMasteryTier MasteryTier;

        [Tooltip("Whether the full type chart entry has been revealed for this species.")]
        public bool TypeChartRevealed;
    }
}
