namespace ProjectAscendant.Core
{
    // Per §9.5.1 — Active node being resolved.
    // TODO: Epic 8/9 — add ShopState, MysteryEventState, CenterState, BranchChoiceState.
    // For VS: always enters CombatState as default node type.
    public sealed class NodeState : GameStateNode
    {
        public readonly CombatState Combat;

        private readonly RunState _run;

        public NodeState(RunState run)
        {
            _run   = run;
            Combat = new CombatState(this);
        }

        // Default entry is CombatState; Epic 8 will branch on node type.
        public override void OnEnter() { EnterChild(Combat); }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.NodeComplete)
                TransitionTo(_run.MapView);
            else
                base.HandleEvent(evt);
        }
    }
}
