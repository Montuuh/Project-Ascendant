using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Editor
{
    // Reorganises Assets/ScriptableObjects/VS/ from flat type-folders to
    // per-Pokémon-line subfolders so related assets are easy to find in the
    // Project window.
    //
    // SAFE to run multiple times — skips assets that are already in the
    // correct location. Uses AssetDatabase.MoveAsset() so GUIDs are preserved
    // (all existing cross-references stay valid).
    //
    // After running, the layout becomes:
    //
    //   Branches/
    //     Bulbasaur/   bulbasaur_vanguard, ivysaur_vanguard_va1, …
    //     Squirtle/    squirtle_vanguard, wartortle_vanguard_va1, …
    //     …
    //   Species/
    //     Starters/
    //       Bulbasaur/ Bulbasaur, Ivysaur_Vanguard, Venusaur_VanguardA1, …
    //       Squirtle/  …
    //     Wild/
    //       Caterpie/  Caterpie, Metapod, Butterfree
    //       …
    //   Abilities/
    //     Bulbasaur/   overgrow, chlorophyll, …
    //     …
    //   Moves/          ← already per-Pokémon; untouched
    //   GrowthCurves/   ← only 6 assets; not worth reorganising
    public static class VS_AssetReorganiser
    {
        private const string ROOT = "Assets/ScriptableObjects/VS";

        [MenuItem("Project Ascendant/Reorganise Assets by Pokémon")]
        public static void Reorganise()
        {
            // ── Phase 1 (outside batch): discover roots + create all folders ──
            // AssetDatabase.CreateFolder() inside StartAssetEditing() writes the
            // directory to disk but does NOT register it in the asset database
            // until the batch ends. MoveAsset() then fails with "Parent directory
            // is not in asset database." Fix: create all folders first, Refresh
            // to register them, then start the batch for the actual moves.

            Dictionary<PokemonSpeciesSO, string> lineRoots = DiscoverLineRoots();

            if (lineRoots.Count == 0)
            {
                Debug.LogWarning("[Reorganiser] No line roots found. " +
                    "Run the VS_ContentSeeder first, or check that Species SOs exist.");
                return;
            }

            // Pre-create every target folder (idempotent — EnsureFolder skips
            // folders that already exist in the asset database).
            foreach (var kv in lineRoots)
            {
                PokemonSpeciesSO root = kv.Key;
                string lineName       = kv.Value;

                bool isWild = IsWildLine(root);
                string speciesRoot = isWild
                    ? $"{ROOT}/Species/Wild"
                    : $"{ROOT}/Species/Starters";

                EnsureFolder($"{ROOT}/Branches/{lineName}");
                EnsureFolder($"{ROOT}/Abilities/{lineName}");
                EnsureFolder($"{speciesRoot}/{lineName}");
            }

            // Force Unity to register all newly-created folders before the batch.
            AssetDatabase.Refresh();

            // ── Phase 2 (inside batch): move all assets ────────────────────────
            AssetDatabase.StartAssetEditing();
            try
            {
                int moved = 0;

                foreach (var kv in lineRoots)
                {
                    PokemonSpeciesSO root = kv.Key;
                    string lineName       = kv.Value;  // e.g. "Bulbasaur"

                    bool isWild = IsWildLine(root);
                    string speciesRoot = isWild
                        ? $"{ROOT}/Species/Wild"
                        : $"{ROOT}/Species/Starters";

                    // BFS through all evolution stages + branches in this line
                    var visitedSpecies  = new HashSet<PokemonSpeciesSO>();
                    var visitedBranches = new HashSet<EvolutionBranchSO>();
                    var queue           = new Queue<PokemonSpeciesSO>();
                    queue.Enqueue(root);

                    while (queue.Count > 0)
                    {
                        PokemonSpeciesSO sp = queue.Dequeue();
                        if (!visitedSpecies.Add(sp)) continue;

                        // Move the species SO itself
                        moved += MoveIfNeeded(sp, $"{speciesRoot}/{lineName}");

                        // Move its ability
                        if (sp.PrimaryAbility != null)
                            moved += MoveIfNeeded(sp.PrimaryAbility,
                                $"{ROOT}/Abilities/{lineName}");

                        // Walk branches
                        if (sp.Branches == null) continue;
                        foreach (EvolutionBranchSO branch in sp.Branches)
                        {
                            if (branch == null || !visitedBranches.Add(branch)) continue;

                            moved += MoveIfNeeded(branch,
                                $"{ROOT}/Branches/{lineName}");

                            // Walk sub-branches (Stage 2 choices)
                            if (branch.SubBranches != null)
                            {
                                foreach (EvolutionBranchSO sub in branch.SubBranches)
                                {
                                    if (sub == null || !visitedBranches.Add(sub)) continue;
                                    moved += MoveIfNeeded(sub,
                                        $"{ROOT}/Branches/{lineName}");

                                    // Queue the evolved species from sub-branch
                                    if (sub.EvolvedSpecies != null)
                                        queue.Enqueue(sub.EvolvedSpecies);
                                }
                            }

                            // Queue the evolved species from this branch
                            if (branch.EvolvedSpecies != null)
                                queue.Enqueue(branch.EvolvedSpecies);
                        }
                    }
                }

                Debug.Log($"[Reorganiser] Done — {moved} asset(s) moved. " +
                    "Run 'Project Ascendant → Run Bulk Validator' to verify integrity.");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        // ── Line-root discovery ────────────────────────────────────────────
        // Loads all branch SOs, collects every EvolvedSpecies reference, then
        // returns all species SOs that are NOT in that set — i.e. base forms.
        private static Dictionary<PokemonSpeciesSO, string> DiscoverLineRoots()
        {
            // Collect all species that appear as EvolvedSpecies in some branch
            var evolved = new HashSet<PokemonSpeciesSO>();
            string[] branchGuids = AssetDatabase.FindAssets("t:EvolutionBranchSO");
            foreach (string guid in branchGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                EvolutionBranchSO br =
                    AssetDatabase.LoadAssetAtPath<EvolutionBranchSO>(path);
                if (br == null) continue;
                if (br.EvolvedSpecies != null)
                    evolved.Add(br.EvolvedSpecies);
            }

            // All species not in 'evolved' are line roots
            var roots = new Dictionary<PokemonSpeciesSO, string>();
            string[] spGuids = AssetDatabase.FindAssets("t:PokemonSpeciesSO");
            foreach (string guid in spGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                PokemonSpeciesSO sp =
                    AssetDatabase.LoadAssetAtPath<PokemonSpeciesSO>(path);
                if (sp == null) continue;
                if (!evolved.Contains(sp))
                    roots[sp] = sp.DisplayName;
            }

            return roots;
        }

        // ── Helpers ────────────────────────────────────────────────────────

        // Returns 1 if the asset was moved, 0 if it was already in the right place.
        private static int MoveIfNeeded(Object asset, string targetFolder)
        {
            string currentPath = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(currentPath)) return 0;

            string currentFolder = System.IO.Path.GetDirectoryName(currentPath)
                .Replace('\\', '/');

            if (currentFolder == targetFolder) return 0; // already correct

            string fileName  = System.IO.Path.GetFileName(currentPath);
            string newPath   = $"{targetFolder}/{fileName}";

            string error = AssetDatabase.MoveAsset(currentPath, newPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning(
                    $"[Reorganiser] Could not move '{currentPath}' → '{newPath}': {error}");
                return 0;
            }

            return 1;
        }

        // Heuristic: wild lines (Caterpie, Pidgey, Geodude) have no Starters folder path.
        // We detect them by checking whether their current path contains "Wild".
        private static bool IsWildLine(PokemonSpeciesSO sp)
        {
            string path = AssetDatabase.GetAssetPath(sp);
            return path.Contains("/Wild/");
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string parent = folderPath.Substring(0, folderPath.LastIndexOf('/'));
            string name   = folderPath.Substring(folderPath.LastIndexOf('/') + 1);
            EnsureFolder(parent); // recurse to ensure parent exists
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
