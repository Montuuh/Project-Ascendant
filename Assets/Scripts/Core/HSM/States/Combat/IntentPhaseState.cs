namespace ProjectAscendant.Core
{
    // Per §9.5.2 — enemy intents revealed. Epic 8 implements AI intent generation.
    public sealed class IntentPhaseState : GameStateNode
    {
        private readonly TurnLoopState _turnLoop;

        public IntentPhaseState(TurnLoopState turnLoop) { _turnLoop = turnLoop; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.IntentRevealComplete)
                TransitionTo(_turnLoop.ActionPhase);
            else
                base.HandleEvent(evt);
        }
    }
}
