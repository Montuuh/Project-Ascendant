using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.EditorSetup
{
    // Per CL-024 — one-shot Elite split patch: adds 3 Rare relics to existing Elite, creates R1 roster + placeholder Rival + placeholder EliteWild.
    // Run once via "Project Ascendant/CL-024 Patch Elite Split Content" to reach GREEN (1187/1187).
    public static class CL024_ElitePatch
    {
        [MenuItem("Project Ascendant/CL-024 Patch Elite Split Content")]
        public static void Patch()
        {
            // 1. Find existing ace_trainer_r1 Elite asset and add 3 Rare relics.
            EliteTrainerSO existingElite = FindByName<EliteTrainerSO>("ace_trainer_r1");
            if (existingElite == null)
            {
                Debug.LogError("[CL-024] ace_trainer_r1 EliteTrainerSO not found. Create it first.");
                return;
            }

            List<RelicSO> rareRelics = FindRelicsOfRarity(RarityTier.Rare, 3);
            if (rareRelics.Count < 3)
            {
                Debug.LogError($"[CL-024] Found only {rareRelics.Count} Rare relics, need 3. Create more Rare relics.");
                return;
            }

            existingElite.RareRelicChoices = rareRelics;
            EditorUtility.SetDirty(existingElite);
            Debug.Log($"[CL-024] Patched {existingElite.name} with 3 Rare relics: {string.Join(", ", rareRelics.Select(r => r.name))}");

            // 2. Create R1 EliteTrainerRosterSO (Rival 80% + Specialist 20%).
            EliteTrainerRosterSO r1Roster = EnsureRoster("EliteTrainerRoster_R1", 0);
            r1Roster.OccupantPool = new List<EliteOccupantEntry>
            {
                new EliteOccupantEntry { Occupant = existingElite, Weight = 20f }
            };
            EditorUtility.SetDirty(r1Roster);
            Debug.Log($"[CL-024] Created/updated {r1Roster.name} (specialist-only placeholder; Rival TBD).");

            // 3. Create placeholder R1 EliteWildSO (reuse existing VS species as boss-wild).
            EliteWildSO r1Wild = EnsureEliteWild("EliteWild_R1_Snorlax", "snorlax_elite_wild",
                "Snorlax", FindFirstSpecies());
            EditorUtility.SetDirty(r1Wild);
            Debug.Log($"[CL-024] Created/updated {r1Wild.name} (placeholder species: {r1Wild.Species?.name ?? "MISSING"}).");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CL-024] Patch complete. Re-run 'Seed Run Content Catalog' to wire rosters + wild, then run EditMode tests.");
        }

        private static EliteTrainerRosterSO EnsureRoster(string assetName, int regionIndex)
        {
            string path = $"Assets/ScriptableObjects/VS/Elites/{assetName}.asset";
            EliteTrainerRosterSO roster = AssetDatabase.LoadAssetAtPath<EliteTrainerRosterSO>(path);
            if (roster != null) return roster;

            roster = ScriptableObject.CreateInstance<EliteTrainerRosterSO>();
            roster.RegionIndex = regionIndex;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(roster, path);
            return roster;
        }

        private static EliteWildSO EnsureEliteWild(string assetName, string wildId, string displayName, PokemonSpeciesSO species)
        {
            string path = $"Assets/ScriptableObjects/VS/Elites/{assetName}.asset";
            EliteWildSO wild = AssetDatabase.LoadAssetAtPath<EliteWildSO>(path);
            if (wild != null) return wild;

            wild = ScriptableObject.CreateInstance<EliteWildSO>();
            wild.EliteWildId = wildId;
            wild.DisplayName = displayName;
            wild.Species = species;
            wild.Level = 15;
            wild.PhaseCount = 2;
            // Assign first available Rare relic as DefeatRelic.
            List<RelicSO> rare = FindRelicsOfRarity(RarityTier.Rare, 1);
            wild.DefeatRelic = rare.Count > 0 ? rare[0] : null;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            AssetDatabase.CreateAsset(wild, path);
            return wild;
        }

        private static List<RelicSO> FindRelicsOfRarity(RarityTier rarity, int count)
        {
            string[] guids = AssetDatabase.FindAssets("t:RelicSO");
            List<RelicSO> matches = new();
            foreach (string g in guids)
            {
                RelicSO r = AssetDatabase.LoadAssetAtPath<RelicSO>(AssetDatabase.GUIDToAssetPath(g));
                if (r != null && r.Rarity == rarity)
                {
                    matches.Add(r);
                    if (matches.Count >= count) break;
                }
            }
            return matches;
        }

        private static PokemonSpeciesSO FindFirstSpecies()
        {
            string[] guids = AssetDatabase.FindAssets("t:PokemonSpeciesSO");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static T FindByName<T>(string nameHint) where T : Object
        {
            foreach (string g in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == nameHint.ToLowerInvariant())
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }
    }
}
