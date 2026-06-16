// Per Epic 8 Task 8.4 — VS Elite Trainer seeder.
// Authors the single R1 Elite (§7.5): a bespoke "Ace Trainer" fielding 2
// evolved, mixed-type Pokémon (NO type lock per §7.5.1), each with a 2-phase
// design (§4.4.3). Reward = 1 guaranteed Uncommon relic + 25 XP + 300₽ (§7.12).
//
// R1 identity is bespoke because §7.5.1's nominal "Ace Trainer pool" is R3-only
// / out of VS scope (§7.4.3, §7.13). See ⚠ OPEN flag on §7.5.1 + BACKLOG #31.
//
// Menu: Project Ascendant / Seed VS Elite
// Idempotent — recreates the asset. Run AFTER VS_ContentSeeder + VS_ItemSeeder
// (species + relics must exist). Species/relic are resolved by name via
// AssetDatabase.FindAssets so the seeder is robust to nested-folder layout.

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    public static class VS_EliteSeeder
    {
        const string ROOT = "Assets/ScriptableObjects/VS";
        const string ELITES = ROOT + "/Elites";

        [MenuItem("Project Ascendant/Seed VS Elite")]
        public static void SeedAll()
        {
            // Resolve cross-references first; abort loudly if any are missing so
            // we never author a broken asset (the audit test would catch it too).
            PokemonSpeciesSO pidgeotto = FindByName<PokemonSpeciesSO>("Pidgeotto");
            PokemonSpeciesSO ivysaur   = FindByName<PokemonSpeciesSO>("Ivysaur_Vanguard");
            RelicSO relic              = FindByName<RelicSO>("tacticians_coin");

            if (pidgeotto == null || ivysaur == null || relic == null)
            {
                Debug.LogError("[VS_EliteSeeder] Missing cross-reference — "
                    + $"Pidgeotto={pidgeotto}, Ivysaur_Vanguard={ivysaur}, "
                    + $"tacticians_coin={relic}. Run VS_ContentSeeder + VS_ItemSeeder first.");
                return;
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                MkDir(ELITES);
                string path = $"{ELITES}/ace_trainer_r1.asset";
                AssetDatabase.DeleteAsset(path); // idempotent recreate

                EliteTrainerSO elite = ScriptableObject.CreateInstance<EliteTrainerSO>();
                elite.EliteId = "ace_trainer_r1";
                elite.DisplayName = "Ace Trainer";
                elite.TacticalIdentity =
                    "No type lock — fields evolved Pokémon of mixed types. Pidgeotto "
                    + "opens with tempo chip and Feather Dance to shred Attack; Ivysaur "
                    + "sets up with Leech Seed / status, then escalates. At or below 50% "
                    + "HP each Pokémon abandons setup and presses for damage (Phase 2).";
                // §7.5.1 — 2 Pokémon, sequential, each 2-phase. Mixed types
                // (Normal/Flying + Grass/Poison) deliver the no-type-lock test.
                // Levels are provisional — a systems-designer balance pass owns
                // final tuning (a notch above standard trainers, below the Gym).
                elite.Composition = new List<ElitePokemonSlot>
                {
                    new ElitePokemonSlot { Species = pidgeotto, Level = 12, PhaseCount = 2 },
                    new ElitePokemonSlot { Species = ivysaur,   Level = 13, PhaseCount = 2 },
                };
                // Per §7.5.1 (CL-024) — Rare-relic choice (1 of 3). Placeholder: 3 copies of same relic.
                elite.RareRelicChoices = new List<RelicSO> { relic, relic, relic };
                elite.TrainerXPReward = 25;      // §7.12
                elite.PokeDollarReward = 300;    // §7.12
                elite.IsRival = false;           // CL-024 — this is the 20% specialist, not Rival
                elite.GDDReference =
                    "§7.5.1 | R1 Elite — bespoke Ace Trainer, no type lock, 2×2-phase. "
                    + "See ⚠ OPEN gap #31 (Ace Trainer pool R3-only).";

                AssetDatabase.CreateAsset(elite, path);
                EditorUtility.SetDirty(elite);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            Debug.Log("[VS_EliteSeeder] Done — ace_trainer_r1 authored under " + ELITES);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        static T FindByName<T>(string assetName) where T : Object
        {
            string[] guids = AssetDatabase.FindAssets(
                $"t:{typeof(T).Name} {assetName}", new[] { ROOT });
            foreach (string guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == assetName)
                    return AssetDatabase.LoadAssetAtPath<T>(p);
            }
            return null;
        }

        static void MkDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) MkDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
