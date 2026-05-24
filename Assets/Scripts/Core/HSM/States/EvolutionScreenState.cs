namespace ProjectAscendant.Core
{
    // Per §9.5.1 — evolution branch-choice screen, entered from MapView.
    public sealed class EvolutionScreenState : GameStateNode
    {
        private readonly RunState _run;

        public EvolutionScreenState(RunState run) { _run = run; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.EvolutionComplete)
                TransitionTo(_run.MapView);
            else
                base.HandleEvent(evt);
        }
    }
}
