namespace ProjectAscendant.Core
{
    // Per §9.5.3 — Abstract base for all HSM nodes.
    // Parent/child wiring is set at runtime by EnterChild; never in constructors.
    public abstract class GameStateNode
    {
        private GameStateNode _parent;
        private GameStateNode _currentChild;

        public GameStateNode Parent      => _parent;
        public GameStateNode CurrentChild => _currentChild;

        // ── Lifecycle hooks ─────────────────────────────────────────────────────

        public virtual void OnEnter() { }
        public virtual void OnExit()  { }

        // Default propagates update to active child. Override and call base to add own logic.
        public virtual void OnUpdate(float dt)
        {
            _currentChild?.OnUpdate(dt);
        }

        // Default bubbles event DOWN to current child. Override to intercept before bubbling.
        public virtual void HandleEvent(GameEvent evt)
        {
            _currentChild?.HandleEvent(evt);
        }

        // ── Tree manipulation ───────────────────────────────────────────────────

        // Exits current child subtree (if any), then enters the new child.
        protected void EnterChild(GameStateNode child)
        {
            _currentChild?.ExitSubtree();
            _currentChild = null;
            if (child == null) return;
            child._parent = this;
            _currentChild = child;
            child.OnEnter();
        }

        // Transitions THIS node to 'next' within the same parent.
        // Exits this node's full subtree, then enters 'next'.
        // Per §9.5.3 — all transitions log to InputLog for determinism replay.
        protected void TransitionTo(GameStateNode next)
        {
            GameStateNode parent = _parent;
            if (parent == null) return;

            ExitSubtree();
            parent._currentChild = null;

            next._parent = parent;
            parent._currentChild = next;
            next.OnEnter();

            // TODO: Task 2.6 — log this transition to RunState.RecordedInputs (InputLog).
        }

        // Recursively exits the subtree leaf-first, then this node.
        // Called by TransitionTo and EnterChild to clean up the outgoing branch.
        internal void ExitSubtree()
        {
            _currentChild?.ExitSubtree();
            _currentChild = null;
            OnExit();
        }
    }
}
