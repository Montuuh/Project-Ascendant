using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2 + Epic 3.2.3 — per-species kill counts and Pokedex mastery tiers.
    // Serialized as part of meta.dat via SaveSystem. Epic 6 owns Pokedex unlock rewards.
    // §4.3.9 is explicitly post-vertical-slice; VS only needs the data shape +
    // RecordKill/EvaluateTier helpers wired so combat-end can later increment.
    [CreateAssetMenu(menuName = "Project Ascendant/Runtime/Pokedex Progress")]
    public class PokedexProgressSO : ScriptableObject
    {
        // Per-species record. Keyed by SpeciesId for addressable compatibility.
        public List<PokedexEntry> Entries;

        // Per §4.3.9.1 — thresholds scale by species rarity.
        // Index order matches RarityTier: Common(0), Uncommon(1), Rare(2), Legendary(3).
        // Defaults match the locked spec; tunable per save via the inspector.
        [Header("§4.3.9.1 — Familiar tier thresholds (defeats by rarity)")]
        [SerializeField] private int[] _familiarThresholdsByRarity = { 10, 5, 2, 2 };

        [Header("§4.3.9.1 — Veteran tier thresholds (defeats by rarity)")]
        [SerializeField] private int[] _veteranThresholdsByRarity = { 30, 15, 5, 5 };

        [Header("§4.3.9.1 — Master tier thresholds (defeats by rarity)")]
        [SerializeField] private int[] _masterThresholdsByRarity = { 50, 25, 10, 10 };

        public PokedexEntry GetOrCreate(string speciesId)
        {
            Entries ??= new List<PokedexEntry>(); // robust for a fresh / JSON-empty instance
            foreach (PokedexEntry entry in Entries)
                if (entry.SpeciesId == speciesId)
                    return entry;

            PokedexEntry newEntry = new() { SpeciesId = speciesId };
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
            PokedexEntry e = GetOrCreate(speciesId);
            e.TimesRecruited++;
            if (e.TimesEncountered == 0) e.TimesEncountered = 1; // a recruit implies a sighting
        }

        // Per §6.9 — has this species ever been seen (encountered, defeated, or recruited)?
        // Drives the Pokédex silhouette/discovery state.
        public bool IsSeen(string speciesId)
        {
            if (Entries == null || string.IsNullOrEmpty(speciesId)) return false;
            foreach (PokedexEntry e in Entries)
                if (e.SpeciesId == speciesId)
                    return e.TimesEncountered > 0 || e.TimesDefeated > 0 || e.TimesRecruited > 0;
            return false;
        }

        // Per §6.9 — count of distinct species seen at least once (Pokédex completion numerator).
        public int SeenSpeciesCount()
        {
            if (Entries == null) return 0;
            int n = 0;
            foreach (PokedexEntry e in Entries)
                if (e.TimesEncountered > 0 || e.TimesDefeated > 0 || e.TimesRecruited > 0) n++;
            return n;
        }

        public void RecordKill(string speciesId, RarityTier rarity)
        {
            PokedexEntry entry = GetOrCreate(speciesId);
            entry.TimesDefeated++;
            entry.MasteryTier = EvaluateTier(entry.TimesDefeated, rarity);
            // Per §4.3.9.1 / §6.9 + Task 11.8.3 — reaching Familiar reveals the species' type-chart entry
            // in the Pokedex. (The §4.3.9 in-combat intent reveal-by-tier is post-VS: the VS shows all
            // intents per the Telegraphed-Tactics pillar, so the VS-active reveal is this Pokedex unlock.)
            if (entry.MasteryTier >= PokedexMasteryTier.Familiar) entry.TypeChartRevealed = true;
        }

        // Per §4.3.9.1 / Task 11.8.3 — the current Pokedex tier for a species (None if unseen).
        public PokedexMasteryTier TierFor(string speciesId)
        {
            if (Entries == null) return PokedexMasteryTier.None;
            foreach (PokedexEntry e in Entries)
                if (e.SpeciesId == speciesId) return e.MasteryTier;
            return PokedexMasteryTier.None;
        }

        // Per §4.3.9.1 — defeats required to reach `tier` for a species of `rarity` (Pokédex HUD
        // progress bars). Returns 0 for None / out-of-range.
        public int DefeatsForTier(PokedexMasteryTier tier, RarityTier rarity)
        {
            int idx = (int)rarity;
            switch (tier)
            {
                case PokedexMasteryTier.Familiar: return AtRarity(_familiarThresholdsByRarity, idx);
                case PokedexMasteryTier.Veteran:  return AtRarity(_veteranThresholdsByRarity, idx);
                case PokedexMasteryTier.Master:   return AtRarity(_masterThresholdsByRarity, idx);
                default: return 0;
            }
        }

        private static int AtRarity(int[] arr, int idx) =>
            (arr != null && idx >= 0 && idx < arr.Length) ? arr[idx] : 0;

        // Per §4.3.9.1 — how many times this species has been defeated (0 if unseen). HUD progress.
        public int DefeatsOf(string speciesId)
        {
            if (Entries == null || string.IsNullOrEmpty(speciesId)) return 0;
            foreach (PokedexEntry e in Entries)
                if (e.SpeciesId == speciesId) return e.TimesDefeated;
            return 0;
        }

        // Per §4.3.9.1 — pure function so it can be unit-tested without state.
        public PokedexMasteryTier EvaluateTier(int defeats, RarityTier rarity)
        {
            int idx = (int)rarity;
            if (idx < 0 || idx >= _masterThresholdsByRarity.Length)
                return PokedexMasteryTier.None;

            if (defeats >= _masterThresholdsByRarity[idx])
                return PokedexMasteryTier.Master;
            if (defeats >= _veteranThresholdsByRarity[idx])
                return PokedexMasteryTier.Veteran;
            if (defeats >= _familiarThresholdsByRarity[idx])
                return PokedexMasteryTier.Familiar;
            return PokedexMasteryTier.None;
        }
    }

    // Per §4.3.9.1 — Pokedex reveal tiers (separate from §5.11 Mastery Move
    // achievement tiers; those gate the active card, these gate UI reveals).
    public enum PokedexMasteryTier { None, Familiar, Veteran, Master }

    [Serializable]
    public class PokedexEntry
    {
        public string SpeciesId;

        [Tooltip("How many times this species has been seen in any form.")]
        public int TimesEncountered;

        [Tooltip("How many times this species has been defeated.")]
        public int TimesDefeated;

        [Tooltip("How many times this species has been recruited.")]
        public int TimesRecruited;

        // Per §4.3.9.1 — mastery tier unlocks reveals in the Pokedex UI.
        public PokedexMasteryTier MasteryTier;

        [Tooltip("Whether the full type chart entry has been revealed for this species.")]
        public bool TypeChartRevealed;
    }
}
