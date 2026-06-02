using System;
using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §9.5 + Epic 9 run-flow integration — orchestrates a Region run end-to-end and drives the
    // HSM at the coarse macro level via an injected GameEvent dispatcher (runtime: → GameStateMachine
    // .HandleEvent; tests: capture). Owns navigation + node entry/resolution routing; combat EXECUTION
    // stays with the caller (the combat screen / tests run CombatController and hand the outcome back),
    // keeping this class headless-testable.
    //
    // Flow:
    //   StartRun()          generate map (MapRNG) → dispatch StartNewRun (HSM: → RunState → MapView)
    //   SelectableNodes()   the nodes reachable from the current position
    //   EnterNode(node)     validate → Loadout.Lock (§2.3) → factory-build + Enter (save §9.8.1) →
    //                       dispatch NodeConfirmed (HSM: MapView → Node)
    //   <caller resolves the active node — combat outcome / utility interaction — via its controller>
    //   CompleteActiveNode() route the controller's NodeOutcome → NodeComplete / RunEnded / GameOver
    //                       (unlock loadout unless the run is over), advance position
    public sealed class RunController
    {
        private readonly RunContext _ctx;
        private readonly NodeControllerFactory _factory;
        private readonly Action<GameEvent> _dispatch;

        public RegionMap Map { get; private set; }
        // The node most recently entered; null before the first node (player is at the start).
        public MapNode CurrentNode { get; private set; }
        public NodeController ActiveNode { get; private set; }
        public bool RunOver { get; private set; }
        // Per §7.13 / §3.3.6 — set once RunOver becomes true: Victory on a Gym win (RunEnded),
        // Defeat on a player wipe (GameOver). Null while the run is still in progress.
        public RunOutcome? Outcome { get; private set; }
        // Per §2.1.7 / Task 11.4 — the run-summary produced at run-end (meta XP/Tokens/Level + tallies).
        // Null until RunOver. Read by the result screen.
        public RunEndService.RunSummary? LastSummary { get; private set; }

        public RunController(RunContext context, NodeControllerFactory factory, Action<GameEvent> dispatch)
        {
            _ctx      = context ?? throw new ArgumentNullException(nameof(context));
            _factory  = factory ?? throw new ArgumentNullException(nameof(factory));
            _dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
        }

        // Generates the Region map from the MapRNG stream and boots the run into Map View.
        public void StartRun(int regionIndex = 0)
        {
            Map = RegionMapGenerator.Generate(_ctx.MapConfig, _ctx.Streams.MapRNG, regionIndex);
            _ctx.Run.CurrentRegionIndex = regionIndex;
            CurrentNode = null;
            ActiveNode = null;
            RunOver = false;
            Outcome = null;
            _dispatch(new GameEvent(GameEventType.StartNewRun));
        }

        // Per §2.1.2 — nodes reachable from the current position: the forced Layer-0 entry before the
        // first step, then the current node's forward connections. Empty once the run is over.
        public IReadOnlyList<MapNode> SelectableNodes()
        {
            if (RunOver || Map == null) return Array.Empty<MapNode>();
            if (CurrentNode == null) return new List<MapNode> { Map.Entry };

            // §6.8.2 One Path — difficulty can cap the route branches shown at each junction (Task 11.6).
            int cap = DifficultyModifiers.MaxRouteBranches(_ctx.Run.ActiveDifficultyModifiers);
            if (cap < CurrentNode.Next.Count)
            {
                List<MapNode> trimmed = new(cap);
                for (int i = 0; i < cap; i++) trimmed.Add(CurrentNode.Next[i]);
                return trimmed;
            }
            return CurrentNode.Next;
        }

        public bool IsSelectable(MapNode node)
        {
            if (node == null) return false;
            IReadOnlyList<MapNode> options = SelectableNodes();
            for (int i = 0; i < options.Count; i++) if (options[i] == node) return true;
            return false;
        }

        // Enters a selected node: lock loadout (§2.3), build + Enter the controller (save-on-entry,
        // §9.8.1), dispatch NodeConfirmed. Returns false (no change) if the node isn't reachable.
        public bool EnterNode(MapNode node)
        {
            if (RunOver || node == null) return false;

            // Per §7.6 + Bug R2-2 — allow RE-ENTERING the CURRENT utility node (Center / Shop / Mystery)
            // while the player is still parked on it (before advancing a layer). Utility services are
            // repeatable / idempotent, so re-entry is safe. Combat nodes are NEVER re-enterable (no
            // reward re-farming): the current node is not in its own SelectableNodes, so a combat
            // CurrentNode fails the IsSelectable gate below.
            bool reentry = node == CurrentNode && IsUtilityNode(node.NodeType);
            if (!reentry && !IsSelectable(node)) return false;

            _ctx.Loadout?.Lock();
            ActiveNode = _factory.Build(node, _ctx.Run);
            ActiveNode.Enter();
            CurrentNode = node;

            _dispatch(new GameEvent(GameEventType.NodeConfirmed));
            return true;
        }

        // Non-combat nodes whose current-node re-entry is allowed (§7.6 / §7.7 / §7.9).
        private static bool IsUtilityNode(NodeType t)
            => t == NodeType.Center || t == NodeType.Shop || t == NodeType.Mystery;

        // Routes the resolved active node to the next macro state. The caller must have resolved the
        // node first (so ActiveNode.IsCompleted is true). RunEnded/GameOver end the run; otherwise the
        // loadout unlocks for the next Map View. No-op if there is no completed active node.
        public bool CompleteActiveNode()
        {
            if (ActiveNode == null || !ActiveNode.IsCompleted) return false;

            GameEventType evt = ActiveNode.ToGameEventType();
            if (evt == GameEventType.RunEnded || evt == GameEventType.GameOver)
            {
                RunOver = true;
                // §7.13 Gym victory (RunEnded) = Victory; §3.3.6 player wipe (GameOver) = Defeat.
                Outcome = evt == GameEventType.RunEnded ? RunOutcome.Victory : RunOutcome.Defeat;

                // §2.1.7 / Task 11.4 — commit meta progression + build the run summary.
                int layersCleared = CurrentNode != null ? CurrentNode.Layer : 0;
                LastSummary = RunEndService.Finalize(
                    _ctx.Run, _ctx.Box, _ctx.Meta, _ctx.MetaConfig, Outcome.Value, layersCleared, _ctx.Bestiary);
            }
            else
                _ctx.Loadout?.Unlock();

            ActiveNode = null;
            _dispatch(new GameEvent(evt));
            return true;
        }
    }
}
