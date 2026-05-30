using System.IO;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Map;

namespace ProjectAscendant.EditorSetup
{
    // Run-flow smoke harness (Project Ascendant ▸ Run Smoke Harness). Loads the authored
    // RunContentCatalog, builds a live RunController via RunBootstrapper, and auto-walks a seeded
    // Region-1 run Entry→Gym, logging each node to the Console. Combat is auto-resolved (Victory) —
    // this exercises the macro run loop + real content, not the per-turn combat (tested separately).
    // The RUNTIME equivalent is RunLauncher (Boot scene); this is the editor-time version.
    public static class RunSmokeHarness
    {
        private const uint SEED = 20260529u;
        private const string TAG = "[RunSmoke]";
        private const string CATALOG_PATH = "Assets/ScriptableObjects/VS/Configs/RunContentCatalog.asset";

        [MenuItem("Project Ascendant/Run Smoke Harness")]
        public static void Execute()
        {
            RunContentCatalogSO catalog = AssetDatabase.LoadAssetAtPath<RunContentCatalogSO>(CATALOG_PATH);
            if (catalog == null)
            {
                Debug.LogError($"{TAG} No RunContentCatalog at {CATALOG_PATH}. " +
                               "Run 'Project Ascendant ▸ Seed Run Content Catalog' first.");
                return;
            }

            string prevSave = SaveSystem.SaveDirectoryOverride;
            string tempSave = Path.Combine(Path.GetTempPath(), "PA_RunSmoke");
            SaveSystem.SaveDirectoryOverride = tempSave;

            try
            {
                RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
                RunController rc = RunBootstrapper.CreateRunController(
                    catalog, run, new PokemonInstanceFactory(), new RNGStreams(SEED),
                    evt => Debug.Log($"{TAG} » HSM event: {evt.Type}"), out RunContext ctx);

                // Seed a starter (Squirtle if present, else the first) so the Active Team is real.
                PokemonSpeciesSO starter = PickStarter(catalog);
                if (starter != null)
                {
                    RunBootstrapper.SeedStarter(ctx, starter, 5);
                    Debug.Log($"{TAG} Starter: {starter.DisplayName} (Box={ctx.Box.Count})");
                }

                rc.StartRun();
                Debug.Log($"{TAG} Map seeded (seed {SEED}): {rc.Map.LayerCount} layers, " +
                          $"{System.Linq.Enumerable.Count(rc.Map.AllNodes())} nodes, {rc.Map.GymNodes.Count} gym(s).");

                int steps = RunAutoPilot.WalkToEnd(rc, s => Debug.Log($"{TAG} {s}"));

                Debug.Log($"{TAG} ════ RUN {(rc.RunOver ? "ENDED" : "INCOMPLETE")} ════ " +
                          $"final: L{rc.CurrentNode?.Layer} {rc.CurrentNode?.NodeType} · ₽={run.PokeDollars} · " +
                          $"relics={run.HeldRelics?.Count ?? 0} · badges={run.EarnedBadges?.Count ?? 0} · " +
                          $"box={ctx.Box.Count} · nodes/saves={steps}");
            }
            finally
            {
                SaveSystem.SaveDirectoryOverride = prevSave;
                if (Directory.Exists(tempSave)) Directory.Delete(tempSave, recursive: true);
            }
        }

        private static PokemonSpeciesSO PickStarter(RunContentCatalogSO catalog)
        {
            if (catalog.Starters == null || catalog.Starters.Count == 0) return null;
            foreach (PokemonSpeciesSO s in catalog.Starters)
                if (s != null && s.DisplayName != null && s.DisplayName.ToLowerInvariant().Contains("squirtle"))
                    return s;
            return catalog.Starters[0];
        }
    }
}
