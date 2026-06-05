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

        // Per gap #43 — clear the controller back to a pre-run (idle) state so the Map View shows
        // starter-select again. RunLauncher.BeginNewRun calls this; resetting RunState/Box alone is not
        // enough — the controller's Map/position must also clear or Refresh keeps rendering the old run.
        public void ResetForNewRun()
        {
            Map = null;
            _ctx.CurrentMap = null;
            CurrentNode = null;
            ActiveNode = null;
            RunOver = false;
            Outcome = null;
            LastSummary = null;
        }

        // Generates the Region map from the MapRNG stream and boots the run into Map View.
        public void StartRun(int regionIndex = 0)
        {
            Map = RegionMapGenerator.Generate(_ctx.MapConfig, _ctx.Streams.MapRNG, regionIndex, _ctx.GymPool);
            _ctx.CurrentMap = Map; // Per §7.2 v2 — context needs map to resolve gyms.
            _ctx.Run.CurrentRegionIndex = regionIndex;
            // §7.3.4 (Option 1) — Pokéball grant: starting stock on region 0, +PerRegion on later regions.
            if (_ctx.Economy != null)
                _ctx.Run.PokeballCount = regionIndex == 0
                    ? _ctx.Economy.StartingPokeballs
                    : _ctx.Run.PokeballCount + _ctx.Economy.PokeballsPerRegion;
            CurrentNode = null;
            ActiveNode = null;
            RunOver = false;
            Outcome = null;
            _dispatch(new GameEvent(GameEventType.StartNewRun));
        }

        // Per §9.8.1 + gap #43 — resume an in-progress run from a loaded save. The caller (RunLauncher)
        // has already installed the saved RunState + Box into the RunContext; this regenerates the
        // deterministic map from the seed and restores the player's position to the saved node, then
        // enters Map View. The player continues forward from the restored node (the node that was
        // entered-but-not-yet-resolved at save time is treated as standing-on; utility nodes remain
        // re-enterable). NOTE: per-stream RNG cursors are not persisted (gap follow-up), so encounters
        // beyond the restored node re-roll deterministically from the seed rather than continuing the
        // exact pre-save sequence.
        public bool Resume()
        {
            if (_ctx.Run == null) return false;

            int regionIndex = _ctx.Run.CurrentRegionIndex;
            Map = RegionMapGenerator.Generate(_ctx.MapConfig, _ctx.Streams.MapRNG, regionIndex, _ctx.GymPool);
            _ctx.CurrentMap = Map; // Per §7.2 v2 — context needs map to resolve gyms.
            RunOver = false;
            Outcome = null;
            ActiveNode = null;
            _ctx.Loadout?.Unlock(); // between nodes the player may re-loadout
            CurrentNode = FindNode(
                _ctx.Run.CurrentLayerIndex, _ctx.Run.CurrentLaneIndex, _ctx.Run.CurrentNodeIndexInLane);

            _dispatch(new GameEvent(GameEventType.StartNewRun));
            return true;
        }

        // Locate the regenerated map node matching a saved (layer, lane, indexInLane). Falls back to
        // the first node in the lane, then the first node in the layer (defensive against drift).
        private MapNode FindNode(int layer, int lane, int indexInLane)
        {
            if (Map == null || layer < 0 || layer >= Map.LayerCount) return null;
            List<MapNode> nodes = Map.Layers[layer];
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Lane == lane && nodes[i].IndexInLane == indexInLane) return nodes[i];
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].Lane == lane) return nodes[i];
            return nodes.Count > 0 ? nodes[0] : null;
        }

        // Per §7.2 v2 — nodes reachable from the current position: the L0 entry nodes before the
        // first step, then the current node's forward connections. Empty once the run is over.
        public IReadOnlyList<MapNode> SelectableNodes()
        {
            if (RunOver || Map == null) return Array.Empty<MapNode>();
            if (CurrentNode == null) return Map.EntryNodes;

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

            // Per §7.6 + Bug R2-2 — allow RE-ENTERING the CURRENT REPEATABLE utility node (Center / Shop)
            // while parked on it (before advancing a layer). Those services are repeatable/idempotent
            // (heal is a no-op when already full; shop purchases deduct ₽), so re-entry is safe. MYSTERY
            // (§7.9) is a ONE-SHOT event — re-entry would re-farm its reward — so it is excluded once
            // resolved. Combat nodes are never re-enterable (the current node fails IsSelectable below).
            bool reentry = node == CurrentNode && IsReenterable(node.NodeType);
            if (!reentry && !IsSelectable(node)) return false;

            _ctx.Loadout?.Lock();
            ActiveNode = _factory.Build(node, _ctx.Run);
            // Per §9.8.1 + gap #43 — save-on-entry persists run-state AND the live Box (team). The
            // callback is supplied here because this is the layer that owns the RunContext.Box.
            ActiveNode.Enter(run => SaveSystem.SaveRun(run, _ctx.Box?.Members, _ctx.Box?.Capacity ?? 0));
            CurrentNode = node;

            _dispatch(new GameEvent(GameEventType.NodeConfirmed));
            return true;
        }

        // Per §7.6 — REPEATABLE utility nodes whose current-node re-entry is allowed. Mystery (§7.9) is
        // a one-shot event and is intentionally excluded to prevent reward re-farming; combat nodes are
        // never re-enterable. Public so the Map View renders the current node clickable iff re-enterable.
        public static bool IsReenterable(NodeType t)
            => t == NodeType.Center || t == NodeType.Shop;

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
                    _ctx.Run, _ctx.Box, _ctx.Meta, _ctx.MetaConfig, Outcome.Value, layersCleared, _ctx.Pokedex,
                    _ctx.PokedexTotalSpecies);

                // Per §9.8.1 + gap #43 — the run is finished: clear the in-progress save so a saved
                // file always denotes a resumable run (nothing to continue after victory/defeat).
                SaveSystem.DeleteRun();
            }
            else
                _ctx.Loadout?.Unlock();

            ActiveNode = null;
            _dispatch(new GameEvent(evt));
            return true;
        }
    }
}
