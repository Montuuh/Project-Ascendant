using System;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §7.2 / §9.5 / §9.8.1 + Epic 9 Task 9.2 — abstract base for every node-type controller
    // (Wild/Trainer/Center/Shop/Mystery/Gym/Elite — authored in Tasks 9.3–9.8).
    //
    // Pure C#: no MonoBehaviour, no view references. The HSM NodeState builds a concrete
    // NodeController for the active MapNode and drives this lifecycle:
    //
    //   Enter()              → set run position + SAVE-ON-ENTRY (§9.8.1) → publish
    //                          NodeEnteredContext → OnEnter() (concrete resolution)
    //   Complete(outcome)    → publish NodeCompletedContext → OnComplete()
    //   ToGameEventType()    → NodeState maps Outcome to the HSM transition
    //
    // Concrete controllers do their work in OnEnter() (combat via the Epic 8 controllers,
    // or a utility service) and call Complete(...) when the node is resolved.
    public abstract class NodeController
    {
        public MapNode Node { get; }
        public RunStateSO RunState { get; }

        public NodeType NodeType => Node.NodeType;

        public bool IsEntered { get; private set; }
        public bool IsCompleted { get; private set; }
        // Null until Complete(...) is called.
        public NodeOutcome? Outcome { get; private set; }

        protected NodeController(MapNode node, RunStateSO runState)
        {
            Node     = node ?? throw new ArgumentNullException(nameof(node));
            RunState = runState ?? throw new ArgumentNullException(nameof(runState));
        }

        // Per §9.8.1 — save on EVERY node entry (Epic 9 DoD). Idempotent: a second Enter() is a no-op.
        public void Enter()
        {
            if (IsEntered) return;
            IsEntered = true;

            // Record current position so a resumed save lands the player back on this layer.
            RunState.CurrentLayerIndex = Node.Layer;
            SaveSystem.SaveRun(RunState);

            // Publish AFTER the save so subscribers (UI/analytics) observe committed state.
            EventBus.Publish(new NodeEnteredContext(NodeType, Node.Layer, Node.Lane));

            OnEnter();
        }

        // Concrete node resolution. Implementations call Complete(...) when finished
        // (may be synchronously inside OnEnter for utility nodes, or later for combat).
        protected abstract void OnEnter();

        // Marks the node resolved and publishes NodeCompletedContext. Idempotent.
        protected void Complete(NodeOutcome outcome)
        {
            if (IsCompleted) return;
            IsCompleted = true;
            Outcome = outcome;

            EventBus.Publish(new NodeCompletedContext(NodeType, Node.Layer, Node.Lane, outcome));
            OnComplete(outcome);
        }

        protected virtual void OnComplete(NodeOutcome outcome) { }

        // Per §9.5 — pure mapping of the node Outcome to the HSM event the NodeState fires.
        // Keeps HSM knowledge out of the base (no GameStateNode reference); the driver issues it.
        public GameEventType ToGameEventType()
        {
            return Outcome switch
            {
                NodeOutcome.Cleared     => GameEventType.NodeComplete,
                NodeOutcome.RunEnded    => GameEventType.RunEnded,
                NodeOutcome.PlayerWiped => GameEventType.GameOver,
                _                       => GameEventType.None,
            };
        }

        // Per §7.2.3 / Task 9.2.3 — the data the Map View renders for this node. Base returns the
        // type-default (loc key by node type); concrete controllers override to enrich the preview
        // (e.g. Wild node species choices, Mystery risk profile).
        public virtual NodePreview BuildPreview()
            => new NodePreview(NodeType, Node.Layer, Node.Lane, DefaultNameKey(NodeType), null);

        // Conventional localisation key for a node type, e.g. NodeType.Wild → "node.wild.name".
        public static string DefaultNameKey(NodeType type)
            => "node." + type.ToString().ToLowerInvariant() + ".name";
    }
}
