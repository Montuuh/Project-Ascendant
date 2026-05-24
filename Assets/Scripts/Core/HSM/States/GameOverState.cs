namespace ProjectAscendant.Core
{
    public sealed class GameOverState : GameStateNode
    {
        private readonly GameRootState _root;

        public GameOverState(GameRootState root) { _root = root; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.ReturnToHub)
                TransitionTo(_root.Hub);
            else
                base.HandleEvent(evt);
        }
    }
}
