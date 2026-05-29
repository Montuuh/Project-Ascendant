using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;
using ProjectAscendant.Map;

namespace ProjectAscendant.EditorSetup
{
    // Run-flow smoke harness — loads the REAL authored VS content, builds a live RunContext +
    // RunController, and walks a seeded Region-1 run Entry→Gym, logging each node to the console.
    // Combat EXECUTION is auto-resolved (Victory) — this exercises the macro run loop + real
    // content (offers, rewards, save-on-entry, loadout lock), not the per-turn combat (tested
    // separately). Run via the coplay bridge: execute_script(RunSmokeHarness, Execute).
    public static class RunSmokeHarness
    {
        private const uint SEED = 20260529u;
        private const string TAG = "[RunSmoke]";

        // Run from the Unity menu: Project Ascendant ▸ Run Smoke Harness. Logs a full Region-1
        // run (Entry→Gym) to the Console against the authored VS content. Combat auto-resolved.
        [MenuItem("Project Ascendant/Run Smoke Harness")]
        public static void Execute()
        {
            string prevSave = SaveSystem.SaveDirectoryOverride;
            string tempSave = Path.Combine(Path.GetTempPath(), "PA_RunSmoke");
            SaveSystem.SaveDirectoryOverride = tempSave;
            int saveCount = 0;

            try
            {
                RunContext ctx = BuildContext(out RunStateSO run, out PokemonSpeciesSO starter);
                if (ctx == null) return;

                NodeControllerFactory factory = new();
                ctx.RegisterBuilders(factory);

                // Seed the Box with the starter + commit it as the Active Team (so a real team exists).
                PokemonInstance starterInst = ctx.PokemonFactory.Create(starter, 5);
                ctx.Box.Members.Add(starterInst);
                ctx.Loadout.Confirm(new List<int> { 0 }, 0);
                Debug.Log($"{TAG} Starter: {starter.DisplayName} (Box={ctx.Box.Count}, ActiveTeam={run.ActiveTeamIndices?.Count})");

                RunController rc = new(ctx, factory, evt => Debug.Log($"{TAG} » HSM event: {evt.Type}"));

                rc.StartRun();
                int nodes = rc.Map.AllNodes().Count();
                Debug.Log($"{TAG} Map seeded (seed {SEED}): {rc.Map.LayerCount} layers, {nodes} nodes, " +
                          $"{rc.Map.GymNodes.Count} gym(s).");

                int step = 0;
                while (!rc.RunOver && step < 32)
                {
                    IReadOnlyList<MapNode> options = rc.SelectableNodes();
                    if (options.Count == 0) { Debug.LogWarning($"{TAG} Dead end — no selectable nodes."); break; }
                    MapNode node = options[0]; // policy: first reachable

                    int dollarsBefore = run.PokeDollars;
                    int relicsBefore = run.HeldRelics?.Count ?? 0;
                    int badgesBefore = run.EarnedBadges?.Count ?? 0;

                    rc.EnterNode(node);
                    saveCount++;
                    Debug.Log($"{TAG} ── Step {step}: entered L{node.Layer} {node.NodeType}  {Detail(rc.ActiveNode)}");

                    string outcome = ResolveActive(rc, ctx);
                    rc.CompleteActiveNode();

                    int dD = run.PokeDollars - dollarsBefore;
                    int dR = (run.HeldRelics?.Count ?? 0) - relicsBefore;
                    int dB = (run.EarnedBadges?.Count ?? 0) - badgesBefore;
                    string rewards = (dD != 0 || dR != 0 || dB != 0)
                        ? $"  rewards: {(dD >= 0 ? "+" : "")}{dD}₽ +{dR} relic +{dB} badge" : "";
                    Debug.Log($"{TAG}    → {outcome}{rewards}");
                    step++;
                }

                Debug.Log($"{TAG} ════ RUN {(rc.RunOver ? "ENDED" : "INCOMPLETE")} ════ " +
                          $"final node: L{rc.CurrentNode?.Layer} {rc.CurrentNode?.NodeType} · " +
                          $"₽={run.PokeDollars} · relics={run.HeldRelics?.Count ?? 0} · " +
                          $"badges={run.EarnedBadges?.Count ?? 0} · box={ctx.Box.Count} · " +
                          $"nodes entered (saves)={saveCount}");
            }
            finally
            {
                SaveSystem.SaveDirectoryOverride = prevSave;
                if (Directory.Exists(tempSave)) Directory.Delete(tempSave, recursive: true);
            }
        }

        // ── Per-node detail / resolution ──────────────────────────────────────

        private static string Detail(NodeController node)
        {
            switch (node)
            {
                case WildAreaNodeController w:
                    return $"biome={w.SelectedBiome?.DisplayName}, offers=[{string.Join(", ", w.Choices.Select(s => s.DisplayName))}]";
                case TrainerBattleNodeController t: return $"archetype={t.Archetype?.DisplayName}";
                case EliteNodeController e:         return $"elite={e.EliteSO?.DisplayName}";
                case RegionShopNodeController s:    return $"{s.Slots.Count} slots";
                case MysteryEventNodeController m:  return $"event={m.SelectedEvent?.EventId} (risk {m.SelectedEvent?.RiskProfile})";
                case GymNodeController g:           return $"gym={g.GymSO?.DisplayName} ({g.GymSO?.GymType})";
                case PokemonCenterNodeController:   return "services: Heal / Tutor / Therapy";
                default: return "";
            }
        }

        private static string ResolveActive(RunController rc, RunContext ctx)
        {
            List<PokemonInstance> team = new() { ctx.Box.Members[0] };
            switch (rc.ActiveNode)
            {
                case WildAreaNodeController w:
                    w.ResolveCombat(CombatController.CombatOutcome.Victory, null);
                    return "wild combat auto-resolved: Victory (no catch)";
                case TrainerBattleNodeController t:
                    t.ResolveCombat(CombatController.CombatOutcome.Victory);
                    return "trainer combat auto-resolved: Victory";
                case EliteNodeController e:
                    e.ResolveCombat(CombatController.CombatOutcome.Victory);
                    return "elite combat auto-resolved: Victory";
                case GymNodeController g:
                    g.ResolveCombat(CombatController.CombatOutcome.Victory);
                    return "GYM combat auto-resolved: Victory — run complete";
                case PokemonCenterNodeController c:
                    c.Heal();
                    c.Leave();
                    return "healed Box + left";
                case RegionShopNodeController s:
                    s.Leave();
                    return "browsed shop + left (no purchase)";
                case MysteryEventNodeController m:
                    m.Choose(1);
                    return "mystery: chose option B";
                default:
                    return "unhandled node";
            }
        }

        // ── Asset loading (real authored VS content) ──────────────────────────

        private static RunContext BuildContext(out RunStateSO run, out PokemonSpeciesSO starter)
        {
            run = ScriptableObject.CreateInstance<RunStateSO>();
            starter = LoadOne<PokemonSpeciesSO>("Squirtle");

            MapGenerationConfigSO mapConfig = LoadFirst<MapGenerationConfigSO>();
            EconomyConfigSO economy = LoadFirst<EconomyConfigSO>();
            WildEncounterConfigSO wildConfig = LoadFirst<WildEncounterConfigSO>();
            RegionShopConfigSO shopConfig = LoadFirst<RegionShopConfigSO>();
            MysteryConfigSO mysteryConfig = LoadFirst<MysteryConfigSO>();
            EliteTrainerSO elite = LoadFirst<EliteTrainerSO>();
            GymLeaderSO gym = LoadFirst<GymLeaderSO>();

            if (mapConfig == null || economy == null || wildConfig == null || shopConfig == null ||
                mysteryConfig == null || elite == null || gym == null || starter == null)
            {
                Debug.LogError($"{TAG} Missing required VS asset(s) — aborting. " +
                               $"map={mapConfig} econ={economy} wild={wildConfig} shop={shopConfig} " +
                               $"mystery={mysteryConfig} elite={elite} gym={gym} starter={starter}");
                return null;
            }

            List<RelicSO> relics = LoadAll<RelicSO>();
            List<ConsumableSO> consumables = LoadAll<ConsumableSO>();

            Box box = new(economy.BoxCapacity);

            return new RunContext
            {
                Run = run,
                Box = box,
                Loadout = new LoadoutManager(run, box), // same Box the node controllers use
                PokemonFactory = new PokemonInstanceFactory(),
                Streams = new RNGStreams(SEED),
                Economy = economy,
                MapConfig = mapConfig,
                WildConfig = wildConfig,
                Pokeball = LoadOne<ConsumableSO>("pokeball"),
                BoxOverflow = new AutoSkipBoxOverflowHandler(),
                ArchetypePool = LoadAll<TrainerArchetypeSO>(),
                EliteSO = elite,
                ShopConfig = shopConfig,
                ShopPools = new RegionShopNodeController.ShopItemPools
                {
                    Consumables = consumables,
                    CommonRelics = relics.Where(r => r.Rarity == RarityTier.Common).ToList(),
                    UncommonRelics = relics.Where(r => r.Rarity == RarityTier.Uncommon).ToList(),
                    Pokeball = LoadOne<ConsumableSO>("pokeball"),
                    HeldItems = LoadAll<HeldItemSO>(),
                    TMs = LoadAll<TMSO>(),
                },
                MysteryPool = LoadAll<MysteryEventSO>(),
                MysteryConfig = mysteryConfig,
                MysteryItems = new MysteryEventNodeController.MysteryItemRefs
                {
                    StoneRelicPool = relics,
                    Potion = LoadOne<ConsumableSO>("potion"),
                    TutorPlaceholder = LoadOne<ConsumableSO>("potion"),
                },
                GymSO = gym,
            };
        }

        private static T LoadFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(g => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null).ToList();
        }

        // Loads an asset of type T whose file name contains `nameHint` (case-insensitive).
        private static T LoadOne<T>(string nameHint) where T : Object
        {
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
