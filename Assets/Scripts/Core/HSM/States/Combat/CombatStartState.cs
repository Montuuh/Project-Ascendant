namespace ProjectAscendant.Core
{
    // Per §9.5.2 — initialises combat encounter; transitions to TurnLoop on CombatBegin.
    // Epic 4 will add: spawn enemy, deal opening-hand, reveal first intents.
    public sealed class CombatStartState : GameStateNode
    {
        private readonly CombatState _combat;

        public CombatStartState(CombatState combat) { _combat = combat; }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.CombatBegin)
                TransitionTo(_combat.TurnLoop);
            else
                base.HandleEvent(evt);
        }
    }
}
