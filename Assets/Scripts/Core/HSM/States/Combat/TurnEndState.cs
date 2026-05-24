namespace ProjectAscendant.Core
{
    // Per §9.5.2 — end-of-turn cleanup, then loops back to DrawPhase.
    // Epic 4 adds: relic end-of-turn procs, Trauma application triggers.
    public sealed class TurnEndState : GameStateNode
    {
        private readonly TurnLoopState _turnLoop;

        public TurnEndState(TurnLoopState turnLoop) { _turnLoop = turnLoop; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.TurnEndComplete)
                TransitionTo(_turnLoop.DrawPhase); // loop to next turn
            else
                base.HandleEvent(evt);
        }
    }
}
