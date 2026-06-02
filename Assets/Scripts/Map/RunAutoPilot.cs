using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per Epic 9 runtime wiring — a headless "auto-pilot" that walks a RunController to the end with
    // a win/proceed policy (combat auto-resolved to Victory; utility nodes left/declined). Combat
    // EXECUTION is stubbed here — this is for the run-flow smoke harness and the dev auto-run, not
    // real play. Shared by RunSmokeHarness (editor) and RunLauncher (runtime).
    public static class RunAutoPilot
    {
        // Resolves whatever node is active and returns a human-readable description of the outcome.
        // Per R3-5 — ResolveCombat now requires finalLeadIndex; auto-pilot passes 0 (default Lead).
        public static string ResolveActive(RunController rc)
        {
            switch (rc.ActiveNode)
            {
                case WildAreaNodeController w:
                    w.ResolveCombat(CombatController.CombatOutcome.Victory, null, finalLeadIndex: 0);
                    return "wild combat auto-resolved: Victory (no catch)";
                case EliteNodeController e:
                    e.ResolveCombat(CombatController.CombatOutcome.Victory, finalLeadIndex: 0);
                    return "elite combat auto-resolved: Victory";
                case TrainerBattleNodeController t:
                    t.ResolveCombat(CombatController.CombatOutcome.Victory, finalLeadIndex: 0);
                    return "trainer combat auto-resolved: Victory";
                case GymNodeController g:
                    g.ResolveCombat(CombatController.CombatOutcome.Victory, finalLeadIndex: 0);
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

        // Short detail string for a node about to be / just entered (offers, archetype, etc.).
        public static string Detail(NodeController node)
        {
            switch (node)
            {
                case WildAreaNodeController w:
                    return $"biome={Name(w.SelectedBiome)}, offers=[{Join(w.Choices)}]";
                case TrainerBattleNodeController t: return $"archetype={Name(t.Archetype)}";
                case EliteNodeController e:         return $"elite={Name(e.EliteSO)}";
                case RegionShopNodeController s:    return $"{s.Slots.Count} slots";
                case MysteryEventNodeController m:  return $"event={m.SelectedEvent?.EventId} (risk {m.SelectedEvent?.RiskProfile})";
                case GymNodeController g:           return $"gym={Name(g.GymSO)} ({g.GymSO?.GymType})";
                case PokemonCenterNodeController:   return "services: Heal / Tutor / Therapy";
                default: return "";
            }
        }

        // Walks the run from its current position to a terminal node. `log` receives one line per
        // step. Caps iterations defensively. Returns the number of nodes entered.
        public static int WalkToEnd(RunController rc, Action<string> log, int maxSteps = 32)
        {
            int step = 0;
            while (!rc.RunOver && step < maxSteps)
            {
                IReadOnlyList<MapNode> options = rc.SelectableNodes();
                if (options.Count == 0) { log?.Invoke("dead end — no selectable nodes"); break; }

                MapNode node = options[0]; // policy: first reachable
                rc.EnterNode(node);
                log?.Invoke($"Step {step}: L{node.Layer} {node.NodeType}  {Detail(rc.ActiveNode)}");

                string outcome = ResolveActive(rc);
                rc.CompleteActiveNode();
                log?.Invoke($"   → {outcome}");
                step++;
            }
            return step;
        }

        private static string Name(UnityEngine.Object o) => o != null ? o.name : "null";

        private static string Join(IReadOnlyList<PokemonSpeciesSO> species)
        {
            if (species == null || species.Count == 0) return "";
            List<string> names = new(species.Count);
            for (int i = 0; i < species.Count; i++) names.Add(species[i] != null ? species[i].DisplayName : "?");
            return string.Join(", ", names);
        }
    }
}
