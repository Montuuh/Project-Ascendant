using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.EditorSetup
{
    // Authors / refreshes the RunContentCatalog asset from the authored VS content (Project Ascendant
    // ▸ Seed Run Content Catalog). The single content catalog RunLauncher loads at runtime.
    public static class VS_RunCatalogSeeder
    {
        private const string ASSET_PATH = "Assets/ScriptableObjects/VS/Configs/RunContentCatalog.asset";

        [MenuItem("Project Ascendant/Seed Run Content Catalog")]
        public static void Seed()
        {
            RunContentCatalogSO catalog = AssetDatabase.LoadAssetAtPath<RunContentCatalogSO>(ASSET_PATH);
            bool isNew = catalog == null;
            if (isNew) catalog = ScriptableObject.CreateInstance<RunContentCatalogSO>();

            catalog.MapConfig = First<MapGenerationConfigSO>();
            catalog.Economy = First<EconomyConfigSO>();
            catalog.WildConfig = First<WildEncounterConfigSO>();
            catalog.ShopConfig = First<RegionShopConfigSO>();
            catalog.MysteryConfig = First<MysteryConfigSO>();
            catalog.BattleConfig = First<BattleConfigSO>();
            catalog.Pokeball = ByName<ConsumableSO>("pokeball");
            catalog.Potion = ByName<ConsumableSO>("potion");
            catalog.Archetypes = All<TrainerArchetypeSO>();
            catalog.Elite = First<EliteTrainerSO>();
            catalog.Gym = First<GymLeaderSO>();
            catalog.MysteryEvents = All<MysteryEventSO>();
            catalog.Relics = All<RelicSO>();
            catalog.Consumables = All<ConsumableSO>();
            catalog.HeldItems = All<HeldItemSO>();
            catalog.TMs = All<TMSO>();
            catalog.Starters = new List<PokemonSpeciesSO>
            {
                ByName<PokemonSpeciesSO>("Bulbasaur"),
                ByName<PokemonSpeciesSO>("Charmander"),
                ByName<PokemonSpeciesSO>("Squirtle"),
            }.Where(s => s != null).ToList();

            if (isNew)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ASSET_PATH)!);
                AssetDatabase.CreateAsset(catalog, ASSET_PATH);
            }
            else
            {
                EditorUtility.SetDirty(catalog);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RunCatalog] {(isNew ? "Created" : "Updated")} {ASSET_PATH} — " +
                      $"archetypes={catalog.Archetypes.Count}, mystery={catalog.MysteryEvents.Count}, " +
                      $"relics={catalog.Relics.Count}, consumables={catalog.Consumables.Count}, " +
                      $"heldItems={catalog.HeldItems.Count}, TMs={catalog.TMs.Count}, starters={catalog.Starters.Count}, " +
                      $"elite={(catalog.Elite ? catalog.Elite.name : "MISSING")}, gym={(catalog.Gym ? catalog.Gym.name : "MISSING")}, " +
                      $"pokeball={(catalog.Pokeball ? "ok" : "MISSING")}, potion={(catalog.Potion ? "ok" : "MISSING")}");
        }

        private static T First<T>() where T : Object
        {
            string[] g = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return g.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g[0]));
        }

        private static List<T> All<T>() where T : Object =>
            AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null).ToList();

        private static T ByName<T>(string nameHint) where T : Object
        {
            foreach (string g in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant() == nameHint.ToLowerInvariant())
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            // fall back to "contains" if no exact filename match
            foreach (string g in AssetDatabase.FindAssets($"t:{typeof(T).Name}"))
            {
                string path = AssetDatabase.GUIDToAssetPath(g);
                if (Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Contains(nameHint.ToLowerInvariant()))
                    return AssetDatabase.LoadAssetAtPath<T>(path);
            }
            return null;
        }
    }
}
