namespace ProjectAscendant.Core
{
    // Per §9.5.1 — Trainer Hub state.
    // Sub-states HubMenu / PokemartScreen / PCTerminalScreen / DaycareScreen: Epic 11.
    public sealed class HubState : GameStateNode
    {
        private readonly GameRootState _root;

        public HubState(GameRootState root) { _root = root; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.StartNewRun)
                TransitionTo(_root.Run);
            else if (evt.Type == GameEventType.GameOver)
                TransitionTo(_root.GameOver);
            else
                base.HandleEvent(evt);
        }
    }
}
