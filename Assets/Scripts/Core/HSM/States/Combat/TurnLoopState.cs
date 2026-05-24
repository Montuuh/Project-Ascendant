namespace ProjectAscendant.Core
{
    // Per §9.5.2 — repeating turn loop: Draw → Intent → Action → Resolution → TurnEnd → Draw …
    // Intercepts CombatVictory / CombatDefeat before they reach sub-phases.
    public sealed class TurnLoopState : GameStateNode
    {
        public readonly DrawPhaseState       DrawPhase;
        public readonly IntentPhaseState     IntentPhase;
        public readonly ActionPhaseState     ActionPhase;
        public readonly ResolutionPhaseState ResolutionPhase;
        public readonly TurnEndState         TurnEnd;

        private readonly CombatState _combat;

        public TurnLoopState(CombatState combat)
        {
            _combat         = combat;
            DrawPhase       = new DrawPhaseState(this);
            IntentPhase     = new IntentPhaseState(this);
            ActionPhase     = new ActionPhaseState(this);
            ResolutionPhase = new ResolutionPhaseState(this);
            TurnEnd         = new TurnEndState(this);
        }

        public override void OnEnter() { EnterChild(DrawPhase); }

        public override void HandleEvent(GameEvent evt)
        {
            // Intercept outcome signals before sub-phases see them.
            if (evt.Type == GameEventType.CombatVictory)
                TransitionTo(_combat.Victory);
            else if (evt.Type == GameEventType.CombatDefeat)
                TransitionTo(_combat.Defeat);
            else
                base.HandleEvent(evt); // bubble to active phase
        }
    }
}
