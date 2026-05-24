namespace ProjectAscendant.Core
{
    public sealed class MainMenuState : GameStateNode
    {
        private readonly GameRootState _root;

        public MainMenuState(GameRootState root) { _root = root; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.StartNewRun)
                TransitionTo(_root.Run);
            else if (evt.Type == GameEventType.ReturnToHub)
                TransitionTo(_root.Hub);
            else
                base.HandleEvent(evt);
        }
    }
}
