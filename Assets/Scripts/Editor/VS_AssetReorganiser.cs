using System.Collections.Generic;
using System.Linq;
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
            // ── Phase 1: discover roots ────────────────────────────────────────
            Dictionary<PokemonSpeciesSO, string> lineRoots = DiscoverLineRoots();
            if (lineRoots.Count == 0)
            {
                Debug.LogWarning("[Reorganiser] No line roots found. " +
                    "Run the VS_ContentSeeder first, or check that Species SOs exist.");
                return;
            }

            // ── Phase 2: BFS all lines — plan every move ───────────────────────
            //
            // Species and branches each belong to exactly one line, so their
            // target folder is determined during the BFS.
            //
            // Abilities are trickier: some (e.g. tough_claws) are referenced as
            // EvolutionBranchSO.GrantedAbility across *multiple* lines. We must
            // collect all usages first, then:
            //   • unique to one line  → Abilities/{lineName}/
            //   • shared by 2+ lines  → Abilities/Shared/
            //
            // Note: VS_ContentSeeder stores the ability reference only on
            // EvolutionBranchSO.GrantedAbility — PokemonSpeciesSO.PrimaryAbility
            // is the per-evolution primary, but secondaryAbility is not a
            // PokemonSpeciesSO field.

            var speciesMoves = new List<(Object asset, string folder)>();
            var branchMoves  = new List<(Object asset, string folder)>();
            // Ability SO → set of all line names that reference it.
            var abilityLines = new Dictionary<AbilitySO, HashSet<string>>();

            foreach (var kv in lineRoots)
            {
                PokemonSpeciesSO root = kv.Key;
                string lineName       = kv.Value;
                bool isWild = IsWildLine(root);
                string speciesRoot = isWild
                    ? $"{ROOT}/Species/Wild"
                    : $"{ROOT}/Species/Starters";

                var visitedSpecies  = new HashSet<PokemonSpeciesSO>();
                var visitedBranches = new HashSet<EvolutionBranchSO>();
                var queue           = new Queue<PokemonSpeciesSO>();
                queue.Enqueue(root);

                while (queue.Count > 0)
                {
                    PokemonSpeciesSO sp = queue.Dequeue();
                    if (!visitedSpecies.Add(sp)) continue;

                    speciesMoves.Add((sp, $"{speciesRoot}/{lineName}"));

                    // PrimaryAbility: gained at first evolution, typically shared
                    // across all stages in the line.
                    TrackAbility(sp.PrimaryAbility, lineName, abilityLines);

                    if (sp.Branches == null) continue;
                    foreach (EvolutionBranchSO branch in sp.Branches)
                    {
                        if (branch == null || !visitedBranches.Add(branch)) continue;

                        branchMoves.Add((branch, $"{ROOT}/Branches/{lineName}"));

                        // GrantedAbility: the ability unlocked by choosing this
                        // branch (§5.5.1). This is the only reference for
                        // secondaryAbilities in the VS content set.
                        TrackAbility(branch.GrantedAbility, lineName, abilityLines);

                        // Walk sub-branches (Stage 2 choices, if any)
                        if (branch.SubBranches != null)
                        {
                            foreach (EvolutionBranchSO sub in branch.SubBranches)
                            {
                                if (sub == null || !visitedBranches.Add(sub)) continue;
                                branchMoves.Add((sub, $"{ROOT}/Branches/{lineName}"));
                                TrackAbility(sub.GrantedAbility, lineName, abilityLines);
                                if (sub.EvolvedSpecies != null)
                                    queue.Enqueue(sub.EvolvedSpecies);
                            }
                        }

                        if (branch.EvolvedSpecies != null)
                            queue.Enqueue(branch.EvolvedSpecies);
                    }
                }
            }

            // Build the final ability → target-folder map.
            bool needsShared = false;
            var abilityMoves = new Dictionary<AbilitySO, string>();
            foreach (var kv in abilityLines)
            {
                if (kv.Value.Count == 1)
                {
                    // Unique to one line — lives in that line's folder.
                    string line = kv.Value.First();
                    abilityMoves[kv.Key] = $"{ROOT}/Abilities/{line}";
                }
                else
                {
                    // Shared across lines — goes to Abilities/Shared/.
                    abilityMoves[kv.Key] = $"{ROOT}/Abilities/Shared";
                    needsShared = true;
                    Debug.Log($"[Reorganiser] Shared ability: {kv.Key.name} " +
                        $"(lines: {string.Join(", ", kv.Value)}) → Abilities/Shared/");
                }
            }

            // ── Phase 3: create all target folders outside the batch ───────────
            // CreateFolder() inside StartAssetEditing() registers on disk but NOT
            // in the asset DB until the batch ends, causing MoveAsset to fail.
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
            if (needsShared)
                EnsureFolder($"{ROOT}/Abilities/Shared");

            // Force Unity to register all newly-created folders before the batch.
            AssetDatabase.Refresh();

            // ── Phase 4 (inside batch): execute all moves ──────────────────────
            AssetDatabase.StartAssetEditing();
            try
            {
                int moved = 0;

                foreach (var (asset, folder) in speciesMoves)
                    moved += MoveIfNeeded(asset, folder);

                foreach (var (asset, folder) in branchMoves)
                    moved += MoveIfNeeded(asset, folder);

                foreach (var kv in abilityMoves)
                    moved += MoveIfNeeded(kv.Key, kv.Value);

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

        // Adds an ability → lineName mapping entry. Null-safe.
        private static void TrackAbility(
            AbilitySO ability, string lineName,
            Dictionary<AbilitySO, HashSet<string>> map)
        {
            if (ability == null) return;
            if (!map.TryGetValue(ability, out HashSet<string> lines))
                map[ability] = lines = new HashSet<string>();
            lines.Add(lineName);
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
