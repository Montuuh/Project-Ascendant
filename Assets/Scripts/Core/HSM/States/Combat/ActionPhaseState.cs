namespace ProjectAscendant.Core
{
    // Per §9.5.2 — player plays cards / swaps / ends turn.
    // Sub-states PlayerActing / PlayerEndTurn: Epic 4 (input handling + AP economy).
    public sealed class ActionPhaseState : GameStateNode
    {
        private readonly TurnLoopState _turnLoop;

        public ActionPhaseState(TurnLoopState turnLoop) { _turnLoop = turnLoop; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.PlayerEndedTurn)
                TransitionTo(_turnLoop.ResolutionPhase);
            else
                base.HandleEvent(evt);
        }
    }
}
