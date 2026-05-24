namespace ProjectAscendant.Core
{
    // Per §9.5.2 — applies enemy intents, status ticks, faints, checks victory/defeat.
    // Sub-states ApplyEnemyIntents / ApplyStatusTicks / ResolveFaints / CheckVictoryDefeat:
    // Epic 4 will implement the full sequential sub-machine.
    public sealed class ResolutionPhaseState : GameStateNode
    {
        private readonly TurnLoopState _turnLoop;

        public ResolutionPhaseState(TurnLoopState turnLoop) { _turnLoop = turnLoop; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.ResolutionComplete)
                TransitionTo(_turnLoop.TurnEnd);
            else
                base.HandleEvent(evt);
        }
    }
}
