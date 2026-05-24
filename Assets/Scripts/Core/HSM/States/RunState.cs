using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.5.1 + §9.3.2.4 — active run state; owns the transition log stub.
    public sealed class RunState : GameStateNode
    {
        public readonly MapViewState        MapView;
        public readonly NodeState           Node;
        public readonly EvolutionScreenState EvolutionScreen;
        public readonly RunEndState          RunEnd;

        // TODO: Task 2.6 — replace with InputLog RecordedInputs (§9.3.2.4).
        // Stub stores raw transition strings until InputLog struct is defined.
        public List<string> RecordedTransitions { get; } = new();

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
