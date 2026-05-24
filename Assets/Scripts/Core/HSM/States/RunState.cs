namespace ProjectAscendant.Core
{
    // Per §9.5.1 + §9.3.2.4 — active run state; owns the input log for determinism replay.
    public sealed class RunState : GameStateNode
    {
        public readonly MapViewState         MapView;
        public readonly NodeState            Node;
        public readonly EvolutionScreenState EvolutionScreen;
        public readonly RunEndState          RunEnd;

        // Per §9.3.2.4 + §9.7.4 — transition log for determinism replay.
        // Full wiring (RecordedInputs ↔ InputLogRecorder) deferred to Epic 3 (RunStateSO).
        public readonly InputLog RecordedInputs = new();

        private readonly GameRootState _root;

        public RunState(GameRootState root)
        {
            _root          = root;
            MapView        = new MapViewState(this);
            Node           = new NodeState(this);
            EvolutionScreen = new EvolutionScreenState(this);
            RunEnd         = new RunEndState(this);
        }

        public override void OnEnter() { EnterChild(MapView); }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.GameOver)
                TransitionTo(_root.GameOver);
            else if (evt.Type == GameEventType.ReturnToHub)
                TransitionTo(_root.Hub);
            else
                base.HandleEvent(evt);
        }
    }
}
