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
            if (RunOver || !IsSelectable(node)) return false;

            _ctx.Loadout?.Lock();
            ActiveNode = _factory.Build(node, _ctx.Run);
            ActiveNode.Enter();
            CurrentNode = node;

            _dispatch(new GameEvent(GameEventType.NodeConfirmed));
            return true;
        }

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
            }
            else
                _ctx.Loadout?.Unlock();

            ActiveNode = null;
            _dispatch(new GameEvent(evt));
            return true;
        }
    }
}
