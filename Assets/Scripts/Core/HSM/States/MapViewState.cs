namespace ProjectAscendant.Core
{
    // Per §9.5.1 — Map view sub-state of RunState.
    // Sub-states LoadoutAdjust / NodeSelect / PauseMenu: Epic 9 (stubs deferred).
    public sealed class MapViewState : GameStateNode
    {
        private readonly RunState _run;

        public MapViewState(RunState run) { _run = run; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.NodeConfirmed)
                TransitionTo(_run.Node);
            else if (evt.Type == GameEventType.EvolutionTriggered)
                TransitionTo(_run.EvolutionScreen);
            else if (evt.Type == GameEventType.RunEnded)
                TransitionTo(_run.RunEnd);
            else
                base.HandleEvent(evt);
        }
    }
}
