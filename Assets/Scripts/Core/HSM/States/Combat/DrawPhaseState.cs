namespace ProjectAscendant.Core
{
    // Per §9.5.2 — player draws to fill hand. Epic 5 implements deck draw logic.
    public sealed class DrawPhaseState : GameStateNode
    {
        private readonly TurnLoopState _turnLoop;

        public DrawPhaseState(TurnLoopState turnLoop) { _turnLoop = turnLoop; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.DrawComplete)
                TransitionTo(_turnLoop.IntentPhase);
            else
                base.HandleEvent(evt);
        }
    }
}
